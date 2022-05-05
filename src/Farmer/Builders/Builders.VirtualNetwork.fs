[<AutoOpen>]
module Farmer.Builders.VirtualNetwork

open Farmer
open Farmer.Builders
open Farmer.Network
open Farmer.Arm.Network

type PeeringMode = 
    | TwoWay
    | OneWayToRemote
    | OneWayFromRemote

type SubnetConfig =
    { Name: ResourceName
      Prefix: IPAddressCidr
      VirtualNetwork : LinkedResource option
      NetworkSecurityGroup : LinkedResource option
      Delegations:  SubnetDelegationService list
      ServiceEndpoints: (EndpointServiceType * Location list) list
      AssociatedServiceEndpointPolicies : ResourceId list
      AllowPrivateEndpoints: FeatureFlag option
      PrivateLinkServiceNetworkPolicies: FeatureFlag option
      RouteTable: LinkedResource option
      Dependencies: ResourceId Set }
    member internal this.AsSubnetResource =
        { Name = this.Name
          Prefix = IPAddressCidr.format this.Prefix
          VirtualNetwork = this.VirtualNetwork
          NetworkSecurityGroup = this.NetworkSecurityGroup
          Delegations = this.Delegations |> List.map (fun (SubnetDelegationService(delegation)) ->
              { Name = ResourceName delegation; ServiceName = delegation })
          ServiceEndpoints = this.ServiceEndpoints
          AssociatedServiceEndpointPolicies = this.AssociatedServiceEndpointPolicies
          // PrivateEndpointNetworkPolicies prevents the use of private endpoints so 
          // to ENable private endpoints we have to DISable PrivateEndpointNetworkPolicies
          PrivateEndpointNetworkPolicies = this.AllowPrivateEndpoints |> Option.map FeatureFlag.invert 
          PrivateLinkServiceNetworkPolicies = this.PrivateLinkServiceNetworkPolicies
          RouteTable = this.RouteTable
          DependsOn = this.Dependencies
            |> LinkedResource.addToSetIfSomeManaged this.VirtualNetwork
            |> LinkedResource.addToSetIfSomeManaged this.RouteTable
            |> LinkedResource.addToSetIfSomeManaged this.NetworkSecurityGroup
        }
    interface IBuilder with
        member this.ResourceId =
            match this.VirtualNetwork with
            | Some vnet ->
                subnets.resourceId (vnet.Name, this.Name)
            | None -> raiseFarmer "Subnet record must be linked to a virtual network to properly assign the resourceId."
        member this.BuildResources _ = [ this.AsSubnetResource ]

