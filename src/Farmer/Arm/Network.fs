[<AutoOpen>]
module Farmer.Arm.Network

open System.Net.Mail
open Farmer
open Farmer.Arm
open Farmer.ExpressRoute
open Farmer.Route
open Farmer.VirtualNetworkGateway

let connections = ResourceType("Microsoft.Network/connections", "2020-04-01")

let expressRouteCircuits =
    ResourceType("Microsoft.Network/expressRouteCircuits", "2019-02-01")

let expressRouteCircuitAuthorizations =
    ResourceType("Microsoft.Network/expressRouteCircuits/authorizations", "2019-02-01")

let networkInterfaces =
    ResourceType("Microsoft.Network/networkInterfaces", "2018-11-01")

let networkProfiles =
    ResourceType("Microsoft.Network/networkProfiles", "2020-04-01")

let publicIPAddresses =
    ResourceType("Microsoft.Network/publicIPAddresses", "2018-11-01")

let publicIPPrefixes =
    ResourceType("Microsoft.Network/publicIPPrefixes", "2021-08-01")

let serviceEndpointPolicies =
    ResourceType("Microsoft.Network/serviceEndpointPolicies", "2020-07-01")

let subnets =
    ResourceType("Microsoft.Network/virtualNetworks/subnets", "2020-07-01")

let virtualNetworks =
    ResourceType("Microsoft.Network/virtualNetworks", "2020-07-01")

let virtualNetworkGateways =
    ResourceType("Microsoft.Network/virtualNetworkGateways", "2020-05-01")

let localNetworkGateways =
    ResourceType("Microsoft.Network/localNetworkGateways", "")

let natGateways = ResourceType("Microsoft.Network/natGateways", "2021-08-01")

let privateEndpoints =
    ResourceType("Microsoft.Network/privateEndpoints", "2021-05-01")

let virtualNetworkPeering =
    ResourceType("Microsoft.Network/virtualNetworks/virtualNetworkPeerings", "2020-05-01")

let routeTables = ResourceType("Microsoft.Network/routeTables", "2021-01-01")
let routes = ResourceType("Microsoft.Network/routeTables/routes", "2021-01-01")


type SubnetReference =
    | ViaManagedVNet of (ResourceId * ResourceName)
    | Direct of LinkedResource

    member this.ResourceId: ResourceId =
        match this with
        | ViaManagedVNet (vnetId, subnet) ->
            { vnetId with
                Type = subnets
                Segments = [ subnet ]
            }
        | Direct subnet -> subnet.ResourceId

    member this.Dependency =
        match this with
        | ViaManagedVNet (id, _)
        | Direct (Managed id) -> Some id
        | _ -> None

    static member create(vnetRef: LinkedResource, subnetName: ResourceName) =
        if vnetRef.ResourceId.Type.Type <> virtualNetworks.Type then
            raiseFarmer $"given resource was not of type '{virtualNetworks.Type}'."

        match vnetRef with
        | Managed vnetId -> ViaManagedVNet(vnetId, subnetName)
        | Unmanaged vnetId ->
            Direct(
                Unmanaged
                    { vnetId with
                        Type = subnets
                        Segments = [ subnetName ]
                    }
            )

    static member create(subnetRef: LinkedResource) =
        if subnetRef.ResourceId.Type.Type <> subnets.Type then
            raiseFarmer $"given resource was not of type '{subnets.Type}'."

        Direct subnetRef

type Route =
    {
        Name: ResourceName
        AddressPrefix: IPAddressCidr
        NextHopType: Route.HopType
        HasBgpOverride: FeatureFlag
    }

    member internal this.JsonModelProperties =
        {|
            addressPrefix = IPAddressCidr.format this.AddressPrefix
            nextHopType = this.NextHopType.ArmValue
            nextHopIpAddress =
                match this.NextHopType with
                | VirtualAppliance ip ->
                    ip
                    |> Option.map (fun x -> x.ToString())
                    |> Option.defaultValue Unchecked.defaultof<_>
                | _ -> Unchecked.defaultof<_>
            hasBgpOverride = this.HasBgpOverride.AsBoolean
        |}

    interface IArmResource with
        member this.ResourceId = routes.resourceId this.Name

        member this.JsonModel =
            {| routes.Create(this.Name) with
                properties = this.JsonModelProperties
            |}

