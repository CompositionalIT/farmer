module Cosmos

open Expecto
open Farmer
open Farmer.Builders
open Farmer.Storage
open Farmer.Arm.DocumentDb

let tests =
    testList
        "Cosmos"
        [
            test "Cosmos container should ignore duplicate unique keys" {

                let container =
                    cosmosContainer {
                        name "people"
                        partition_key [ "/id" ] CosmosDb.Hash
                        add_unique_key [ "/FirstName" ]
                        add_unique_key [ "/LastName" ]
                        add_unique_key [ "/LastName" ]
                    }

                Expect.equal container.UniqueKeys.Count 2 "There should be 2 unique keys."
                Expect.contains container.UniqueKeys [ "/FirstName" ] "UniqueKeys should contain /FirstName"
                Expect.contains container.UniqueKeys [ "/LastName" ] "UniqueKeys should contain /LastName"
            }
            test "Serverless template should include 'EnableServerless' and should not contains 'throughput'" {
                let t =
                    arm {
                        add_resource (
                            cosmosDb {
                                name "foo"
                                throughput CosmosDb.Serverless
                            }
                        )
                    }

                let json = t.Template |> Writer.toJson

                Expect.isTrue
                    (json.Contains("EnableServerless"))
                    "Serverless template should contain 'EnableServerless'."

                Expect.isFalse (json.Contains("throughput")) "Serverless template should not contain 'throughput'."
            }
            test "Serverless template should include one locations.location with filled locationName" {
                let t =
                    arm {
                        add_resource (
                            cosmosDb {
                                name "foo"
                                throughput CosmosDb.Serverless
                            }
                        )
                    }

                let jobj = t.Template |> Writer.toJson |> Newtonsoft.Json.Linq.JObject.Parse

                let locationJOjb =
                    jobj.SelectToken(
                        "$.resources[?(@.type=='Microsoft.DocumentDb/databaseAccounts')].properties.locations[0]"
                    )

                Expect.isNotEmpty (locationJOjb |> string) "location should be filled"

                let locationName = locationJOjb.SelectToken("locationName") |> string
                Expect.isNotEmpty locationName "location should be filled"
            }
            test "Provisioned template should include 'throughput' and should not contain 'EnableServerless'" {
                let t =
                    arm {
                        add_resource (
                            cosmosDb {
                                name "foo"
                                throughput 400<CosmosDb.RU>
                            }
                        )
                    }

                let json = t.Template |> Writer.toJson

                Expect.isTrue
                    (json.Contains("\"throughput\": \"400\""))
                    "Shared throughput template should contain 'throughput'."

                Expect.isFalse
                    (json.Contains("EnableServerless"))
                    "Shared throughput template should not contain 'EnableServerless'."
            }
            test "DB properties are correctly evaluated" {
                let db = cosmosDb { name "test" }

                Expect.equal
                    (db.Endpoint.Eval())
                    "[reference(resourceId('Microsoft.DocumentDb/databaseAccounts', 'test-account'), '2021-04-15').documentEndpoint]"
                    "Endpoint is incorrect"

                Expect.equal
                    (db.PrimaryKey.Eval())
                    "[listKeys(resourceId('Microsoft.DocumentDb/databaseAccounts', 'test-account'), providers('Microsoft.DocumentDb','databaseAccounts').apiVersions[0]).primaryMasterKey]"
                    "Primary Key is incorrect"

                Expect.equal
                    (db.SecondaryKey.Eval())
                    "[listKeys(resourceId('Microsoft.DocumentDb/databaseAccounts', 'test-account'), providers('Microsoft.DocumentDb','databaseAccounts').apiVersions[0]).secondaryMasterKey]"
                    "Secondary Key is incorrect"

                Expect.equal
                    (db.PrimaryReadonlyKey.Eval())
                    "[listKeys(resourceId('Microsoft.DocumentDb/databaseAccounts', 'test-account'), providers('Microsoft.DocumentDb','databaseAccounts').apiVersions[0]).primaryreadonlyMasterKey]"
                    "Primary Readonly Key is incorrect"

                Expect.equal
                    (db.SecondaryReadonlyKey.Eval())
                    "[listKeys(resourceId('Microsoft.DocumentDb/databaseAccounts', 'test-account'), providers('Microsoft.DocumentDb','databaseAccounts').apiVersions[0]).secondaryreadonlyMasterKey]"
                    "Secondary Readonly Key is incorrect"

                Expect.equal
                    (db.PrimaryConnectionString.Eval())
                    "[listConnectionStrings(resourceId('Microsoft.DocumentDb/databaseAccounts', 'test-account'), providers('Microsoft.DocumentDb','databaseAccounts').apiVersions[0]).connectionStrings[0].connectionString]"
                    "Primary Connection String is incorrect"

                Expect.equal
                    (db.SecondaryConnectionString.Eval())
                    "[listConnectionStrings(resourceId('Microsoft.DocumentDb/databaseAccounts', 'test-account'), providers('Microsoft.DocumentDb','databaseAccounts').apiVersions[0]).connectionStrings[1].connectionString]"
                    "Secondary Connection String is incorrect"
            }

            testList
                "db type"
                [
                    test "default" {
                        let db =
                            cosmosDb {
                                name "test"
                                account_name "account"
                            }

                        Expect.equal db.Kind Document ""
                    }

                    test "sql" {
                        let db =
                            cosmosDb {
                                name "test"
                                kind Document
                            }

                        Expect.equal db.Kind Document ""
                    }

                    test "mongoDB" {
                        let db =
                            cosmosDb {
                                name "test"
                                kind Mongo
                            }

                        Expect.equal db.Kind Mongo ""
                    }
                ]

            test "Correctly serializes to JSON" {
                let t = arm { add_resource (cosmosDb { name "test" }) }

                t.Template |> Writer.toJson |> ignore
            }
            test "Creates connection string and keys with resource groups" {
                let conn =
                    CosmosDb
                        .getConnectionString(
                            ResourceId.create (Arm.DocumentDb.databaseAccounts, ResourceName "db", "group"),
                            PrimaryConnectionString
                        )
                        .Eval()

                let key =
                    CosmosDb
                        .getKey(
                            ResourceId.create (Arm.DocumentDb.databaseAccounts, ResourceName "db", "group"),
                            PrimaryKey,
                            ReadWrite
                        )
                        .Eval()

                Expect.equal
                    key
                    "[listKeys(resourceId('group', 'Microsoft.DocumentDb/databaseAccounts', 'db'), providers('Microsoft.DocumentDb','databaseAccounts').apiVersions[0]).primaryMasterKey]"
                    "Primary Key is incorrect"

                Expect.equal
                    conn
                    "[listConnectionStrings(resourceId('group', 'Microsoft.DocumentDb/databaseAccounts', 'db'), providers('Microsoft.DocumentDb','databaseAccounts').apiVersions[0]).connectionStrings[0].connectionString]"
                    "Primary Connection String is incorrect"
            }
            test "Backup policy should not be set by default" {
                let t = arm { add_resource (cosmosDb { name "test" }) }

                let json = t.Template |> Writer.toJson 
                let jobj = json |> Newtonsoft.Json.Linq.JObject.Parse

                let found, _ = jobj.TryGetValue("$.resources[?(@.type=='Microsoft.DocumentDb/databaseAccounts')].properties")
                Expect.isFalse found "backup policy should not be included by default"
            }
            test "Continuous backup policy" {
                let t = arm { add_resource (cosmosDb {
                    name "test"
                    backup_policy CosmosDb.BackupPolicy.Continuous
                })}

                let json = t.Template |> Writer.toJson 
                let jobj = json |> Newtonsoft.Json.Linq.JObject.Parse

                let policy = jobj.SelectToken("$.resources[?(@.type=='Microsoft.DocumentDb/databaseAccounts')].properties.backupPolicy.type").ToString()
                Expect.equal policy "Continuous" "backup policy should be Continuous"
            }
            test "Periodic backup policy" {
                let t = arm { add_resource (cosmosDb {
                    name "test"
                    backup_policy (CosmosDb.BackupPolicy.Periodic(
                        BackupIntervalInMinutes = 60,
                        BackupRetentionIntervalInHours = 168,
                        BackupStorageRedundancy = CosmosDb.BackupStorageRedundancy.Geo))
                })}

                let json = t.Template |> Writer.toJson 
                let jobj = json |> Newtonsoft.Json.Linq.JObject.Parse

                let resourcePrefix = "$.resources[?(@.type=='Microsoft.DocumentDb/databaseAccounts')].properties.backupPolicy"

                Expect.equal (jobj.SelectToken($"{resourcePrefix}.type").ToString()) "Periodic" "backup policy should be Periodic"
                Expect.equal (jobj.SelectToken($"{resourcePrefix}.periodicModeProperties.backupIntervalInMinutes").ToString()) "60" "backup interval should be 60"
                Expect.equal (jobj.SelectToken($"{resourcePrefix}.periodicModeProperties.backupRetentionIntervalInHours").ToString()) "168" "backup interval should be 168"
                Expect.equal (jobj.SelectToken($"{resourcePrefix}.periodicModeProperties.backupStorageRedundancy").ToString()) "Geo" "backup redundancy should be geo"
            }
            test "Autoscale settings" {
                let t = arm { add_resource (cosmosDb {
                    name "test"
                    throughput (CosmosDb.Throughput.Autoscale(1000<CosmosDb.RU>))
                })}

                let json = t.Template |> Writer.toJson 
                let jobj = json |> Newtonsoft.Json.Linq.JObject.Parse

                let resourcePrefix = "$.resources[?(@.type=='Microsoft.DocumentDb/databaseAccounts/sqlDatabases')]"
                Expect.equal (jobj.SelectToken($"{resourcePrefix}.properties.options.autoscaleSettings.maxThroughput").ToString()) "1000" "Max throughput should be 1000"
            }
            testList
                "Account Name Validation tests"
                [
                    let invalidAccountNameCases =
                        [
                            "Empty Account", "", "cannot be empty", "Name too short"
                            "Min Length",
                            "zz",
                            "min length is 3, but here is 2. The invalid value is 'zz'",
                            "Name too short"
                            "Max Length",
                            "abcdefghij1234567890abcde12345678901234567890",
                            "max length is 44, but here is 45. The invalid value is 'abcdefghij1234567890abcde12345678901234567890'",
                            "Name too long"
                            "Lowercase Only",
                            "zzzT",
                            "can only contain lowercase letters. The invalid value is 'zzzT'",
                            "Upper case character allowed"
                            "Alphanumeric or dash",
                            "zzz!",
                            "can only contain alphanumeric characters or the dash (-). The invalid value is 'zzz!'",
                            "Non alpha numeric (except dash) character allowed"
                        ]

                    for testName, accountName, error, why in invalidAccountNameCases ->
                        test testName {
                            Expect.equal
                                (CosmosDbValidation.CosmosDbName.Create accountName)
                                (Error("CosmosDb account names " + error))
                                why
                        }

                    let validAccountNameCases =
                        [
                            "Valid Name 1",
                            "abcdefghij1234567890abcd",
                            "Should have created a valid CosmosDb account name"
                            "Valid Name 2", "a-b-c-d-e-12-3-4", "Should have created a valid CosmosDb account name"
                        ]

                    for testName, accountName, why in validAccountNameCases ->
                        test testName {
                            Expect.equal
                                (StorageResourceName.Create(accountName).OkValue.ResourceName)
                                (ResourceName accountName)
                                why
                        }
                ]
        ]
