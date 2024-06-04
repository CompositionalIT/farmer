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
let client =
    new StorageManagementClient(Uri "http://management.azure.com", TokenCredentials "NotNullOrWhiteSpace")

let getStorageResource =
    findAzureResources<StorageAccount> client.SerializationSettings >> List.head

type PropertiesResource =
    {| ``type``: string
       properties: {| cors: {| corsRules: {| allowedHeaders: string array
                                             allowedMethods: string array
                                             allowedOrigins: string array
                                             exposedHeaders: string array
                                             maxAgeInSeconds: int |} array |}
                      IsVersioningEnabled: bool
                      deleteRetentionPolicy: {| enabled: bool; days: int |}
                      restorePolicy: {| enabled: bool; days: int |}
                      containerDeleteRetentionPolicy: {| enabled: bool; days: int |}
                      lastAccessTimeTrackingPolicy: {| enable: bool
                                                       name: string
                                                       trackingGranularityInDays: int
                                                       blobType: string[] |}
                      changeFeed: {| enabled: bool
                                     retentionInDays: int |} |} |}

let findPropertiesResource typeName x =
    x
    |> toTemplate Location.NorthEurope
    |> Writer.toJson
    |> Serialization.ofJson<TypedArmTemplate<PropertiesResource>>
    |> fun r ->
        r.Resources
        |> Seq.find (fun r -> r.``type`` = $"Microsoft.Storage/storageAccounts/%s{typeName}")

