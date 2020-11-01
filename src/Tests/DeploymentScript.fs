module DeploymentScript

open Expecto
open Farmer
open Farmer.Builders

let tests = testList "deploymentScripts" [
    test "creates a script" {
        let script = deploymentScript {
            name "some-script"
            arguments [ "foo"; "bar" ]
            env_vars [
                env_var "FOO" "bar"
            ]
            content """ echo 'hello' """
        }
        
        Expect.equal script.Name.Value "some-script" "Deployment script resource name incorrect"
        Expect.isSome script.ScriptContent "Script content not set"
        Expect.equal script.Cli (Arm.DeploymentScript.AzCli "2.9.1") "Script default CLI was not az cli 2.9.1"
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
            content """ set -e;
                az aks install-cli;
                az aks get-credentials -n my-cluster;
                kubectl apply -f https://some/awesome/deployment.yml;
                """
        }
        
        Expect.equal deployToAks.Name.Value "some-kubectl-stuff" "Deployment script resource name incorrect"
        let scriptIdentityValue = Expect.wantSome deployToAks.Identity "Script identity not set"
        Expect.equal scriptIdentityValue scriptIdentity.UserAssignedIdentity "Script did not have identity assigned"
    }
]
