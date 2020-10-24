[<AutoOpen>]
module Farmer.Arm.DataLakeStore

open Farmer
open Farmer.CoreTypes
open Farmer.DataLake

let accounts = ResourceType ("Microsoft.DataLakeStore/accounts", "2016-11-01")

type Account =
    { Name : ResourceName
      Location : Location
      EncryptionState : FeatureFlag
      Sku : Sku
      Tags: Map<string,string>  }
    interface IArmResource with
        member this.ResourceName = this.Name
        member this.JsonModel =
            {| accounts.Create(this.Name, this.Location, tags = this.Tags) with
                 properties =
                  {| newTier = this.Sku.ToString()
                     encryptionState = this.EncryptionState.ToString() |}
            |} :> _