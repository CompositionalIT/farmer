#r @"../../src/Farmer/bin/Debug/net5.0/Farmer.dll"

open Farmer
open Farmer.Builders
open Farmer.KeyVault

let vault =
    keyVault {
        name "TestFarmVault"
        sku Sku.Standard
        tenant_id Subscription.TenantId
        add_secret "simpleSecret"
        add_tag "test" "test"
    }


let key: Arm.KeyVault.Vaults.Key = {
    VaultName = vault.Name
    KeyName = ResourceName "TestKey"
    Attributes = None
    Location = Location.EastUS
    CurveName = Some KeyCurveName.P256
    KeyOps = Some KeyOperation.Encrypt
    KeySize = None
    KTY = Some KeyType.RSA
    Dependencies = Set.empty
    Tags = Map.empty
}

let deployment = arm {
    location Location.EastUS
    add_resource vault
    add_resource key
}

deployment
|> Writer.quickWrite (System.IO.Path.GetFileNameWithoutExtension __SOURCE_FILE__)
