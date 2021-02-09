---
title: "Storage Account"
date: 2020-02-05T08:53:46+01:00
chapter: false
weight: 23
---

#### Overview

The Storage Account builder creates storage accounts and their associated containers.

* Storage Accounts (`Microsoft.Storage/storageAccounts`)
* Storage Containers (`blobServices/containers`)
* File Shares (`fileServices/shares`)
* Queues (`Microsoft.Storage/storageAccounts/queueServices/queues`)
* Tables (`Microsoft.Storage/storageAccounts/tableServices/tables`)

#### Builder Keywords

| Keyword | Purpose |
|-|-|
| name | Specifies the name of the storage account |
| sku | Sets the SKU of the storage account. A set of predefined SKU values are available as members in `Storage.Sku`, but you can create the full range of combinations of Kind and SKU as needed. |
| default_blob_access_tier | Sets the default access tier for blob containers |
| add_public_container | Adds a general-purpose public storage container |
| add_private_container | Adds a general-purpose private storage container |
| add_blob_container | Adds a general-purpose private blob container |
| add_file_share | Adds a file share to storage account |
| add_file_share_with_quota | Adds a file share to storage account with a share quota in Gb |
| add_queue | Adds a queue to the storage account |
| add_queues | Adds a list of queues to the storage account |
| add_table | Adds a table to the storage account |
| add_tables | Adds a list of tables to the storage account |
| use_static_website | Activates static website host, and uploads the provided local content as a post-deployment task to the storage with the specified index page |
| static_website_error_page | Specifies the 404 page to display for static website hosting |
| enable_data_lake | Enables Azure Data Lake Gen2 support on the storage account |
| add_lifecycle_policy | Given a rule name, a list of PolicyActions and a list of string filters, creates a lifecycle policy for the storage account |
| grant_access | Given a managed identity (can be either user- or system- assigned), and a specific RoleId from the Roles module, grants access to the identity for the provided role. |

#### Configuration Members

| Member | Purpose |
|-|-|
| Key | Returns an ARM expression to retrieve the storage account's primary connection string. Useful for e.g. supplying the connection string to another resource e.g. KeyVault or an app setting in the App Service. |
| WebsitePrimaryEndpoint | Returns an ARM Expression for the Primary endpoint for static website (if enabled). |
| WebsitePrimaryEndpointHost | Returns an ARM Expression for the Host of the Primary endpoint for static website (if enabled). Use this for e.g. Azure CDN integration. |

#### Helpers
The `StorageAccount` type contains helper methods to quickly create ARM expressions for Storage Account connection strings.

#### Example

```fsharp
open Farmer
open Farmer.Builders

let storage = storageAccount {
    name "isaacssuperstorage"
    sku Storage.Premium_LRS
    add_public_container "mypubliccontainer"
    add_private_container "myprivatecontainer"
    add_blob_container "myblobcontainer"
    add_file_share "share1"
    add_file_share_with_quota "share2" 1024<Gb>
    add_queue "myqueue"
    add_table "mytable"
    use_static_website "local/path/to/folder/content" "index.html"
    static_website_error_page "error.html"
    enable_data_lake true
    add_lifecycle_rule "moveToCool" [ Storage.CoolAfter 30<Days>; Storage.ArchiveAfter 90<Days> ] Storage.NoRuleFilters
    add_lifecycle_rule "cleanup" [ Storage.DeleteAfter 7<Days> ] [ "data/recyclebin" ]
    grant_access myWebApp.SystemIdentity Roles.StorageBlobDataReader
}
```