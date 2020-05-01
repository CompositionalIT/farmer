---
title: "Container Registry"
date: 2020-04-30T19:10:46+02:00
weight: 3
chapter: false
---

#### Overview
The Container Registry builder is used to create Azure Container Registry (ACR) instances.

* Container Registry (`Microsoft.ContainerRegistry/registries`)

#### Builder Keywords
| Keyword | Purpose |
|-|-|
| name | Sets the name of the Container Registry instance. |
| sku | Sets the SKU of the instance. Defaults to Basic. |
| enable_admin_user | The value that indicates whether the admin user is enabled. |

#### Example
```fsharp
open Farmer
open Farmer.Resources

let myRegistry = containerRegistry {
    name "myRegistry"
    sku ContainerRegistrySku.Basic
    enable_admin_user
}
```