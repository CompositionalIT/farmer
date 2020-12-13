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
| pricing_tier | Sets the pricing tier of the workspace. Defaults to Standard Tier |
| enable_encryption | Turns on workspace encryption encryption. |
| key_vault | Sets the name of the key vault where the encryption key is stored |
| key_name | Sets the name of the encryption key secret|
| key_version | Sets the version of the encryption key secret. Defaults to latest |
| disable_public_ip | Turn off public IP addresses for cluster virtual machines. Defaults to false|
| enable_byov_mode | Turn on Bring Your Own VNet networking mode. Defaults to false |
| byov_vnet | Sets the name of the VNet to use for BYOV mode |
| byov_public_subnet | Sets the name of the public subnet within the VNet to use for BYOV mode |
| byov_private_subnet | Sets the name of the private subnet within the VNet to use for BYOV mode |

#### Example
```fsharp
open Farmer
open Farmer.Builders

let myWorkspace = databricksWorkspace {
    name "my-databricks-workspace"
    pricing_tier Databricks.PricingTier.Standard
    
    disable_public_ip
    
    enable_encryption
    key_vault "databricks-kv"
    key_name "workspace-encryption-key"
    key_version "latest"
    
    enable_byov_mode
    byov_vnet "databricks-vnet"
    byov_public_subnet "databricks-pub-snet"
    byov_private_subnet "databricks-priv-snet"
}
```