type RouteTable =
    {
        Name: ResourceName
        Location: Location
        Tags: Map<string, string>
        DisableBGPRoutePropagation: FeatureFlag
        Routes: Route list
    }

    member internal this.JsonModelProperties =
        {|
            disableBgpRoutePropagation = this.DisableBGPRoutePropagation.AsBoolean
            routes = this.Routes |> Seq.map (fun x -> (x :> IArmResource).JsonModel)
        |}

    interface IArmResource with
        member this.ResourceId = routeTables.resourceId this.Name

        member this.JsonModel =
            {| routeTables.Create(this.Name, this.Location, tags = this.Tags) with
                properties = this.JsonModelProperties
            |}

type PublicIpAddress =
    {
        Name: ResourceName
        Location: Location
        Sku: PublicIpAddress.Sku
        AllocationMethod: PublicIpAddress.AllocationMethod
        DomainNameLabel: string option
        Tags: Map<string, string>
    }

    interface IArmResource with
        member this.ResourceId = publicIPAddresses.resourceId this.Name

        member this.JsonModel =
            {| publicIPAddresses.Create(this.Name, this.Location, tags = this.Tags) with
                sku = {| name = this.Sku.ArmValue |}
                properties =
                    {|
                        publicIPAllocationMethod = this.AllocationMethod.ArmValue
                        dnsSettings =
                            match this.DomainNameLabel with
                            | Some label -> box {| domainNameLabel = label.ToLower() |}
                            | None -> null
                    |}
            |}

/// If using the IPs in the frontend of a cross-region laod balancer, public IPs and prefixes must be in
/// the Global tier, otherwise regional IPs are sufficient.
type PublicIpPrefixTier =
    | Global
    | Regional

    member this.ArmValue =
        match this with
        | Global -> "Global"
        | Regional -> "Regional"

/// Public IP Prefix creates a block of contiguous public IP addresses that can be assigned to resources.
type PublicIpPrefix =
    {
        Name: ResourceName
        Location: Location
        PrefixLength: int
        Tier: PublicIpPrefixTier
        Tags: Map<string, string>
    }

    interface IArmResource with
        member this.ResourceId = publicIPPrefixes.resourceId this.Name

        member this.JsonModel =
            {| publicIPPrefixes.Create(this.Name, this.Location, tags = this.Tags) with
                sku =
                    {|
                        name = "Standard"
                        tier = this.Tier.ArmValue
                    |}
                properties =
                    {|
                        prefixLength = this.PrefixLength
                        publicIPAddressVersion = "IPv4"
                    |}
            |}

type SubnetDelegation =
    {
        Name: ResourceName
        ServiceName: string
    }