type SubnetBuilder() =
    member _.Yield _ =
        { Name = ResourceName.Empty
          Prefix = { Address = System.Net.IPAddress.Parse("10.100.0.0"); Prefix = 16 }
          VirtualNetwork = None
          NetworkSecurityGroup = None
          Delegations = []
          ServiceEndpoints = []
          AssociatedServiceEndpointPolicies = [] 
          AllowPrivateEndpoints = None
          PrivateLinkServiceNetworkPolicies = None
          RouteTable = None
          Dependencies = Set.empty }
    /// Sets the name of the subnet
    [<CustomOperation "name">]
    member _.Name(state:SubnetConfig, name) = { state with Name = ResourceName name }
    /// Sets the network prefix in CIDR notation
    [<CustomOperation "prefix">]
    member _.Prefix(state:SubnetConfig, prefix) = { state with Prefix = IPAddressCidr.parse prefix }
    /// Sets the network security group for subnet
    [<CustomOperation "network_security_group">]
    member _.NetworkSecurityGroup(state:SubnetConfig, nsg:IArmResource) =
        { state with NetworkSecurityGroup = Some (Managed nsg.ResourceId) }
    member _.NetworkSecurityGroup(state:SubnetConfig, nsg:ResourceId) =
        { state with NetworkSecurityGroup = Some (Managed nsg) }
    member _.NetworkSecurityGroup(state:SubnetConfig, nsg:NsgConfig) =
        { state with NetworkSecurityGroup = Some (Managed (nsg :> IBuilder).ResourceId) }
    /// Links the subnet to an existing network security group.
    [<CustomOperation "link_to_network_security_group">]
    member _.LinkToNetworkSecurityGroup(state:SubnetConfig, nsg:IArmResource) =
        { state with NetworkSecurityGroup = Some (Unmanaged (nsg.ResourceId)) }
    member _.LinkToNetworkSecurityGroup(state:SubnetConfig, nsg:ResourceId) =
        { state with NetworkSecurityGroup = Some (Unmanaged nsg) }
    member _.LinkToNetworkSecurityGroup(state:SubnetConfig, nsg:NsgConfig) =
        { state with NetworkSecurityGroup = Some (Unmanaged (nsg :> IBuilder).ResourceId) }
    /// Links the subnet to an managed virtual network.
    [<CustomOperation "link_to_vnet">]
    member _.LinkToVirtualNetwork(state:SubnetConfig, vnet:IArmResource) =
        { state with VirtualNetwork = Some (Managed (vnet.ResourceId)) }
    member _.LinkToVirtualNetwork(state:SubnetConfig, vnet:ResourceId) =
        { state with VirtualNetwork = Some (Managed vnet) }
    member _.LinkToVirtualNetwork(state:SubnetConfig, vnet:NsgConfig) =
        { state with VirtualNetwork = Some (Managed (vnet :> IBuilder).ResourceId) }
    /// Links the subnet to an existing, externally defined virtual network.
    [<CustomOperation "link_to_unmanaged_vnet">]
    member _.LinkToUnmanagedVirtualNetwork(state:SubnetConfig, vnet:IArmResource) =
        { state with VirtualNetwork = Some (Unmanaged (vnet.ResourceId)) }
    member _.LinkToUnmanagedVirtualNetwork(state:SubnetConfig, vnet:ResourceId) =
        { state with VirtualNetwork = Some (Unmanaged vnet) }
    member _.LinkToUnmanagedVirtualNetwork(state:SubnetConfig, vnet:NsgConfig) =
        { state with VirtualNetwork = Some (Unmanaged (vnet :> IBuilder).ResourceId) }
    /// Sets the network prefix in CIDR notation
    [<CustomOperation "add_delegations">]
    member _.AddDelegations(state:SubnetConfig, delegations) = { state with Delegations = state.Delegations @ delegations }
    /// Add service endpoint types to this subnet
    [<CustomOperation "add_service_endpoints">]
    member _.AddServiceEndpoints(state:SubnetConfig, serviceEndpoints) = { state with ServiceEndpoints = state.ServiceEndpoints @ serviceEndpoints }
    /// Associates service endpoint policies with this subnet
    [<CustomOperation "associate_service_endpoint_policies">]
    member _.AssociateServiceEndpointPolicies(state:SubnetConfig, servicePolicyIds) = { state with AssociatedServiceEndpointPolicies = state.AssociatedServiceEndpointPolicies @ servicePolicyIds }
    /// Disable private endpoint network policies
    [<CustomOperation "allow_private_endpoints">]
    member _.PrivateEndpoints(state:SubnetConfig, value:FeatureFlag ) = { state with AllowPrivateEndpoints = Some value }
    /// Enable or disable private link service network policies on this subnet to allow specifying the private link IP.
    [<CustomOperation "private_link_service_network_policies">]
    member _.PrivateLinkServiceNetworkPolicies(state:SubnetConfig, flag:FeatureFlag ) =
        { state with PrivateLinkServiceNetworkPolicies = Some flag}
    [<CustomOperation "link_to_route_table">]
    member _.LinkToRouteTable( state:SubnetConfig, routeTable: ResourceId) = {state with RouteTable = Some (Managed routeTable) }
    [<CustomOperation "link_to_unmanaged_route_table">]
    member _.LinkToUnmanagedRouteTable( state:SubnetConfig, routeTable: ResourceId) = {state with RouteTable = Some (Unmanaged routeTable) }

    /// Enable support for additional dependencies.
    interface IDependable<SubnetConfig> with member _.Add state newDeps = { state with Dependencies = state.Dependencies + newDeps }

