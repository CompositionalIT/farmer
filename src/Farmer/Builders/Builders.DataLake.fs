[<AutoOpen>]
module Farmer.Builders.DataLake

open Farmer
open Farmer.DataLake
open Farmer.Arm.DataLakeStore

type DataLakeConfig =
    { Name : ResourceName
      EncryptionState : FeatureFlag
      Sku : Sku
      Tags: Map<string,string>  }
    interface IBuilder with
        member this.ResourceId = accounts.resourceId this.Name
        member this.BuildResources location = [
            { Name = this.Name
              Location = location
              EncryptionState = this.EncryptionState
              Sku = this.Sku
              Tags = this.Tags  }
        ]

type DataLakeBuilder() =
    interface ITaggable<DataLakeConfig> with member _.Add state tags = { state with Tags = state.Tags |> Map.merge tags }
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

let dataLake = DataLakeBuilder()