type Subnet =
    {
        Name: ResourceName
        Prefix: string
        VirtualNetwork: LinkedResource option
        NetworkSecurityGroup: LinkedResource option
        Delegations: SubnetDelegation list
        NatGateway: LinkedResource option
        ServiceEndpoints: (Network.EndpointServiceType * Location list) list
        AssociatedServiceEndpointPolicies: ResourceId list
        PrivateEndpointNetworkPolicies: FeatureFlag option
        PrivateLinkServiceNetworkPolicies: FeatureFlag option
    }

    member internal this.JsonModelProperties =
        {|
            addressPrefix = this.Prefix
            natGateway =
                this.NatGateway
                |> Option.map LinkedResource.AsIdObject
                |> Option.defaultValue Unchecked.defaultof<_>
            networkSecurityGroup =
                this.NetworkSecurityGroup
                |> Option.map (fun nsg ->
                    {|
                        id = nsg.ResourceId.ArmExpression.Eval()
                    |})
                |> Option.defaultValue Unchecked.defaultof<_>
            delegations =
                this.Delegations
                |> List.map (fun delegation ->
                    {|
                        name = delegation.Name.Value
                        properties =
                            {|
                                serviceName = delegation.ServiceName
                            |}
                    |})
            serviceEndpoints =
                if this.ServiceEndpoints.IsEmpty then
                    Unchecked.defaultof<_>
                else
                    this.ServiceEndpoints
                    |> List.map (fun (Network.EndpointServiceType (serviceEndpoint), locations) ->
                        {|
                            service = serviceEndpoint
                            locations = locations |> List.map (fun location -> location.ArmValue)
                        |})
            serviceEndpointPolicies =
                if this.AssociatedServiceEndpointPolicies.IsEmpty then
                    Unchecked.defaultof<_>
                else
                    this.AssociatedServiceEndpointPolicies
                    |> List.map (fun policyId -> {| id = policyId.ArmExpression.Eval() |})
            privateEndpointNetworkPolicies =
                this.PrivateEndpointNetworkPolicies
                |> Option.map (fun x -> x.ArmValue)
                |> Option.defaultValue Unchecked.defaultof<_>
            privateLinkServiceNetworkPolicies =
                this.PrivateLinkServiceNetworkPolicies
                |> Option.map (fun x -> x.ArmValue)
                |> Option.defaultValue Unchecked.defaultof<_>
        |}

    interface IArmResource with
        member this.JsonModel =
            match this.VirtualNetwork with
            | Some (Managed vnet) ->
                {| subnets.Create(vnet.Name / this.Name, dependsOn = [ vnet ]) with
                    properties = this.JsonModelProperties
                |}
            | Some (Unmanaged vnet) ->
                {| subnets.Create(vnet.Name / this.Name) with
                    properties = this.JsonModelProperties
                |}
            | None -> raiseFarmer "Subnet record must be linked to a virtual network to properly assign the resourceId."

        member this.ResourceId =
            match this.VirtualNetwork with
            | Some vnet -> subnets.resourceId (vnet.Name, this.Name)
            | None -> raiseFarmer "Subnet record must be linked to a virtual network to properly assign the resourceId."


type VirtualNetwork =
    {
        Name: ResourceName
        Location: Location
        AddressSpacePrefixes: string list
        Subnets: Subnet list
        Tags: Map<string, string>
    }

    interface IArmResource with
        member this.ResourceId = virtualNetworks.resourceId this.Name

        member this.JsonModel =
            let dependencies =
                seq {
                    for subnet in this.Subnets do
                        match subnet.NetworkSecurityGroup with
                        | Some (Managed id) -> id
                        | _ -> ()

                        match subnet.NatGateway with
                        | Some (Managed id) -> id
                        | _ -> ()
                }
                |> Set

            {| virtualNetworks.Create(this.Name, this.Location, dependsOn = dependencies, tags = this.Tags) with
                properties =
                    {|
                        addressSpace =
                            {|
                                addressPrefixes = this.AddressSpacePrefixes
                            |}
                        subnets =
                            this.Subnets
                            |> List.map (fun subnet ->
                                {|
                                    name = subnet.Name.Value
                                    properties = subnet.JsonModelProperties
                                |})
                    |}
            |}

type VPNClientProtocol =
    | IkeV2
    | SSTP
    | OpenVPN

type VpnClientConfiguration =
    {
        ClientAddressPools: IPAddressCidr list
        ClientRootCertificates: {| Name: string
                                   PublicCertData: string |} list
        ClientRevokedCertificates: {| Name: string; Thumbprint: string |} list
        ClientProtocols: VPNClientProtocol list
    }

