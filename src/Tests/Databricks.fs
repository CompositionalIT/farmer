module Databricks

open Expecto
open Farmer
open Farmer.Builders
open Farmer.Databricks
open Farmer.Arm.Databricks
open System

let tests = testList "Databricks Tests" [
    test "Creates a basic workspace" {
        let db = dataBricks {
            name "databricks-workspace"
            managed_resource_group_id "databricks-rg"
            sku Databricks.Sku.Standard
        }

        let builder = db :> IBuilder
        let workspace = builder.BuildResources Location.NorthEurope |> List.head :?> Workspace

        Expect.equal workspace.Name (ResourceName "databricks-workspace") "Wrong workspace name"
        Expect.equal workspace.ManagedResourceGroupId (ResourceName "databricks-rg") "Wrong managed resource group name"
        Expect.equal (workspace.Sku.ToString()) "StandardTier" "Wrong pricing tier"
    }
    test "Handles use_public_ip feature flag" {
        let workspace = dataBricks {
            use_public_ip Enabled
        }
        Expect.equal workspace.EnablePublicIp Enabled "Enable public IP not set correctly"
    }
    test "Sets BYOV configuration" {
        let workspace = dataBricks {
            name "databricks-workspace"
            managed_resource_group_id "databricks-rg"
            sku Databricks.Sku.Standard
            byov_vnet "databricks-vnet"
            byov_public_subnet "databricks-pub-snet"
            byov_private_subnet "databricks-priv-snet"
        }
        Expect.equal (workspace.ByovConfig.Value.Vnet.resourceId(workspace.ByovConfig.Value).Name.Value) "databricks-vnet" "BYOV vnet is not set correctly"
        Expect.equal workspace.ByovConfig.Value.PublicSubnet.Value "databricks-pub-snet" "BYOV public subnet not set correctly"
        Expect.equal workspace.ByovConfig.Value.PrivateSubnet.Value "databricks-priv-snet" "BYOV private subnet not set correctly"
    }
    test "Sets encryption configuration" {
        let workspace = dataBricks {
            name "databricks-workspace"
            managed_resource_group_id "databricks-rg"
            sku Databricks.Sku.Standard
            key_vault "databricks-kv" KeyVault.KeySource.Default
            encryption_key "databricks-encryption-key" "latest"
        }
        Expect.equal workspace.PrepareEncryption Enabled "Prepare encryption not set correctly"
        Expect.equal workspace.Encryption.Value.KeySource KeyVault.KeySource.Default "Key source not initialised correctly"
        Expect.equal workspace.Encryption.Value.KeyName "databricks-encryption-key" "Key name not initialised correctly"
        Expect.equal workspace.Encryption.Value.KeyVersion "latest" "Key version not initialised correctly"
        Expect.equal workspace.Encryption.Value.KeyVault.Value "databricks-kv" "Key vault uri not initialised correctly"
    }
]