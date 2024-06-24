module Identity

open Expecto
open Farmer
open Farmer.Arm
open Farmer.Identity
open Farmer.Builders
open Newtonsoft.Json.Linq

let tests =
    testList "Identity" [
        test "Can add two identities together" {
            let systemOnly = {
                ManagedIdentity.Empty with
                    SystemAssigned = Enabled
            }

            let userOnlyA = {
                ManagedIdentity.Empty with
                    UserAssigned = [ UserAssignedIdentity(userAssignedIdentities.resourceId "a") ]
            }

            let userOnlyB = {
                ManagedIdentity.Empty with
                    UserAssigned = [ UserAssignedIdentity(userAssignedIdentities.resourceId "b") ]
            }

            Expect.isTrue (userOnlyA + systemOnly).SystemAssigned.AsBoolean "Should have System Assigned on"

            Expect.sequenceEqual
                (userOnlyA + userOnlyB).UserAssigned
                [
                    UserAssignedIdentity(userAssignedIdentities.resourceId "a")
                    UserAssignedIdentity(userAssignedIdentities.resourceId "b")
                ]
                "User Assigned not added correctly"

            Expect.sequenceEqual
                (userOnlyA + userOnlyA).UserAssigned
                [ UserAssignedIdentity(userAssignedIdentities.resourceId "a") ]
                "User Assigned duplicates exist"
        }
        test "Creates ARM JSON correctly" {
            let json = ManagedIdentity.Empty |> ManagedIdentity.toArmJson
            Expect.equal json.``type`` "None" "Should be empty json"
            Expect.isNull json.userAssignedIdentities "Should be empty json"

            let testIdentity =
                userAssignedIdentities.resourceId "test" |> ManagedIdentity.create

            let json = testIdentity |> ManagedIdentity.toArmJson
            Expect.equal json.``type`` "UserAssigned" "Should be user assigned"

            Expect.sequenceEqual
                (json.userAssignedIdentities |> Seq.map (fun s -> s.Key))
                [ "[resourceId('Microsoft.ManagedIdentity/userAssignedIdentities', 'test')]" ]
                "Should be single UAI"

            Expect.equal
                (json.userAssignedIdentities |> Seq.map (fun r -> r.Value.GetType()) |> Seq.head)
                typeof<obj>
                "Should be an object"

            let json =
                {
                    SystemAssigned = Enabled
                    UserAssigned = []
                }
                |> ManagedIdentity.toArmJson

            Expect.equal json.``type`` "SystemAssigned" "Wrong type"
            Expect.isNull json.userAssignedIdentities "Wrong identities"

            let json =
                let testIdentity2 =
                    userAssignedIdentities.resourceId "test2" |> ManagedIdentity.create

                {
                    ManagedIdentity.Empty with
                        SystemAssigned = Enabled
                }
                + testIdentity
                + testIdentity2
                + testIdentity2
                |> ManagedIdentity.toArmJson

            Expect.equal json.``type`` "SystemAssigned, UserAssigned" "Wrong type"
            Expect.hasLength json.userAssignedIdentities 2 "Wrong identities"
        }
        test "Create identity with federated credential" {
            let deployment = arm {
                location Location.EastUS

                add_resources [
                    userAssignedIdentity {
                        name "cicd-msi"

                        add_federated_identity_credentials [
                            federatedIdentityCredential {
                                name "gh-actions-cred"
                                audience EntraIdAudience
                                issuer "https://token.actions.githubusercontent.com"
                                subject "repo:compositionalit/farmer:pull_request"
                            }
                        ]
                    }
                ]
            }

            let jobj = deployment.Template |> Writer.toJson |> JToken.Parse
            let cred = jobj.SelectToken "resources[?(@.name=='cicd-msi/gh-actions-cred')]"
            Expect.isNotNull cred "Credential resource not found by expected name"
            let dependencies = cred.SelectToken "dependsOn"
            Expect.hasLength dependencies 1 "Incorrect number of dependencies"

            Expect.contains
                dependencies
                (JValue "[resourceId('Microsoft.ManagedIdentity/userAssignedIdentities', 'cicd-msi')]")
                "Should have dependency on user assigned identity"

            Expect.equal
                (string (cred.SelectToken "type"))
                "Microsoft.ManagedIdentity/userAssignedIdentities/federatedIdentityCredentials"
                "Incorrect type for federated credential"

            Expect.contains
                (cred.SelectToken "properties.audiences")
                (JValue "api://AzureADTokenExchange")
                "Missing AzureAD audience."

            Expect.equal
                (string (cred.SelectToken "properties.issuer"))
                "https://token.actions.githubusercontent.com"
                "Incorrect issuer"

            Expect.equal
                (string (cred.SelectToken "properties.subject"))
                "repo:compositionalit/farmer:pull_request"
                "Incorrect subject"
        }
    ]