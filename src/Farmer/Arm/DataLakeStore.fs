[<AutoOpen>]
module Farmer.Arm.DataLakeStore

open Farmer
open Farmer.CoreTypes
open Farmer.DataLake

type Account =
    { Name : ResourceName
      Location : Location
      EncryptionState : FeatureFlag
      Sku : Sku }
    interface IArmResource with
        member this.ResourceName = this.Name
        member this.JsonModel =
            {| name = this.Name.Value
               ``type`` = "Microsoft.DataLakeStore/accounts"
               apiVersion = "2016-11-01"
               location = this.Location.ArmValue
               properties =
                {| newTier = this.Sku.ToString()
                   encryptionState = this.EncryptionState.ToString() |}
            |} :> _