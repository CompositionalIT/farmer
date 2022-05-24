module DeploymentScript

open Expecto
open Farmer
open Farmer.Arm.DeploymentScript
open Farmer.Arm.Storage
open Farmer.Builders

let tests =
    testList
        "deploymentScripts"
        [ test "creates a script" {
              let script =
                  deploymentScript {
                      name "some-script"
                      arguments [ "foo"; "bar" ]
                      env_vars [ "FOO", "bar" ]
                      script_content """ echo 'hello' """
                  }

              Expect.equal script.Name.Value "some-script" "Deployment script resource name incorrect"
              Expect.equal script.ScriptSource (Content " echo 'hello' ") "Script content not set"
              Expect.equal script.Cli (AzCli "2.9.1") "Script default CLI was not az cli 2.9.1"
              Expect.equal script.Timeout None "Script timeout should not have a value"
              Expect.equal script.CleanupPreference Cleanup.Always "Script should default to cleanup Always"
              Expect.hasLength script.Arguments 2 "Incorrect number of script arguments"
              Expect.hasLength script.EnvironmentVariables 1 "Incorrect number of environment variables"
          }

          test "creates a script that is cleaned up only on success" {
              let script =
                  deploymentScript {
                      name "some-script"
                      script_content """ echo 'hello' """
                      cleanup_on_success
                  }

              Expect.equal script.CleanupPreference Cleanup.OnSuccess "Cleanup preference was incorrect"
          }

          test "creates a script that is cleaned up after the retention interval" {
              let script =
                  deploymentScript {
                      name "some-script"
                      script_content """ echo 'hello' """
                      retention_interval 4<Farmer.Hours>
                  }

              Expect.equal
                  script.CleanupPreference
                  (Cleanup.OnExpiration(System.TimeSpan.FromHours 4.))
                  "Cleanup preference should be on expiration of 4 hours"
          }

          test "creates a script with explicit identity" {
              let scriptIdentity = userAssignedIdentity { name "my-aks-user" }

              let deployToAks =
                  deploymentScript {
                      name "some-kubectl-stuff"
                      identity scriptIdentity

                      script_content
                          """ set -e;
                az aks install-cli;
                az aks get-credentials -n my-cluster;
                kubectl apply -f https://some/awesome/deployment.yml;
                """
                  }

              Expect.equal deployToAks.Name.Value "some-kubectl-stuff" "Deployment script resource name incorrect"

              let scriptIdentityValue =
                  Expect.wantSome deployToAks.CustomIdentity "Script identity not set"

              Expect.equal
                  scriptIdentityValue
                  scriptIdentity.UserAssignedIdentity
                  "Script did not have identity assigned"
          }

          test "Outputs are generated correctly" {
              let s = deploymentScript { name "thing" }

              Expect.equal
                  (s.Outputs.["test"].Eval())
                  "[reference(resourceId('Microsoft.Resources/deploymentScripts', 'thing'), '2019-10-01-preview').outputs.test]"
                  ""
          }

          test "Secure parameters are generated correctly" {
              let s =
                  deploymentScript {
                      name "thing"
                      env_vars [ EnvVar.createSecure "foo" "secret-foo" ]
                  }

              let deployment = arm { add_resource s }
              Expect.hasLength deployment.Template.Parameters 1 "Should have a secure parameter"

              Expect.equal
                  (deployment.Template.Parameters.Head.ArmExpression.Eval())
                  "[parameters('secret-foo')]"
                  "Generated incorrect secure parameter."
          }
          test "Script runs after dependency is created" {
              let storage =
                  storageAccount {
                      name "storagewithstuff"
                      add_public_container "public"
                  }

              let script =
                  deploymentScript {
                      name "write-files"

                      script_content
                          "echo 'hello world' > hello && az storage blob upload --account-name storagewithstuff -f hello -c public -n hello"

                      depends_on storage
                  }

              Expect.hasLength script.Dependencies 1 "Should have additional dependency"

              Expect.equal
                  (Set.toList script.Dependencies).[0].Name.Value
                  "storagewithstuff"
                  "Dependency should be on storage account"
          }

          test "Retention period cannot be more than 26 hours" {
              let createScript hours () =
                  deploymentScript { retention_interval (hours * 1<Hours>) }
                  |> ignore

              Expect.equal (createScript 26 ()) () "Should have not thrown"
              Expect.throws (createScript 27) ""
          } ]
