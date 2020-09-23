---
title: "Cognitive Services"
date: 2020-04-10T08:53:46+01:00
chapter: false
weight: 4
---

#### Overview
The Cognitive Services builder is used to create Azure Cognitive Services instances.

* Cognitive Services (`Microsoft.CognitiveServices/accounts`)

#### Builder Keywords
| Keyword | Purpose |
|-|-|
| name | Sets the name of the Cognitive Services instance. |
| sku | Sets the SKU of the instance. Defaults to F0 (free). |
| api | Specifies the Kind of api to use for the service instance. Defaults to `AllInOne`. |

#### Configuration Members

| Member | Purpose |
|-|-|
| Key | Gets the ARM expression path to the Key of this Cognitive Services instance. |

#### Example
```fsharp
open Farmer
open Farmer.Builders

let translator = cognitiveServices {
    name "mytranslator"
    sku CognitiveServices.F0
    api CognitiveServices.AnomalyDetector
}

let key : ArmExpression = translator.Key
```