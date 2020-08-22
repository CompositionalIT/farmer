---
title: "Maps"
date: 2020-05-26T11:24:00+01:00
chapter: false
weight: 15
---

#### Overview
The Maps builder creates Azure Maps accounts.

* Maps (`Microsoft.Maps/accounts`)

#### Builder Keywords
| Keyword | Purpose |
|-|-|
| name | Sets the name of the Azure Maps account. |
| sku | Sets the sku of the Azure Maps account. |

#### Example

```fsharp
open Farmer
open Farmer.Builders

let myMaps = maps {
    name "mymaps"
    sku Maps.S0
}
```