[<AutoOpen>]
module Farmer.Arm.Network

open Farmer
open Farmer.CoreTypes
open Farmer.ExpressRoute
open Farmer.VirtualNetworkGateway
open System.Net

type PublicIpAddress =
    { Name : ResourceName
      Location : Location
      DomainNameLabel : string option }
    interface IArmResource with
        member this.ResourceName = this.Name
        member this.JsonModel =
            {| ``type`` = "Microsoft.Network/publicIPAddresses"
               apiVersion = "2018-11-01"
               name = this.Name.Value
               location = this.Location.ArmValue
               properties =
                  {| publicIPAllocationMethod = "Dynamic"
                     dnsSettings =
                        match this.DomainNameLabel with
                        | Some label -> box {| domainNameLabel = label.ToLower() |}
                        | None -> null |}
            |} :> _

type VirtualNetwork =
    { Name : ResourceName
      Location : Location
      AddressSpacePrefixes : string list
      Subnets : {| Name : ResourceName; Prefix : string; Delegations: {| Name: ResourceName; ServiceName: string |} list |} list; }
    interface IArmResource with
        member this.ResourceName = this.Name
        member this.JsonModel =
            {| ``type`` = "Microsoft.Network/virtualNetworks"
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
      EnableBgp : bool }
    interface IArmResource with
        member this.ResourceName = this.Name
        member this.JsonModel =
            {| ``type`` = "Microsoft.Network/virtualNetworkGateways"
               apiVersion = "2020-05-01"
               name = this.Name.Value
               location = this.Location.ArmValue
               dependsOn = [
                   sprintf "[resourceId('Microsoft.Network/virtualNetworks', '%s')]" this.VirtualNetwork.Value
                   for config in this.IpConfigs do
                       sprintf "[resourceId('Microsoft.Network/publicIPAddresses','%s')]" config.PublicIpName.Value 
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
                                   publicIPAddress = {| id = sprintf "[resourceId('Microsoft.Network/publicIPAddresses','%s')]" ipConfig.PublicIpName.Value |}
                                   subnet = {| id = sprintf "[resourceId('Microsoft.Network/virtualNetworks/subnets', '%s', 'GatewaySubnet')]" this.VirtualNetwork.Value |} |}
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
            |} :> _
type Connection =
    { Name : ResourceName
      Location : Location
      ConnectionType : ConnectionType
      VirtualNetworkGateway1 : ResourceName
      VirtualNetworkGateway2 : ResourceName option
      LocalNetworkGateway : ResourceName option
      PeerId : string option
      AuthorizationKey : string option }
    interface IArmResource with
        member this.ResourceName = this.Name
        member this.JsonModel =
            {| ``type`` = "Microsoft.Network/connections"
               apiVersion = "2020-04-01"
               name = this.Name.Value
               location = this.Location.ArmValue
               dependsOn = [
                   sprintf "[resourceId('Microsoft.Network/virtualNetworksGateways', '%s')]" this.VirtualNetworkGateway1.Value
                   if this.VirtualNetworkGateway2.IsSome then
                       sprintf "[resourceId('Microsoft.Network/virtualNetworksGateways', '%s')]" this.VirtualNetworkGateway2.Value.Value
                   if this.LocalNetworkGateway.IsSome then
                       sprintf "[resourceId('Microsoft.Network/localNetworksGateways', '%s')]" this.LocalNetworkGateway.Value.Value
               ]
               properties =
                    {| authorizationKey =
                           match this.AuthorizationKey with
                           | Some key -> key
                           | None -> Unchecked.defaultof<_>
                       connectionType = this.ConnectionType.ArmValue
                       virtualNetworkGateway1 = {| id = sprintf "[resourceId('Microsoft.Network/virtualNetworksGateways', '%s')]" this.VirtualNetworkGateway1.Value |}
                       virtualNetworkGateway2 =
                           match this.VirtualNetworkGateway2 with
                           | Some vng2 -> {| id = sprintf "[resourceId('Microsoft.Network/virtualNetworksGateways', '%s')]" vng2.Value |}
                           | None -> Unchecked.defaultof<_>
                       localNetworkGateway1 =
                           match this.LocalNetworkGateway with
                           | Some lng -> {| id = sprintf "[resourceId('Microsoft.Network/localNetworksGateways', '%s')]" lng.Value |}
                           | None -> Unchecked.defaultof<_>
                       peer =
                           match this.PeerId with
                           | Some peerId -> {| id = peerId |}
                           | None -> Unchecked.defaultof<_>
                    |}
            |} :> _
type NetworkInterface =
    { Name : ResourceName
      Location : Location
      IpConfigs :
        {| SubnetName : ResourceName
           PublicIpName : ResourceName |} list
      VirtualNetwork : ResourceName }
    interface IArmResource with
        member this.ResourceName = this.Name
        member this.JsonModel =
            {| ``type`` = "Microsoft.Network/networkInterfaces"
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
                                   publicIPAddress = {| id = sprintf "[resourceId('Microsoft.Network/publicIPAddresses','%s')]" ipConfig.PublicIpName.Value |}
                                   subnet = {| id = sprintf "[resourceId('Microsoft.Network/virtualNetworks/subnets', '%s', '%s')]" this.VirtualNetwork.Value ipConfig.SubnetName.Value |}
                                |}
                            |})
                   |}
            |} :> _
type NetworkProfile =
    { Name : ResourceName
      Location : Location
      ContainerNetworkInterfaceConfigurations :
        {| IpConfigs :
            {| SubnetName : ResourceName |} list
        |} list
      VirtualNetwork : ResourceName }
    interface IArmResource with
        member this.ResourceName = this.Name
        member this.JsonModel =
            {| ``type`` = "Microsoft.Network/networkProfiles"
               apiVersion = "2020-04-01"
               name = this.Name.Value
               location = this.Location.ArmValue
               dependsOn = [ sprintf "[resourceId('Microsoft.Network/virtualNetworks','%s')]" this.VirtualNetwork.Value ]
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
                                            {| subnet = {| id = sprintf "[resourceId('Microsoft.Network/virtualNetworks/subnets', '%s', '%s')]" this.VirtualNetwork.Value ipConfig.SubnetName.Value |} |}
                                      |})
                                |}
                           |}
                       )
                   |}
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
           PrimaryPeerAddressPrefix : {| Address : IPAddress; Prefix : int |}
           SecondaryPeerAddressPrefix : {| Address : IPAddress; Prefix : int |}
           SharedKey : string option
           VlanId : int
        |} list }

    interface IArmResource with
        member this.ResourceName = this.Name
        member this.JsonModel =
            {| ``type`` = "Microsoft.Network/expressRouteCircuits"
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
            |} :> _
