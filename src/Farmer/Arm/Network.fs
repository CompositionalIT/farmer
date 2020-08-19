[<AutoOpen>]
module Farmer.Arm.Network

open Farmer
open Farmer.CoreTypes
open Farmer.ExpressRoute
open Farmer.VirtualNetworkGateway

let connections = ResourceType "Microsoft.Network/connections"
let expressRouteCircuits = ResourceType "Microsoft.Network/expressRouteCircuits"
let networkInterfaces = ResourceType "Microsoft.Network/networkInterfaces"
let networkProfiles = ResourceType "Microsoft.Network/networkProfiles"
let publicIPAddresses = ResourceType "Microsoft.Network/publicIPAddresses"
let subnets = ResourceType "Microsoft.Network/virtualNetworks/subnets"
let virtualNetworks = ResourceType "Microsoft.Network/virtualNetworks"
let virtualNetworkGateways = ResourceType "Microsoft.Network/virtualNetworkGateways"
let localNetworkGateways = ResourceType "Microsoft.Network/localNetworkGateways"

type PublicIpAddress =
    { Name : ResourceName
      Location : Location
      DomainNameLabel : string option
      Tags: Map<string,string>  }
    interface IArmResource with
        member this.ResourceName = this.Name
        member this.JsonModel =
            {| ``type`` = publicIPAddresses.ArmValue
               apiVersion = "2018-11-01"
               name = this.Name.Value
               location = this.Location.ArmValue
               properties =
                  {| publicIPAllocationMethod = "Dynamic"
                     dnsSettings =
                        match this.DomainNameLabel with
                        | Some label -> box {| domainNameLabel = label.ToLower() |}
                        | None -> null |}
               tags = this.Tags
            |} :> _

type VirtualNetwork =
    { Name : ResourceName
      Location : Location
      AddressSpacePrefixes : string list
      Subnets : {| Name : ResourceName; Prefix : string; Delegations: {| Name: ResourceName; ServiceName: string |} list |} list;
      Tags: Map<string,string>  }
    interface IArmResource with
        member this.ResourceName = this.Name
        member this.JsonModel =
            {| ``type`` = virtualNetworks.ArmValue
               apiVersion = "2018-11-01"
               name = this.Name.Value
               location = this.Location.ArmValue
               properties =
                    {| addressSpace = {| addressPrefixes = this.AddressSpacePrefixes |}
                       subnets =
                        this.Subnets
                        |> List.map(fun subnet ->
                           {| name = subnet.Name.Value
                              properties =
                                  {| addressPrefix = subnet.Prefix
                                     delegations = subnet.Delegations
                                     |> List.map (fun delegation ->
                                         {| name = delegation.Name.Value
                                            properties = {| serviceName = delegation.ServiceName |}
                                         |})
                                  |}
                           |})
                    |}
               tags = this.Tags
            |} :> _
