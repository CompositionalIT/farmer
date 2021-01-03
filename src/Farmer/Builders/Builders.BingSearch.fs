[<AutoOpen>]
module Farmer.Builders.BingSearch

open Farmer
open Farmer.Arm.BingSearch
open Farmer.BingSearch

type BingSearch =
    /// Gets an ARM Expression key for any Bing Search instance.
    static member getKey (resourceId: ResourceId) =
        ArmExpression.create(sprintf "listKeys(%s, '%s').key1" resourceId.ArmExpression.Value accounts.ApiVersion, resourceId)
    static member getKey (name: ResourceName) = BingSearch.getKey (accounts.resourceId name)

type BingSearchConfig =
    { Name: ResourceName
      Sku: Sku
      Tags: Map<string,string>
      StatisticsEnabled: bool }
    /// Gets an ARM expression to the key of this Bing Search instance.
    member this.Key = BingSearch.getKey (accounts.resourceId this.Name)
    interface IBuilder with
        member this.ResourceId = accounts.resourceId this.Name
        member this.BuildResources location = [
            { Name = this.Name
              Location = location
              Sku = this.Sku
              Tags = this.Tags
              Properties = {| statisticsEnabled = this.StatisticsEnabled |} }
        ]

type BingSearchBuilder () =
    member _.Yield _ =
        { Name = ResourceName.Empty
          Sku = F1
          Tags = Map.empty
          StatisticsEnabled = false }
    [<CustomOperation "name">]
    member _.Name (state:BingSearchConfig, name) = { state with Name = ResourceName name }
    [<CustomOperation "sku">]
    member _.Sku (state:BingSearchConfig, sku) = { state with Sku = sku }
    [<CustomOperation "enable_statistics">]
    member _.EnableStatistics (state:BingSearchConfig) = { state with StatisticsEnabled = true }
    [<CustomOperation "add_tags">]
    member _.Tags(state:BingSearchConfig, pairs) =
        { state with
            Tags = pairs |> List.fold (fun map (key,value) -> Map.add key value map) state.Tags }
    [<CustomOperation "add_tag">]
    member this.Tag(state:BingSearchConfig, key, value) = this.Tags(state, [ (key,value) ])

let bingSearch = BingSearchBuilder()