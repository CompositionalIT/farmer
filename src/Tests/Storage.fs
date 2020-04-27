module Storage

open Expecto
open Farmer
open Farmer.Resources
open Microsoft.Azure.Management.Storage
open Microsoft.Azure.Management.Storage.Models
open Microsoft.Rest
open System

/// Client instance needed to get the serializer settings.
let client = new StorageManagementClient(Uri "http://management.azure.com", TokenCredentials "NotNullOrWhiteSpace")
let tests = testList "Storage Tests" [
    test "Can create a basic storage account" {
        let resource =
            let account = storageAccount {
                name "myStorage123~@"
                sku Sku.PremiumLRS
            }
            arm { add_resource account }
            |> findAzureResources<StorageAccount> client.SerializationSettings
            |> List.head

        resource.Validate()
        Expect.equal resource.Name "mystorage123" ""
        Expect.equal resource.Sku.Name "Premium_LRS" ""
    }
]