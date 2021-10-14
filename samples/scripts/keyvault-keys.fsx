#r @"..\..\src\Farmer\bin\Debug\net5.0\Farmer.dll"

open Farmer
open Farmer.Builders
open Farmer.KeyVault
open Farmer.Arm.KeyVault.Keys

let vault =
    keyVault {
        name "TestFarmVault"
        sku Sku.Standard
        tenant_id Subscription.TenantId
        add_secret "simpleSecret"
        add_tag "test" "test"
    }


let key: KeyVaultKey = {
    VaultName = vault.Name
    KeyName = ResourceName "TestKey"
    Attributes = None
    Location = Location.EastUS
    CurveName = Some JSONWebKeyCurveName.P256
    KeyOps = Some JsonWebKeyOperation.Encrypt
    KeySize = None
    KTY = Some JsonWebKeyType.RSAHSM
    Tags = Map.empty
}

let deployment = arm {
    location Location.EastUS
    add_resource key
}

deployment
|> Writer.quickWrite (System.IO.Path.GetFileNameWithoutExtension __SOURCE_FILE__)