type VirtualNetworkGateway =
    {
        Name: ResourceName
        Location: Location
        IpConfigs: {| Name: ResourceName
                      PrivateIpAllocationMethod: PrivateIpAddress.AllocationMethod
                      PublicIpName: ResourceName |} list
        VirtualNetwork: ResourceName
        GatewayType: GatewayType
        VpnType: VpnType
        EnableBgp: bool

        VpnClientConfiguration: VpnClientConfiguration option

        Tags: Map<string, string>
    }

    interface IArmResource with
        member this.ResourceId = virtualNetworkGateways.resourceId this.Name

        member this.JsonModel =
            let dependsOn =
                [
                    virtualNetworks.resourceId this.VirtualNetwork
                    for config in this.IpConfigs do
                        publicIPAddresses.resourceId config.PublicIpName
                ]

            {| virtualNetworkGateways.Create(this.Name, this.Location, dependsOn, this.Tags) with
                properties =
                    {|
                        ipConfigurations =
                            this.IpConfigs
                            |> List.mapi (fun index ipConfig ->
                                {|
                                    name = $"ipconfig{index + 1}"
                                    properties =
                                        let allocationMethod, ip =
                                            match ipConfig.PrivateIpAllocationMethod with
                                            | DynamicPrivateIp -> "Dynamic", null
                                            | StaticPrivateIp ip -> "Static", string ip

                                        {|
                                            privateIpAllocationMethod = allocationMethod
                                            privateIpAddress = ip
                                            publicIPAddress =
                                                {|
                                                    id = publicIPAddresses.resourceId(ipConfig.PublicIpName).Eval()
                                                |}
                                            subnet =
                                                {|
                                                    id =
                                                        subnets
                                                            .resourceId(
                                                                this.VirtualNetwork,
                                                                ResourceName "GatewaySubnet"
                                                            )
                                                            .Eval()
                                                |}
                                        |}
                                |})
                        sku =
                            match this.GatewayType with
                            | GatewayType.ExpressRoute sku ->
                                {|
                                    name = sku.ArmValue
                                    tier = sku.ArmValue
                                |}
                            | GatewayType.Vpn sku ->
                                {|
                                    name = sku.ArmValue
                                    tier = sku.ArmValue
                                |}
                        gatewayType = this.GatewayType.ArmValue
                        vpnType = this.VpnType.ArmValue
                        enableBgp = this.EnableBgp
                        vpnClientConfiguration =
                            match this.VpnClientConfiguration with
                            | Some vpnClientConfig ->
                                box
                                    {|
                                        vpnClientAddressPool =
                                            {|
                                                addressPrefixes =
                                                    [
                                                        for prefix in vpnClientConfig.ClientAddressPools do
                                                            IPAddressCidr.format prefix
                                                    ]
                                            |}
                                        vpnClientProtocols =
                                            [
                                                for protocol in vpnClientConfig.ClientProtocols do
                                                    match protocol with
                                                    | SSTP -> "SSTP"
                                                    | IkeV2 -> "IkeV2"
                                                    | OpenVPN -> "OpenVPN"
                                            ]
                                        vpnClientRootCertificates =
                                            [
                                                for cert in vpnClientConfig.ClientRootCertificates do
                                                    {|
                                                        name = cert.Name
                                                        properties =
                                                            {|
                                                                publicCertData = cert.PublicCertData
                                                            |}
                                                    |}
                                            ]
                                        vpnClientRevokedCertificates =
                                            [
                                                for cert in vpnClientConfig.ClientRevokedCertificates do
                                                    {|
                                                        name = cert.Name
                                                        properties = {| thumbprint = cert.Thumbprint |}
                                                    |}
                                            ]
                                        radiusServers = []
                                        vpnClientIpsecPolicies = []
                                    |}
                            | None -> null
                        activeActive = this.IpConfigs |> List.length > 1
                    |}
            |}

type Connection =
    {
        Name: ResourceName
        Location: Location
        ConnectionType: ConnectionType
        VirtualNetworkGateway1: ResourceName
        VirtualNetworkGateway2: ResourceName option
        LocalNetworkGateway: ResourceName option
        PeerId: string option
        AuthorizationKey: string option
        Tags: Map<string, string>
    }

    member private this.VNetGateway1ResourceId =
        virtualNetworkGateways.resourceId this.VirtualNetworkGateway1

    member private this.VNetGateway2ResourceId =
        this.VirtualNetworkGateway2 |> Option.map virtualNetworkGateways.resourceId

    member private this.LocalNetworkGatewayResourceId =
        this.LocalNetworkGateway |> Option.map localNetworkGateways.resourceId

    interface IArmResource with
        member this.ResourceId = connections.resourceId this.Name

        member this.JsonModel =
            let dependsOn =
                [
                    Some this.VNetGateway1ResourceId
                    this.VNetGateway2ResourceId
                    this.LocalNetworkGatewayResourceId
                ]
                |> List.choose id

            {| connections.Create(this.Name, this.Location, dependsOn, this.Tags) with
                properties =
                    {|
                        authorizationKey = this.AuthorizationKey |> Option.toObj
                        connectionType = this.ConnectionType.ArmValue
                        virtualNetworkGateway1 =
                            {|
                                id = this.VNetGateway1ResourceId.Eval()
                            |}
                        virtualNetworkGateway2 =
                            match this.VNetGateway2ResourceId with
                            | Some vng2 -> box {| id = vng2.Eval() |}
                            | None -> null
                        localNetworkGateway2 =
                            match this.LocalNetworkGatewayResourceId with
                            | Some lng -> box {| id = lng.Eval() |}
                            | None -> null
                        peer =
                            match this.PeerId with
                            | Some peerId -> box {| id = peerId |}
                            | None -> null
                    |}
            |}

