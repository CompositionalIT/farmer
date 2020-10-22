module DeploymentScript

open Expecto
open Farmer
open Farmer.Builders

let tests = testList "deploymentScripts" [
    test "creates a script" {
        let scriptIdentity = userAssignedIdentity {
            name "script-user"
        }

        let deployToAks = deploymentScript {
            name "some-kubectl-stuff"
            identity scriptIdentity
            arguments [ "foo"; "bar" ]
            env_vars [
                env_var "FOO" "bar"
            ]
            content """ set -e;
                az aks install-cli;
                az aks get-credentials -n my-cluster;
                kubectl apply -f https://some/awesome/deployment.yml;
                """
        }
        
        Expect.equal deployToAks.Name.Value "some-kubectl-stuff" "Deployment script resource name incorrect"
        Expect.isSome deployToAks.ScriptContent "Script content not set"
        Expect.equal deployToAks.Identity.UserAssigned.Head scriptIdentity.UserAssignedIdentity "Script did not have identity assigned"
        Expect.equal deployToAks.Cli (Arm.DeploymentScript.AzCli "2.9.1") "Script default CLI was not az cli 2.9.1"
        Expect.equal deployToAks.Timeout None "Script timeout should not have a value"
        Expect.hasLength deployToAks.Arguments 2 "Incorrect number of script arguments"
        Expect.hasLength deployToAks.EnvironmentVariables 1 "Incorrect number of environment variables"
    }
]
