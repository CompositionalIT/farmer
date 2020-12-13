module Databricks

open Expecto
open Farmer
open Farmer.Builders
open Farmer.Databricks
open System

let tests = testList "Databricks Tests" [
    test "Creates a basic workspace" {
        let workspace = databricksWorkspace { 
            name "databricks-workspace"
            managed_resource_group_id "databricks-rg"
            pricing_tier Databricks.PricingTier.Standard
        }
        Expect.equal workspace.Name.Value "databricks-workspace" "Wrong workspace name"
        Expect.equal workspace.ManagedResourceGroupId "databricks-rg" "Wrong managed resource group name"
        Expect.equal (workspace.PricingTier.ToString()) "StandardTier" "Wrong pricing tier"
    }
    test "Handles disable_public_ip flag" {
        let workspace = databricksWorkspace { 
            name "databricks-workspace"
            managed_resource_group_id "databricks-rg"
            pricing_tier Databricks.PricingTier.Standard
            disable_public_ip
        }
        Expect.equal workspace.DisablePublicIp.Value true "Disable public IP not set correctly"
    }
    test "Handles use_byov flaf and sets empty BYOV settings if only flag is given" {
        let workspace = databricksWorkspace { 
            name "databricks-workspace"
            managed_resource_group_id "databricks-rg"
            pricing_tier Databricks.PricingTier.Standard
            enable_byov_mode
        }
        Expect.equal workspace.ByovMode.Value true "BYOV mode not set correctly"
        Expect.equal workspace.ByovConfig.Value.Vnet "" "BYOV vnet not initialised correctly"
        Expect.equal workspace.ByovConfig.Value.PublicSubnet "" "BYOV public subnet not initialised correctly"
        Expect.equal workspace.ByovConfig.Value.PrivateSubnet "" "BYOV private subnet not initialised correctly"
    }
    test "Sets BYOV configuration" {
        let workspace = databricksWorkspace { 
            name "databricks-workspace"
            managed_resource_group_id "databricks-rg"
            pricing_tier Databricks.PricingTier.Standard
            enable_byov_mode
            byov_vnet "databricks-vnet"
            byov_public_subnet "databricks-pub-snet"
            byov_private_subnet "databricks-priv-snet"           
        }
        Expect.equal workspace.ByovMode.Value true "BYOV mode not set correctly"
        Expect.equal workspace.ByovConfig.Value.Vnet "databricks-vnet" "BYOV vnet not set correctly"
        Expect.equal workspace.ByovConfig.Value.PublicSubnet "databricks-pub-snet" "BYOV public subnet not set correctly"
        Expect.equal workspace.ByovConfig.Value.PrivateSubnet "databricks-priv-snet" "BYOV private subnet not set correctly"
    }
    test "Handles prepareEncryption flag and sets encryption configuration if flag is given" {
        let workspace = databricksWorkspace { 
            name "databricks-workspace"
            managed_resource_group_id "databricks-rg"
            pricing_tier Databricks.PricingTier.Standard
            enable_encryption
        }
        Expect.equal workspace.PrepareEncryption.Value true "Disable public IP not set correctly"
        Expect.equal workspace.Encryption.Value.KeySource "Microsoft.Keyvault" "Key source not initialised correctly"
        Expect.equal workspace.Encryption.Value.KeyName "" "Key name not initialised correctly"
        Expect.equal workspace.Encryption.Value.KeyVersion "latest" "Key version not initialised correctly"
        Expect.equal workspace.Encryption.Value.KeyVaultUri "" "Key vault uri not initialised correctly"
    }
    test "Sets encryption configuration" {
        let workspace = databricksWorkspace { 
            name "databricks-workspace"
            managed_resource_group_id "databricks-rg"
            pricing_tier Databricks.PricingTier.Standard
            enable_encryption
            key_name "databricks-encryption-key"
            key_version "latest"
            key_vault "databricks-kv"
        }
        Expect.equal workspace.PrepareEncryption.Value true "Disable public IP not set correctly"
        Expect.equal workspace.Encryption.Value.KeySource "Microsoft.Keyvault" "Key source not initialised correctly"
        Expect.equal workspace.Encryption.Value.KeyName "databricks-encryption-key" "Key name not initialised correctly"
        Expect.equal workspace.Encryption.Value.KeyVersion "latest" "Key version not initialised correctly"
        Expect.equal workspace.Encryption.Value.KeyVaultUri "https://databricks-kv.vault.azure.net" "Key vault uri not initialised correctly"
    }
]