type NetworkInterface =
    {
        Name: ResourceName
        Location: Location
        IpConfigs: {| SubnetName: ResourceName
                      PublicIpAddress: LinkedResource option
                      LoadBalancerBackendAddressPools: LinkedResource list |} list
        VirtualNetwork: LinkedResource
        NetworkSecurityGroup: ResourceId option
        PrivateIpAllocation: PrivateIpAddress.AllocationMethod option
        Tags: Map<string, string>
    }

    interface IArmResource with
        member this.ResourceId = networkInterfaces.resourceId this.Name

        member this.JsonModel =
            let dependsOn =
                [
                    match this.VirtualNetwork with
                    | Managed resId -> resId
                    | _ -> ()
                    for config in this.IpConfigs do
                        match config.PublicIpAddress with
                        | Some ipName -> ipName.ResourceId
                        | _ -> ()

                        for linkedResource in config.LoadBalancerBackendAddressPools do
                            match linkedResource with
                            | Managed resId -> resId
                            | _ -> ()
                    if this.NetworkSecurityGroup.IsSome then
                        this.NetworkSecurityGroup.Value
                ]

            let props =
                {|
                    ipConfigurations =
                        this.IpConfigs
                        |> List.mapi (fun index ipConfig ->
                            {|
                                name = $"ipconfig{index + 1}"
                                properties =
                                    let allocationMethod, ip =
                                        match this.PrivateIpAllocation with
                                        | Some (StaticPrivateIp ip) -> "Static", string ip
                                        | _ -> "Dynamic", null

                                    {|
                                        loadBalancerBackendAddressPools =
                                            match ipConfig.LoadBalancerBackendAddressPools with
                                            | [] -> null // Don't emit the field if there are none set.
                                            | backendPools ->
                                                backendPools
                                                |> List.map (fun lr -> lr.ResourceId |> ResourceId.AsIdObject)
                                                |> box
                                        privateIPAllocationMethod = allocationMethod
                                        privateIPAddress = ip
                                        publicIPAddress =
                                            ipConfig.PublicIpAddress
                                            |> Option.map (fun pip ->
                                                {|
                                                    id = pip.ResourceId.ArmExpression.Eval()
                                                |})
                                            |> Option.defaultValue Unchecked.defaultof<_>
                                        subnet =
                                            {|
                                                id =
                                                    { this.VirtualNetwork.ResourceId with
                                                        Type = subnets
                                                        Segments = [ ipConfig.SubnetName ]
                                                    }
                                                        .Eval()
                                            |}
                                    |}






                            |})
                |}

            match this.NetworkSecurityGroup with
            | None ->
                {| networkInterfaces.Create(this.Name, this.Location, dependsOn, this.Tags) with
                    properties = props
                |}
            | Some nsg ->
                {| networkInterfaces.Create(this.Name, this.Location, dependsOn, this.Tags) with
                    properties =
                        {| props with
                            networkSecurityGroup = {| id = nsg.Eval() |}
                        |}
                |}


