[<AutoOpen>]
module Farmer.Builders.DataLake

open Farmer
open Farmer.CoreTypes
open Farmer.DataLake
open Farmer.Arm.DataLakeStore

type DataLakeConfig =
    { Name : ResourceName
      EncryptionState : FeatureFlag
      Sku : Sku }
    interface IBuilder with
        member this.DependencyName = this.Name
        member this.BuildResources location _ = [
            { Name = this.Name
              Location = location
              EncryptionState = this.EncryptionState
              Sku = this.Sku }
        ]

type DataLakeBuilder() =
    member __.Yield _ =
        { Name = ResourceName ""
          EncryptionState = Disabled
          Sku = Sku.Consumption }

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