#r "nuget:Farmer"

open Farmer
open Farmer.Builders

let createFileScript = deploymentScript {
    name "custom-deploy-steps"
    force_update
    retention_interval 3<Hours>
    env_vars [
        EnvVar.createSecureParameter "foo" "secret-foo"
    ]
    supporting_script_uris []
    /// Set the script content directly
    /// Format output as JSON and pipe to $AZ_SCRIPTS_OUTPUT_PATH to make it available as output.
    script_content """printf "{'date':'%s'"} "`date`" > $AZ_SCRIPTS_OUTPUT_PATH """
}

let template = arm {
    add_resource createFileScript
    output "date" createFileScript.Outputs.["date"]
}

template |> Writer.quickWrite "dep-script"