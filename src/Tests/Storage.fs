module Storage

open Expecto
open Farmer
open Farmer.Builders
open Farmer.Storage
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
                sku Premium_LRS
            }
            arm { add_resource account }
            |> findAzureResources<StorageAccount> client.SerializationSettings
            |> List.head

        resource.Validate()
        Expect.equal resource.Name "mystorage123" "Account name is wrong"
        Expect.equal resource.Sku.Name "Premium_LRS" "SKU is wrong"
    }
    test "Creates containers correctly" {
        let resources : BlobContainer list =
            let account = storageAccount {
                name "storage"
                add_blob_container "blob"
                add_private_container "private"
                add_public_container "public"
            }
            [ for i in 1 .. 3 do account |> getResourceAtIndex client.SerializationSettings i ]

        Expect.equal resources.[0].Name "storage/default/blob" "blob name is wrong"
        Expect.equal resources.[0].PublicAccess.Value PublicAccess.Blob "blob access is wrong"
        Expect.equal resources.[1].Name "storage/default/private" "private name is wrong"
        Expect.equal resources.[1].PublicAccess.Value PublicAccess.None "private access is wrong"
        Expect.equal resources.[2].Name "storage/default/public" "public name is wrong"
        Expect.equal resources.[2].PublicAccess.Value PublicAccess.Container "container access is wrong"
    }
    test "Creates file shares correctly" {
        let resources : FileShare list =
            let account = storageAccount {
                name "storage"
                add_file_share "share1"
                add_file_share_with_quota "share2" 1024
            }
            [ for i in 1 .. 2 do account |> getResourceAtIndex client.SerializationSettings i ]

        Expect.equal resources.[0].Name "storage/default/share1" "file share name for 'share1' is wrong"
        Expect.equal resources.[1].Name "storage/default/share2" "file share name for 'share2' is wrong"
        Expect.equal resources.[1].ShareQuota (Nullable 1024) "file share quota for 'share2' is wrong"
    }
    test "Can support hierarchical namespace" {
        let resource =
            let account = storageAccount {
                name "myStorage123~@"
                sku Premium_LRS
                use_hns
            }
            arm { add_resource account }
            |> findAzureResources<StorageAccount> client.SerializationSettings
            |> List.head

        resource.Validate()
        Expect.equal resource.Name "mystorage123" "Account name is wrong"
        Expect.equal resource.Sku.Name "Premium_LRS" "SKU is wrong"
        Expect.equal resource.IsHnsEnabled.Value true "Hierarchical namespace not enabled"
    }
]