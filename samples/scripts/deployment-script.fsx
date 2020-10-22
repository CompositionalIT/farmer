#r @"./libs/Newtonsoft.Json.dll"
#r @"../../src/Farmer/bin/Debug/netstandard2.0/Farmer.dll"

open Farmer
open Farmer.Arm.RoleAssignment
open Farmer.Builders
open Farmer.CoreTypes

let scriptIdentity = userAssignedIdentity {
    name "script-user"
}

let scriptRole:Assignment =
    {
        Name = ArmExpression.create("guid(resourceGroup().id)").Eval() |> ResourceName
        RoleDefinitionId = Roles.Contributor
        PrincipalId = scriptIdentity.PrincipalId
        Scope = ResourceName.Empty
    }

let createFileScript = deploymentScript {
    name "custom-deploy-steps"
    add_identity scriptIdentity
    force_update_tag (System.DateTime.Now.ToString("o"))
    /// Set the script content directly
    /// Format output as JSON and pipe to $AZ_SCRIPTS_OUTPUT_PATH to make it available as output.
    content """printf "{'date':'%s'"} "`date`" > $AZ_SCRIPTS_OUTPUT_PATH """
}

let deployToAks = deploymentScript {
    name "some-kubectl-stuff"
    add_identity scriptIdentity
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
