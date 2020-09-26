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
                ArmExpression.resourceId(virtualNetworks, this.VirtualNetwork).Eval() |> ResourceName
                for config in this.IpConfigs do
                    ArmExpression.resourceId(publicIPAddresses, config.PublicIpName).Eval() |> ResourceName
            ]
            {| bastionHosts.Create(this.Name, this.Location, dependsOn, this.Tags) with
                properties =
                    {| ipConfigurations =
                           this.IpConfigs
                           |> List.mapi(fun index ipConfig ->
                               {| name = sprintf "ipconfig%i" (index + 1)
                                  properties =
                                   {| publicIPAddress = {| id = ArmExpression.resourceId(publicIPAddresses, ipConfig.PublicIpName).Eval() |}
                                      subnet = {| id = ArmExpression.resourceId(subnets, this.VirtualNetwork, ResourceName "AzureBastionSubnet").Eval() |}
                                   |}
                               |})
                    |}
            |} :> _