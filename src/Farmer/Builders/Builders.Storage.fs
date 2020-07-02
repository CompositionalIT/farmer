[<AutoOpen>]
module Farmer.Builders.Storage

open Farmer
open Farmer.CoreTypes
open Farmer.Storage
open Farmer.Arm.Storage
open BlobServices
open FileShares

let internal buildKey (ResourceName name) =
    sprintf
        "concat('DefaultEndpointsProtocol=https;AccountName=%s;AccountKey=', listKeys('%s', '2017-10-01').keys[0].value)"
            name
            name
    |> ArmExpression.create

type ShareQuotaInGb = int

type StorageAccountConfig =
    { /// The name of the storage account.
      Name : ResourceName
      /// The sku of the storage account.
      Sku : Sku
      /// Containers for the storage account.
      Containers : (ResourceName * StorageContainerAccess) list
      /// File shares
      FileShares: (ResourceName * ShareQuotaInGb option) list }
    /// Gets the ARM expression path to the key of this storage account.
    member this.Key = buildKey this.Name
    member this.Endpoint = sprintf "%s.blob.core.windows.net" this.Name.Value
    interface IBuilder with
        member this.DependencyName = this.Name
        member this.BuildResources location = [
            { Name = this.Name
              Location = location
              Sku = this.Sku }
            for name, access in this.Containers do
                { Name = name
                  StorageAccount = this.Name
                  Accessibility = access }
            for (name, shareQuota) in this.FileShares do
                { Name = name
                  ShareQuota = shareQuota
                  StorageAccount = this.Name }
        ]

type StorageAccountBuilder() =
    member __.Yield _ = { Name = ResourceName.Empty; Sku = Standard_LRS; Containers = []; FileShares = [] }
    member _.Run(state:StorageAccountConfig) =
        { state with
            Name = state.Name |> Helpers.sanitiseStorage |> ResourceName }
    /// Sets the name of the storage account.
    [<CustomOperation "name">]
    member __.Name(state:StorageAccountConfig, name) = { state with Name = name }
    member this.Name(state:StorageAccountConfig, name) = this.Name(state, ResourceName name)
    /// Sets the sku of the storage account.
    [<CustomOperation "sku">]
    member __.Sku(state:StorageAccountConfig, sku) = { state with Sku = sku }
    static member private AddContainer(state, access, name) =
        { state with Containers = state.Containers @ [ (ResourceName name, access) ] }
    /// Adds private container.
    [<CustomOperation "add_private_container">]
    member __.AddPrivateContainer(state:StorageAccountConfig, name) = StorageAccountBuilder.AddContainer(state, Private, name)
    /// Adds container with anonymous read access for blobs and containers.
    [<CustomOperation "add_public_container">]
    member __.AddPublicContainer(state:StorageAccountConfig, name) =  StorageAccountBuilder.AddContainer(state, Container, name)
    /// Adds container with anonymous read access for blobs only.
    [<CustomOperation "add_blob_container">]
    member __.AddBlobContainer(state:StorageAccountConfig, name) = StorageAccountBuilder.AddContainer(state, Blob, name)
    [<CustomOperation "add_file_share">]
    member __.AddFileShare(state:StorageAccountConfig, name) = { state with FileShares = state.FileShares @ [ ResourceName name, None ]}
    [<CustomOperation "add_file_share">]
    member __.AddFileShare(state:StorageAccountConfig, name, quota) = { state with FileShares = state.FileShares @ [ ResourceName name, Some quota ]}

/// Allow adding storage accounts directly to CDNs
type EndpointBuilder with
    member this.Origin(state:EndpointConfig, storage:StorageAccountConfig) =
        let state = this.Origin(state, storage.Endpoint)
        this.DependsOn(state, storage.Name)

let storageAccount = StorageAccountBuilder()