let subnet = SubnetBuilder ()
/// Specification for a subnet to build from an address space.
type SubnetBuildSpec =
    { Name: string
      Size: int
      NetworkSecurityGroup : LinkedResource option
      Delegations: SubnetDelegationService list
      ServiceEndpoints: (EndpointServiceType * Location list) list
      AssociatedServiceEndpointPolicies : ResourceId list
      AllowPrivateEndpoints: FeatureFlag option
      PrivateLinkServiceNetworkPolicies: FeatureFlag option
      RouteTable: LinkedResource option }
/// Builds a subnet of a certain CIDR block size.
let buildSubnet name size =
    { Name = name
      Size = size
      NetworkSecurityGroup = None
      Delegations = []
      ServiceEndpoints = []
      AssociatedServiceEndpointPolicies = []
      AllowPrivateEndpoints = None
      PrivateLinkServiceNetworkPolicies = None
      RouteTable = None}
/// Builds a subnet of a certain CIDR block size with service delegations.
let buildSubnetDelegations name size delegations =
    { Name = name
      Size = size
      NetworkSecurityGroup = None
      Delegations = delegations
      ServiceEndpoints = []
      AssociatedServiceEndpointPolicies = []
      AllowPrivateEndpoints = None
      PrivateLinkServiceNetworkPolicies = None
      RouteTable = None }
let buildSubnetAllowPrivateEndpoints name size =
    { Name = name
      Size = size
      NetworkSecurityGroup = None
      Delegations = []
      ServiceEndpoints = []
      AssociatedServiceEndpointPolicies = []
      AllowPrivateEndpoints = None
      PrivateLinkServiceNetworkPolicies = None
      RouteTable = None }

type SubnetSpecBuilder () =
    member _.Yield _ =
        { Name = ""
          Size = 24
          NetworkSecurityGroup = None
          Delegations = []
          ServiceEndpoints = []
          AssociatedServiceEndpointPolicies = []
          AllowPrivateEndpoints = None
          PrivateLinkServiceNetworkPolicies = None
          RouteTable = None }
    /// Sets the name of the subnet to build
    [<CustomOperation "name">]
    member _.Name(state:SubnetBuildSpec, name) =
        { state with Name = name }
    /// Sets the size for the network prefix to build
    [<CustomOperation "size">]
    member _.Size(state:SubnetBuildSpec, size) =
        { state with Size = size }
    /// Sets the network security group for subnet
    [<CustomOperation "network_security_group">]
    member _.NetworkSecurityGroup(state:SubnetBuildSpec, nsg:IArmResource) =
        { state with NetworkSecurityGroup = Some (Managed (nsg.ResourceId)) }
    member _.NetworkSecurityGroup(state:SubnetBuildSpec, nsg:ResourceId) =
        { state with NetworkSecurityGroup = Some (Managed nsg) }
    member _.NetworkSecurityGroup(state:SubnetBuildSpec, nsg:NsgConfig) =
        { state with NetworkSecurityGroup = Some (Managed (nsg :> IBuilder).ResourceId) }
    /// Links the subnet to an existing network security group.
    [<CustomOperation "link_to_network_security_group">]
    member _.LinkToNetworkSecurityGroup(state:SubnetBuildSpec, nsg:IArmResource) =
        { state with NetworkSecurityGroup = Some (Unmanaged (nsg.ResourceId)) }
    member _.LinkToNetworkSecurityGroup(state:SubnetBuildSpec, nsg:ResourceId) =
        { state with NetworkSecurityGroup = Some (Unmanaged nsg) }
    member _.LinkToNetworkSecurityGroup(state:SubnetBuildSpec, nsg:NsgConfig) =
        { state with NetworkSecurityGroup = Some (Unmanaged (nsg :> IBuilder).ResourceId) }
    /// Adds any services to delegate this subnet
    [<CustomOperation "add_delegations">]
    member _.AddDelegations(state:SubnetBuildSpec, delegations) =
        { state with Delegations = state.Delegations @ delegations }
    /// Adds service endpoints to build for this subnet
    [<CustomOperation "add_service_endpoints">]
    member _.AddServiceEndpoints(state:SubnetBuildSpec, serviceEndpoints) =
        { state with ServiceEndpoints = state.ServiceEndpoints @ serviceEndpoints }
    /// Associates the built subnet with service endpoint policies
    [<CustomOperation "add_service_endpoint_policies">]
    member _.AddAssociatedServiceEndpointPolicies(state:SubnetBuildSpec, policies) =
        { state with AssociatedServiceEndpointPolicies = state.AssociatedServiceEndpointPolicies @ policies }
    /// Disable private endpoint netwokj security policies to enable use of private endpoints
    [<CustomOperation "allow_private_endpoints">]
    member _.PrivateEndpoints(state:SubnetBuildSpec, flag:FeatureFlag ) =
        { state with AllowPrivateEndpoints = Some flag}
    /// Enable or disable private link service network policies on this subnet to allow specifying the private link IP.
    [<CustomOperation "private_link_service_network_policies">]
    member _.PrivateLinkServiceNetworkPolicies(state:SubnetBuildSpec, flag:FeatureFlag ) =
        { state with PrivateLinkServiceNetworkPolicies = Some flag}
    [<CustomOperation "link_to_route_table">]
    member _.LinkToRouteTable( state:SubnetBuildSpec, routeTableReference) = {state with RouteTable = routeTableReference }
    member _.LinkToRouteTable( state:SubnetBuildSpec, routeTable: ResourceId) = {state with RouteTable = Some (Managed routeTable) }
    [<CustomOperation "link_to_unmanaged_route_table">]
    member _.LinkToUnmanagedRouteTable( state:SubnetBuildSpec, routeTable: ResourceId) = {state with RouteTable = Some (Unmanaged routeTable) }

