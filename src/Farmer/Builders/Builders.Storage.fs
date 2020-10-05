[<AutoOpen>]
module Farmer.Builders.Storage

open Farmer
open Farmer.CoreTypes
open Farmer.Storage
open Farmer.Arm.Storage
open BlobServices
open FileShares

type StorageAccount =
    /// Gets an ARM Expression connection string for any Storage Account.
    static member getConnectionString (storageAccount:ResourceId) =
        let storageAccount = storageAccount.WithType storageAccounts
        let expr =
            sprintf
                "concat('DefaultEndpointsProtocol=https;AccountName=%s;AccountKey=', listKeys(%s, '2017-10-01').keys[0].value)"
                storageAccount.Name.Value
                storageAccount.ArmExpression.Value
        ArmExpression.create(expr, storageAccount)
    /// Gets an ARM Expression connection string for any Storage Account.
    static member getConnectionString (storageAccountName:StorageAccountName, ?group) =
        StorageAccount.getConnectionString(ResourceId.create(storageAccountName.ResourceName, ?group = group))

type StoragePolicy =
    { CoolBlobAfter : int<Days> option
      ArchiveBlobAfter : int<Days> option
      DeleteBlobAfter : int<Days> option
      DeleteSnapshotAfter : int<Days> option
      Filters : string list }

type StorageAccountConfig =
    { /// The name of the storage account.
      Name : StorageAccountName
      /// The sku of the storage account.
      Sku : Sku
      /// Whether to enable Data Lake Storage Gen2.
      EnableDataLake : bool option
      /// Containers for the storage account.
      Containers : (StorageResourceName * StorageContainerAccess) list
      /// File shares
      FileShares: (StorageResourceName * int<Gb> option) list
      /// Queues
      Queues : StorageResourceName Set
      /// Rules
      Rules : Map<ResourceName, StoragePolicy>
      /// Static Website Settings
      StaticWebsite : {| IndexPage : string; ContentPath : string; ErrorPage : string option |} option
      /// Tags to apply to the storage account
      Tags: Map<string,string> }
    /// Gets the ARM expression path to the key of this storage account.
    member this.Key = StorageAccount.getConnectionString this.Name
    /// Gets the Primary endpoint for static website (if enabled)
    member this.WebsitePrimaryEndpoint = sprintf "https://%s.z6.web.core.windows.net" this.Name.ResourceName.Value
    member this.Endpoint = sprintf "%s.blob.core.windows.net" this.Name.ResourceName.Value
    interface IBuilder with
        member this.DependencyName = this.Name.ResourceName
        member this.BuildResources location = [
            { Name = this.Name
              Location = location
              Sku = this.Sku
              EnableHierarchicalNamespace = this.EnableDataLake
              StaticWebsite = this.StaticWebsite
              Tags = this.Tags }
            for name, access in this.Containers do
                { Name = name
                  StorageAccount = this.Name.ResourceName
                  Accessibility = access }
            for (name, shareQuota) in this.FileShares do
                { Name = name
                  ShareQuota = shareQuota
                  StorageAccount = this.Name.ResourceName }
            for queue in this.Queues do
                { Queues.Queue.Name = queue
                  Queues.Queue.StorageAccount = this.Name.ResourceName }
            match this.Rules |> Map.toList with
            | [] ->
                ()
            | rules ->
                { ManagementPolicies.ManagementPolicy.StorageAccount = this.Name.ResourceName
                  ManagementPolicies.ManagementPolicy.Rules = [
                      for name, rule in rules do
                        {| rule with Name = name |}
                  ]
                }
        ]

