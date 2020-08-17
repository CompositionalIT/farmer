[<AutoOpen>]
module Farmer.Arm.DataLakeStore

open Farmer
open Farmer.CoreTypes
open Farmer.DataLake

let accounts = ResourceType "Microsoft.DataLakeStore/accounts"

type Account =
    { Name : ResourceName
      Location : Location
      EncryptionState : FeatureFlag
      Sku : Sku
      Tags: Map<string,string>  }
    interface IArmResource with
        member this.ResourceName = this.Name
        member this.JsonModel =
            {| name = this.Name.Value
               ``type`` = accounts.ArmValue
               apiVersion = "2016-11-01"
               location = this.Location.ArmValue
               properties =
                {| newTier = this.Sku.ToString()
                   encryptionState = this.EncryptionState.ToString() |}
               tags = this.Tags
            |} :> _