let subnetSpec = SubnetSpecBuilder()

/// A specification building an address space and subnets.
type AddressSpaceSpec =
    { Space : string
      Subnets : SubnetBuildSpec list }
open System.Runtime.InteropServices
/// Builder for an address space with automatically carved subnets.
type AddressSpaceBuilder() =
    member _.Yield _ = { Space = ""; Subnets = [] }
    [<CustomOperation("space")>]
    member _.Space(state:AddressSpaceSpec, space) = { state with Space = space }
    [<CustomOperation("subnets")>]
    member _.Subnets(state:AddressSpaceSpec, subnets) = { state with Subnets = state.Subnets @ subnets }
    member private _.buildSubnet(state:AddressSpaceSpec,
                                 name:string,
                                 size:int,
                                 ?delegations:SubnetDelegationService list,
                                 ?serviceEndpoints:(EndpointServiceType * Location list) list,
                                 ?associatedServiceEndpointPolicies:ResourceId list,
                                 ?allowPrivateEndpoints: FeatureFlag,
                                 ?privateLinkServiceNetworkPolicies: FeatureFlag,
                                 ?nsg:LinkedResource,
                                 ?routeTable:LinkedResource) =
        let subnetBuildSpec =
            { Name = name
              Size = size
              NetworkSecurityGroup = nsg
              Delegations = delegations |> Option.defaultValue []
              ServiceEndpoints = serviceEndpoints |> Option.defaultValue []
              AssociatedServiceEndpointPolicies = associatedServiceEndpointPolicies |> Option.defaultValue [] 
              AllowPrivateEndpoints = allowPrivateEndpoints
              PrivateLinkServiceNetworkPolicies = privateLinkServiceNetworkPolicies 
              RouteTable = routeTable}
        { state with Subnets = state.Subnets @ [ subnetBuildSpec ] }
    [<CustomOperation("build_subnet")>]
    member this.BuildSubnet(state:AddressSpaceSpec, name:string, size:int) =
        this.buildSubnet(state, name, size)
    [<CustomOperation("build_subnet_delegated")>]
    member this.BuildSubnetDelegated(state:AddressSpaceSpec, name:string, size:int, delegations:SubnetDelegationService list) =
        this.buildSubnet(state, name, size, delegations=delegations)

