---
title: "Resource Group"
date: 2020-02-05T08:53:46+01:00
weight: 2
chapter: false
---

#### Overview
The Resource Group deployment builder is used to create an Azure Resource Groups and deploy resources to it. It may be used as a top-level element in your deployment or nested within a Subscription Deployment or within another Resource Group deployment.

* Resource Groups (`Microsoft.Resources/resourceGroups`)
* Deployments (`Microsoft.Resources/deployments`)

#### Builder Keywords
| Keyword | Purpose |
|-|-|
| name | Sets the name of the resource group |
| location | Sets the default location of all resources. |
| add_resource | Adds a resource to the template. |
| add_resources | Adds a collection of resources to the template. |
| output | Creates an output value that will be returned by the ARM template. Since Farmer does not require variables, and the only parameters supported are secure strings, these will typically be an ARM expressions that are generated at deployment-time, such as the publishing password of a web app or the fully-qualified domain name of a SQL instance etc. |
| add_tag | Adds a tag to the resource group |
| add_tags | Adds multiple tags to the resource group |

#### Example
```fsharp
let deployment =
    resourceGroup { // `arm` may also be used an alias for `resourceGroup`
        // All resources will share this location
        location Location.NorthEurope

        // Assume myStorageAccount and myWebApp have been defined...
        add_resource myStorageAccount
        add_resource myWebApp

        output "webAppName" myWebApp.Name
        output "webAppPassword" myWebApp.PublishingPassword
    }
```

#### Remarks
When used at the top-level or within a subscription deployment, the Resource Group builder is capable of creating a resource group and deploying resources to it at the same time. However, resource groups cannot be created within other resource groups so when nesting a resource group builder within another resource group builder, the nested resource group must already exist - only the resource will e created.
```fsharp
let deployment = 
    let nestedRg = resourceGroup { 
        name "nested-rg" 
        add_resource (storageAccount {name "mystorage" })
    }
    resourceGroup {
        name "outer-rg"

        // nested-rg cannot be created automatically so must already exist.
        // Resources within nestedRg (mystorage), will be created as expected.
        add_resource nestedRg
    }
```