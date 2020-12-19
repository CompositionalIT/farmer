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
let getStorageResource = findAzureResources<StorageAccount> client.SerializationSettings >> List.head

let tests = testList "Storage Tests" [
    test "Can create a basic storage account" {
        let resource =
            let account = storageAccount {
                name "mystorage123"
            }
            arm { add_resource account }
            |> getStorageResource

        resource.Validate()
        Expect.equal resource.Name "mystorage123" "Account name is wrong"
        Expect.equal resource.Sku.Name "Standard_LRS" "SKU is wrong"
        Expect.equal resource.Kind "StorageV2" "Kind"
        Expect.equal resource.IsHnsEnabled (Nullable<bool>()) "Hierarchical namespace shouldn't be included"
    }
    test "Data lake is not enabled by default" {
        let resource =
            let account = storageAccount {
                name "mystorage123"
                sku Sku.Premium_LRS
                enable_data_lake true
            }
            arm { add_resource account }
            |> getStorageResource

        resource.Validate()
        Expect.equal resource.Sku.Name "Premium_LRS" "SKU is wrong"
        Expect.isTrue resource.IsHnsEnabled.Value "Hierarchical namespace not enabled"
    }
    test "When data lake can be disabled" {
        let resource =
            let account = storageAccount {
                name "mystorage123"
                enable_data_lake false
            }
            arm { add_resource account }
            |> getStorageResource

        resource.Validate()
        Expect.isFalse resource.IsHnsEnabled.Value "Hierarchical namespace should be false"
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
                add_file_share_with_quota "share2" 1024<Gb>
            }
            [ for i in 1 .. 2 do account |> getResourceAtIndex client.SerializationSettings i ]

        Expect.equal resources.[0].Name "storage/default/share1" "file share name for 'share1' is wrong"
        Expect.equal resources.[1].Name "storage/default/share2" "file share name for 'share2' is wrong"
        Expect.equal resources.[1].ShareQuota (Nullable 1024) "file share quota for 'share2' is wrong"
    }
    test "Rejects invalid storage accounts" {
        let check (v:string) m = Expect.equal (StorageAccountName.Create v) (Error ("Storage account names " + m))

        check "" "cannot be empty" "Name too short"
        check "zz" "min length is 3, but here is 2 ('zz')" "Name too short"
        check "abcdefghij1234567890abcde" "max length is 24, but here is 25 ('abcdefghij1234567890abcde')" "Name too long"
        check "zzzT" "can only contain lowercase letters ('zzzT')" "Upper case character allowed"
        check "zzz!" "can only contain alphanumeric characters ('zzz!')" "Non alpha numeric character allowed"
        Expect.equal (StorageResourceName.Create("abcdefghij1234567890abcd").OkValue.ResourceName) (ResourceName "abcdefghij1234567890abcd") "Should have created a valid storage account name"
    }
    test "Rejects invalid storage resource names" {
        let check (v:string) m = Expect.equal (StorageResourceName.Create v) (Error ("Storage resource names " + m))

        check "" "cannot be empty" "Name too short"
        check "zz" "min length is 3, but here is 2 ('zz')" "Name too short"
        let longName = Array.init 64 (fun _ -> 'a') |> String
        check longName ("max length is 63, but here is 64 ('" + longName + "')") "Name too long"
        check "zzzT" "can only contain lowercase letters ('zzzT')" "Upper case character allowed"
        check "zz!z" "can only contain alphanumeric characters or the dash ('zz!z')" "Bad character allowed"
        check "zzz--z" "do not allow consecutive dashes ('zzz--z')" "Double dash allowed"
        check "-zz" "must start with an alphanumeric character ('-zz')" "Start with dash"
        check "zz-" "must end with an alphanumeric character ('zz-')" "End with dash"

        Expect.equal (StorageResourceName.Create("abcdefghij1234567890abcd").OkValue.ResourceName) (ResourceName "abcdefghij1234567890abcd") "Should have created a valid storage resource name"
    }
    test "Adds lifecycle policies correctly" {
        let resource : ManagementPolicy =
            let account = storageAccount {
                name "storage"
                add_lifecycle_rule "cleanup" [ Storage.DeleteAfter 7<Days> ] Storage.NoRuleFilters
                add_lifecycle_rule "test" [ Storage.DeleteAfter 1<Days>; Storage.DeleteAfter 2<Days>; Storage.ArchiveAfter 2<Days>; ] [ "foo/bar" ]
            }
            account |> getResourceAtIndex client.SerializationSettings 1

        Expect.equal resource.Name "storage/default" "policy name for is wrong"
        Expect.hasLength resource.Policy.Rules 2 "Should be two rules"

        let rule = resource.Policy.Rules.[0]
        Expect.equal rule.Name "cleanup" "rule name is wrong"
        Expect.equal rule.Definition.Actions.BaseBlob.Delete.DaysAfterModificationGreaterThan 7. "Incorrect policy action"
        Expect.isEmpty rule.Definition.Filters.PrefixMatch "should be no filters"

        let rule = resource.Policy.Rules.[1]
        Expect.equal rule.Definition.Actions.BaseBlob.Delete.DaysAfterModificationGreaterThan 1. "should ignore duplicate actions"
        Expect.equal rule.Definition.Actions.BaseBlob.TierToArchive.DaysAfterModificationGreaterThan 2. "should add multiple actions to a rule"
        Expect.equal (rule.Definition.Filters.PrefixMatch |> Seq.toList) [ "foo/bar" ] "incorrect filter"
    }
    test "Creates connection strings correctly" {
        let strongConn = StorageAccount.getConnectionString (StorageAccountName.Create("account").OkValue)
        let rgConn = StorageAccount.getConnectionString (StorageAccountName.Create("account").OkValue, "rg")

        Expect.equal "concat('DefaultEndpointsProtocol=https;AccountName=account;AccountKey=', listKeys(resourceId('Microsoft.Storage/storageAccounts', 'account'), '2017-10-01').keys[0].value)" strongConn.Value "Strong connection string"
        Expect.equal "concat('DefaultEndpointsProtocol=https;AccountName=account;AccountKey=', listKeys(resourceId('rg', 'Microsoft.Storage/storageAccounts', 'account'), '2017-10-01').keys[0].value)" rgConn.Value "Complex connection string"
    }
    test "Creates Role Assignment correctly" {
        let uai = UserAssignedIdentity.createUserAssignedIdentity "user"
        let builder = storageAccount { name "foo"; grant_access uai Roles.StorageBlobDataOwner } :> IBuilder
        let roleAssignment = builder.BuildResources Location.NorthEurope |> List.last :?> Farmer.Arm.RoleAssignment.RoleAssignment
        Expect.equal roleAssignment.PrincipalId uai.PrincipalId "PrincipalId"
        Expect.equal roleAssignment.RoleDefinitionId Roles.StorageBlobDataOwner "RoleId"
        let expectedRoleAssignmentName = "efad7c9d-881a-5ca8-9177-eb1c95550036" // Deterministic guid for this input.
        Expect.equal roleAssignment.Name.Value expectedRoleAssignmentName "Storage Account Name"

        let storage = builder.BuildResources Location.NorthEurope |> List.head :?> Farmer.Arm.Storage.StorageAccount

        Expect.sequenceEqual storage.Dependencies [ uai.ResourceId ] "ResourceId"
    }
    test "WebsitePrimaryEndpoint creation" {
        let builder = storageAccount { name "foo" }

        Expect.equal builder.WebsitePrimaryEndpoint.Value "reference(resourceId('Microsoft.Storage/storageAccounts', 'foo'), '2019-06-01').primaryEndpoints.web" "Zone names are not fixed and should be related to a storage account name"
    }
    test "Creates different SKU kinds correctly" {
        let account = storageAccount { sku (Blobs (BlobReplication.LRS, Some Hot)) }
        let resource = arm { add_resource account } |> getStorageResource
        Expect.equal resource.Kind "BlobStorage" "Kind"
        Expect.equal resource.AccessTier (Nullable AccessTier.Hot) "Access Tier"
        Expect.equal resource.Sku.Name "Standard_LRS" "Sku Name"

        let account = storageAccount { sku (Files BasicReplication.ZRS) }
        let resource = arm { add_resource account } |> getStorageResource
        Expect.equal resource.Kind "FileStorage" "Kind"
        Expect.equal resource.Sku.Name "Premium_ZRS" "Sku Name"

        let account = storageAccount { sku (BlockBlobs BasicReplication.LRS) }
        let resource = arm { add_resource account } |> getStorageResource
        Expect.equal resource.Kind "BlockBlobStorage" "Kind"
        Expect.equal resource.Sku.Name "Premium_LRS" "Sku Name"

        let account = storageAccount { sku (GeneralPurpose (V1 V1Replication.RAGRS)) }
        let resource = arm { add_resource account } |> getStorageResource
        Expect.equal resource.Kind "Storage" "Kind"
        Expect.equal resource.Sku.Name "Standard_RAGRS" "Sku Name"

        let account = storageAccount { sku (GeneralPurpose (V2 (V2Replication.LRS Premium, Some Cool))) }
        let resource = arm { add_resource account } |> getStorageResource
        Expect.equal resource.Kind "StorageV2" "Kind"
        Expect.equal resource.Sku.Name "Premium_LRS" "Sku Name"
    }
    test "Sets blob access tier correctly different SKU kinds correctly" {
        let account = storageAccount { default_blob_access_tier Cool }
        let resource = arm { add_resource account } |> getStorageResource
        Expect.equal resource.AccessTier (Nullable AccessTier.Cool) "Access Tier"

        let account = storageAccount { default_blob_access_tier Hot }
        let resource = arm { add_resource account } |> getStorageResource
        Expect.equal resource.AccessTier (Nullable AccessTier.Hot) "Access Tier"

        let account = storageAccount { sku (GeneralPurpose (V2 (V2Replication.LRS Premium, None))) }
        let resource = arm { add_resource account } |> getStorageResource
        Expect.equal resource.AccessTier (Nullable ()) "Access Tier"

        let account = storageAccount { sku (Blobs (BlobReplication.LRS, Some Hot)) }
        let resource = arm { add_resource account } |> getStorageResource
        Expect.equal resource.AccessTier (Nullable AccessTier.Hot) "Access Tier"
    }
    test "Setting default access tier with incompatible sku throws an exception" {
        Expect.throws
            (fun _ ->
                storageAccount {
                    sku (BlockBlobs BasicReplication.LRS)
                    default_blob_access_tier Cool
                } |> ignore)
            "Can't set default tier for  Block Blobs"
    }
]