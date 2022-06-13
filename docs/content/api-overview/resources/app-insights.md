---
title: "App Insights"
date: 2020-02-05T08:53:46+01:00
weight: 1
chapter: false
---

#### Overview
The App Insights builder is used to create Application Insights accounts. Use this if you need a standalone AI instance; if you need one for a web app, the web app will create one by default and configure the application settings automatically.

* Application Insights (`Microsoft.Insights/components`)

> This builder supports both "Classic" (standalone) and "Workspace Enabled" (Log Analytics-backed) instances of App Insights. See the `log_analytics_workspace` keyword to see how to create the latter type of instance.

#### Builder Keywords

| Keyword | Purpose |
|-|-|
| name | Sets the name of the App Insights instance. |
| disable_ip_masking | Disable IP masking. |
| sampling_percentage | Define sampling percentage (0-100) |
| log_analytics_workspace | Use a Log Analytics workspace as the backing store for this AI instance. You can supply either a Farmer-generate Log Analytics`WorkspaceConfig` instance that exists in the same resource group, or a fully-qualified Resource ID path to that instance. This will also switch the AI instance over to creating a "workspace enabled" AI instance. |

#### Configuration Members

| Member | Purpose |
|-|-|
| InstrumentationKey | Gets the ARM expression path to the instrumentation key of this App Insights instance. |
| ConnectionString | Gets the ARM expression path to the connection string of this App Insights instance. |

#### Example

```fsharp
open Farmer
open Farmer.Builders

let ai = appInsights {
    name "myAI"
    log_analytics_workspace myWorkspace // use to activate workspace-enabled AI instances.
}
```