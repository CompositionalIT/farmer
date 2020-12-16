---
title: "Databricks Workspace"
date: 2020-12-13T15:43:30+00:00
chapter: false
weight: 8
---

#### Overview
The Databricks Workspace builder is used to create Azure Databricks Workspaces

* Workspace (`Microsoft.Databricks/workspaces`)

#### Builder Keywords
| Keyword | Purpose |
|-|-|
| name | Sets the name of the workspace. |
| sku | Sets the pricing tier of the workspace. Defaults to Standard Tier |
| key_vault | Sets the name of the key vault where the encryption key is stored. Can be given string, keyVaultConfig or KeyVault resource |
| encryption_key | Sets the name and version of the encryption key secret. Version defaults to "latest"|
| use_public_ip | Turn on public IP addresses for cluster virtual machines. Defaults to Enabled|
| byov_vnet | Sets the name of the VNet to use for BYOV mode |
| byov_public_subnet | Sets the name of the public subnet within the VNet to use for BYOV mode |
| byov_private_subnet | Sets the name of the private subnet within the VNet to use for BYOV mode |

#### Example
```fsharp
open Farmer
open Farmer.Builders

let myWorkspace = databricksWorkspace {
    name "my-databricks-workspace"
    sku  Databricks.Sku.Standard

    key_vault "databricks-kv"
    encryption_key "workspace-encryption-key" "latest"
    
    byov_vnet "databricks-vnet"
    byov_public_subnet "databricks-pub-snet"
    byov_private_subnet "databricks-priv-snet"
}
```