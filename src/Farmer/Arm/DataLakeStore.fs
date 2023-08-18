[<AutoOpen>]
module Farmer.Arm.DataLakeStore

open Farmer
open Farmer.DataLake

let accounts = ResourceType("Microsoft.DataLakeStore/accounts", "2016-11-01")

type Account = {
    Name: ResourceName
    Location: Location
    EncryptionState: FeatureFlag
    Sku: Sku
    Tags: Map<string, string>
} with

    interface IArmResource with
        member this.ResourceId = accounts.resourceId this.Name

        member this.JsonModel = {|
            accounts.Create(this.Name, this.Location, tags = this.Tags) with
                properties = {|
                    newTier = this.Sku.ToString()
                    encryptionState = this.EncryptionState.ToString()
                |}
        |}
