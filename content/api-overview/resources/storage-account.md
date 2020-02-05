---
title: "Storage Account"
date: 2020-02-05T08:53:46+01:00
weight: 1
chapter: false
---

#### Overview
The Storage Account builder creates storage accounts and their associated containers.

* Storage Accounts (`Microsoft.Storage/storageAccounts`)
* Storage Containers (`blobServices/containers`)

#### Builder Keywords

| Keyword | Purpose |
|-|-|
| name | Specifies the name of the storage account |
| sku | Sets the SKU of the storage account |
| add_public_container | Adds a general-purpose public storage container |
| add_private_container | Adds a general-purpose private storage container |
| add_blob_container | Adds a general-purpose private blob container |

#### Configuration Members

| Member | Purpose |
|-|-|
| Key | Returns an ARM expression to retrieve the storage account's primary connection string. Useful for e.g. supplying the connection string to another resource e.g. KeyVault or an app setting in the App Service. |

#### Example

```fsharp
open Farmer
open Farmer.Resources

let storage = storageAccount {
    name "isaacssuperstorage"
    sku Sku.PremiumLRS
    add_public_container "myPublicContainer"
    add_private_container "myPrivateContainer"
    add_blob_container "myBlobContainer"
}
```