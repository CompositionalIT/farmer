#r @"./libs/Newtonsoft.Json.dll"
#r @"../../src/Farmer/bin/Debug/netstandard2.0/Farmer.dll"

open Farmer
open Farmer.Builders
open Farmer.CoreTypes

let scriptIdentity = userAssignedIdentity {
    name "script-user"
}

/// The script identity must be a contributor over this resource group.
let scriptRole =
    role_assignment
        (ArmExpression.create("guid(resourceGroup().id)").Eval())
        Roles.Contributor
        scriptIdentity.PrincipalId

let createFileScript = deploymentScript {
    name "custom-deploy-steps"
    identity scriptIdentity
    force_update_tag (System.DateTime.Now.ToString("o"))
    /// Set the script content directly
    /// Format output as JSON and pipe to $AZ_SCRIPTS_OUTPUT_PATH to make it available as output.
    content """printf "{'date':'%s'"} "`date`" > $AZ_SCRIPTS_OUTPUT_PATH """
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

let template = arm {
    location Location.EastUS
    add_resource scriptIdentity
    add_resource scriptRole
    add_resource createFileScript
    output "date" "[reference('custom-deploy-steps').outputs.date]"
}

template |> Writer.quickWrite "dep-script"
