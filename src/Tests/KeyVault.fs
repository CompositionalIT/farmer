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
        Expect.equal parameterSecret.SecretName "test" "Invalid name of simple secret"
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
        Expect.sequenceEqual vault.Dependencies [ ResourceId.create(Arm.Web.sites, a.Name.ResourceName) ] "Web App dependency"
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
    test "Get Vault URI from output" {
        let kv = keyVault {
            name "my-test-kv-9876"
        }
        let json =
            let template = 
                arm {
                    add_resource kv
                    output "kv-uri" kv.VaultUri
                }
            template.Template |> Writer.toJson
        let jobj = JObject.Parse(json)
        let kvUri = jobj.SelectToken("outputs.kv-uri.value")
        Expect.equal (string kvUri) "[reference(resourceId('Microsoft.KeyVault/vaults', 'my-test-kv-9876')).vaultUri]" "Vault URI not set properly in output"
    }
    test "Key Vault with purge protection emits correct value" {
        let kv = keyVault {
            name "my-test-kv-9876"
            enable_soft_delete_with_purge_protection
        }
        let json =
            let template = 
                arm {
                    add_resource kv
                }
            template.Template |> Writer.toJson
        let jobj = JObject.Parse(json)
        let purgeProtection = jobj.SelectToken("resources[0].properties.enablePurgeProtection")
        Expect.equal (purgeProtection |> string |> Boolean.Parse) true "Purge protection not enabled"
    }
    test "Add access policies on existing key vault" {
        let additionalPolicies =
            keyVaultAddPolicies {
                key_vault (Farmer.Arm.KeyVault.vaults.resourceId "existing-vault")
                add_access_policies [
                    accessPolicy {
                        object_id (Guid "ad731a70-fd25-452f-b9d8-a0c0ae8033af")
                        application_id (Guid "12ef53f8-98a0-4513-b081-6b5e70db76e1")
                        certificate_permissions [ KeyVault.Certificate.List ]
                        secret_permissions KeyVault.Secret.All
                        key_permissions [ KeyVault.Key.List ]
                    }
                ]
            }
        let template =
            arm {
                add_resource additionalPolicies
            }
        let jobj = JObject.Parse(template.Template |> Writer.toJson)
        let name = jobj.SelectToken("resources[0].name")
        Expect.equal (name |> string) "existing-vault/add" "Incorrect name for adding kv access policies"
        let dependsOn = jobj.SelectToken("resources[0].dependsOn") :?> JArray
        Expect.hasLength dependsOn 0 "Should have no dependencies"
        let accessPolicies = jobj.SelectToken("resources[0].properties.accessPolicies") :?> JArray
        Expect.hasLength accessPolicies 1 "Should include one access policy to add to the key vault"
        let tenant = jobj.SelectToken("resources[0].properties.accessPolicies[0].tenantId") |> string
        Expect.equal tenant "[subscription().tenantId]" "If tenant was not specified, access policies default to target subscription's tenant"
    }
    test "Adding access policies on existing key vault without specifying the key vault fails" {
        Expect.throws (fun _ ->
            let additionalPolicies =
                keyVaultAddPolicies {
                    add_access_policies [
                        accessPolicy {
                            object_id (Guid "ad731a70-fd25-452f-b9d8-a0c0ae8033af")
                            application_id (Guid "12ef53f8-98a0-4513-b081-6b5e70db76e1")
                            certificate_permissions [ KeyVault.Certificate.List ]
                            secret_permissions KeyVault.Secret.All
                            key_permissions [ KeyVault.Key.List ]
                        }
                    ]
                }
            let template =
                arm {
                    add_resource additionalPolicies
                }
            template |> Writer.quickWrite |> ignore
        ) "Should have failed to build the key vault policy addition resource"
    }
    test "Standalone secret can be added as resource" {
        let vaultId = (Arm.KeyVault.vaults.resourceId (ResourceName "my-kv"))
        let secret = secret { name "my-secret"; content_type "ConnectionString"; link_to_unmanaged_keyvault (Arm.KeyVault.vaults.resourceId "my-kv")}
        let resources = (arm { add_resource secret }).Template.Resources
        Expect.hasLength resources 1 "Should only be one resource"
        match resources.[0] with
        | :? Arm.KeyVault.Vaults.Secret as secret -> ()
        | x -> failwith $"resource was expected to be of type {typeof<Arm.KeyVault.Vaults.Secret>} but was {x.GetType()}"
    }
    test "Adding keys with key vault" {
        let vault =
            keyVault {
                name "TestFarmVault"
                tenant_id Subscription.TenantId
                add_keys [
                    key {
                        name "testKeyInlineRsa"
                        key_type KeyType.RSA_4096
                        key_operations [ KeyOperation.Encrypt; KeyOperation.Sign; KeyOperation.Verify ]
                    }
                    key {
                        name "testKeyInlineEc"
                        key_type KeyType.EC_P256
                    }
                ]
            }
        let deployment = arm { add_resource vault }
        let jobj = JObject.Parse (deployment.Template |> Writer.toJson)
        let rsaKey = jobj.SelectToken("$.resources[?(@.name=='TestFarmVault/testKeyInlineRsa')]")
        Expect.isNotNull rsaKey "Unable to find 'testKeyInlineRsa'"
        Expect.equal (int rsaKey.["properties"].["keySize"]) 4096 "Incorrect RSA key size"
        Expect.equal (string rsaKey.["properties"].["kty"]) "RSA" "Incorrect RSA key type"
        Expect.isNull rsaKey.["properties"].["curveName"] "RSA key should not have curveName"
        let ecKey = jobj.SelectToken("$.resources[?(@.name=='TestFarmVault/testKeyInlineEc')]")
        Expect.isNotNull ecKey "Unable to find 'testKeyInlineEc'"
        Expect.equal (string ecKey.["properties"].["kty"]) "EC" "Incorrect EC key type"
        Expect.equal (string ecKey.["properties"].["curveName"]) "P-256" "Incorrect EC curveName"
        Expect.isNull ecKey.["properties"].["keySize"] "Elliptic curve key should not have keySize"
    }
    test "Adding standalone keys to key vault" {
        let vault = keyVault { name "TestFarmVault" }
        let myKey = key {
            name "testKey"
            key_type KeyType.RSA_4096
            link_to_unmanaged_keyvault vault
            key_operations [ KeyOperation.Encrypt ]
        }
        let deployment = arm { add_resource myKey }
        let jobj = JObject.Parse (deployment.Template |> Writer.toJson)
        let key = jobj.SelectToken("$.resources[?(@.name=='TestFarmVault/testKey')]")
        Expect.isNotNull key "Unable to find 'testKey'"
        Expect.equal (int key.["properties"].["keySize"]) 4096 "Incorrect RSA key size"
        Expect.equal (string key.["properties"].["kty"]) "RSA" "Incorrect RSA key type"
        Expect.isEmpty key.["dependsOn"] "Standalone key should not have dependsOn"
    }
]
