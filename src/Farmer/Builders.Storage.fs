[<AutoOpen>]
module Farmer.Storage

open Farmer

module Sku =
    let StandardLRS = "Standard_LRS"
    let StandardGRS = "Standard_GRS"
    let StandardRAGRS = "Standard_RAGRS"
    let StandardZRS = "Standard_ZRS"
    let StandardGZRS = "Standard_GZRS"
    let StandardRAGZRS = "Standard_RAGZRS"
    let PremiumLRS = "Premium_LRS"
    let PremiumZRS = "Premium_ZRS"
let buildKey (ResourceName name) =
    sprintf
        "[concat('DefaultEndpointsProtocol=https;AccountName=%s;AccountKey=', listKeys('%s', '2017-10-01').keys[0].value)]"
            name
            name

type StorageContainerAccess =
    | Private
    | Container
    | Blob

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
    [<CustomOperation "name">]
    /// Sets the name of the storage account.
    member __.Name(state:StorageAccountConfig, name) = { state with Name = name }
    member this.Name(state:StorageAccountConfig, name) = this.Name(state, ResourceName name)
    [<CustomOperation "sku">]
    /// Sets the sku of the storage account.
    member __.Sku(state:StorageAccountConfig, sku) = { state with Sku = sku }
    [<CustomOperation "private_container">]
    /// Adds private container.
    member __.PrivateContainer(state:StorageAccountConfig, name) = { state with Containers = (name, StorageContainerAccess.Private) :: state.Containers }
    [<CustomOperation "public_container">]
    /// Adds container with anonymous read access for blobs and containers.
    member __.PublicContainer(state:StorageAccountConfig, name) = { state with Containers = (name, StorageContainerAccess.Container) :: state.Containers }
    [<CustomOperation "blob_container">]
    /// Adds container with anonymous read access for blobs only.
    member __.BlobContainer(state:StorageAccountConfig, name) = { state with Containers = (name, StorageContainerAccess.Blob) :: state.Containers }

module Converters =
    open Farmer.Internal
    
    let private containerAccess (a:StorageContainerAccess) =
        match a with
        | Private -> "None"
        | Container -> "Container"
        | Blob -> "Blob"
    
    let storage location (sac:StorageAccountConfig) =
        {
            Location = location
            Name = sac.Name
            Sku = sac.Sku
            Containers =
                sac.Containers
                |> List.map (fun (n,a) -> n, containerAccess a)
        }

let storageAccount = StorageAccountBuilder()
