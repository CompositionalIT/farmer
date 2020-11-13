---
title: "Resource Groups"
date: 2020-02-05T08:53:46+01:00
chapter: false
weight: 23
---

#### Overview

The Resource Group builder creates resource groups and allows deploying resources into multiple resource groups

* Resource Groups (Microsoft.Resources/resourceGroups
* Deployments (Microsoft.Resources/deployments

Note that Resource Groups can only be created in subscription-scope ARM templates so the `scope` keyword must be used on the `ArmBuilder`.

#### Builder Keywords

| Keyword | Purpose |
|-|-|
| name | Specifies the name of the resource group |
| location | Sets the location of the resource group and its resources |
| add_resource | Adds a resource to be deployed in the resource group |
| add_resources | Adds resources to be deployed in the resource group |

#### Example

```fsharp
open Farmer
open Farmer.Builders
open Farmer.CoreTypes

let grp1Storage = storageAccount {
    name "grp1storage"
}

let grp2Storage = storageAccount {
    name "grp2storage"
}

let group1 = resourceGroup {
    name "grp1"
    add_resource grp1Storage
}

let group2 = resourceGroup {
    name "grp2"
    add_resource grp2Storage
}

let template = arm {
    scope Subscription
    add_resource resGroup1
    add_resource resGroup2
}

template
|> Deploy.executeSubscription Location.WestEurope Deploy.NoParameters
```