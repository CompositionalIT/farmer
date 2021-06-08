module Storage

open Expecto
open Farmer
open Farmer.Builders
open Farmer.Network
open Farmer.Storage
open Microsoft.Azure.Management.Storage
open Microsoft.Azure.Management.Storage.Models
open Microsoft.Rest
open System

/// Client instance needed to get the serializer settings.
let client = new StorageManagementClient(Uri "http://management.azure.com", TokenCredentials "NotNullOrWhiteSpace")
let getStorageResource = findAzureResources<StorageAccount> client.SerializationSettings >> List.head
type CorsResource = {| ``type`` : string; properties : {| cors : {| corsRules : {| allowedHeaders : string array; allowedMethods : string array; allowedOrigins : string array; exposedHeaders : string array; maxAgeInSeconds : int |} array |} |} |}
let findCorsResource typeName x = x |> toTemplate Location.NorthEurope |> Writer.toJson |> Serialization.ofJson<TypedArmTemplate<CorsResource>> |> fun r -> r.Resources |> Seq.find(fun r -> r.``type`` = $"Microsoft.Storage/storageAccounts/%s{typeName}")

let tests = testList "Storage Tests" [
    test "Can create a basic storage account" {
        let resource =
            let account = storageAccount { name "mystorage123" }
            arm { add_resource account } |> getStorageResource

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
    test "Creates tables correctly" {
        let resources : Table list =
            let account = storageAccount {
                name "storage"
                add_table "table1"
                add_tables ["table2"; "table3"]
            }
            [ for i in 1 .. 3 do account |> getResourceAtIndex client.SerializationSettings i ]

        Expect.equal resources.[0].Name "storage/default/table1" "table name for 'table1' is wrong"
        Expect.equal resources.[1].Name "storage/default/table2" "table name for 'table2' is wrong"
        Expect.equal resources.[2].Name "storage/default/table3" "table name for 'table3' is wrong"
    }
    test "Creates queues correctly" {
        let resources : StorageQueue list =
            let account = storageAccount {
                name "storage"
                add_queue "queue1"
                add_queues ["queue2"; "queue3"]
            }
            [ for i in 1 .. 3 do account |> getResourceAtIndex client.SerializationSettings i ]

        Expect.equal resources.[0].Name "storage/default/queue1" "queue name for 'queue1' is wrong"
        Expect.equal resources.[1].Name "storage/default/queue2" "queue name for 'queue2' is wrong"
        Expect.equal resources.[2].Name "storage/default/queue3" "queue name for 'queue3' is wrong"
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
        Expect.equal rule.Definition.Actions.BaseBlob.Delete.DaysAfterModificationGreaterThan (Nullable 7.) "Incorrect policy action"
        Expect.isEmpty rule.Definition.Filters.PrefixMatch "should be no filters"

        let rule = resource.Policy.Rules.[1]
        Expect.equal rule.Definition.Actions.BaseBlob.Delete.DaysAfterModificationGreaterThan (Nullable 1.) "should ignore duplicate actions"
        Expect.equal rule.Definition.Actions.BaseBlob.TierToArchive.DaysAfterModificationGreaterThan (Nullable 2.) "should add multiple actions to a rule"
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
        Expect.equal roleAssignment.Name.Value "105eb550-eb9f-56b6-955d-1def9d3139ec" "Storage Account Name"
        Expect.equal roleAssignment.Scope Farmer.Arm.RoleAssignment.AssignmentScope.ResourceGroup "Scope"
        Expect.sequenceEqual roleAssignment.Dependencies [ uai.ResourceId; builder.ResourceId ] "Role Assignment Dependencies"

        let storage = builder.BuildResources Location.NorthEurope |> List.head :?> Farmer.Arm.Storage.StorageAccount
        Expect.sequenceEqual storage.Dependencies [ uai.ResourceId ] "Storage Dependencies"
    }
    test "WebsitePrimaryEndpoint creation" {
        let builder = storageAccount { name "foo" }

        Expect.equal builder.WebsitePrimaryEndpoint.Value "reference(resourceId('Microsoft.Storage/storageAccounts', 'foo'), '2019-06-01').primaryEndpoints.web" "Zone names are not fixed and should be related to a storage account name"
    }
    test "Creates different SKU kinds correctly" {
        let account = storageAccount { sku (Blobs (BlobReplication.LRS, Some DefaultAccessTier.Hot)) }
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
        let account = storageAccount { default_blob_access_tier DefaultAccessTier.Cool }
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
    test "Restrict by IP" {
        let storage = storageAccount {
            name "onlymyhouse24125"
            restrict_to_ip "8.8.8.8"
            restrict_to_prefix "8.8.8.0/24"
        }
        let generated = arm { add_resource storage; } |> getStorageResource
        Expect.hasLength generated.NetworkRuleSet.IpRules 2 "Wrong number of IP rules"
        Expect.containsAll (generated.NetworkRuleSet.IpRules |> Seq.map (fun rule -> rule.IPAddressOrRange)) [ "8.8.8.8"; "8.8.8.0/24" ] "Missing IP rules"
    }
    test "Restrict to vnet" {
        let vnetName = "my-vnet"
        let servicesSubnet = "services"
        let containerSubnet = "containers"
        let storage = storageAccount {
            name "onlymynet"
            restrict_to_subnet vnetName servicesSubnet
            restrict_to_subnet vnetName containerSubnet
        }
        let generatedStorage = arm { add_resource storage; } |> getStorageResource
        Expect.hasLength generatedStorage.NetworkRuleSet.VirtualNetworkRules 2 "Wrong number of vnet rules"
        let allowedSubnets = [
            (Arm.Network.subnets.resourceId (ResourceName vnetName, ResourceName servicesSubnet)).ArmExpression.Eval()
            (Arm.Network.subnets.resourceId (ResourceName vnetName, ResourceName containerSubnet)).ArmExpression.Eval()
        ]
        Expect.containsAll allowedSubnets (generatedStorage.NetworkRuleSet.VirtualNetworkRules |> Seq.map (fun rule -> rule.VirtualNetworkResourceId)) "Missing subnet rules"
    }
    test "Sets CORS correctly" {
        let account = storageAccount {
            add_cors_rules [
                StorageService.Blobs, CorsRule.AllowAll
                StorageService.Blobs, { CorsRule.AllowAll with AllowedOrigins = Specific [ Uri "https://compositional-it.com" ] }
                StorageService.Queues, CorsRule.create([ "https://compositional-it.com" ], [ GET ], 15, [ "exposed1"; "exposed2" ], [ "ALLOWED1"; "ALLOWED2" ] )
            ]
        }

        let rules = (account |> findCorsResource "blobServices").properties.cors.corsRules
        Expect.equal 2 rules.Length "Incorrect number of CORS rules"

        let blobAllowAllRule = rules.[0]
        Expect.equal [| "*" |] blobAllowAllRule.allowedHeaders "Incorrect default headers"
        Expect.equal (HttpMethod.All.Value |> List.map string |> List.toArray) blobAllowAllRule.allowedMethods "Incorrect default methods"
        Expect.equal [| "*" |] blobAllowAllRule.allowedOrigins "Incorrect default origin"
        Expect.equal [| "*" |] blobAllowAllRule.exposedHeaders "Incorrect default exposed headers"
        Expect.equal 0 blobAllowAllRule.maxAgeInSeconds "Incorrect default max age is seconds"

        let blobSpecificRule = rules.[1]
        Expect.equal [| "https://compositional-it.com/" |] blobSpecificRule.allowedOrigins "Incorrect custom allowed origin"

        let queueRule = (account |> findCorsResource "queueServices").properties.cors.corsRules |> Seq.exactlyOne
        Expect.equal [| "ALLOWED1"; "ALLOWED2" |] queueRule.allowedHeaders "Incorrect factory headers"
        Expect.equal [| string GET |] queueRule.allowedMethods "Incorrect factory methods"
        Expect.equal [| "https://compositional-it.com/" |] queueRule.allowedOrigins "Incorrect factory origin"
        Expect.equal [| "exposed1"; "exposed2" |] queueRule.exposedHeaders "Incorrect factory exposed headers"
        Expect.equal 15 queueRule.maxAgeInSeconds "Incorrect factory max age is seconds"
    }
]
