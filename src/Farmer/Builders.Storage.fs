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

type StorageAccountConfig =
    { /// The name of the storage account.
        Name : ResourceName
        /// The sku of the storage account.
        Sku : string }
    /// Gets the ARM expression path to the key of this storage account.
    member this.Key = buildKey this.Name
type StorageAccountBuilder() =
    member __.Yield _ = { Name = ResourceName.Empty; Sku = Sku.StandardLRS }
    [<CustomOperation "name">]
    /// Sets the name of the storage account.
    member __.Name(state:StorageAccountConfig, name) = { state with Name = name }
    member this.Name(state:StorageAccountConfig, name) = this.Name(state, ResourceName name)
    [<CustomOperation "sku">]
    /// Sets the sku of the storage account.
    member __.Sku(state:StorageAccountConfig, sku) = { state with Sku = sku }

module Converters =
    open Farmer.Internal
    let storage location (sac:StorageAccountConfig) =
        { Location = location; Name = sac.Name; Sku = sac.Sku }

let storageAccount = StorageAccountBuilder()
