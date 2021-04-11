---
title: "Bing Search"
date: 2021-01-29T07:33:46+01:00
chapter: false
weight: 2
---

#### Overview
The Bing Search builder is used to create Azure Bing Search instances.

* Bing Search (`Microsoft.Bing/accounts`, kind: `Bing.Search.v7`)

#### Builder Keywords
| Keyword | Purpose |
|-|-|
| name | Sets the name of the Bing Search instance. |
| sku | Sets the SKU of the instance. Defaults to `F1` (free). |
| statistics | Sets the `statisticsEnabled` property of the instance. Defaults to `false` |

#### Configuration Members

| Member | Purpose |
|-|-|
| Key | Gets the ARM expression path to the Key of this Bing Search instance. |

#### Example
```fsharp
open Farmer
open Farmer.Builders

let tags = [ "a", "1"; "b", "2" ]
let translator = bingSearch {
    name "test"
    sku S0
    add_tags tags
    statistics Enabled
}

let key : ArmExpression = translator.Key
```