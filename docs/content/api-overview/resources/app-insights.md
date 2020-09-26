---
title: "App Insights"
date: 2020-02-05T08:53:46+01:00
weight: 1
chapter: false
---

#### Overview
The App Insights builder is used to create Application Insights accounts. Use this if you need a standalone AI instance; if you need one for a web app, the web app will create one by default and configure the application settings automatically.

* Application Insights (`Microsoft.Insights/components`)

#### Builder Keywords

| Keyword | Purpose |
|-|-|
| name | Sets the name of the App Insights instance. |
| disable_ip_masking | Disable IP masking. |
| sampling_percentage | Define sampling percentage (0-100) |

#### Configuration Members

| Member | Purpose |
|-|-|
| InstrumentationKey | Gets the ARM expression path to the instrumentation key of this App Insights instance. |

#### Example

```fsharp
open Farmer
open Farmer.Builders

let ai = appInsights {
    name "myAI"
}
```