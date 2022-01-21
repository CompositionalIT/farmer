[<AutoOpen>]
module Farmer.Arm.Bastion

open Farmer
open Farmer.Arm.Network

let bastionHosts = ResourceType ("Microsoft.Network/bastionHosts", "2020-05-01")

type BastionHost =
    { Name : ResourceName
      Location : Location
      VirtualNetwork : ResourceName
      IpConfigs : {| PublicIpName : ResourceName |} list
      Tags : Map<string,string> }
    interface IArmResource with
        member this.ResourceId = bastionHosts.resourceId this.Name
        member this.JsonModel =
            let dependsOn = [
                virtualNetworks.resourceId this.VirtualNetwork
                for config in this.IpConfigs do
                    publicIPAddresses.resourceId config.PublicIpName
            ]
            {| bastionHosts.Create(this.Name, this.Location, dependsOn, this.Tags) with
                properties =
                    {| ipConfigurations =
                           this.IpConfigs
                           |> List.mapi(fun index ipConfig ->
                               {| name = $"ipconfig{index + 1}"
                                  properties =
                                   {| publicIPAddress = {| id = publicIPAddresses.resourceId(ipConfig.PublicIpName).Eval() |}
                                      subnet = {| id = subnets.resourceId(this.VirtualNetwork, ResourceName "AzureBastionSubnet").Eval() |}
                                   |}
                               |})
                    |}
            |}