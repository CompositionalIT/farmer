---
title: "Storage Account"
date: 2020-02-05T08:53:46+01:00
weight: 19
chapter: false
---

#### Overview

The Storage Account builder creates storage accounts and their associated containers.

* Storage Accounts (`Microsoft.Storage/storageAccounts`)
* Storage Containers (`blobServices/containers`)
* File Shares (`fileServices/shares`)

#### Builder Keywords

| Keyword | Purpose |
|-|-|
| name | Specifies the name of the storage account |
| sku | Sets the SKU of the storage account |
| add_public_container | Adds a general-purpose public storage container |
| add_private_container | Adds a general-purpose private storage container |
| add_blob_container | Adds a general-purpose private blob container |
| add_file_share | Adds a file share to storage account (optional add the share quota in Gb) |

#### Configuration Members

| Member | Purpose |
|-|-|
| Key | Returns an ARM expression to retrieve the storage account's primary connection string. Useful for e.g. supplying the connection string to another resource e.g. KeyVault or an app setting in the App Service. |

#### Example

```fsharp
open Farmer
open Farmer.Builders

let storage = storageAccount {
    name "isaacssuperstorage"
    sku Storage.Premium_LRS
    add_public_container "myPublicContainer"
    add_private_container "myPrivateContainer"
    add_blob_container "myBlobContainer"
    add_file_share "share1"
    add_file_share "share2" 1024
}
```