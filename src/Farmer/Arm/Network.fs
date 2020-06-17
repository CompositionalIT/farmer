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
      Subnets : {| Name : ResourceName; Prefix : string |} list }
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
                              properties = {| addressPrefix = subnet.Prefix |}
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
      EnableBgp : bool }
    interface IArmResource with
        member this.ResourceName = this.Name
        member this.JsonModel =
            {| ``type`` = "Microsoft.Network/virtualNetworkGateways"
               apiVersion = "2020-05-01"
               name = this.Name.Value
               location = this.Location.ArmValue
               dependsOn = [
                   sprintf "[resourceId('Microsoft.Network/virtualNetworks/subnets', '%s')]" this.VirtualNetwork.Value
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
                           | GatewayType.Vpn (sku, _, _) -> {| name = sku.ArmValue; tier = sku.ArmValue |}
                       gatewayType = this.GatewayType.ArmValue
                       vpnType = this.GatewayType.VpnType
                       enableBgp = this.EnableBgp
                       activeActive = this.GatewayType.ActiveActive
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
    static member FormatCidr address prefix = sprintf "%O/%d" address prefix

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
                                      primaryPeerAddressPrefix = ExpressRouteCircuit.FormatCidr peer.PrimaryPeerAddressPrefix.Address peer.PrimaryPeerAddressPrefix.Prefix
                                      secondaryPeerAddressPrefix = ExpressRouteCircuit.FormatCidr peer.SecondaryPeerAddressPrefix.Address peer.SecondaryPeerAddressPrefix.Prefix
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
