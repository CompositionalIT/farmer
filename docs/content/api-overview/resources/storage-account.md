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
* Queues (`Microsoft.Storage/storageAccounts/queueServices/queues`)

#### Builder Keywords

| Keyword | Purpose |
|-|-|
| name | Specifies the name of the storage account |
| sku | Sets the SKU of the storage account |
| add_public_container | Adds a general-purpose public storage container |
| add_private_container | Adds a general-purpose private storage container |
| add_blob_container | Adds a general-purpose private blob container |
| add_file_share | Adds a file share to storage account |
| add_file_share_with_quota | Adds a file share to storage account with a share quota in Gb |
| add_queue | Adds a queue to the storage account. |
| add_queues | Adds a list of queues to the storage account |
| static_website | Supplying an index and error document will instruct Farmer to enable the static website feature. |

#### Post-deployment Builder Keywords
The StorageAccount builder contains special commands that are executed *after* the ARM deployment is completed.

| Keyword | Purpose |
|-|-|
| static_website_content | Specifies a folder to deploy to the $web container folder once the ARM deployment is complete. You must have already provided the `static_website` keyword. |

#### Configuration Members

| Member | Purpose |
|-|-|
| Key | Returns an ARM expression to retrieve the storage account's primary connection string. Useful for e.g. supplying the connection string to another resource e.g. KeyVault or an app setting in the App Service. |
| WebsitePrimaryEndpoint | Returns the Primary endpoint for static website (if enabled). |

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
    add_file_share_with_quota "share2" 1024
    static_website "index.html" "error.html"
    static_website_content "local/path/to/folder/content"
}
```