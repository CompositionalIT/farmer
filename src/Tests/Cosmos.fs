module Cosmos

open Expecto
open Farmer
open Farmer.Builders
open Farmer.Storage
open Microsoft.Azure.Management.Storage
open Microsoft.Azure.Management.Storage.Models
open Microsoft.Rest
open System

let tests = testList "Cosmos" [
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
        Expect.contains container.UniqueKeys ["/FirstName"] "UniqueKeys should contain /FirstName"
        Expect.contains container.UniqueKeys ["/LastName"] "UniqueKeys should contain /LastName"
    }
    test "DB properties are correctly evaluated" {
        let db = cosmosDb { name "test" }
        Expect.equal "[reference(resourceId('Microsoft.DocumentDb/databaseAccounts', 'test-account'), '2020-03-01').documentEndpoint]" (db.Endpoint.Eval()) "Endpoint is incorrect"
        Expect.equal "[listKeys(resourceId('Microsoft.DocumentDb/databaseAccounts', 'test-account'), providers('Microsoft.DocumentDb','databaseAccounts').apiVersions[0]).primaryMasterKey]" (db.PrimaryKey.Eval()) "Primary Key is incorrect"
        Expect.equal "[listKeys(resourceId('Microsoft.DocumentDb/databaseAccounts', 'test-account'), providers('Microsoft.DocumentDb','databaseAccounts').apiVersions[0]).secondaryMasterKey]" (db.SecondaryKey.Eval()) "Secondary Key is incorrect"
        Expect.equal "[listKeys(resourceId('Microsoft.DocumentDb/databaseAccounts', 'test-account'), providers('Microsoft.DocumentDb','databaseAccounts').apiVersions[0]).primaryreadonlyMasterKey]" (db.PrimaryReadonlyKey.Eval()) "Primary Readonly Key is incorrect"
        Expect.equal "[listKeys(resourceId('Microsoft.DocumentDb/databaseAccounts', 'test-account'), providers('Microsoft.DocumentDb','databaseAccounts').apiVersions[0]).secondaryreadonlyMasterKey]" (db.SecondaryReadonlyKey.Eval()) "Secondary Readonly Key is incorrect"
        Expect.equal "[listConnectionStrings(resourceId('Microsoft.DocumentDb/databaseAccounts', 'test-account'), providers('Microsoft.DocumentDb','databaseAccounts').apiVersions[0]).connectionStrings[0].connectionString]" (db.PrimaryConnectionString.Eval()) "Primary Connection String is incorrect"
        Expect.equal "[listConnectionStrings(resourceId('Microsoft.DocumentDb/databaseAccounts', 'test-account'), providers('Microsoft.DocumentDb','databaseAccounts').apiVersions[0]).connectionStrings[1].connectionString]" (db.SecondaryConnectionString.Eval()) "Secondary Connection String is incorrect"
    }
    test "Correctly serializes to JSON" {
        let t = arm { add_resource (cosmosDb { name "test" }) }

        Deployment.getTemplate "farmer-resources" t
        |> Writer.toJson
        |> ignore
    }
    test "Creates connection string and keys with resource groups" {
        let conn = CosmosDb.getConnectionString(ResourceId.create(Arm.DocumentDb.databaseAccounts, ResourceName "db", "group"), PrimaryConnectionString).Eval()
        let key = CosmosDb.getKey(ResourceId.create(Arm.DocumentDb.databaseAccounts, ResourceName "db", "group"), PrimaryKey, ReadWrite).Eval()

        Expect.equal key "[listKeys(resourceId('group', 'Microsoft.DocumentDb/databaseAccounts', 'db'), providers('Microsoft.DocumentDb','databaseAccounts').apiVersions[0]).primaryMasterKey]" "Primary Key is incorrect"
        Expect.equal conn "[listConnectionStrings(resourceId('group', 'Microsoft.DocumentDb/databaseAccounts', 'db'), providers('Microsoft.DocumentDb','databaseAccounts').apiVersions[0]).connectionStrings[0].connectionString]" "Primary Connection String is incorrect"
    }
    testList "Account Name Validation tests" [
        let invalidAccountNameCases = [
            "Empty Account", "", "cannot be empty", "Name too short"
            "Min Length", "zz", "min length is 3, but here is 2 ('zz')", "Name too short"
            "Max Length", "abcdefghij1234567890abcde12345678901234567890", "max length is 44, but here is 45 ('abcdefghij1234567890abcde12345678901234567890')", "Name too long"
            "Lowercase Only", "zzzT", "can only contain lowercase letters ('zzzT')", "Upper case character allowed"
            "Alphanumeric or dash", "zzz!", "can only contain alphanumeric characters or the dash ('zzz!')", "Non alpha numeric (except dash) character allowed"
        ]

        for testName, accountName, error, why in invalidAccountNameCases ->
            test testName {
                Expect.equal (CosmosDbValidation.CosmosDbName.Create accountName) (Error ("CosmosDb account names " + error)) why
            }

        let validAccountNameCases = [
            "Valid Name 1", "abcdefghij1234567890abcd", "Should have created a valid CosmosDb account name"
            "Valid Name 2", "a-b-c-d-e-12-3-4", "Should have created a valid CosmosDb account name"
        ]
        for testName, accountName, why in validAccountNameCases ->
            test testName {
                Expect.equal (StorageResourceName.Create(accountName).OkValue.ResourceName) (ResourceName accountName) why
            }
    ]
]
