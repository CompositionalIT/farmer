[<AutoOpen>]
module Farmer.Resources.Storage

open Farmer
open Farmer.Models

module Sku =
    let StandardLRS = "Standard_LRS"
    let StandardGRS = "Standard_GRS"
    let StandardRAGRS = "Standard_RAGRS"
    let StandardZRS = "Standard_ZRS"
    let StandardGZRS = "Standard_GZRS"
    let StandardRAGZRS = "Standard_RAGZRS"
    let PremiumLRS = "Premium_LRS"
    let PremiumZRS = "Premium_ZRS"
let internal buildKey (ResourceName name) =
    sprintf
        "concat('DefaultEndpointsProtocol=https;AccountName=%s;AccountKey=', listKeys('%s', '2017-10-01').keys[0].value)"
            name
            name
    |> ArmExpression

type StorageAccountConfig =
    { /// The name of the storage account.
      Name : ResourceName
      /// The sku of the storage account.
      Sku : string
      /// Containers for the storage account.
      Containers : (string * StorageContainerAccess) list}
    /// Gets the ARM expression path to the key of this storage account.
    member this.Key = buildKey this.Name
type StorageAccountBuilder() =
    member __.Yield _ = { Name = ResourceName.Empty; Sku = Sku.StandardLRS; Containers = [] }
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

module Converters =
    let storage location (sac:StorageAccountConfig) =
        { Location = location
          Name = sac.Name
          Sku = sac.Sku
          Containers = sac.Containers }

type Farmer.ArmBuilder.ArmBuilder with
    member this.AddResource(state:ArmConfig, config:StorageAccountConfig) =
        { state with
            Resources = StorageAccount (Converters.storage state.Location config) :: state.Resources
        }
    member this.AddResources (state, configs) = addResources this.AddResource state configs

let storageAccount = StorageAccountBuilder()