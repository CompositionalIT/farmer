#r @"./libs/Newtonsoft.Json.dll"
#r @"../../src/Farmer/bin/Debug/netstandard2.0/Farmer.dll"

open Farmer
open Farmer.Builders
open System

let createFileScript = deploymentScript {
    name "custom-deploy-steps"
    force_update
    retention_interval 1<Hours>
    script_content """printf "{'date':'%s' }" "`date`" > $AZ_SCRIPTS_OUTPUT_PATH """
    env_vars [
        EnvVar.createSecureParameter "foo" "secret-foo"
    ]
}

let template = arm {
    add_resource createFileScript
    location Location.NorthEurope
    output "fromscript" createFileScript.Outputs.["date"]
    output "name" "isaac"
    output "age" "21"
    output "mydate" "2020-11-07T20:09:45.4329796Z"
}

let outputs = template.Deploy<{| FromScript : string; Name : string; Age : int; MyDate : DateTime |}> "my-resource-group"

outputs.