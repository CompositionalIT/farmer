#r @"./libs/Newtonsoft.Json.dll"
#r @"../../src/Farmer/bin/Debug/netstandard2.0/Farmer.dll"

open Farmer
open Farmer.Builders
open Farmer.CoreTypes
open Farmer.KeyVault
open System

let policy =
    accessPolicy {
        object_id Guid.Empty
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

let store = storageAccount { name "foo" }
let principal = PrincipalId (ArmExpression.create "GETS BACK OBJECT ID OF ACCOUNT")

let vault =
    keyVault {
        name "MyVault"
        sku Sku.Standard
        tenant_id Subscription.TenantId

        enable_disk_encryption_access
        enable_resource_manager_access
        enable_soft_delete_with_purge_protection

        add_access_policy policy
        add_access_policies [
            AccessPolicy.create principal
            AccessPolicy.create (principal, Secret.All)
            accessPolicy { object_id principal; secret_permissions [ Secret.Get; Secret.Delete ] }
        ]

        disable_vm_access
        enable_recovery_mode
        enable_azure_services_bypass

        allow_default_traffic

        add_secret complexSecret
        add_secret "simpleSecret"
        add_secrets ["firstSecret"; "secondSecret"]
        add_secret ("thirdSecret", store.Key)
    }

vault.Policies

let deployment = arm {
    add_resource vault
    location Location.NorthEurope
}

deployment
|> Writer.quickWrite "output"
