module KeyVault

open Expecto
open Farmer.Builders
open Farmer.CoreTypes
open Farmer.KeyVault
open System

let tests = testList "KeyVault" [
    test "Can create secrets without popping" {
        secret { name "test" } |> ignore
    }
    test "Can create quick secrets" {
        let vault =
            keyVault {
                name "test"
                add_secret "test1"
                add_secret (secret { name "test2" })
            }
        Expect.hasLength vault.Secrets 2 "Bad secrets"
    }
    test "Throws on empty inline secret" {
        Expect.throws(fun () ->
            keyVault {
                name "test"
                add_secret ""
            } |> ignore
        ) "Empty secret should throw"
    }
    test "Throws on empty full secret" {
        Expect.throws(fun () ->
            secret { name "" } |> ignore
        ) "Empty secret should throw"
    }
    test "Default access policy settings is GET and LIST" {
        let p = AccessPolicy.create (ObjectId Guid.Empty)
        Expect.equal (set [ Secret.Get; Secret.List ]) p.Permissions.Secrets "Incorrect default secrets"
    }
    test "Creates key vault secrets correctly" {
        let parameterSecret = SecretConfig.create "test"
        Expect.equal parameterSecret.Key "test" "Invalid name of simple secret"
        Expect.equal parameterSecret.Value (ParameterSecret (SecureParameter "test")) "Invalid value of parameter secret"

        let sa = storageAccount { name "storage" }
        let expressionSecret = SecretConfig.create("test", sa.Key)
        Expect.equal expressionSecret.Value (ExpressionSecret sa.Key) "Invalid value of expression secret"
        Expect.sequenceEqual expressionSecret.Dependencies [ ResourceId.create(Farmer.Arm.Storage.storageAccounts, sa.Name.ResourceName) ] "Missing storage account dependency"

        Expect.throws (fun _ -> SecretConfig.create("bad", (ArmExpression.literal "foo")) |> ignore) "Should throw exception on expression with no owner"
    }
]
