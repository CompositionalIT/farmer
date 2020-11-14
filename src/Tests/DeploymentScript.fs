module DeploymentScript

open Expecto
open Farmer
open Farmer.Arm.DeploymentScript
open Farmer.Arm.Storage
open Farmer.Builders
open Farmer.CoreTypes

let tests = testList "deploymentScripts" [
    test "creates a script" {
        let script = deploymentScript {
            name "some-script"
            arguments [ "foo"; "bar" ]
            env_vars [ "FOO", "bar" ]
            script_content """ echo 'hello' """
        }

        Expect.equal script.Name.Value "some-script" "Deployment script resource name incorrect"
        Expect.equal script.ScriptSource (Content " echo 'hello' ") "Script content not set"
        Expect.equal script.Cli (AzCli "2.9.1") "Script default CLI was not az cli 2.9.1"
        Expect.equal script.Timeout None "Script timeout should not have a value"
        Expect.equal script.CleanupPreference None "Script should not have a cleanup preference"
        Expect.hasLength script.Arguments 2 "Incorrect number of script arguments"
        Expect.hasLength script.EnvironmentVariables 1 "Incorrect number of environment variables"
    }

    test "creates a script that is cleaned up only on success" {
        let script = deploymentScript {
            name "some-script"
            script_content """ echo 'hello' """
            cleanup_on_success
        }
        let cleanup = Expect.wantSome script.CleanupPreference "Script should have a cleanup preference"
        Expect.equal cleanup Cleanup.OnSuccess "Cleanup preference was incorrect"
    }

    test "creates a script that is cleaned up after the retention interval" {
        let script = deploymentScript {
            name "some-script"
            script_content """ echo 'hello' """
            retention_interval 4<Farmer.Hours>
        }
        let cleanup = Expect.wantSome script.CleanupPreference "Script should have a cleanup preference"
        Expect.equal cleanup (Cleanup.OnExpiration (System.TimeSpan.FromHours 4.)) "Cleanup preference should be on expiration of 4 hours"
    }

    test "creates a script with explicit identity" {
        let scriptIdentity = userAssignedIdentity {
            name "my-aks-user"
        }

        let deployToAks = deploymentScript {
            name "some-kubectl-stuff"
            identity scriptIdentity
            script_content """ set -e;
                az aks install-cli;
                az aks get-credentials -n my-cluster;
                kubectl apply -f https://some/awesome/deployment.yml;
                """
        }

        Expect.equal deployToAks.Name.Value "some-kubectl-stuff" "Deployment script resource name incorrect"
        let scriptIdentityValue = Expect.wantSome deployToAks.CustomIdentity "Script identity not set"
        Expect.equal scriptIdentityValue scriptIdentity.UserAssignedIdentity "Script did not have identity assigned"
    }

    test "Outputs are generated correctly" {
        let s = deploymentScript { name "thing" }
        Expect.equal (s.Outputs.["test"].Eval()) "[reference(resourceId('Microsoft.Resources/deploymentScripts', 'thing'), '2019-10-01-preview').outputs.test]" ""
    }

    test "Script runs after dependency is created" {
        let storage = storageAccount {
            name "storagewithstuff"
            add_public_container "public"
        }
        let script = deploymentScript {
            name "write-files"
            script_content "echo 'hello world' > hello && az storage blob upload --account-name storagewithstuff -f hello -c public -n hello"
            run_after (ResourceId.create (storageAccounts, storage.Name.ResourceName))
        }
        Expect.hasLength script.AdditionalDependencies 1 "Should have additional dependency"
        Expect.equal script.AdditionalDependencies.Head.Name.Value "storagewithstuff" "Dependency should be on storage account"
    }
]