let addressSpace = AddressSpaceBuilder ()

type VNetPeeringSpec = 
    { RemoteVNet: LinkedResource
      Direction : PeeringMode
      Access: PeerAccess
      Transit: GatewayTransit 
      DependsOn: ResourceId Set}

type VirtualNetworkConfig =
    { Name : ResourceName
      AddressSpacePrefixes : string list
      Subnets : SubnetConfig list
      Peers: VNetPeeringSpec list 
      Tags: Map<string,string> }
    member this.SubnetIds = 
      this.Subnets
      |> List.map (fun subnet -> subnet.Name.Value, subnets.resourceId(this.Name, subnet.Name) )
      |> Map.ofList
    member this.ResourceId = virtualNetworks.resourceId this.Name
    interface IBuilder with
        member this.ResourceId = this.ResourceId
        member this.BuildResources location = [
            { Name = this.Name
              Location = location
              AddressSpacePrefixes = this.AddressSpacePrefixes
              Subnets = this.Subnets |> List.map (fun subnetConfig -> subnetConfig.AsSubnetResource)
              Tags = this.Tags
            }
            for {RemoteVNet=remote; Direction=direction; Access=access; Transit=transit; DependsOn = deps} in this.Peers do
                match direction with
                | OneWayToRemote| TwoWay -> 
                    { Location = location
                      OwningVNet = Managed this.ResourceId
                      RemoteVNet = remote
                      RemoteAccess = access
                      GatewayTransit = transit
                      DependsOn = deps} 
                | _ -> ()
                match direction with
                | OneWayFromRemote | TwoWay -> 
                    { Location = location
                      OwningVNet = remote
                      RemoteVNet = Managed this.ResourceId
                      RemoteAccess = access
                      GatewayTransit = 
                        match transit with 
                        | UseRemoteGateway -> UseLocalGateway 
                        | UseLocalGateway -> UseRemoteGateway 
                        | GatewayTransitDisabled -> GatewayTransitDisabled
                      DependsOn = deps }
                | _ -> ()
        ]
        
