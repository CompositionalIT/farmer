[<AutoOpen>]
module Farmer.Arm.Storage

open Farmer
open Farmer.Storage
open Farmer.CoreTypes

let storageAccounts = ResourceType ("Microsoft.Storage/storageAccounts", "2018-07-01")
let containers = ResourceType ("Microsoft.Storage/storageAccounts/blobServices/containers", "2018-03-01-preview")
let fileShares = ResourceType ("Microsoft.Storage/storageAccounts/fileServices/shares", "2019-06-01")
let queues = ResourceType ("Microsoft.Storage/storageAccounts/queueServices/queues", "2019-06-01")
let managementPolicies = ResourceType ("Microsoft.Storage/storageAccounts/managementPolicies", "2019-06-01")

type StorageAccount =
    { Name : StorageAccountName
      Location : Location
      Sku : Sku
      EnableHierarchicalNamespace : bool option
      StaticWebsite : {| IndexPage : string; ErrorPage : string option; ContentPath : string |} option
      Tags: Map<string,string>}
    interface IArmResource with
        member this.ResourceName = this.Name.ResourceName
        member this.JsonModel =
            {| ``type`` = storageAccounts.Path
               apiVersion = storageAccounts.Version
               sku = {| name = this.Sku.ArmValue |}
               kind = "StorageV2"
               name = this.Name.ResourceName.Value
               location = this.Location.ArmValue
               properties =
                match this.EnableHierarchicalNamespace with
                | Some hnsEnabled -> {| isHnsEnabled = hnsEnabled |} :> obj
                | _ -> {||} :> obj
               tags = this.Tags
            |} :> _
    interface IPostDeploy with
        member this.Run _ =
            this.StaticWebsite
            |> Option.map(fun staticWebsite -> result {
                let! enableStaticResponse = Deploy.Az.enableStaticWebsite this.Name.ResourceName.Value staticWebsite.IndexPage staticWebsite.ErrorPage
                printfn "Deploying content of %s folder to $web container for storage account %s" staticWebsite.ContentPath this.Name.ResourceName.Value
                let! uploadResponse = Deploy.Az.batchUploadStaticWebsite this.Name.ResourceName.Value staticWebsite.ContentPath
                return enableStaticResponse + ", " + uploadResponse
            })

module BlobServices =
    type Container =
        { Name : StorageResourceName
          StorageAccount : ResourceName
          Accessibility : StorageContainerAccess }
        interface IArmResource with
            member this.ResourceName = this.Name.ResourceName
            member this.JsonModel =
                {| ``type`` = containers.Path
                   apiVersion = containers.Version
                   name = this.StorageAccount.Value + "/default/" + this.Name.ResourceName.Value
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
        { Name: StorageResourceName
          ShareQuota: int<Gb> option
          StorageAccount: ResourceName }
        interface IArmResource with
            member this.ResourceName = this.Name.ResourceName
            member this.JsonModel =
                {| ``type`` = fileShares.Path
                   apiVersion = fileShares.Version
                   name = this.StorageAccount.Value + "/default/" + this.Name.ResourceName.Value
                   properties = {| shareQuota = this.ShareQuota |> Option.defaultValue 5120<Gb> |}
                   dependsOn = [ this.StorageAccount.Value ]
                |} :> _

module Queues =
    type Queue =
        { Name : StorageResourceName
          StorageAccount : ResourceName }
        interface IArmResource with
            member this.ResourceName = this.Name.ResourceName
            member this.JsonModel =
                {| name = this.StorageAccount.Value + "/default/" + this.Name.ResourceName.Value
                   ``type`` = queues.Path
                   apiVersion = queues.Version
                   dependsOn = [ this.StorageAccount.Value ]
                |} :> _

module ManagementPolicies =
    type ManagementPolicy =
        { Rules :
            {| Name : ResourceName
               CoolBlobAfter : int<Days> option
               ArchiveBlobAfter : int<Days> option
               DeleteBlobAfter : int<Days> option
               DeleteSnapshotAfter : int<Days> option
               Filters : string list |} list
          StorageAccount : ResourceName }
        member this.ResourceName = this.StorageAccount.Value + "/default" |> ResourceName
        interface IArmResource with
            member this.ResourceName = this.ResourceName
            member this.JsonModel =
                {| name = this.ResourceName.Value
                   ``type`` = managementPolicies.Path
                   apiVersion = managementPolicies.Version
                   dependsOn = [ this.StorageAccount.Value ]
                   properties =
                    {| policy =
                        {| rules = [
                            for rule in this.Rules do
                                {| enabled = true
                                   name = rule.Name.Value
                                   ``type`` = "Lifecycle"
                                   definition =
                                    {| actions =
                                        {| baseBlob =
                                            {| tierToCool = rule.CoolBlobAfter |> Option.map (fun days -> {| daysAfterModificationGreaterThan = days |} |> box) |> Option.toObj
                                               tierToArchive = rule.ArchiveBlobAfter |> Option.map (fun days -> {| daysAfterModificationGreaterThan = days |} |> box) |> Option.toObj
                                               delete = rule.DeleteBlobAfter |> Option.map (fun days -> {| daysAfterModificationGreaterThan = days |} |> box) |> Option.toObj |}
                                           snapshot =
                                            rule.DeleteSnapshotAfter
                                            |> Option.map (fun days -> {| delete = {| daysAfterCreationGreaterThan = days |} |} |> box)
                                            |> Option.toObj
                                        |}
                                       filters =
                                        {| blobTypes = [ "blockBlob" ]
                                           prefixMatch = rule.Filters |}
                                    |}
                                |}
                            ]
                        |}
                    |}
                |} :> _