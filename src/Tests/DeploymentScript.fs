module DeploymentScript

open Expecto
open Farmer.Arm.DeploymentScript
open Farmer.Builders

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
        Expect.hasLength script.Arguments 2 "Incorrect number of script arguments"
        Expect.hasLength script.EnvironmentVariables 1 "Incorrect number of environment variables"
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
]
