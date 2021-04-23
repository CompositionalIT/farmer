module KeyVault

open Expecto
open Farmer.Builders
open Farmer.KeyVault
open Farmer.Arm.RoleAssignment
open System
open Farmer
open Newtonsoft.Json.Linq

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
    test "Can create secrets with tags" {
        let s = 
            secret {
                name "test"
                add_tag "foo" "bar"
                add_tag "fizz" "buzz"
            }
        Expect.hasLength s.Tags 2 "Incorrect number of tags on secret"
        Expect.equal s.Tags.["foo"] "bar" "Incorrect value on secret tag 'foo'"
        Expect.equal s.Tags.["fizz"] "buzz" "Incorrect value on secret tag 'fizz'"
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

    test "Works with identities" {
        let a = webApp { name "test" }
        let v = keyVault { add_access_policy (AccessPolicy.create a.SystemIdentity.PrincipalId) } :> IBuilder
        let vault = v.BuildResources Location.NorthEurope |> List.head :?> Farmer.Arm.KeyVault.Vault
        Expect.sequenceEqual vault.Dependencies [ ResourceId.create(Arm.Web.sites, a.Name) ] "Web App dependency"
    }
    
    test "Create a basic key vault" {
        let kv = keyVault {
            name "my-test-kv-9876abcd"
        }
        let json =
            let template = 
                arm {
                    add_resource kv
                }
            template.Template |> Writer.toJson
        let jobj = JObject.Parse(json)
        let kvName = jobj.SelectToken("resources[0].name")
        Expect.equal kvName (JValue.CreateString "my-test-kv-9876abcd" :> JToken) "Incorrect name set on key vault"
    }
    
    test "Create a key vault with RBAC enabled" {
        let kv = keyVault {
            name "my-test-kv-9876rbac"
            enable_rbac
        }
        let msi = createUserAssignedIdentity "kvUser"
        let roleAssignment = 
            { Name =
                ArmExpression.create($"guid(concat(resourceGroup().id, '{Roles.KeyVaultReader.Id}'))")
                             .Eval()
                |> ResourceName
              RoleDefinitionId = Roles.KeyVaultReader
              PrincipalId = msi.PrincipalId
              PrincipalType = PrincipalType.ServicePrincipal
              Scope = ResourceGroup
              Dependencies = Set.empty }
        let json =
            let template = 
                arm {
                    add_resource kv
                    add_resource msi
                    add_resource roleAssignment
                }
            template.Template |> Writer.toJson
        let jobj = JObject.Parse(json)
        let enableRbac = jobj.SelectToken("resources[0].properties.enableRbacAuthorization")
        Expect.isTrue (enableRbac.Value<bool>()) "RBAC was not enabled on the key vault"
    }
]