type VirtualNetworkBuilder() =
    member _.Yield _ =
      { Name = ResourceName.Empty
        AddressSpacePrefixes = []
        Subnets = List.empty
        Peers = List.empty
        Tags = Map.empty }
    /// Sets the name of the virtual network
    [<CustomOperation "name">]
    member _.Name(state:VirtualNetworkConfig, name) = { state with Name = ResourceName name }
    /// Adds address spaces prefixes
    [<CustomOperation "add_address_spaces">]
    member _.AddAddressSpaces(state:VirtualNetworkConfig, prefixes) = { state with AddressSpacePrefixes = state.AddressSpacePrefixes @ prefixes }
    /// Adds subnets
    [<CustomOperation "add_subnets">]
    member _.AddSubnets(state:VirtualNetworkConfig, subnets) = { state with Subnets = state.Subnets @ subnets }
    /// Peers this VNet with other VNets to allow communication between the VNets as if they were one
    [<CustomOperation "add_peerings">]
    member _.AddPeers(state:VirtualNetworkConfig, peers) = { state with Peers = state.Peers @ peers }
    member this.AddPeers(state:VirtualNetworkConfig, peers:(LinkedResource * PeeringMode) list) = 
        let makeSpec (peer:LinkedResource, direction) = 
            { RemoteVNet = Managed peer.ResourceId
              Direction = direction
              Access = AccessAndForward
              Transit = GatewayTransitDisabled 
              DependsOn = Set.empty }
        this.AddPeers (state, peers |> List.map makeSpec )
    member this.AddPeers(state:VirtualNetworkConfig, peers:LinkedResource list) = this.AddPeers (state, peers |> List.map (fun peer -> (peer, TwoWay)) )
    member this.AddPeers(state:VirtualNetworkConfig, peers:VirtualNetworkConfig list) = this.AddPeers (state, peers |> List.map (fun x -> Managed x.ResourceId))
    member this.AddPeers(state:VirtualNetworkConfig, peers:(VirtualNetworkConfig * PeeringMode) list) = this.AddPeers (state, peers |> List.map (fun (peer, mode) -> (Managed peer.ResourceId, mode)) )
    /// Peers this VNet with another VNet to allow communication between the VNets as if they were one
    [<CustomOperation "add_peering">]
    member this.AddPeer(state:VirtualNetworkConfig, spec:VNetPeeringSpec) = this.AddPeers(state, [spec])
    member this.AddPeer(state:VirtualNetworkConfig, peer:LinkedResource) = this.AddPeers(state, [peer])
    member this.AddPeer(state:VirtualNetworkConfig, (peer,direction):LinkedResource*PeeringMode) = this.AddPeers(state, [peer,direction])
    member this.AddPeer(state:VirtualNetworkConfig, peer:VirtualNetworkConfig) = this.AddPeers(state, [peer])
    member this.AddPeer(state:VirtualNetworkConfig, (peer,direction):VirtualNetworkConfig*PeeringMode) = this.AddPeers(state, [peer,direction])

    [<CustomOperation "build_address_spaces">]
    member _.BuildAddressSpaces(state:VirtualNetworkConfig, addressSpaces:AddressSpaceSpec list) =
        let newSubnets =
            addressSpaces
            |> List.collect (fun addressSpaceConfig ->
                let addressSpace = IPAddressCidr.parse addressSpaceConfig.Space
                let sizes = [
                    for subnet in addressSpaceConfig.Subnets do
                        if subnet.Size > 29 then invalidArg "size" $"Subnet must be of /29 or larger, cannot carve subnet {subnet.Name} of /{subnet.Size}"
                        subnet.Size
                ]
                IPAddressCidr.carveAddressSpace addressSpace sizes
                |> List.zip (addressSpaceConfig.Subnets |> List.map (fun s -> s.Name, s.Delegations, s.ServiceEndpoints, s.AssociatedServiceEndpointPolicies, s.AllowPrivateEndpoints, s.PrivateLinkServiceNetworkPolicies, s.NetworkSecurityGroup, s.RouteTable))
                |> List.map (fun ((name, delegations, serviceEndpoints, serviceEndpointPolicies, allowPrivateEndpoints, privateLinkServiceNetworkPolicies, nsg, routeTable), cidr) ->
                    { Name = ResourceName name
                      Prefix = cidr
                      VirtualNetwork = Some (Managed (virtualNetworks.resourceId state.Name))
                      NetworkSecurityGroup = nsg
                      Delegations = delegations
                      ServiceEndpoints = serviceEndpoints
                      AssociatedServiceEndpointPolicies = serviceEndpointPolicies
                      AllowPrivateEndpoints = allowPrivateEndpoints
                      PrivateLinkServiceNetworkPolicies = privateLinkServiceNetworkPolicies
                      RouteTable = routeTable
                      Dependencies = Set.empty }
                ))
        let newAddressSpaces = addressSpaces |> List.map (fun addressSpace -> addressSpace.Space)
        { state with
            Subnets = state.Subnets @ newSubnets
            AddressSpacePrefixes = state.AddressSpacePrefixes @ newAddressSpaces }
    interface ITaggable<VirtualNetworkConfig> with member _.Add state tags = { state with Tags = state.Tags |> Map.merge tags }

let vnet = VirtualNetworkBuilder ()

