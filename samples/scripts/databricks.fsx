#r @"./libs/Newtonsoft.Json.dll"
#r @"../../src/Farmer/bin/Debug/netstandard2.0/Farmer.dll"

open Farmer
open Farmer.Builders
open Farmer.Databricks

let workspace = databricksWorkspace {
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

let deployment = arm {
    location Location.NorthEurope
    add_resource workspace
}

// Generate the ARM template here...
deployment
|> Writer.quickWrite "farmer-deploy"