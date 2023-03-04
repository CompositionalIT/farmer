#r "nuget:Farmer"

open Farmer
open Farmer.Builders
open System

let myVault = keyVault { name "my-vault" }

let workspace = databricks {
    name "isaac-databricks"
    encrypt_with_key_vault myVault "workspace-encryption-key"
    key_vault_key_version Guid.Empty
    attach_to_vnet "databricks-vnet" "databricks-pub-snet" "databricks-priv-snet"
}

let deployment = arm {
    location Location.NorthEurope
    add_resource workspace
}

// Generate the ARM template here...
deployment |> Deploy.execute "my-resource-group" []
