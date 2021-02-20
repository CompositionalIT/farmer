---
title: "Container Registry"
date: 2020-04-30T19:10:46+02:00
chapter: false
weight: 3
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

#### Configuration Members

| Member | Purpose |
|-|-|
| Password | Gets the ARM expression path to the first admin password of this container registry if admin user was enabled. |
| Password2 | Gets the ARM expression path to the second admin password of this container registry if admin user was enabled. |
| Username | Gets the ARM expression path to the admin username of this container registry if admin user was enabled. |

#### Example
```fsharp
open Farmer
open Farmer.Builders

let myRegistry = containerRegistry {
    name "myRegistry"
    sku ContainerRegistry.Basic
    enable_admin_user
}
```