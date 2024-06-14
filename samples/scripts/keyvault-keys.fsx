#r @"../../src/Farmer/bin/Debug/net5.0/Farmer.dll"

open Farmer
open Farmer.Builders
open Farmer.KeyVault

let vault = keyVault {
    name "TestFarmVault"
    sku Sku.Standard
    tenant_id Subscription.TenantId
    add_tag "test" "test"

    add_keys [
        key {
            name "testKeyInline1"
            key_type KeyType.RSA_4096
        }
        key {
            name "testKeyInline2"
            key_type KeyType.EC_P256
        }
    ]
}


let myKey = key {
    name "testKey3"
    key_type KeyType.RSA_4096
    link_to_unmanaged_keyvault vault
    key_operations [ KeyOperation.Encrypt ]
}

let deployment = arm {
    location Location.EastUS
    add_resource vault
    add_resource myKey
}

deployment
|> Writer.quickWrite (System.IO.Path.GetFileNameWithoutExtension __SOURCE_FILE__)