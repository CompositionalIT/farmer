[<AutoOpen>]
module Farmer.Arm.ContainerRegistry

open Farmer
open Farmer.ContainerRegistry

let registries =
    ResourceType("Microsoft.ContainerRegistry/registries", "2023-07-01")

type Registries = {
    Name: ResourceName
    Location: Location
    Sku: Sku
    AdminUserEnabled: bool
    PublicNetworkAccess: ContainerRegistry.PublicNetworkAccess option
    NetworkRuleSet: ContainerRegistry.NetworkRuleSet option
    Tags: Map<string, string>
} with

    interface IArmResource with
        member this.ResourceId = registries.resourceId this.Name

        member this.JsonModel =
            let properties = {|
                adminUserEnabled = this.AdminUserEnabled
                publicNetworkAccess =
                    this.PublicNetworkAccess
                    |> Option.map (function
                        | ContainerRegistry.PublicNetworkAccess.Enabled -> "Enabled" :> obj
                        | ContainerRegistry.PublicNetworkAccess.Disabled -> "Disabled" :> obj)
                    |> Option.toObj
                networkRuleSet =
                    this.NetworkRuleSet
                    |> Option.map (fun nrs ->
                        {|
                            defaultAction =
                                match nrs.DefaultAction with
                                | ContainerRegistry.NetworkRuleAction.Allow -> "Allow"
                                | ContainerRegistry.NetworkRuleAction.Deny -> "Deny"
                            ipRules = nrs.IpRules |> List.map (fun ip -> {| value = ip.Value; action = "Allow" |})
                        |}
                        :> obj)
                    |> Option.toObj
            |}

            {|
                registries.Create(this.Name, this.Location, tags = this.Tags) with
                    sku = {| name = this.Sku.ToString() |}
                    properties = properties
            |}