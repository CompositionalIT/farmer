---
title: "Deployment Script"
date: 2020-10-22T15:19:14.3221080-04:00
chapter: false
weight: 5
---

#### Overview
The Deployment Script builder is used to execute Azure CLI scripts as part of an ARM deployment.

* Deployment Script (`Microsoft.Resources/deploymentScripts`)

#### Builder Keywords
| Keyword | Purpose |
|-|-|
| name | Sets the name of the deployment script resource. |
| arguments | List of arguments to pass to the script. |
| cli | Specifies the CLI runtime, default is az cli. |
| content | Sets script content for the resource. |
| env_vars | Defines environment variables in the script environment. |
| force_update_tag | A tag that cn be changed to force a resource update so the script is run again. |
| identity | Sets the user assigned identity for the deployment script resource (must be a contributor in the resource group). |
| primary_script_uri | Sets a URI to download script content. |
| retention_interval_days | Sets the days to retain the script runtime infrastructure to run again quickly. |
| script_content | Sets script content for the resource. |
| supporting_script_uris | Sets a URI to download additional content for the script. |
| timeout | Sets the maximum amount of time to allow the script to run. |
| add_tags | Adds tags to the script runtime resource. |
| add_tag | Adds a tag to the script runtime resource. |


#### Example
```fsharp
open Farmer
open Farmer.Builders
open Farmer.CoreTypes

/// The deployment script must run under an identity with any necessary permissions
/// to perform the commands in the script. Also must be a contributor in the 
/// resource group. 
let scriptIdentity = userAssignedIdentity {
    name "script-user"
}

/// The script identity must be a contributor over this resource group.
let scriptRole =
    role_assignment
        (ArmExpression.create("guid(resourceGroup().id)").Eval())
        Roles.Contributor
        scriptIdentity.PrincipalId

/// Define the parameters, identity, and content for the script
let getDateScript = deploymentScript {
    name "custom-script"
    identity scriptIdentity
    force_update_tag (System.DateTime.Now.ToString("o"))
    /// Format output as JSON and pipe to $AZ_SCRIPTS_OUTPUT_PATH to make it available as an output variable.
    content """printf "{'date':'%s'"} "`date`" > $AZ_SCRIPTS_OUTPUT_PATH """
}

/// The deployment runs the script and gets any output variables.
let template = arm {
    location Location.EastUS
    add_resource scriptIdentity
    add_resource scriptRole
    add_resource getDateScript
    output "date" "[reference('custom-script').outputs.date]"
}
```
