[<AutoOpen>]
module Farmer.Arm.Bastion

open Farmer
open Farmer.CoreTypes
open Farmer.Arm.Network

let bastionHosts = ResourceType ("Microsoft.Network/bastionHosts", "2020-05-01")

type BastionHost =
    { Name : ResourceName
      Location : Location
      VirtualNetwork : ResourceName
      IpConfigs : {| PublicIpName : ResourceName |} list
      Tags : Map<string,string> }
    interface IArmResource with
        member this.ResourceName = this.Name
        member this.JsonModel =
            let dependsOn = [
                ResourceId.create(virtualNetworks, this.VirtualNetwork)
                for config in this.IpConfigs do
                    ResourceId.create (publicIPAddresses, config.PublicIpName)
            ]
            {| bastionHosts.Create(this.Name, this.Location, dependsOn, this.Tags) with
                properties =
                    {| ipConfigurations =
                           this.IpConfigs
                           |> List.mapi(fun index ipConfig ->
                               {| name = sprintf "ipconfig%i" (index + 1)
                                  properties =
                                   {| publicIPAddress = {| id = ResourceId.create(publicIPAddresses, ipConfig.PublicIpName).Eval() |}
                                      subnet = {| id = ResourceId.create(subnets, this.VirtualNetwork, ResourceName "AzureBastionSubnet").Eval() |}
                                   |}
                               |})
                    |}
            |} :> _