type StorageAccountBuilder() =
    member _.Yield _ = {
        Name = StorageAccountName.Create("default").OkValue
        Sku = Standard_LRS
        EnableDataLake = None
        Containers = []
        FileShares = []
        Rules = Map.empty
        Queues = Set.empty
        StaticWebsite = None
        Tags = Map.empty
    }
    static member private AddContainer(state, access, name:string) = { state with Containers = state.Containers @ [ ((StorageResourceName.Create name).OkValue, access) ] }
    static member private AddFileShare(state:StorageAccountConfig, name:string, quota) = { state with FileShares = state.FileShares @ [ (StorageResourceName.Create(name).OkValue, quota) ] }

    /// Sets the name of the storage account.
    [<CustomOperation "name">]
    member _.Name(state:StorageAccountConfig, name:ResourceName) = { state with Name = StorageAccountName.Create(name).OkValue }
    member this.Name(state:StorageAccountConfig, name) = this.Name(state, ResourceName name)
    /// Sets the sku of the storage account.
    [<CustomOperation "sku">]
    member _.Sku(state:StorageAccountConfig, sku) = { state with Sku = sku }
    /// Adds private container.
    [<CustomOperation "add_private_container">]
    member _.AddPrivateContainer(state:StorageAccountConfig, name) = StorageAccountBuilder.AddContainer(state, Private, name)
    /// Adds container with anonymous read access for blobs and containers.
    [<CustomOperation "add_public_container">]
    member _.AddPublicContainer(state:StorageAccountConfig, name) =  StorageAccountBuilder.AddContainer(state, Container, name)
    /// Adds container with anonymous read access for blobs only.
    [<CustomOperation "add_blob_container">]
    member _.AddBlobContainer(state:StorageAccountConfig, name) = StorageAccountBuilder.AddContainer(state, Blob, name)
    /// Adds a file share with no quota.
    [<CustomOperation "add_file_share">]
    member _.AddFileShare(state:StorageAccountConfig, name) = StorageAccountBuilder.AddFileShare(state, name, None)
    /// Adds a file share with specified quota.
    [<CustomOperation "add_file_share_with_quota">]
    member _.AddFileShareWithQuota(state:StorageAccountConfig, name:string, quota) = StorageAccountBuilder.AddFileShare(state, name, Some quota)
    /// Adds a single queue to the storage account.
    [<CustomOperation "add_queue">]
    member _.AddQueue(state:StorageAccountConfig, name:string) = { state with Queues = state.Queues.Add (StorageResourceName.Create(name).OkValue) }
    /// Adds a set of queues to the storage account.
    [<CustomOperation "add_queues">]
    member this.AddQueues(state:StorageAccountConfig, names) =
        (state, names) ||> Seq.fold(fun state name -> this.AddQueue(state, name))
    /// Enable static website support, using the supplied local content path to the storage account's $web folder as a post-deployment task, and setting the index page as supplied.
    [<CustomOperation "use_static_website">]
    member _.StaticWebsite(state:StorageAccountConfig, contentPath, indexPage) =
        { state with StaticWebsite = Some {| IndexPage = indexPage; ErrorPage = None; ContentPath = contentPath |} }
    /// Sets the error page for the static website.
    [<CustomOperation "static_website_error_page">]
    member _.StaticWebsiteErrorPage(state:StorageAccountConfig, errorPage) =
        { state with StaticWebsite = state.StaticWebsite |> Option.map(fun staticWebsite -> {| staticWebsite with ErrorPage = Some errorPage |}) }
    /// Enables support for hierarchical namespace, also known as Data Lake Storage Gen2.
    [<CustomOperation "enable_data_lake">]
    member _.UseHns(state:StorageAccountConfig, value) = { state with EnableDataLake = Some value }
    /// Adds tags to the storage account
    [<CustomOperation "add_tags">]
    member _.Tags(state:StorageAccountConfig, pairs) =
        { state with
            Tags = pairs |> List.fold (fun map (key,value) -> Map.add key value map) state.Tags }
    /// Adds a tag to the storage account
    [<CustomOperation "add_tag">]
    member this.Tag(state:StorageAccountConfig, key, value) = this.Tags(state, [ (key,value) ])
    /// Adds a lifecycle rule
    [<CustomOperation "add_lifecycle_rule">]
    member _.AddLifecycleRule(state:StorageAccountConfig, ruleName, actions, filters) =
        let rule =
            { Filters = filters
              CoolBlobAfter = actions |> List.tryPick(function CoolAfter days -> Some days | _ -> None)
              ArchiveBlobAfter = actions |> List.tryPick(function ArchiveAfter days -> Some days | _ -> None)
              DeleteBlobAfter = actions |> List.tryPick(function DeleteAfter days -> Some days | _ -> None)
              DeleteSnapshotAfter = actions |> List.tryPick(function DeleteSnapshotAfter days -> Some days | _ -> None) }
        { state with Rules = state.Rules.Add (ResourceName ruleName, rule) }

/// Allow adding storage accounts directly to CDNs
type EndpointBuilder with
    member this.Origin(state:EndpointConfig, storage:StorageAccountConfig) =
        let state = this.Origin(state, storage.Endpoint)
        this.DependsOn(state, storage.Name.ResourceName)

let storageAccount = StorageAccountBuilder()