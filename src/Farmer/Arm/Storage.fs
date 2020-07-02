[<AutoOpen>]
module Farmer.Arm.Storage

open Farmer
open Farmer.Storage
open Farmer.CoreTypes

let storageAccounts = ResourceType "Microsoft.Storage/storageAccounts"
let containers = ResourceType "Microsoft.Storage/storageAccounts/blobServices/containers"
let fileShares = ResourceType "Microsoft.Storage/storageAccounts/fileServices/shares"

type StorageAccount =
    { Name : ResourceName
      Location : Location
      Sku : Sku }
    interface IArmResource with
        member this.ResourceName = this.Name
        member this.JsonModel =
            {| ``type`` = storageAccounts.ArmValue
               sku = {| name = this.Sku.ArmValue |}
               kind = "StorageV2"
               name = this.Name.Value
               apiVersion = "2018-07-01"
               location = this.Location.ArmValue
            |} :> _

module BlobServices =
    type Container =
        { Name : ResourceName
          StorageAccount : ResourceName
          Accessibility : StorageContainerAccess }
        interface IArmResource with
            member this.ResourceName = this.Name
            member this.JsonModel =
                {| ``type`` = containers.ArmValue
                   apiVersion = "2018-03-01-preview"
                   name = this.StorageAccount.Value + "/default/" + this.Name.Value
                   dependsOn = [ this.StorageAccount.Value ]
                   properties =
                    {| publicAccess =
                        match this.Accessibility with
                        | Private -> "None"
                        | Container -> "Container"
                        | Blob -> "Blob" |}
                |} :> _

module FileShares =
  type FileShare = 
      { Name: ResourceName
        StorageAccount: ResourceName }
      interface IArmResource with
        member this.ResourceName = this.Name
        member this.JsonModel =
            {| ``type`` = fileShares.ArmValue
               apiVersion = "2019-06-01"
               name = this.StorageAccount.Value + "/default/" + this.Name.Value
               dependsOn = [ this.StorageAccount.Value ]
            |} :> _