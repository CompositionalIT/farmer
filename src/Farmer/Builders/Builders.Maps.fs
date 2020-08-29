[<AutoOpen>]
module Farmer.Builders.Maps

open Farmer
open Farmer.Maps
open Farmer.Helpers
open Farmer.Arm.Maps

type MapsConfig =
    { Name : ResourceName
      Sku : Sku 
      Tags: Map<string,string> }
    interface IBuilder with
        member this.DependencyName = this.Name
        member this.BuildResources _ = [
            { Name = this.Name
              Location = Location "global"
              Sku = this.Sku
              Tags = this.Tags  }
        ]

type MapsBuilder() =
    member _.Yield _ =
        { Name = ResourceName.Empty
          Sku = S0
          Tags = Map.empty  }
    member _.Run(state:MapsConfig) =
        { state with Name = state.Name |> sanitiseMaps |> ResourceName }
    /// Sets the name of the Azure Maps instance.
    [<CustomOperation("name")>]
    member _.Name(state:MapsConfig, name) = { state with Name = name }
    member this.Name(state:MapsConfig, name) = this.Name(state, ResourceName name)
    /// Sets the SKU of the Azure Maps instance.
    [<CustomOperation("sku")>]
    member _.Sku(state:MapsConfig, sku) = { state with Sku = sku }    
    [<CustomOperation "add_tags">]
    member _.Tags(state:MapsConfig, pairs) = 
        { state with 
            Tags = pairs |> List.fold (fun map (key,value) -> Map.add key value map) state.Tags }
    [<CustomOperation "add_tag">]
    member this.Tag(state:MapsConfig, key, value) = this.Tags(state, [ (key,value) ])
let maps = MapsBuilder()