type NetworkProfile =
    {
        Name: ResourceName
        Location: Location
        Dependencies: ResourceId Set
        ContainerNetworkInterfaceConfigurations: {| IpConfigs: {| Name: ResourceName
                                                                  SubnetName: ResourceName |} list |} list
        VirtualNetwork: ResourceId
        Tags: Map<string, string>
    }

    interface IArmResource with
        member this.ResourceId = networkProfiles.resourceId this.Name

        member this.JsonModel =
            {| networkProfiles.Create(this.Name, this.Location, this.Dependencies, this.Tags) with
                properties =
                    {|
                        containerNetworkInterfaceConfigurations =
                            this.ContainerNetworkInterfaceConfigurations
                            |> List.mapi (fun index containerIfConfig ->
                                {|
                                    name = $"eth{index}"
                                    properties =
                                        {|
                                            ipConfigurations =
                                                containerIfConfig.IpConfigs
                                                |> List.mapi (fun index ipConfig ->
                                                    {|
                                                        name = (ipConfig.Name.IfEmpty $"ipconfig{index + 1}").Value
                                                        properties =
                                                            {|
                                                                subnet =
                                                                    {|
                                                                        id =
                                                                            { subnets.resourceId (
                                                                                  this.VirtualNetwork.Name,
                                                                                  ipConfig.SubnetName
                                                                              ) with
                                                                                ResourceGroup =
                                                                                    this.VirtualNetwork.ResourceGroup
                                                                            }
                                                                                .Eval()
                                                                    |}
                                                            |}
                                                    |})
                                        |}
                                |})
                    |}
            |}

type ExpressRouteCircuit =
    {
        Name: ResourceName
        Location: Location
        Tier: Tier
        Family: Family
        ServiceProviderName: string
        PeeringLocation: string
        Bandwidth: int<Mbps>
        GlobalReachEnabled: bool
        Peerings: {| PeeringType: PeeringType
                     AzureASN: int
                     PeerASN: int64
                     PrimaryPeerAddressPrefix: IPAddressCidr
                     SecondaryPeerAddressPrefix: IPAddressCidr
                     SharedKey: string option
                     VlanId: int |} list
        Tags: Map<string, string>
    }

    interface IArmResource with
        member this.ResourceId = expressRouteCircuits.resourceId this.Name

        member this.JsonModel =
            {| expressRouteCircuits.Create(this.Name, this.Location, tags = this.Tags) with
                sku =
                    {|
                        name = $"{this.Tier}_{this.Family}"
                        tier = string this.Tier
                        family = string this.Family
                    |}
                properties =
                    {|
                        peerings =
                            [
                                for peer in this.Peerings do
                                    {|
                                        name = peer.PeeringType.Value
                                        properties =
                                            {|
                                                peeringType = peer.PeeringType.Value
                                                azureASN = peer.AzureASN
                                                peerASN = peer.PeerASN
                                                primaryPeerAddressPrefix =
                                                    IPAddressCidr.format peer.PrimaryPeerAddressPrefix
                                                secondaryPeerAddressPrefix =
                                                    IPAddressCidr.format peer.SecondaryPeerAddressPrefix
                                                vlanId = peer.VlanId
                                                sharedKey = peer.SharedKey
                                            |}
                                    |}
                            ]
                        serviceProviderProperties =
                            {|
                                serviceProviderName = this.ServiceProviderName
                                peeringLocation = this.PeeringLocation
                                bandwidthInMbps = this.Bandwidth
                            |}
                        globalReachEnabled = this.GlobalReachEnabled
                    |}
            |}

type ExpressRouteCircuitAuthorization =
    {
        Name: ResourceName
        Circuit: LinkedResource
    }

    interface IArmResource with
        member this.ResourceId =
            expressRouteCircuitAuthorizations.resourceId (this.Circuit.Name, this.Name)

        member this.JsonModel =
            expressRouteCircuitAuthorizations.Create(
                this.Circuit.Name / this.Name,
                dependsOn = [ this.Circuit.ResourceId ]
            )


