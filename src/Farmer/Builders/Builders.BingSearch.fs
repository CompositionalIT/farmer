[<AutoOpen>]
module Farmer.Builders.BingSearch

open Farmer
open Farmer.Arm.BingSearch
open Farmer.BingSearch

type BingSearch =
    /// Gets an ARM Expression key for any Bing Search instance.
    static member getKey(resourceId: ResourceId) =
        ArmExpression.create ($"listKeys({resourceId.ArmExpression.Value}, '{accounts.ApiVersion}').key1", resourceId)

    static member getKey(name: ResourceName) =
        BingSearch.getKey (accounts.resourceId name)

type BingSearchConfig =
    {
        Name: ResourceName
        Sku: Sku
        Tags: Map<string, string>
        Statistics: FeatureFlag
    }

    /// Gets an ARM expression to the key of this Bing Search instance.
    member this.Key = BingSearch.getKey (accounts.resourceId this.Name)

    interface IBuilder with
        member this.ResourceId = accounts.resourceId this.Name

        member this.BuildResources location =
            [
                {
                    Name = this.Name
                    Location = location
                    Sku = this.Sku
                    Tags = this.Tags
                    Statistics = this.Statistics
                }
            ]

type BingSearchBuilder() =
    member _.Yield _ =
        {
            Name = ResourceName.Empty
            Sku = F1
            Tags = Map.empty
            Statistics = FeatureFlag.Disabled
        }

    [<CustomOperation "name">]
    member _.Name(state: BingSearchConfig, name) = { state with Name = ResourceName name }

    [<CustomOperation "sku">]
    member _.Sku(state: BingSearchConfig, sku) = { state with Sku = sku }

    [<CustomOperation "statistics">]
    member _.EnableStatistics(state: BingSearchConfig, value) = { state with Statistics = value }

    interface ITaggable<BingSearchConfig> with
        member _.Add state tags =
            { state with
                Tags = state.Tags |> Map.merge tags
            }

let bingSearch = BingSearchBuilder()
