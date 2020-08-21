[<AutoOpen>]
module Farmer.Builders.DataLake

open Farmer
open Farmer.CoreTypes
open Farmer.DataLake
open Farmer.Arm.DataLakeStore

type DataLakeConfig =
    { Name : ResourceName
      EncryptionState : FeatureFlag
      Sku : Sku
      Tags: Map<string,string>  }
    interface IBuilder with
        member this.DependencyName = this.Name
        member this.BuildResources location = [
            { Name = this.Name
              Location = location
              EncryptionState = this.EncryptionState
              Sku = this.Sku
              Tags = this.Tags  }
        ]

type DataLakeBuilder() =
    member __.Yield _ =
        { Name = ResourceName ""
          EncryptionState = Disabled
          Sku = Sku.Consumption
          Tags = Map.empty}

    /// Sets the name of the data lake.
    [<CustomOperation "name">]
    member __.Name (state:DataLakeConfig, name) =
        { state with Name = ResourceName name }
    [<CustomOperation "enable_encryption">]
    member _.EncryptionState (state:DataLakeConfig) =
        { state with EncryptionState = Enabled }
    [<CustomOperation "sku">]
    member _.Sku (state:DataLakeConfig, sku) =
        { state with Sku = sku }
    [<CustomOperation "add_tags">]
    member _.Tags(state:DataLakeConfig, pairs) = 
        { state with 
            Tags = pairs |> List.fold (fun map (key,value) -> Map.add key value map) state.Tags }
    [<CustomOperation "add_tag">]
    member this.Tag(state:DataLakeConfig, key, value) = this.Tags(state, [ (key,value) ])

let dataLake = DataLakeBuilder()