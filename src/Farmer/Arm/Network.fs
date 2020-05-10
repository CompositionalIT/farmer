[<AutoOpen>]
module Farmer.Arm.Network

open Farmer
open System

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
                  match this.DomainNameLabel with
                  | Some label ->
                      box
                          {| publicIPAllocationMethod = "Dynamic"
                             dnsSettings = {| domainNameLabel = label.ToLower() |}
                          |}
                  | None ->
                      box {| publicIPAllocationMethod = "Dynamic" |}
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
      Tier : string
      Family : string
      ServiceProviderName : string
      PeeringLocation : string
      Bandwidth : int
      GlobalReachEnabled : bool
      Peerings :
        {| PeeringType : string
           AzureASN : int
           PeerASN : int64
           PrimaryPeerAddressPrefix : string
           SecondaryPeerAddressPrefix : string
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
               sku = {| name = String.Format("{0}_{1}", this.Tier, this.Family); tier = this.Tier; family = this.Family |}
               properties =
                   {| peerings = [
                        for peer in this.Peerings do
                            {| name = peer.PeeringType
                               properties =
                                   {| peeringType = peer.PeeringType
                                      azureASN = peer.AzureASN
                                      peerASN = peer.PeerASN
                                      primaryPeerAddressPrefix = peer.PrimaryPeerAddressPrefix
                                      secondaryPeerAddressPrefix = peer.SecondaryPeerAddressPrefix
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
