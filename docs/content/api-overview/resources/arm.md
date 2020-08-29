---
title: "ARM Deployment"
date: 2020-02-05T08:53:46+01:00
weight: 2
chapter: false
---

#### Overview
The ARM deployment builder is always the top-level element of your deployment. It contains the manifest of all Farmer resources that you create.

#### Builder Keywords
| Keyword | Purpose |
|-|-|
| location | Sets the default location of all resources. |
| add_resource | Adds a resource to the template. |
| add_resources | Adds a collection of resources to the template. |
| output | Creates an output value that will be returned by the ARM template. Since Farmer does not require variables, and the only parameters supported are secure strings, these will typically be an ARM expressions that are generated at deployment-time, such as the publishing password of a web app or the fully-qualified domain name of a SQL instance etc. |

#### Example
```fsharp
let deployment =
    arm {
        // All resources will share this location
        location Location.NorthEurope

        // Assume myStorageAccount and myWebApp have been defined...
        add_resource myStorageAccount
        add_resource myWebApp

        output "webAppName" myWebApp.Name
        output "webAppPassword" myWebApp.PublishingPassword
    }
```