type VNetPeeringSpecBuilder() = 
    member _.Yield _ =
        { RemoteVNet = Unmanaged (virtualNetworks.resourceId "")
          Direction = TwoWay
          Access = AccessAndForward
          Transit = GatewayTransitDisabled
          DependsOn = Set.empty }
    [<CustomOperation "remote_vnet">]
    member _.VNet(state:VNetPeeringSpec, vnet) = {state with RemoteVNet = vnet}
    member _.VNet(state:VNetPeeringSpec, vnet:VirtualNetworkConfig) = {state with RemoteVNet = Managed vnet.ResourceId}
    [<CustomOperation "direction">]
    member _.Mode(state:VNetPeeringSpec, direction) = {state with Direction = direction}
    [<CustomOperation "access">]
    member _.Access(state:VNetPeeringSpec, access) = {state with Access = access}
    [<CustomOperation "transit">]
    member _.GatewayTransit(state:VNetPeeringSpec, transit) = {state with Transit = transit}
    interface IDependable<VNetPeeringSpec> with member _.Add state resources = {state with DependsOn = state.DependsOn |> Set.union resources}

let vnetPeering = VNetPeeringSpecBuilder ()

type PrivateLinkServiceConnection = 
    { Resource: LinkedResource 
      GroupIds: string list }

type PrivateEndpointConfig = 
    { Name: ResourceName
      Subnet : SubnetReference option
      PrivateLinkServiceConnection: PrivateLinkServiceConnection option
      PrivateDnsZone: LinkedResource option }
    interface IBuilder with
        member this.ResourceId = privateEndpoints.resourceId this.Name
        member this.BuildResources location = 
            let serviceConn = 
                match this.PrivateLinkServiceConnection with
                | Some sc -> sc
                | None -> raiseFarmer "Private endpoint must be attached to a resource"

            let endpoint = 
                { Name = this.Name
                  Subnet = match this.Subnet with | Some sn -> sn | None -> raiseFarmer "Must have linked subnet"
                  Location = location
                  Resource = serviceConn.Resource
                  GroupIds = serviceConn.GroupIds }

            [
                endpoint

                match this.PrivateDnsZone with 
                | None -> ()
                | Some zone -> {
                    Name = (ResourceName $"{this.Name.Value}/dnszone")
                    Location = location
                    PrivateEndpoint = (Managed (this :> IBuilder).ResourceId)
                    PrivateDnsZone = zone }
            ]
type SubnetReference with
    static member create (vnetConfig:VirtualNetworkConfig, subnetName) = ViaManagedVNet (vnetConfig.ResourceId,subnetName)
    static member create (vnetConfig:SubnetConfig) = Direct (Managed (vnetConfig:>IBuilder).ResourceId)

type PrivateEndpointBuilder() =
    member _.Yield _ =
        { Name = ResourceName.Empty
          Subnet = None
          PrivateLinkServiceConnection = None
          PrivateDnsZone = None }
    
    [<CustomOperation "name">]
    member _.Name(state:PrivateEndpointConfig, name:string) = { state with Name = (ResourceName name) }
    member _.Name(state:PrivateEndpointConfig, name:ResourceName) = { state with Name = name }

    [<CustomOperation "subnet">]
    member _.Subnet(state:PrivateEndpointConfig, subnet:SubnetReference) = { state with Subnet = Some subnet }
    member _.Subnet(state:PrivateEndpointConfig, subnet:SubnetConfig) = { state with Subnet = Some (SubnetReference.create(subnet)) }

    [<CustomOperation "link_to_resource">]
    member _.PrivateLinkConnection(state:PrivateEndpointConfig, resource:LinkedResource) = 
        let groupIds =
            match resource.ResourceId.Type.Type with
            | "Microsoft.Web/sites" -> "sites"
            | _ -> raiseFarmer $"Invalid resource type. Cannot link private endpoint to type {resource.ResourceId.Type.Type}"

        { state with PrivateLinkServiceConnection = Some { Resource = resource; GroupIds = [groupIds] } }

    [<CustomOperation "link_to_private_dns_zone">]
    member _.LinkToDnsZone(state:PrivateEndpointConfig, zone:DnsZoneConfig) = { state with PrivateDnsZone = Some (Managed (zone:> IBuilder).ResourceId) }

    [<CustomOperation "link_to_unmanaged_private_dns_zone">]
    member _.LinkToDnsZone(state:PrivateEndpointConfig, zone:LinkedResource) = { state with PrivateDnsZone = Some zone }

let privateEndpoint = PrivateEndpointBuilder ()