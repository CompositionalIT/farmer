#r @"..\Farmer\bin\Debug\netstandard2.0\Farmer.dll"

open Farmer
open Farmer.Resources.KeyVault
open System

let vault =
    let policy =
        accessPolicy {
            object_id "test"
            certificate_permissions [ Certificate.Get; Certificate.Backup ]
            secret_permissions [ Secret.Backup; Secret.Get ]
        }
    keyVault {
        name "MyVault"
        sku Standard
        tenant_id (Guid.NewGuid())
        enable_disk_encryption_access
        enable_resource_manager_access
        enable_soft_delete_with_purge_protection
        disable_vm_access
        enable_recovery_mode
        add_access_policy policy
    }

let deployment = arm {
    add_resource vault
    location NorthEurope
}

deployment.Template
|> Writer.toJson
|> Writer.toFile (__SOURCE_DIRECTORY__ + "\\foo")


