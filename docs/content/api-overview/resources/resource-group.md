---
title: "Resource Group"
date: 2020-02-05T08:53:46+01:00
weight: 1
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
| output | Creates an output value that will be returned by the ARM template. Since Farmer does not require variables, and the only parameters supported are secure strings, these will typically be an ARM expressions that are generated at deployment-time, such as the publishing password of a web app or the fully-qualified domain name of a SQL instance etc. |
| add_tag | Add a tag to the resource group for top-level instances or to the deployment for nested resource groups |
| add_tags | Add multiple tags to the resource group for top-level instances or to the deployment for nested resource groups |
| name | the name of the resource group (only used for nested resource group deployments) |

#### Example
```fsharp
let deployment =
    arm { 
        // All resources will share this location
        location Location.NorthEurope

        // Assume myStorageAccount and myWebApp have been defined...
        add_resource myStorageAccount
        add_resource myWebApp

        add_resource (resourceGroup {
            name "nestedResourceGroup"

            add_resource myOtherStorage
        })

        output "webAppName" myWebApp.Name
        output "webAppPassword" myWebApp.PublishingPassword

        add_tag "deployed-by" "farmer"
    }
```