type VirtualNetworkGateway =
    { Name : ResourceName
      Location : Location
      IpConfigs : {| Name : ResourceName
                     PrivateIpAllocationMethod : PrivateIpAllocationMethod
                     PublicIpName : ResourceName |} list
      VirtualNetwork : ResourceName
      GatewayType : GatewayType
      VpnType : VpnType
      EnableBgp : bool
      Tags: Map<string,string>  }
    interface IArmResource with
        member this.ResourceName = this.Name
        member this.JsonModel =
            {| ``type`` = "Microsoft.Network/virtualNetworkGateways"
               apiVersion = "2020-05-01"
               name = this.Name.Value
               location = this.Location.ArmValue
               dependsOn = [
                   ArmExpression.resourceId(virtualNetworks, this.VirtualNetwork).Eval()
                   for config in this.IpConfigs do
                        ArmExpression.resourceId(publicIPAddresses, config.PublicIpName).Eval()
               ]
               properties =
                    {| ipConfigurations = this.IpConfigs
                        |> List.mapi(fun index ipConfig ->
                           {| name = sprintf "ipconfig%i" (index + 1)
                              properties =
                                let allocationMethod, ip =
                                    match ipConfig.PrivateIpAllocationMethod with
                                    | DynamicPrivateIp -> "Dynamic", null
                                    | StaticPrivateIp ip -> "Static", string ip
                                {| privateIpAllocationMethod = allocationMethod; privateIpAddress = ip
                                   publicIPAddress = {| id = ArmExpression.resourceId(publicIPAddresses, ipConfig.PublicIpName).Eval() |}
                                   subnet = {| id = ArmExpression.resourceId(subnets, this.VirtualNetwork, ResourceName "GatewaySubnet").Eval() |}
                                |}
                           |})
                       sku =
                           match this.GatewayType with
                           | GatewayType.ExpressRoute sku -> {| name = sku.ArmValue; tier = sku.ArmValue |}
                           | GatewayType.Vpn sku -> {| name = sku.ArmValue; tier = sku.ArmValue |}
                       gatewayType = this.GatewayType.ArmValue
                       vpnType = this.VpnType.ArmValue
                       enableBgp = this.EnableBgp
                       activeActive = this.IpConfigs |> List.length > 1
                    |}
               tags = this.Tags
            |} :> _
type Connection =
    { Name : ResourceName
      Location : Location
      ConnectionType : ConnectionType
      VirtualNetworkGateway1 : ResourceName
      VirtualNetworkGateway2 : ResourceName option
      LocalNetworkGateway : ResourceName option
      PeerId : string option
      AuthorizationKey : string option
      Tags: Map<string,string>  }
    member private this.VNetGateway1ResourceId = ArmExpression.resourceId(virtualNetworkGateways, this.VirtualNetworkGateway1)
    member private this.VNetGateway2ResourceId = this.VirtualNetworkGateway2 |> Option.map(fun gw -> ArmExpression.resourceId(virtualNetworkGateways, gw))
    member private this.LocalNetworkGatewayResourceId = this.LocalNetworkGateway |> Option.map(fun lng -> ArmExpression.resourceId(localNetworkGateways, lng))

    interface IArmResource with
        member this.ResourceName = this.Name
        member this.JsonModel =
            {| ``type`` = "Microsoft.Network/connections"
               apiVersion = "2020-04-01"
               name = this.Name.Value
               location = this.Location.ArmValue
               dependsOn =
                    [ Some this.VNetGateway1ResourceId; this.VNetGateway2ResourceId; this.LocalNetworkGatewayResourceId ]
                    |> List.choose id
                    |> List.map(fun r -> r.Eval())
               properties =
                    {| authorizationKey =
                            match this.AuthorizationKey with
                            | Some key -> key
                            | None -> null
                       connectionType = this.ConnectionType.ArmValue
                       virtualNetworkGateway1 =
                            {| id = this.VNetGateway1ResourceId.Eval() |}
                       virtualNetworkGateway2 =
                            match this.VNetGateway2ResourceId with
                            | Some vng2 -> box {| id = vng2.Eval() |}
                            | None -> null
                       localNetworkGateway1 =
                            match this.LocalNetworkGatewayResourceId with
                            | Some lng -> box {| id = lng.Eval() |}
                            | None -> null
                       peer =
                            match this.PeerId with
                            | Some peerId -> box {| id = peerId |}
                            | None -> null
                    |}
               tags = this.Tags
            |} :> _
