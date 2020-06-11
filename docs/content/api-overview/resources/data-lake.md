---
title: "Data Lake"
date: 2020-06-11T00:55:30+02:00
weight: 4
chapter: false
---

#### Overview
The Data Lake builder is used to create Azure Data Lake instances.

* Data Lake (`Microsoft.DataLakeStore/accounts`)

#### Builder Keywords
| Keyword | Purpose |
|-|-|
| name | Sets the name of the Cognitive Services instance. |
| sku | Sets the SKU of the instance. Defaults to Consumption. |
| enable_encryption | Turns on data lake encryption. |

#### Example
```fsharp
open Farmer
open Farmer.Builders

let myLake = dataLake {
    name "myDataLake"
    sku DataLake.Commitment_100TB
    enable_encryption
}
```