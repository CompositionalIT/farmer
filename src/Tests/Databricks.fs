module Databricks

open Expecto
open Farmer
open Farmer.Builders.Databricks
open Farmer.Builders.VirtualNetwork
open Farmer.Databricks
open Farmer.Arm.Databricks
open Farmer.Arm.KeyVault
open Farmer.Arm.Network
open System

let tests = testList "Databricks Tests" [
    let getWorkspaceArm (db:DatabricksConfig) = (db :> IBuilder).BuildResources Location.NorthEurope |> List.head :?> Workspace
    test "Creates a basic workspace" {
        let bricks = databricks {
            name "databricks-workspace"
        }
        let bricks = getWorkspaceArm bricks
        Expect.equal bricks.Name (ResourceName "databricks-workspace") "Wrong workspace name"
        Expect.equal bricks.ManagedResourceGroupId.Value "databricks-workspace-rg" "Wrong managed resource group name"
        Expect.equal bricks.Sku Standard "Wrong pricing tier"
        Expect.isTrue bricks.EnablePublicIp.AsBoolean "Public IP enabled by default"
    }

    test "Allows overriding managed resource group name" {
        let bricks = databricks { managed_resource_group_id "databricks-rg" } |> getWorkspaceArm
        Expect.equal bricks.ManagedResourceGroupId.Value "databricks-rg" "Wrong managed resource group name"
    }

    test "Handles use_public_ip feature flag" {
        let bricks = databricks { allow_public_ip Disabled } |> getWorkspaceArm
        Expect.isFalse bricks.EnablePublicIp.AsBoolean "Enable public IP not set correctly"
    }

    test "Sets BYOV configuration" {
        let bricks = databricks { attach_to_vnet "databricks-vnet" "databricks-pub-snet" "databricks-priv-snet" } |> getWorkspaceArm
        Expect.equal (bricks.ByovConfig.Value.Vnet.Name.Value) "databricks-vnet" "BYOV vnet is not set correctly"
        Expect.equal bricks.ByovConfig.Value.PublicSubnet.Value "databricks-pub-snet" "BYOV public subnet not set correctly"
        Expect.equal bricks.ByovConfig.Value.PrivateSubnet.Value "databricks-priv-snet" "BYOV private subnet not set correctly"

        let vn = vnet { name "test" }
        let pubSubnet = buildSubnet "public" 26
        let privSubnet = buildSubnet "private" 24
        let bricks = databricks { attach_to_vnet vn pubSubnet privSubnet } |> getWorkspaceArm
        Expect.equal bricks.ByovConfig.Value.Vnet.Name.Value "test" "BYOV vnet is not set correctly"
        Expect.equal bricks.ByovConfig.Value.PublicSubnet.Value "public" "BYOV public subnet not set correctly"
        Expect.equal bricks.ByovConfig.Value.PrivateSubnet.Value "private" "BYOV private subnet not set correctly"
        Expect.contains bricks.Dependencies (virtualNetworks.resourceId "test") "Incorrect dependency"
    }

    let getKeyVaultConfig : Workspace -> _ = function
        | { KeyEncryption = Some (CustomerManagedEncryption c) } -> c
        | { KeyEncryption = Some InfrastructureEncryption | None } -> failwith "Incorrect encryption mode specified"

    test "Secret scope works correctly" {
        let bricks = databricks { sku Premium; key_vault_secret_scope "databricks-kv" "databricks-encryption-key" } |> getWorkspaceArm
        let config = getKeyVaultConfig bricks
        Expect.equal config.Key "databricks-encryption-key" "Key name not initialised correctly"
        Expect.isNone config.KeyVersion  "Key version not initialised correctly"
        Expect.equal config.Vault.Name.Value "databricks-kv" "Key vault uri not initialised correctly"
        Expect.contains bricks.Dependencies (vaults.resourceId "databricks-kv") "Incorrect dependency"

        let bricks =
            databricks {
                sku Premium
                key_vault_secret_scope "databricks-kv" "databricks-encryption-key"
                key_vault_key_version Guid.Empty
            } |> getWorkspaceArm
        Expect.equal (getKeyVaultConfig bricks).KeyVersion (Some Guid.Empty) "Key vault version not set correctly"

        Expect.throws (fun () -> databricks { key_vault_key_version Guid.Empty } |> ignore) "Should not be able to set key version without vault config"

        let bricks = databricks { sku Premium; databricks_secret_scope } |> getWorkspaceArm
        Expect.equal bricks.KeyEncryption (Some InfrastructureEncryption) "encryption mode should be databricks"
    }
]