#r @"./libs/Newtonsoft.Json.dll"
#r @"../../src/Farmer/bin/Debug/netstandard2.0/Farmer.dll"

open Farmer
open Farmer.Builders.DeploymentScript

let createFileScript = deploymentScript {
    name "custom-deploy-steps"
    identity "script-user"
    force_update_tag (System.DateTime.Now.ToString("o"))
    /// Set the script content directly
    /// Format output as JSON and pipe to $AZ_SCRIPTS_OUTPUT_PATH to make it available as output.
    content """printf "{'date':'%s'"} "`date`" > $AZ_SCRIPTS_OUTPUT_PATH """
}

let deployToAks = deploymentScript {
    name "some-kubectl-stuff"
    identity "script-user"
    content """ set -e;
        az aks install-cli;
        az aks get-credentials -n my-cluster;
        kubectl apply -f https://some/awesome/deployment.yml;
        """
}

let template = arm {
    location Location.WestEurope
    add_resource createFileScript
    output "date" "[reference('custom-deploy-steps').outputs.date]"
}

template |> Writer.quickWrite "dep-script"