type PrivateEndpoint =
    {
        Name: ResourceName
        Location: Location
        Subnet: SubnetReference
        Resource: LinkedResource
        CustomNetworkInterfaceName: string option
        GroupIds: string list
    }

    static member create location (resourceId: ResourceId) groupIds =
        Set.toSeq
        >> Seq.map (fun (subnet: SubnetReference, epName: string option) ->
            {
                Name =
                    epName
                    |> Option.defaultValue $"{resourceId.Name.Value}-ep-{subnet.ResourceId.Name.Value}"
                    |> ResourceName
                Location = location
                Subnet = subnet
                Resource = Managed resourceId
                CustomNetworkInterfaceName = None
                GroupIds = groupIds
            }
            :> IArmResource)

    interface IArmResource with
        member this.ResourceId = privateEndpoints.resourceId this.Name

        member this.JsonModel =
            let dependencies =
                [
                    yield! this.Subnet.Dependency |> Option.toList
                    match this.Resource with
                    | Managed x -> x
                    | _ -> ()
                ]

            {| privateEndpoints.Create(this.Name, this.Location, dependencies) with
                properties =
                    {|
                        subnet = {| id = this.Subnet.ResourceId.Eval() |}
                        customNetworkInterfaceName = this.CustomNetworkInterfaceName |> Option.toObj
                        privateLinkServiceConnections =
                            [
                                {|
                                    name = this.Name.Value
                                    properties =
                                        {|
                                            privateLinkServiceId = this.Resource.ResourceId.Eval()
                                            groupIds = this.GroupIds
                                        |}
                                |}
                            ]
                    |}
            |}

type GatewayTransit =
    | UseRemoteGateway
    | UseLocalGateway
    | GatewayTransitDisabled

type PeerAccess =
    | AccessDenied
    | AccessOnly
    | ForwardOnly
    | AccessAndForward

type NetworkPeering =
    {
        Location: Location
        OwningVNet: LinkedResource
        RemoteVNet: LinkedResource
        RemoteAccess: PeerAccess
        GatewayTransit: GatewayTransit
        DependsOn: ResourceId Set
    }

    member this.Name = this.OwningVNet.Name / $"peering-%s{this.RemoteVNet.Name.Value}"

    interface IArmResource with
        member this.ResourceId = virtualNetworkPeering.resourceId this.Name

        member this.JsonModel =
            let deps =
                [
                    match this.OwningVNet with
                    | Managed id -> id
                    | _ -> ()
                    match this.RemoteVNet with
                    | Managed id -> id
                    | _ -> ()
                    yield! this.DependsOn
                ]

            {| virtualNetworkPeering.Create(this.Name, this.Location, deps) with
                properties =
                    {|
                        allowVirtualNetworkAccess =
                            match this.RemoteAccess with
                            | AccessOnly
                            | AccessAndForward -> true
                            | _ -> false
                        allowForwardedTraffic =
                            match this.RemoteAccess with
                            | ForwardOnly
                            | AccessAndForward -> true
                            | _ -> false
                        allowGatewayTransit =
                            match this.GatewayTransit with
                            | UseLocalGateway
                            | UseRemoteGateway -> true
                            | _ -> false
                        useRemoteGateways =
                            match this.GatewayTransit with
                            | UseRemoteGateway -> true
                            | _ -> false
                        remoteVirtualNetwork =
                            {|
                                id =
                                    match this.RemoteVNet with
                                    | Managed id
                                    | Unmanaged id -> id.ArmExpression.Eval()
                            |}
                    |}
            |}

type NatGateway =
    {
        Name: ResourceName
        Location: Location
        IdleTimeout: int<Minutes>
        PublicIpAddresses: LinkedResource list
        PublicIpPrefixes: LinkedResource list
        Tags: Map<string, string>
    }

    interface IArmResource with
        member this.ResourceId = natGateways.resourceId this.Name

        member this.JsonModel =
            let dependencies =
                seq {
                    for linkedResource in this.PublicIpAddresses @ this.PublicIpPrefixes do
                        match linkedResource with
                        | Managed resId -> resId
                        | _ -> ()
                }
                |> Set.ofSeq

            {| natGateways.Create(this.Name, this.Location, dependsOn = dependencies, tags = this.Tags) with
                sku = {| name = "Standard" |}
                properties =
                    {|
                        idleTimeoutInMinutes = this.IdleTimeout
                        publicIpAddresses = this.PublicIpAddresses |> List.map LinkedResource.AsIdObject
                        publicIpPrefixes = this.PublicIpPrefixes |> List.map LinkedResource.AsIdObject
                    |}
            |}
