#r @"..\Farmer\bin\Debug\netstandard2.0\Farmer.dll"

open Farmer
open Farmer.Resources.KeyVault
open System

let vault =
    let policy =
        accessPolicy {
            object_id Guid.Empty
            application_id Guid.Empty
            certificate_permissions [ Certificate.List ]
            secret_permissions Secret.All
            key_permissions [ Key.List ]
        }

    let complexSecret = secret {
        name "myComplexSecret"
        content_type "application/text"
        enable_secret
        activation_date (DateTime.Today.AddDays -1.)
        expiration_date (DateTime.Today.AddDays 1.)
    }

    keyVault {
        name "MyVault"
        sku Standard
        tenant_id Guid.Empty

        enable_disk_encryption_access
        enable_resource_manager_access
        enable_soft_delete_with_purge_protection

        disable_vm_access
        enable_recovery_mode
        add_access_policy policy
        enable_azure_services_bypass

        add_ip_rule "127.0.0.1"
        add_vnet_rule "/subscriptions/subid/resourceGroups/rg1/providers/Microsoft.Network/virtualNetworks/test-vnet/subnets/subnet1"
        allow_default_traffic

        add_secret complexSecret
        add_secret "simpleSecret"
    }

let deployment = arm {
    add_resource vault
    location NorthEurope
}

deployment
|> Writer.quickWrite "output"