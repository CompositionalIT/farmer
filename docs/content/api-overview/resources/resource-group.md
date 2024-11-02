---
title: "Resource Group"
date: 2021-08-05T17:30:25-04:00
weight: 18
chapter: false
---

#### Overview
The Resource Group builder is always the top-level element of your deployment. It contains the manifest of all Farmer resources that you create.

#### Builder Keywords
| Keyword | Purpose |
|-|-|
| location | Sets the default location of all resources. |
| add_resource | Adds a resource to the template. |
| add_resources | Adds a collection of resources to the template. |
| add_arm_resources | Adds a collection of lower-level IArmResources to the template. |
| add_parameter_values | Adds a collection of parameter values to pass to the nested deployment's parameters. |
| add_secret_references | Adds a collection of key vault secret references to pass to the nested deployment's parameters. |
| output | Creates an output value that will be returned by the ARM template. Since Farmer does not require variables, and the only parameters supported are secure strings, these will typically be ARM expressions that are generated at deployment-time, such as the publishing password of a web app or the fully-qualified domain name of a SQL instance etc. |
| outputs | Create multiple outputs. |
| add_tag | Add a tag to the resource group for top-level instances or to the deployment for nested resource groups |
| add_tags | Add multiple tags to the resource group for top-level instances or to the deployment for nested resource groups |
| name | the name of the resource group (only used for nested resource group deployments) |
| subscription_id | On nested resource group deployments, specify the target subscription. |
| deployment_name | allows manual customisation of the deployment name. By default this will be generated for you. (only used for nested resource group deployments)|

#### Utilities
* The `createResourceGroup` function is used to define a resource group deployment resource by name and location, useful when deploying to a subscription.

#### Example
```fsharp
let deployment =
    arm {
        // All resources will share this location
        location Location.NorthEurope

        // Assume myStorageAccount and myWebApp have been defined...
        add_resource myStorageAccount
        add_resource myWebApp

        // Assuming the role assignments have been defined....
        add_arm_resources [ roleAssignment1; roleAssignment2 ]

        add_resource (resourceGroup {
            name "nestedResourceGroup"

            add_resource myOtherStorage
        })

        output "webAppName" myWebApp.Name
        output "webAppPassword" myWebApp.PublishingPassword

        add_tag "deployed-by" "farmer"
    }
```