type NetworkInterface =
    { Name : ResourceName
      Location : Location
      IpConfigs :
        {| SubnetName : ResourceName
           PublicIpName : ResourceName |} list
      VirtualNetwork : ResourceName
      Tags: Map<string,string>  }
    interface IArmResource with
        member this.ResourceName = this.Name
        member this.JsonModel =
            {| ``type`` = networkInterfaces.ArmValue
               apiVersion = "2018-11-01"
               name = this.Name.Value
               location = this.Location.ArmValue
               dependsOn = [
                   this.VirtualNetwork.Value
                   for config in this.IpConfigs do
                       config.PublicIpName.Value
               ]
               properties =
                   {| ipConfigurations =
                        this.IpConfigs
                        |> List.mapi(fun index ipConfig ->
                            {| name = sprintf "ipconfig%i" (index + 1)
                               properties =
                                {| privateIPAllocationMethod = "Dynamic"
                                   publicIPAddress = {| id = ArmExpression.resourceId(publicIPAddresses, ipConfig.PublicIpName).Eval() |}
                                   subnet = {| id = ArmExpression.resourceId(subnets, this.VirtualNetwork, ipConfig.SubnetName).Eval() |}
                                |}
                            |})
                   |}
               tags = this.Tags
            |} :> _
type NetworkProfile =
    { Name : ResourceName
      Location : Location
      ContainerNetworkInterfaceConfigurations :
        {| IpConfigs :
            {| SubnetName : ResourceName |} list
        |} list
      VirtualNetwork : ResourceName
      Tags: Map<string,string>  }
    interface IArmResource with
        member this.ResourceName = this.Name
        member this.JsonModel =
            {| ``type`` = networkProfiles.ArmValue
               apiVersion = "2020-04-01"
               name = this.Name.Value
               location = this.Location.ArmValue
               dependsOn = [
                   ArmExpression.resourceId(virtualNetworks, this.VirtualNetwork).Eval()
                ]
               properties =
                   {| containerNetworkInterfaceConfigurations =
                       this.ContainerNetworkInterfaceConfigurations
                       |> List.mapi (fun index containerIfConfig ->
                           {| name = sprintf "eth%i" index
                              properties =
                                {| ipConfigurations =
                                   containerIfConfig.IpConfigs
                                   |> List.mapi (fun index ipConfig ->
                                      {| name = sprintf "ipconfig%i" (index + 1)
                                         properties =
                                            {| subnet =
                                                {| id = ArmExpression.resourceId(subnets, this.VirtualNetwork, ipConfig.SubnetName).Eval() |}
                                            |}
                                      |})
                                |}
                           |}
                       )
                   |}
               tags = this.Tags
            |} :> _
type ExpressRouteCircuit =
    { Name : ResourceName
      Location : Location
      Tier : Tier
      Family : Family
      ServiceProviderName : string
      PeeringLocation : string
      Bandwidth : int<Mbps>
      GlobalReachEnabled : bool
      Peerings :
        {| PeeringType : PeeringType
           AzureASN : int
           PeerASN : int64
           PrimaryPeerAddressPrefix : IPAddressCidr
           SecondaryPeerAddressPrefix : IPAddressCidr
           SharedKey : string option
           VlanId : int
        |} list
      Tags: Map<string,string>  }

    interface IArmResource with
        member this.ResourceName = this.Name
        member this.JsonModel =
            {| ``type`` = expressRouteCircuits.ArmValue
               apiVersion = "2019-02-01"
               name = this.Name.Value
               location = this.Location.ArmValue
               sku =
                {| name = sprintf "%O_%O" this.Tier this.Family
                   tier = string this.Tier
                   family = string this.Family |}
               properties =
                   {| peerings = [
                        for peer in this.Peerings do
                            {| name = peer.PeeringType.Value
                               properties =
                                   {| peeringType = peer.PeeringType.Value
                                      azureASN = peer.AzureASN
                                      peerASN = peer.PeerASN
                                      primaryPeerAddressPrefix = IPAddressCidr.format peer.PrimaryPeerAddressPrefix
                                      secondaryPeerAddressPrefix = IPAddressCidr.format peer.SecondaryPeerAddressPrefix
                                      vlanId = peer.VlanId
                                      sharedKey = peer.SharedKey |}
                            |}
                      ]
                      serviceProviderProperties =
                        {| serviceProviderName = this.ServiceProviderName
                           peeringLocation = this.PeeringLocation
                           bandwidthInMbps = this.Bandwidth |}
                      globalReachEnabled = this.GlobalReachEnabled |}
               tags = this.Tags
            |} :> _
