[<AutoOpen>]
module Farmer.Arm.BingSearch

open Farmer

let accounts = ResourceType("Microsoft.Bing/accounts", "2020-06-10")

[<Literal>]
let private kind = "Bing.Search.v7"

type Accounts = {
    Name: ResourceName
    Location: Location
    Sku: BingSearch.Sku
    Tags: Map<string, string>
    Statistics: FeatureFlag
} with

    interface IArmResource with
        member this.ResourceId = accounts.resourceId this.Name

        member this.JsonModel = {|
            accounts.Create(this.Name, this.Location, tags = this.Tags) with
                sku = {| name = string this.Sku |}
                kind = kind
                properties = {|
                    statisticsEnabled = this.Statistics.AsBoolean
                |}
        |}
