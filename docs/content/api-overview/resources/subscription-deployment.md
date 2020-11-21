---
title: "Subscription Deployment"
date: 2020-02-05T08:53:46+01:00
chapter: false
weight: 2
---

#### Overview

The Subscription Deployment builder is a top-level element used for deploying one or more resource groups to a subscription.

#### Builder Keywords
| Keyword | Purpose |
|-|-|
| location | Sets the location in which deployment metadat should be stored. |
| add_resource | Adds a subscription-level resource to the template. |
| add_resources | Adds a collection of subscription-level resources to the template. |

#### Example
```fsharp
let deployment =
    let rg1 = resourceGroup {
        // All resources will share this location
        name "my-storage-group"

        // Assume myStorageAccount and myWebApp have been defined...
        add_resource myStorageAccount
    }
    let rg2 = resourceGroup {
        name "my-web-group"
        // Assume myStorageAccount and myWebApp have been defined...
        add_resource myWebApp

        output "webAppName" myWebApp.Name 
        output "webAppPassword" myWebApp.PublishingPassword
    }
    subscriptionDeployment {
        add_resource rg1
        add_resource rg2
    }
```

#### Remarks
The subscription deployment builder does not support adding outputs directly. However it will automatically expose any outputs of any resource groups added to it.