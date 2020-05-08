[<AutoOpen>]
module Farmer.Builders.Storage

open Farmer.Arm.Storage
open Farmer

let internal buildKey (ResourceName name) =
    sprintf
        "concat('DefaultEndpointsProtocol=https;AccountName=%s;AccountKey=', listKeys('%s', '2017-10-01').keys[0].value)"
            name
            name
    |> ArmExpression


type StorageContainerAccess =
    | Private
    | Container
    | Blob

type StorageAccountConfig =
    { /// The name of the storage account.
      Name : ResourceName
      /// The sku of the storage account.
      Sku : StorageSku
      /// Containers for the storage account.
      Containers : (string * StorageContainerAccess) list}
    /// Gets the ARM expression path to the key of this storage account.
    member this.Key = buildKey this.Name
    interface IBuilder with
        member this.BuildResources location _ = [
            { Location = location
              Name = this.Name
              Sku = this.Sku
              Containers = [
                for container, access in this.Containers do
                    container, match access with
                               | Private -> "None"
                               | Container -> "Container"
                               | Blob -> "Blob"
              ] }
        ]

type StorageAccountBuilder() =
    member __.Yield _ = { Name = ResourceName.Empty; Sku = StorageSku.Standard_LRS; Containers = [] }
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
    /// Adds private container.
    [<CustomOperation "add_private_container">]
    member __.AddPrivateContainer(state:StorageAccountConfig, name) = { state with Containers = (name, StorageContainerAccess.Private) :: state.Containers }
    /// Adds container with anonymous read access for blobs and containers.
    [<CustomOperation "add_public_container">]
    member __.AddPublicContainer(state:StorageAccountConfig, name) = { state with Containers = (name, StorageContainerAccess.Container) :: state.Containers }
    /// Adds container with anonymous read access for blobs only.
    [<CustomOperation "add_blob_container">]
    member __.AddBlobContainer(state:StorageAccountConfig, name) = { state with Containers = (name, StorageContainerAccess.Blob) :: state.Containers }

let storageAccount = StorageAccountBuilder()