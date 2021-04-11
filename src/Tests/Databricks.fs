module Databricks

open Expecto
open Farmer
open Farmer.Arm.Databricks
open Farmer.Arm.KeyVault
open Farmer.Arm.Network
open Farmer.Builders.Databricks
open Farmer.Builders.VirtualNetwork
open Farmer.Databricks
open System

type ValueObj<'T> = {| value : 'T |}
type WorkspaceJson =
    { name : string
      dependsOn : string array
      sku : {| name : string |}
      properties :
        {| managedResourceGroupId : string
           parameters :
            {| enableNoPublicIp : bool ValueObj
               prepareEncryption : bool ValueObj
               customVirtualNetworkId : string ValueObj
               customPublicSubnetName : string ValueObj
               customPrivateSubnetName : string ValueObj
               encryption :
                {| keySource : string
                   keyName : string
                   keyversion : string
                   keyvaulturi : string |} ValueObj |} |} }
let fromJson (db:DatabricksConfig) = toTypedTemplate<WorkspaceJson> Location.NorthEurope db

let tests = testList "Databricks Tests" [
    let getWorkspaceArm (db:DatabricksConfig) = (db :> IBuilder).BuildResources Location.NorthEurope |> List.head :?> Workspace
    test "Creates a basic workspace" {
        let bricks =
            databricks { name "databricks-workspace" }
            |> fromJson
        Expect.equal bricks.name "databricks-workspace" "Wrong workspace name"
        Expect.equal bricks.sku.name "standard" "Wrong pricing tier"
        Expect.equal bricks.properties.managedResourceGroupId "[concat(subscription().id, '/resourceGroups/', 'databricks-workspace-rg')]" "Wrong managed resource group name"
        Expect.isFalse bricks.properties.parameters.enableNoPublicIp.value "Public IP enabled by default"
        Expect.isFalse bricks.properties.parameters.prepareEncryption.value "Encryption off by default"
    }

    test "Allows overriding managed resource group name" {
        let bricks = databricks { managed_resource_group_id "databricks-rg" } |> fromJson
        Expect.equal bricks.properties.managedResourceGroupId "[concat(subscription().id, '/resourceGroups/', 'databricks-rg')]" "Wrong managed resource group name"
    }

    test "Handles use_public_ip feature flag" {
        let bricks = databricks { allow_public_ip Disabled } |> fromJson
        Expect.isTrue bricks.properties.parameters.enableNoPublicIp.value "Enable public IP not set correctly"
    }

    test "Sets BYOV configuration" {
        let bricks = databricks { attach_to_vnet "databricks-vnet" "databricks-pub-snet" "databricks-priv-snet" } |> fromJson
        Expect.equal bricks.properties.parameters.customVirtualNetworkId.value "[resourceId('Microsoft.Network/virtualNetworks', 'databricks-vnet')]" "BYOV vnet is not set correctly"
        Expect.equal bricks.properties.parameters.customPublicSubnetName.value "databricks-pub-snet" "BYOV public subnet not set correctly"
        Expect.equal bricks.properties.parameters.customPrivateSubnetName.value "databricks-priv-snet" "BYOV private subnet not set correctly"

        let vn = vnet { name "test" }
        let pubSubnet = buildSubnet "public" 26
        let privSubnet = buildSubnet "private" 24
        let bricks = databricks { attach_to_vnet vn pubSubnet privSubnet } |> fromJson
        Expect.equal bricks.properties.parameters.customVirtualNetworkId.value "[resourceId('Microsoft.Network/virtualNetworks', 'test')]" "BYOV vnet is not set correctly"
        Expect.equal bricks.properties.parameters.customPublicSubnetName.value "public" "BYOV public subnet not set correctly"
        Expect.equal bricks.properties.parameters.customPrivateSubnetName.value "private" "BYOV private subnet not set correctly"
        Expect.contains bricks.dependsOn (virtualNetworks.resourceId("test").Eval()) "Incorrect dependency"
    }

    test "Encryption works correctly" {
        let bricks = databricks { sku Premium; encrypt_with_key_vault "databricks-kv" "databricks-encryption-key" } |> fromJson
        let parameters = bricks.properties.parameters
        Expect.isTrue parameters.prepareEncryption.value "Encryption off by default"
        Expect.equal parameters.encryption.value.keySource "Microsoft.Keyvault" "Incorrect source"
        Expect.equal parameters.encryption.value.keyName "databricks-encryption-key" "Key name not initialised correctly"
        Expect.isNull parameters.encryption.value.keyversion "Key version not initialised correctly"
        Expect.equal parameters.encryption.value.keyvaulturi "https://databricks-kv.vault.azure.net" "Key vault uri not initialised correctly"
        Expect.equal bricks.sku.name "premium" "Wrong sku"
        Expect.contains bricks.dependsOn (vaults.resourceId("databricks-kv").Eval()) "Incorrect dependency"

        let bricks =
            databricks {
                sku Premium
                encrypt_with_key_vault "databricks-kv" "databricks-encryption-key"
                key_vault_key_version (Guid.Parse "74135499-7a08-45fa-9ebd-94670097b04a") // arbitrary for test
            } |> fromJson

        Expect.equal bricks.properties.parameters.encryption.value.keyversion "74135499-7a08-45fa-9ebd-94670097b04a" "Key vault version not set correctly"
        Expect.throws (fun () -> databricks { key_vault_key_version Guid.Empty } |> ignore) "Should not be able to set key version without vault config"

        let bricks = databricks { sku Premium; encrypt_with_databricks } |> fromJson
        Expect.equal bricks.properties.parameters.encryption.value.keySource "Default" "encryption mode should be databricks"

        Expect.throws (fun () -> databricks { encrypt_with_key_vault "test" "test" } |> ignore) "Should not be able to set key vault without Premium set"
    }
]