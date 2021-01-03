[<AutoOpen>]
module Farmer.Arm.BingSearch

open Farmer

let accounts = ResourceType ("Microsoft.Bing/accounts", "2020-06-10")

type Accounts =
    { Name: ResourceName
      Location: Location
      Sku: BingSearch.Sku
      Kind: BingSearch.Kind
      Tags: Map<string,string>
      Properties: {| StatisticsEnabled: bool |} }
    interface IArmResource with
        member this.ResourceId = accounts.resourceId this.Name
        member this.JsonModel =
            {| accounts.Create(this.Name, this.Location, tags = this.Tags) with
                sku = {| name = string this.Sku |}
                kind = this.Kind.ToString().Replace("_", ".")
                properties = this.Properties
            |} :> _
