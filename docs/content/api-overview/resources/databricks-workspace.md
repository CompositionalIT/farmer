---
title: "Databricks Workspace"
date: 2021-01-31T15:43:30+00:00
chapter: false
weight: 8
---

#### Overview
The Databricks Workspace builder is used to create Azure Databricks Workspaces.

* Workspace (`Microsoft.Databricks/workspaces`)

#### Builder Keywords
| Keyword | Purpose |
|-|-|
| name | Sets the name of the workspace. |
| sku | Sets the pricing tier of the workspace. Defaults to Standard Tier. |
| encrypt_with_key_vault | Given a key vault builder / resourceid / vault name, and the name of a key, activates the use of Key Vault for the key store. |
| encrypt_with_databricks | Specifies to use DataBricks itself for key encryption. |
| encrypt_with | Allows you to programmatically specify whether to use key vault or data bricks encryption. |
| key_vault_key_version | Specifies the version of the key vault key to use; if this is not specified, the latest version of the key is used. |
| allow_public_ip | Whether to use public IP addresses for cluster virtual machines. Defaults to Enabled. |
| attach_to_vnet | Given a Resource Id / Name / VNet Config, and Public & Private Subnets, attaches the workspace to the VNet specified. |
| managed_resource_group_id | Sets the name of the resource group that will be created by the workspace. Optional. |

#### Example
```fsharp
open Farmer
open Farmer.Builders

let myVault = keyVault { name "my-vault" }

let myWorkspace = databricksWorkspace {
    name "my-databricks-workspace"
    sku  Databricks.Sku.Standard
    encrypt_with_key_vault myVault "workspace-encryption-key"
    attach_to_vnet "databricks-vnet" "databricks-pub-snet" "databricks-priv-snet"
}
```