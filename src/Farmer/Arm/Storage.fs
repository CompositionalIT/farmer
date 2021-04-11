[<AutoOpen>]
module Farmer.Arm.Storage

open System
open Farmer
open Farmer.Storage

let storageAccounts = ResourceType ("Microsoft.Storage/storageAccounts", "2019-06-01")
let containers = ResourceType ("Microsoft.Storage/storageAccounts/blobServices/containers", "2018-03-01-preview")
let fileShares = ResourceType ("Microsoft.Storage/storageAccounts/fileServices/shares", "2019-06-01")
let queues = ResourceType ("Microsoft.Storage/storageAccounts/queueServices/queues", "2019-06-01")
let tables = ResourceType ("Microsoft.Storage/storageAccounts/tableServices/tables", "2019-06-01")
let managementPolicies = ResourceType ("Microsoft.Storage/storageAccounts/managementPolicies", "2019-06-01")
let roleAssignments = ResourceType ("Microsoft.Storage/storageAccounts/providers/roleAssignments", "2018-09-01-preview")

type StorageAccount =
    { Name : StorageAccountName
      Location : Location
      Sku : Sku
      Dependencies : ResourceId list
      EnableHierarchicalNamespace : bool option
      StaticWebsite : {| IndexPage : string; ErrorPage : string option; ContentPath : string |} option
      Tags: Map<string,string>}
    interface IArmResource with
        member this.ResourceId = storageAccounts.resourceId this.Name.ResourceName
        member this.JsonModel =
            {| storageAccounts.Create(this.Name.ResourceName, this.Location, this.Dependencies, this.Tags) with
                sku =
                    {| name =
                        let performanceTier =
                            match this.Sku with
                            | GeneralPurpose (V1 (V1Replication.LRS performanceTier))
                            | GeneralPurpose (V2 (V2Replication.LRS performanceTier, _)) ->
                                performanceTier.ToString()
                            | Files _
                            | BlockBlobs _ ->
                                "Premium"
                            | GeneralPurpose _
                            | Blobs _ ->
                                "Standard"
                        let replicationModel =
                            match this.Sku with
                            | GeneralPurpose (V1 (V1Replication.LRS _)) -> "LRS"
                            | GeneralPurpose (V2 (V2Replication.LRS _, _)) -> "LRS"
                            | GeneralPurpose (V1 replication) -> replication.ToString()
                            | GeneralPurpose (V2 (replication, _)) -> replication.ToString()
                            | Blobs (replication, _) -> replication.ToString()
                            | Files replication -> replication.ToString()
                            | BlockBlobs replication -> replication.ToString()
                        $"{performanceTier}_{replicationModel}"
                    |}
                kind =
                    match this.Sku with
                    | GeneralPurpose (V1 _) -> "Storage"
                    | GeneralPurpose (V2 _) -> "StorageV2"
                    | Blobs _ -> "BlobStorage"
                    | Files _ -> "FileStorage"
                    | BlockBlobs _ -> "BlockBlobStorage"
                properties =
                    {| isHnsEnabled = this.EnableHierarchicalNamespace |> Option.toNullable
                       accessTier =
                        match this.Sku with
                        | Blobs (_, Some tier)
                        | GeneralPurpose (V2 (_, Some tier)) ->
                            match tier with
                            | Hot -> "Hot"
                            | Cool -> "Cool"
                        | _ ->
                            null
                    |}
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
            member this.ResourceId = containers.resourceId (this.StorageAccount/"default"/this.Name.ResourceName)
            member this.JsonModel =
                {| containers.Create(this.StorageAccount/"default"/this.Name.ResourceName, dependsOn = [ storageAccounts.resourceId this.StorageAccount ]) with
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
            member this.ResourceId = fileShares.resourceId (this.StorageAccount/"default"/this.Name.ResourceName)
            member this.JsonModel =
                {| fileShares.Create(this.StorageAccount/"default"/this.Name.ResourceName, dependsOn = [ storageAccounts.resourceId this.StorageAccount ]) with
                    properties = {| shareQuota = this.ShareQuota |> Option.defaultValue 5120<Gb> |}
                |} :> _

module Tables =
    type Table =
        { Name : StorageResourceName
          StorageAccount : ResourceName }
        interface IArmResource with
            member this.ResourceId = tables.resourceId (this.StorageAccount/"default"/this.Name.ResourceName)
            member this.JsonModel =
                tables.Create(this.StorageAccount/"default"/this.Name.ResourceName, dependsOn = [ storageAccounts.resourceId this.StorageAccount ]) :> _

module Queues =
    type Queue =
        { Name : StorageResourceName
          StorageAccount : ResourceName }
        interface IArmResource with
            member this.ResourceId = queues.resourceId (this.StorageAccount/"default"/this.Name.ResourceName)
            member this.JsonModel =
                queues.Create(this.StorageAccount/"default"/this.Name.ResourceName, dependsOn = [ storageAccounts.resourceId this.StorageAccount ]) :> _

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
        member this.ResourceName = this.StorageAccount/"default"
        interface IArmResource with
            member this.ResourceId = managementPolicies.resourceId this.ResourceName
            member this.JsonModel =
                {| managementPolicies.Create(this.ResourceName, dependsOn = [ storageAccounts.resourceId this.StorageAccount ]) with
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