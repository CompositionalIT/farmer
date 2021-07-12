[<AutoOpen>]
module Farmer.Arm.AzureFirewall

open System.Net
open Farmer
open Farmer.AzureFirewall

// Further information on properties and examples: https://docs.microsoft.com/en-us/azure/templates/microsoft.network/azurefirewalls
let azureFirewalls = ResourceType ("Microsoft.Network/azureFirewalls", "2020-07-01")

// https://docs.microsoft.com/en-us/azure/templates/microsoft.network/firewallpolicies
let azureFirewallPolicies = ResourceType ("Microsoft.Network/firewallPolicies", "2020-07-01")

type HubPublicIPAddresses =
    { Count : int
      Addresses : IPAddress list } with
    member this.JsonModel =
        {| count = this.Count
           addresses = this.Addresses |> List.map (fun x -> {| address = x.ToString() |})
        |}

type HubIPAddresses =
    { PublicIPAddresses : HubPublicIPAddresses option } with
    member this.JsonModel =
        {| publicIPs = this.PublicIPAddresses |>  Option.map (fun x -> box x.JsonModel) |> Option.defaultValue null
        |}

type Sku =
    { Name : SkuName
      Tier : SkuTier } with
    member this.JsonModel =
        {| name = this.Name.ArmValue
           tier = this.Tier.ArmValue
        |}

type AzureFirewall =
    { Name : ResourceName
      Location : Location
      Dependencies : ResourceId Set
      FirewallPolicy : ResourceId option
      VirtualHub : ResourceId option
      HubIPAddresses : HubIPAddresses option
      Sku : Sku }
    interface IArmResource with
        member this.ResourceId = azureFirewalls.resourceId this.Name
        member this.JsonModel =
            {| azureFirewalls.Create(this.Name, this.Location, this.Dependencies) with
                properties =
                  {| sku = this.Sku.JsonModel
                     virtualHub = this.VirtualHub |>  Option.map (fun resId -> box {| id = resId.ArmExpression.Eval() |}) |> Option.defaultValue null
                     firewallPolicy = this.FirewallPolicy |>  Option.map (fun resId -> box {| id = resId.ArmExpression.Eval() |}) |> Option.defaultValue null
                     hubIPAddresses = this.HubIPAddresses |> Option.map (fun x -> box x.JsonModel) |> Option.defaultValue null |}
            |} :> _

