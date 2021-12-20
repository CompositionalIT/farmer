---
title: "Communication Services"
date: 2021-04-28T23:33:46+01:00
chapter: false
weight: 3
---

#### Overview
The Communication Services builder is used to create Azure Communication Services instances.

* Communication Services (`Microsoft.Communication/communicationServices`)

#### Builder Keywords
| Keyword | Purpose |
|-|-|
| name | Sets the name of the Bing Search instance. |
| data_location | Sets the `dataLocation` property of the instance. Defaults to `United States` |

#### Configuration Members

| Member | Purpose |
|-|-|
| Key | Gets the ARM expression path to the Key of this Bing Search instance. |

#### Example
```fsharp
open Farmer
open Farmer.Builders

let tags = [ "a", "1"; "b", "2" ]
let cs = communicationServices {
    name "test"
    add_tags tags
    data_location DataLocation.Australia
}

let key : ArmExpression = cs.Key
```