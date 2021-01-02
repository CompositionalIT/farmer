module Databricks

open Expecto
open Farmer
open Farmer.Builders.Databricks
open Farmer.Builders.VirtualNetwork
open Farmer.Databricks
open Farmer.Arm.Databricks
open System

let tests = testList "Databricks Tests" [
    let getWorkspaceArm (db:WorkspaceConfig) = (db :> IBuilder).BuildResources Location.NorthEurope |> List.head :?> Workspace
    test "Creates a basic workspace" {
        let workspace = databricks {
            name "databricks-workspace"
            managed_resource_group_id "databricks-rg"
        }
        let workspace = getWorkspaceArm workspace
        Expect.equal workspace.Name (ResourceName "databricks-workspace") "Wrong workspace name"
        Expect.equal workspace.ManagedResourceGroupId (ResourceName "databricks-rg") "Wrong managed resource group name"
        Expect.equal workspace.Sku Standard "Wrong pricing tier"
        Expect.isTrue workspace.EnablePublicIp.AsBoolean "Public IP enabled by default"
    }

    test "Handles use_public_ip feature flag" {
        let workspace = databricks { allow_public_ip Disabled } |> getWorkspaceArm
        Expect.isFalse workspace.EnablePublicIp.AsBoolean "Enable public IP not set correctly"
    }

    test "Sets BYOV configuration" {
        let workspace = databricks { attach_to_vnet "databricks-vnet" "databricks-pub-snet" "databricks-priv-snet" }
        let workspace = getWorkspaceArm workspace
        Expect.equal (workspace.ByovConfig.Value.Vnet.Name.Value) "databricks-vnet" "BYOV vnet is not set correctly"
        Expect.equal workspace.ByovConfig.Value.PublicSubnet.Value "databricks-pub-snet" "BYOV public subnet not set correctly"
        Expect.equal workspace.ByovConfig.Value.PrivateSubnet.Value "databricks-priv-snet" "BYOV private subnet not set correctly"

        let vn = vnet { name "test" }
        let pubSubnet = buildSubnet "public" 26
        let privSubnet = buildSubnet "private" 24
        let workspace = databricks { attach_to_vnet vn pubSubnet privSubnet }
        Expect.equal (workspace.ByovConfig.Value.Vnet.Name.Value) "test" "BYOV vnet is not set correctly"
        Expect.equal workspace.ByovConfig.Value.PublicSubnet.Value "public" "BYOV public subnet not set correctly"
        Expect.equal workspace.ByovConfig.Value.PrivateSubnet.Value "private" "BYOV private subnet not set correctly"
    }

    let getKeyVaultConfig : Workspace -> _ = function
        | { SecretScope = Some (KeyVaultSecretScope c) } -> c
        | { SecretScope = Some DataBricksSecretScope | None } -> failwith "Incorrect encryption mode specified"

    test "Sets encryption configuration correctly" {
        let workspace = databricks { key_vault_secret_scope "databricks-kv" "databricks-encryption-key" } |> getWorkspaceArm
        let config = getKeyVaultConfig workspace
        Expect.equal config.Key "databricks-encryption-key" "Key name not initialised correctly"
        Expect.isNone config.KeyVersion  "Key version not initialised correctly"
        Expect.equal config.Vault (ResourceName "databricks-kv") "Key vault uri not initialised correctly"

        let workspace =
            databricks {
                key_vault_secret_scope "databricks-kv" "databricks-encryption-key"
                key_vault_key_version Guid.Empty
            } |> getWorkspaceArm
        Expect.equal (getKeyVaultConfig workspace).KeyVersion (Some Guid.Empty) "Key vault version not set correctly"

        Expect.throws (fun () -> databricks { key_vault_key_version Guid.Empty } |> ignore) "Should not be able to set key version without vault config"

        let workspace = databricks { databricks_secret_scope }
        Expect.equal workspace.SecretScope (Some DataBricksSecretScope) "encryption mode should be databricks"
    }
]