[<AutoOpen>]
module Farmer.Arm.Storage

open Farmer
open Farmer.Storage
open Farmer.CoreTypes
open System

let storageAccounts = ResourceType "Microsoft.Storage/storageAccounts"
let containers = ResourceType "Microsoft.Storage/storageAccounts/blobServices/containers"
let fileShares = ResourceType "Microsoft.Storage/storageAccounts/fileServices/shares"
let queues = ResourceType "Microsoft.Storage/storageAccounts/queueServices/queues"
let managementPolicies = ResourceType "Microsoft.Storage/storageAccounts/managementPolicies"

type StorageAccount =
    { Name : ResourceName<StorageAccountName>
      Location : Location
      Sku : Sku
      EnableHierarchicalNamespace : bool
      StaticWebsite : {| IndexPage : string; ErrorPage : string option; ContentPath : string |} option
      Tags: Map<string,string>}
    interface IArmResource with
        member this.ResourceName = this.Name.Untyped
        member this.JsonModel =
            {| ``type`` = storageAccounts.ArmValue
               sku = {| name = this.Sku.ArmValue |}
               kind = "StorageV2"
               name = this.Name.Value
               apiVersion = "2018-07-01"
               location = this.Location.ArmValue
               properties = {| isHnsEnabled = this.EnableHierarchicalNamespace |}
               tags = this.Tags
            |} :> _
    interface IPostDeploy with
        member this.Run _ =
            this.StaticWebsite
            |> Option.map(fun staticWebsite -> result {
                let! enableStaticResponse = Deploy.Az.enableStaticWebsite this.Name.Value staticWebsite.IndexPage staticWebsite.ErrorPage
                printfn "Deploying content of %s folder to $web container for storage account %s" staticWebsite.ContentPath this.Name.Value
                let! uploadResponse = Deploy.Az.batchUploadStaticWebsite this.Name.Value staticWebsite.ContentPath
                return enableStaticResponse + ", " + uploadResponse
            })

module BlobServices =
    type Container =
        { Name : ResourceName<ContainerName>
          StorageAccount : ResourceName<StorageAccountName>
          Accessibility : StorageContainerAccess }
        interface IArmResource with
            member this.ResourceName = this.Name.Untyped
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
        { Name: ResourceName<FileShareName>
          ShareQuota: int<Gb> option
          StorageAccount: ResourceName<StorageAccountName> }
        interface IArmResource with
            member this.ResourceName = this.Name.Untyped
            member this.JsonModel =
                {| ``type`` = fileShares.ArmValue
                   apiVersion = "2019-06-01"
                   name = this.StorageAccount.Value + "/default/" + this.Name.Value
                   properties = {| shareQuota = this.ShareQuota |> Option.defaultValue 5120<Gb> |}
                   dependsOn = [ this.StorageAccount.Value ]
                |} :> _

module Queues =
    type Queue =
        { Name : ResourceName<QueueName>
          StorageAccount : ResourceName<StorageAccountName> }
        interface IArmResource with
            member this.ResourceName = this.Name.Untyped
            member this.JsonModel =
                {| name = this.StorageAccount.Value + "/default/" + this.Name.Value
                   ``type`` = queues.ArmValue
                   dependsOn = [ this.StorageAccount.Value ]
                   apiVersion = "2019-06-01"
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
          StorageAccount : ResourceName<StorageAccountName> }
        member this.ResourceName = this.StorageAccount.Value + "/default" |> ResourceName
        interface IArmResource with
            member this.ResourceName = this.ResourceName
            member this.JsonModel =
                {| name = this.ResourceName.Value
                   ``type`` = managementPolicies.ArmValue
                   apiVersion = "2019-06-01"
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