let tests =
    testList
        "Storage Tests"
        [
            test "Can create a basic storage account" {
                let resource =
                    let account = storageAccount { name "mystorage123" }
                    arm { add_resource account } |> getStorageResource

                resource.Validate()
                Expect.equal resource.Name "mystorage123" "Account name is wrong"
                Expect.equal resource.Sku.Name "Standard_LRS" "SKU is wrong"
                Expect.equal resource.Kind "StorageV2" "Kind"
                Expect.equal resource.IsHnsEnabled (Nullable<bool>()) "Hierarchical namespace shouldn't be included"
                Expect.equal resource.MinimumTlsVersion null "Minimum TLS version shouldn't be included"
            }
            test "Data lake is not enabled by default" {
                let resource =
                    let account =
                        storageAccount {
                            name "mystorage123"
                            sku Sku.Premium_LRS
                            enable_data_lake true
                        }

                    arm { add_resource account } |> getStorageResource

                resource.Validate()
                Expect.equal resource.Sku.Name "Premium_LRS" "SKU is wrong"
                Expect.isTrue resource.IsHnsEnabled.Value "Hierarchical namespace not enabled"
            }
            test "When data lake can be disabled" {
                let resource =
                    let account =
                        storageAccount {
                            name "mystorage123"
                            enable_data_lake false
                        }

                    arm { add_resource account } |> getStorageResource

                resource.Validate()
                Expect.isFalse resource.IsHnsEnabled.Value "Hierarchical namespace should be false"
            }
            test "Creates containers correctly" {
                let resources: BlobContainer list =
                    let account =
                        storageAccount {
                            name "storage"
                            add_blob_container "blob"
                            add_private_container "private"
                            add_public_container "public"
                        }

                    [
                        for i in 1..3 do
                            account |> getResourceAtIndex client.SerializationSettings i
                    ]

                Expect.equal resources.[0].Name "storage/default/blob" "blob name is wrong"
                Expect.equal resources.[0].PublicAccess.Value PublicAccess.Blob "blob access is wrong"
                Expect.equal resources.[1].Name "storage/default/private" "private name is wrong"
                Expect.equal resources.[1].PublicAccess.Value PublicAccess.None "private access is wrong"
                Expect.equal resources.[2].Name "storage/default/public" "public name is wrong"
                Expect.equal resources.[2].PublicAccess.Value PublicAccess.Container "container access is wrong"
            }
            test "Creates file shares correctly" {
                let resources: FileShare list =
                    let account =
                        storageAccount {
                            name "storage"
                            add_file_share "share1"
                            add_file_share_with_quota "share2" 1024<Gb>
                        }

                    [
                        for i in 1..2 do
                            account |> getResourceAtIndex client.SerializationSettings i
                    ]

                Expect.equal resources.[0].Name "storage/default/share1" "file share name for 'share1' is wrong"
                Expect.equal resources.[1].Name "storage/default/share2" "file share name for 'share2' is wrong"
                Expect.equal resources.[1].ShareQuota (Nullable 1024) "file share quota for 'share2' is wrong"
            }
            test "Creates tables correctly" {
                let resources: Table list =
                    let account =
                        storageAccount {
                            name "storage"
                            add_table "table1"
                            add_tables [ "table2"; "table3" ]
                        }

                    [
                        for i in 1..3 do
                            account |> getResourceAtIndex client.SerializationSettings i
                    ]

                Expect.equal resources.[0].Name "storage/default/table1" "table name for 'table1' is wrong"
                Expect.equal resources.[1].Name "storage/default/table2" "table name for 'table2' is wrong"
                Expect.equal resources.[2].Name "storage/default/table3" "table name for 'table3' is wrong"
            }
            testList
                "Storage Queue Tests"
                [
                    test "Creates queues correctly" {
                        let resources: StorageQueue list =
                            let queue =
                                storageQueue {
                                    name "queue4"
                                    metadata [ "environment", "dev"; "source", "image" ]
                                }

                            let account =
                                storageAccount {
                                    name "storage"
                                    add_queue "queue1"
                                    add_queues [ storageQueue { name "queue2" }; storageQueue { name "queue3" } ]
                                    add_queue queue
                                }

                            [
                                for i in 1..3 do
                                    account |> getResourceAtIndex client.SerializationSettings i
                            ]

                        Expect.equal resources.[0].Name "storage/default/queue1" "queue name for 'queue1' is wrong"
                        Expect.equal resources.[1].Name "storage/default/queue2" "queue name for 'queue2' is wrong"
                        Expect.equal resources.[2].Name "storage/default/queue3" "queue name for 'queue3' is wrong"
                    }
                    test "Metadata is added correctly to single queue" {
                        let resource: StorageQueue =
                            let account =
                                storageAccount {
                                    name "storage"

                                    add_queue (
                                        storageQueue {
                                            name "queue1"
                                            metadata [ "environment", "dev"; "project", "farmer" ]
                                        }
                                    )
                                }

                            account |> getResourceAtIndex client.SerializationSettings 1

                        Expect.equal resource.Name "storage/default/queue1" "queue name for 'queue1' is wrong"

                        Expect.containsAll
                            resource.Metadata
                            (seq [ ("environment", "dev"); ("project", "farmer") ] |> dict)
                            "Metadata not set correctly"
                    }
                    test "Metadata is added correctly to multiple queues" {
                        let resources: StorageQueue list =
                            let account =
                                storageAccount {
                                    name "storage"

                                    add_queues
                                        [
                                            storageQueue {
                                                name "queue1"
                                                metadata [ "environment", "dev"; "project", "farmer" ]
                                            }
                                            storageQueue {
                                                name "queue2"
                                                metadata [ "environment", "test"; "project", "barnyard" ]
                                            }
                                        ]

                                    add_queues
                                        [ storageQueue { name "queue3" }; storageQueue { name "queue4" } ]
                                        [ "environment", "test"; "project", "barnyard" ]
                                }

                            [
                                for i in 1..2 do
                                    account |> getResourceAtIndex client.SerializationSettings i
                            ]

                        Expect.equal resources.[0].Name "storage/default/queue1" "queue name for 'queue1' is wrong"
                        Expect.equal resources.[1].Name "storage/default/queue2" "queue name for 'queue2' is wrong"

                        let queue1Metadata = seq [ ("environment", "dev"); ("project", "farmer") ]
                        let queue2Metadata = seq [ ("environment", "test"); ("project", "barnyard") ]

                        Expect.containsAll
                            resources.[0].Metadata
                            (queue1Metadata |> dict)
                            "Metadata not set correctly for queue1"

                        Expect.containsAll
                            resources.[1].Metadata
                            (queue2Metadata |> dict)
                            "Metadata not set correctly for queue2"
                    }
                    test "Metadata is added correctly to multiple queues when same for all" {
                        let resources: StorageQueue list =
                            let account =
                                storageAccount {
                                    name "storage"

                                    add_queues
                                        [
                                            storageQueue {
                                                name "queue1"
                                                metadata [ "environment", "dev"; "project", "farmer" ]
                                            }
                                            storageQueue {
                                                name "queue2"
                                                metadata [ "environment", "dev"; "project", "farmer" ]
                                            }
                                        ]
                                }

                            [
                                for i in 1..2 do
                                    account |> getResourceAtIndex client.SerializationSettings i
                            ]

                        Expect.equal resources.[0].Name "storage/default/queue1" "queue name for 'queue1' is wrong"
                        Expect.equal resources.[1].Name "storage/default/queue2" "queue name for 'queue2' is wrong"

                        let queueMetadata = seq [ ("environment", "dev"); ("project", "farmer") ]

                        Expect.containsAll
                            resources.[0].Metadata
                            (queueMetadata |> dict)
                            "Metadata not set correctly for queue1"

                        Expect.containsAll
                            resources.[1].Metadata
                            (queueMetadata |> dict)
                            "Metadata not set correctly for queue2"
                    }
                ]
            test "Rejects invalid storage accounts" {
                let check (v: string) m =
                    Expect.equal (StorageAccountName.Create v) (Error("Storage account names " + m))

                check "" "cannot be empty" "Name too short"
                check "zz" "min length is 3, but here is 2. The invalid value is 'zz'" "Name too short"

                check
                    "abcdefghij1234567890abcde"
                    "max length is 24, but here is 25. The invalid value is 'abcdefghij1234567890abcde'"
                    "Name too long"

                check
                    "zzzT"
                    "can only contain lowercase letters. The invalid value is 'zzzT'"
                    "Upper case character allowed"

                check
                    "zzz!"
                    "can only contain alphanumeric characters. The invalid value is 'zzz!'"
                    "Non alpha numeric character allowed"

                Expect.equal
                    (StorageResourceName.Create("abcdefghij1234567890abcd").OkValue.ResourceName)
                    (ResourceName "abcdefghij1234567890abcd")
                    "Should have created a valid storage account name"
            }
            test "Rejects invalid storage resource names" {
                let check (v: string) m =
                    Expect.equal (StorageResourceName.Create v) (Error("Storage resource names " + m))

                check "" "cannot be empty" "Name too short"
                check "zz" "min length is 3, but here is 2. The invalid value is 'zz'" "Name too short"
                let longName = Array.init 64 (fun _ -> 'a') |> String
                check longName $"max length is 63, but here is 64. The invalid value is '{longName}'" "Name too long"

                check
                    "zzzT"
                    "can only contain lowercase letters. The invalid value is 'zzzT'"
                    "Upper case character allowed"

                check
                    "zz!z"
                    "can only contain alphanumeric characters or the dash (-). The invalid value is 'zz!z'"
                    "Bad character allowed"

                check "zzz--z" "do not allow consecutive dashes. The invalid value is 'zzz--z'" "Double dash allowed"
                check "-zz" "must start with an alphanumeric character. The invalid value is '-zz'" "Start with dash"
                check "zz-" "must end with an alphanumeric character. The invalid value is 'zz-'" "End with dash"

                Expect.equal
                    (StorageResourceName.Create("abcdefghij1234567890abcd").OkValue.ResourceName)
                    (ResourceName "abcdefghij1234567890abcd")
                    "Should have created a valid storage resource name"
            }
            test "Adds lifecycle policies correctly" {
                let resource: ManagementPolicy =
                    let account =
                        storageAccount {
                            name "storage"
                            add_lifecycle_rule "cleanup" [ Storage.DeleteAfter 7<Days> ] Storage.NoRuleFilters

                            add_lifecycle_rule
                                "test"
                                [
                                    Storage.DeleteAfter 1<Days>
                                    Storage.DeleteAfter 2<Days>
                                    Storage.ArchiveAfter 2<Days>
                                ]
                                [ "foo/bar" ]
                        }

                    account |> getResourceAtIndex client.SerializationSettings 1

                Expect.equal resource.Name "storage/default" "policy name for is wrong"
                Expect.hasLength resource.Policy.Rules 2 "Should be two rules"

                let rule = resource.Policy.Rules.[0]
                Expect.equal rule.Name "cleanup" "rule name is wrong"

                Expect.equal
                    rule.Definition.Actions.BaseBlob.Delete.DaysAfterModificationGreaterThan
                    (Nullable 7.)
                    "Incorrect policy action"

                Expect.isEmpty rule.Definition.Filters.PrefixMatch "should be no filters"

                let rule = resource.Policy.Rules.[1]

                Expect.equal
                    rule.Definition.Actions.BaseBlob.Delete.DaysAfterModificationGreaterThan
                    (Nullable 1.)
                    "should ignore duplicate actions"

                Expect.equal
                    rule.Definition.Actions.BaseBlob.TierToArchive.DaysAfterModificationGreaterThan
                    (Nullable 2.)
                    "should add multiple actions to a rule"

                Expect.equal (rule.Definition.Filters.PrefixMatch |> Seq.toList) [ "foo/bar" ] "incorrect filter"
            }
            test "Creates connection strings correctly" {
                let strongConn =
                    StorageAccount.getConnectionString (StorageAccountName.Create("account").OkValue)

                let rgConn =
                    StorageAccount.getConnectionString (StorageAccountName.Create("account").OkValue, "rg")

                Expect.equal
                    "concat('DefaultEndpointsProtocol=https;AccountName=account;AccountKey=', listKeys(resourceId('Microsoft.Storage/storageAccounts', 'account'), '2017-10-01').keys[0].value, ';EndpointSuffix=', environment().suffixes.storage)"
                    strongConn.Value
                    "Strong connection string"

                Expect.equal
                    "concat('DefaultEndpointsProtocol=https;AccountName=account;AccountKey=', listKeys(resourceId('rg', 'Microsoft.Storage/storageAccounts', 'account'), '2017-10-01').keys[0].value, ';EndpointSuffix=', environment().suffixes.storage)"
                    rgConn.Value
                    "Complex connection string"
            }
            test "Creates Role Assignment correctly" {
                let uai = UserAssignedIdentity.createUserAssignedIdentity "user"

                let builder =
                    storageAccount {
                        name "foo"
                        grant_access uai Roles.StorageBlobDataOwner
                    }
                    :> IBuilder

                let roleAssignment =
                    builder.BuildResources Location.NorthEurope |> List.last
                    :?> Farmer.Arm.RoleAssignment.RoleAssignment

                Expect.equal roleAssignment.PrincipalId uai.PrincipalId "PrincipalId"
                Expect.equal roleAssignment.RoleDefinitionId Roles.StorageBlobDataOwner "RoleId"
                Expect.equal roleAssignment.Name.Value "105eb550-eb9f-56b6-955d-1def9d3139ec" "Storage Account Name"

                Expect.equal
                    roleAssignment.Scope
                    (Farmer.Arm.RoleAssignment.AssignmentScope.SpecificResource builder.ResourceId)
                    "Scope"

                Expect.sequenceEqual
                    roleAssignment.Dependencies
                    [ uai.ResourceId; builder.ResourceId ]
                    "Role Assignment Dependencies"

                let storage =
                    builder.BuildResources Location.NorthEurope |> List.head :?> Farmer.Arm.Storage.StorageAccount

                Expect.sequenceEqual storage.Dependencies [ uai.ResourceId ] "Storage Dependencies"
            }
            test "WebsitePrimaryEndpoint creation" {
                let builder = storageAccount { name "foo" }

                Expect.equal
                    builder.WebsitePrimaryEndpoint.Value
                    "reference(resourceId('Microsoft.Storage/storageAccounts', 'foo'), '2022-05-01').primaryEndpoints.web"
                    "Zone names are not fixed and should be related to a storage account name"
            }
            test "Creates different SKU kinds correctly" {
                let account =
                    storageAccount {
                        name "storage"
                        sku (Blobs(BlobReplication.LRS, Some DefaultAccessTier.Hot))
                    }

                let resource = arm { add_resource account } |> getStorageResource
                Expect.equal resource.Kind "BlobStorage" "Kind"
                Expect.equal resource.AccessTier (Nullable AccessTier.Hot) "Access Tier"
                Expect.equal resource.Sku.Name "Standard_LRS" "Sku Name"

                let account =
                    storageAccount {
                        name "storage"
                        sku (Files BasicReplication.ZRS)
                    }

                let resource = arm { add_resource account } |> getStorageResource
                Expect.equal resource.Kind "FileStorage" "Kind"
                Expect.equal resource.Sku.Name "Premium_ZRS" "Sku Name"

                let account =
                    storageAccount {
                        name "storage"
                        sku (BlockBlobs BasicReplication.LRS)
                    }

                let resource = arm { add_resource account } |> getStorageResource
                Expect.equal resource.Kind "BlockBlobStorage" "Kind"
                Expect.equal resource.Sku.Name "Premium_LRS" "Sku Name"

                let account =
                    storageAccount {
                        name "storage"
                        sku (GeneralPurpose(V1 V1Replication.RAGRS))
                    }

                let resource = arm { add_resource account } |> getStorageResource
                Expect.equal resource.Kind "Storage" "Kind"
                Expect.equal resource.Sku.Name "Standard_RAGRS" "Sku Name"

                let account =
                    storageAccount {
                        name "storage"
                        sku (GeneralPurpose(V2(V2Replication.LRS Premium, Some Cool)))
                    }

                let resource = arm { add_resource account } |> getStorageResource
                Expect.equal resource.Kind "StorageV2" "Kind"
                Expect.equal resource.Sku.Name "Premium_LRS" "Sku Name"
            }
            test "Sets blob access tier correctly different SKU kinds correctly" {
                let account =
                    storageAccount {
                        name "storage"
                        default_blob_access_tier DefaultAccessTier.Cool
                    }

                let resource = arm { add_resource account } |> getStorageResource
                Expect.equal resource.AccessTier (Nullable AccessTier.Cool) "Access Tier"

                let account =
                    storageAccount {
                        name "storage"
                        default_blob_access_tier Hot
                    }

                let resource = arm { add_resource account } |> getStorageResource
                Expect.equal resource.AccessTier (Nullable AccessTier.Hot) "Access Tier"

                let account =
                    storageAccount {
                        name "storage"
                        sku (GeneralPurpose(V2(V2Replication.LRS Premium, None)))
                    }

                let resource = arm { add_resource account } |> getStorageResource
                Expect.equal resource.AccessTier (Nullable()) "Access Tier"

                let account =
                    storageAccount {
                        name "storage"
                        sku (Blobs(BlobReplication.LRS, Some Hot))
                    }

                let resource = arm { add_resource account } |> getStorageResource
                Expect.equal resource.AccessTier (Nullable AccessTier.Hot) "Access Tier"
            }
            test "Setting default access tier with incompatible sku throws an exception" {
                Expect.throws
                    (fun _ ->
                        storageAccount {
                            name "storage"
                            sku (BlockBlobs BasicReplication.LRS)
                            default_blob_access_tier Cool
                        }
                        |> ignore)
                    "Can't set default tier for  Block Blobs"
            }
            test "Restrict by IP" {
                let storage =
                    storageAccount {
                        name "onlymyhouse24125"
                        restrict_to_ip "8.8.8.8"
                        restrict_to_ips [ "1.2.3.4" ]
                        restrict_to_prefix "8.8.8.0/24"
                    }

                let generated = arm { add_resource storage } |> getStorageResource
                Expect.hasLength generated.NetworkRuleSet.IpRules 3 "Wrong number of IP rules"

                Expect.containsAll
                    (generated.NetworkRuleSet.IpRules |> Seq.map (fun rule -> rule.IPAddressOrRange))
                    [ "8.8.8.8"; "1.2.3.4"; "8.8.8.0/24" ]
                    "Missing IP rules"
            }
            test "Restrict to vnet" {
                let vnetName = "my-vnet"
                let servicesSubnet = "services"
                let containerSubnet = "containers"

                let storage =
                    storageAccount {
                        name "onlymynet"
                        restrict_to_subnet vnetName servicesSubnet
                        restrict_to_subnet vnetName containerSubnet
                    }

                let generatedStorage = arm { add_resource storage } |> getStorageResource
                Expect.hasLength generatedStorage.NetworkRuleSet.VirtualNetworkRules 2 "Wrong number of vnet rules"

                let allowedSubnets =
                    [
                        (Arm.Network.subnets.resourceId (ResourceName vnetName, ResourceName servicesSubnet))
                            .ArmExpression.Eval()
                        (Arm.Network.subnets.resourceId (ResourceName vnetName, ResourceName containerSubnet))
                            .ArmExpression.Eval()
                    ]

                Expect.containsAll
                    allowedSubnets
                    (generatedStorage.NetworkRuleSet.VirtualNetworkRules
                     |> Seq.map (fun rule -> rule.VirtualNetworkResourceId))
                    "Missing subnet rules"
            }
            test "Sets CORS correctly" {
                let account =
                    storageAccount {
                        name "storage"

                        add_cors_rules
                            [
                                StorageService.Blobs, CorsRule.AllowAll
                                StorageService.Blobs,
                                { CorsRule.AllowAll with
                                    AllowedOrigins = Specific [ Uri "https://compositional-it.com" ]
                                }
                                StorageService.Queues,
                                CorsRule.create (
                                    [ "https://compositional-it.com" ],
                                    [ GET ],
                                    15,
                                    [ "exposed1"; "exposed2" ],
                                    [ "ALLOWED1"; "ALLOWED2" ]
                                )
                            ]
                    }

                let rules =
                    (account |> findPropertiesResource "blobServices").properties.cors.corsRules

                Expect.equal rules.Length 2 "Incorrect number of CORS rules"

                let blobAllowAllRule = rules.[0]
                Expect.equal [| "*" |] blobAllowAllRule.allowedHeaders "Incorrect default headers"

                Expect.equal
                    (HttpMethod.All.Value |> List.map string |> List.toArray)
                    blobAllowAllRule.allowedMethods
                    "Incorrect default methods"

                Expect.equal [| "*" |] blobAllowAllRule.allowedOrigins "Incorrect default origin"
                Expect.equal [| "*" |] blobAllowAllRule.exposedHeaders "Incorrect default exposed headers"
                Expect.equal 0 blobAllowAllRule.maxAgeInSeconds "Incorrect default max age is seconds"

                let blobSpecificRule = rules.[1]

                Expect.isTrue (not <| blobSpecificRule.allowedOrigins.[0].EndsWith('/')) "Should not add trailing slash"

                Expect.equal
                    blobSpecificRule.allowedOrigins
                    [| "https://compositional-it.com" |]
                    "Incorrect custom allowed origin"

                let queueRule =
                    (account |> findPropertiesResource "queueServices").properties.cors.corsRules
                    |> Seq.exactlyOne

                Expect.equal [| "ALLOWED1"; "ALLOWED2" |] queueRule.allowedHeaders "Incorrect factory headers"
                Expect.equal [| string GET |] queueRule.allowedMethods "Incorrect factory methods"
                Expect.equal queueRule.allowedOrigins [| "https://compositional-it.com" |] "Incorrect factory origin"
                Expect.equal [| "exposed1"; "exposed2" |] queueRule.exposedHeaders "Incorrect factory exposed headers"
                Expect.equal 15 queueRule.maxAgeInSeconds "Incorrect factory max age is seconds"
            }

            test "Policies" {
                let account =
                    storageAccount {
                        name "storage"

                        add_policies
                            [
                                StorageService.Blobs,
                                [
                                    Policy.Restore { Enabled = true; Days = 5 }
                                    Policy.DeleteRetention { Enabled = true; Days = 10 }
                                    Policy.LastAccessTimeTracking
                                        {
                                            Enabled = true
                                            TrackingGranularityInDays = 12
                                        }
                                    Policy.ContainerDeleteRetention { Enabled = true; Days = 11 }
                                    Policy.ChangeFeed { Enabled = true; RetentionInDays = 30 }
                                ]
                            ]
                    }

                let properties = (account |> findPropertiesResource "blobServices").properties
                let restore = properties.restorePolicy

                Expect.isTrue restore.enabled ""
                Expect.equal restore.days 5 ""

                let deleteRetention = properties.deleteRetentionPolicy

                Expect.isTrue deleteRetention.enabled ""
                Expect.equal deleteRetention.days 10 ""

                let changeFeed = properties.changeFeed

                Expect.isTrue changeFeed.enabled ""
                Expect.equal changeFeed.retentionInDays 30 ""

                let lastAccessTimeTrackingPolicy = properties.lastAccessTimeTrackingPolicy

                Expect.isTrue lastAccessTimeTrackingPolicy.enable ""
                Expect.equal lastAccessTimeTrackingPolicy.trackingGranularityInDays 12 ""
                Expect.equal lastAccessTimeTrackingPolicy.name "AccessTimeTracking" ""
                Expect.equal lastAccessTimeTrackingPolicy.blobType [| "blockBlob" |] ""

                let containerDeleteRetentionPolicy = properties.containerDeleteRetentionPolicy

                Expect.isTrue containerDeleteRetentionPolicy.enabled ""
                Expect.equal containerDeleteRetentionPolicy.days 11 ""
            }

            test "Versioning" {
                let account =
                    storageAccount {
                        name "storage"
                        enable_versioning [ StorageService.Blobs, true ]
                    }

                let properties = (account |> findPropertiesResource "blobServices").properties

                Expect.isTrue properties.IsVersioningEnabled ""
            }

            test "Sets Min TLS version correctly" {
                let resource =
                    let account =
                        storageAccount {
                            name "mystorage123"
                            min_tls_version Tls12
                        }

                    arm { add_resource account } |> getStorageResource

                Expect.equal resource.MinimumTlsVersion "TLS1_2" "Min TLS version is wrong"
            }
            
            test "Test Disable HTTPS Traffic only" {
                let resource =
                    let account =
                        storageAccount {
                            name "mystorage123"
                            support_https_traffic_only FeatureFlag.Disabled
                        }

                    arm { add_resource account }

                let jsn = resource.Template |> Writer.toJson
                let jobj = jsn |> Newtonsoft.Json.Linq.JObject.Parse

                Expect.equal
                    (jobj.SelectToken("resources[0].properties.supportsHttpsTrafficOnly").ToString())
                    "false"
                    "https traffic only should be disabled"
            }

            test "Test Enable HTTPS Traffic only" {
                let resource =
                    let account =
                        storageAccount {
                            name "mystorage123"
                            support_https_traffic_only
                        }

                    arm { add_resource account }

                let jsn = resource.Template |> Writer.toJson
                let jobj = jsn |> Newtonsoft.Json.Linq.JObject.Parse

                Expect.equal
                    (jobj.SelectToken("resources[0].properties.supportsHttpsTrafficOnly").ToString())
                    "true"
                    "https traffic only should be enabled"
            }
                    
            test "dnsEndpointType can be set to AzureDnsZone" {
                let resource =
                    let account =
                        storageAccount {
                            name "mystorage123"
                            use_azure_dns_zone
                        }

                    arm { add_resource account }

                let jsn = resource.Template |> Writer.toJson
                let jobj = jsn |> Newtonsoft.Json.Linq.JObject.Parse

                Expect.equal
                    (jobj.SelectToken("resources[0].properties.dnsEndpointType").ToString())
                    "AzureDnsZone"
                    "dnsEndpointType should AzureDnsZone"
            }

            test "Must set a storage account name" {
                Expect.throws
                    (fun () -> storageAccount { sku Sku.Standard_ZRS } |> ignore)
                    "Must set a name on a storage account"
            }

            test "Public network access can be disabled" {
                let resource =
                    let account =
                        storageAccount {
                            name "mystorage123"
                            disable_public_network_access
                        }

                    arm { add_resource account }

                let jsn = resource.Template |> Writer.toJson
                let jobj = jsn |> Newtonsoft.Json.Linq.JObject.Parse

                Expect.equal
                    (jobj.SelectToken("resources[0].properties.publicNetworkAccess").ToString())
                    "Disabled"
                    "public network access should be disabled"

                Expect.equal
                    (jobj.SelectToken("resources[0].properties.networkAcls.defaultAction").ToString())
                    "Deny"
                    "network acl should deny traffic when disabling public network access"

                Expect.equal
                    (jobj.SelectToken("resources[0].properties.networkAcls.bypass").ToString())
                    "None"
                    "network acl should not allow bypass by default"
            }

            test "restrict_to_azure_services adds correct network acl" {
                let resource =
                    let account =
                        storageAccount {
                            name "mystorage123"
                            restrict_to_azure_services [ Farmer.Arm.Storage.NetworkRuleSetBypass.AzureServices ]
                            restrict_to_azure_services [ Farmer.Arm.Storage.NetworkRuleSetBypass.Metrics ]
                        }

                    arm { add_resource account }

                let jsn = resource.Template |> Writer.toJson
                let jobj = jsn |> Newtonsoft.Json.Linq.JObject.Parse

                Expect.equal
                    (jobj.SelectToken("resources[0].properties.networkAcls.defaultAction").ToString())
                    "Deny"
                    "network acl should deny traffic when restricting to azure services + private link"

                Expect.equal
                    (jobj.SelectToken("resources[0].properties.networkAcls.bypass").ToString())
                    "AzureServices,Metrics"
                    "network acl should allow bypass for selected services"

                Expect.isEmpty
                    (jobj.SelectToken("resources[0].properties.networkAcls.ipRules").Values<string>())
                    "network acl should not define ip restrictions"

                Expect.isEmpty
                    (jobj
                        .SelectToken("resources[0].properties.networkAcls.virtualNetworkRules")
                        .Values<string>())
                    "network acl should not define vnet restrictions"

                Expect.equal
                    (jobj.SelectToken("resources[0].properties.publicNetworkAccess").ToString())
                    "Enabled"
                    "public network access should be disabled"
            }

            test "Blob public access can be disabled" {
                let resource =
                    let account =
                        storageAccount {
                            name "mystorage123"
                            disable_blob_public_access
                        }

                    arm { add_resource account }

                let jsn = resource.Template |> Writer.toJson
                let jobj = jsn |> Newtonsoft.Json.Linq.JObject.Parse

                Expect.equal
                    (jobj.SelectToken("resources[0].properties.allowBlobPublicAccess").ToString())
                    "false"
                    "blob public access should be disabled"
            }

            test "Blob public access can be toggled" {
                let resource =
                    let account =
                        storageAccount {
                            name "mystorage123"
                            disable_blob_public_access
                            disable_blob_public_access FeatureFlag.Disabled
                        }

                    arm { add_resource account }

                let jsn = resource.Template |> Writer.toJson
                let jobj = jsn |> Newtonsoft.Json.Linq.JObject.Parse

                Expect.equal
                    (jobj.SelectToken("resources[0].properties.allowBlobPublicAccess").ToString())
                    "true"
                    "blob public access should be enabled"
            }

            test "Shared key access can be disabled" {
                let resource =
                    let account =
                        storageAccount {
                            name "mystorage123"
                            disable_shared_key_access
                        }

                    arm { add_resource account }

                let jsn = resource.Template |> Writer.toJson
                let jobj = jsn |> Newtonsoft.Json.Linq.JObject.Parse

                Expect.equal
                    (jobj.SelectToken("resources[0].properties.allowSharedKeyAccess").ToString())
                    "false"
                    "shared key access should be disabled"
            }

            test "Shared key access can be toggled" {
                let resource =
                    let account =
                        storageAccount {
                            name "mystorage123"
                            disable_shared_key_access
                            disable_shared_key_access FeatureFlag.Disabled
                        }

                    arm { add_resource account }

                let jsn = resource.Template |> Writer.toJson
                let jobj = jsn |> Newtonsoft.Json.Linq.JObject.Parse

                Expect.equal
                    (jobj.SelectToken("resources[0].properties.allowSharedKeyAccess").ToString())
                    "true"
                    "shared key access should be enabled"
            }

            test "Default to OAuth can be disabled" {
                let resource =
                    let account =
                        storageAccount {
                            name "mystorage123"
                            default_to_oauth_authentication
                        }

                    arm { add_resource account }

                let jsn = resource.Template |> Writer.toJson
                let jobj = jsn |> Newtonsoft.Json.Linq.JObject.Parse

                Expect.equal
                    (jobj
                        .SelectToken("resources[0].properties.defaultToOAuthAuthentication")
                        .ToString())
                    "true"
                    "default to OAuth should be enabled"
            }

            test "Default to OAuth can be toggled" {
                let resource =
                    let account =
                        storageAccount {
                            name "mystorage123"
                            default_to_oauth_authentication
                            default_to_oauth_authentication FeatureFlag.Disabled
                        }

                    arm { add_resource account }

                let jsn = resource.Template |> Writer.toJson
                let jobj = jsn |> Newtonsoft.Json.Linq.JObject.Parse

                Expect.equal
                    (jobj
                        .SelectToken("resources[0].properties.defaultToOAuthAuthentication")
                        .ToString())
                    "false"
                    "default to OAuth should be disabled"
            }
        ]
