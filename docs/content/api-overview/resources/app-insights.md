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
| production_sampling | Sets sampling to 20% - recommended for high-traffic production apps (keeps all errors, samples successes to reduce costs). |
| development_sampling | Sets sampling to 100% - recommended for development environments where you want to see all telemetry. |
| log_analytics_workspace | Use a Log Analytics workspace as the backing store for this AI instance. You can supply either a Farmer-generate Log Analytics`WorkspaceConfig` instance that exists in the same resource group, or a fully-qualified Resource ID path to that instance. This will also switch the AI instance over to create a "workspace enabled" AI instance. |

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

#### Production Sampling Example
For high-traffic production applications, use `production_sampling` to reduce costs while keeping all errors:

```fsharp
open Farmer
open Farmer.Builders

// Production: 20% sampling (keeps all errors, samples successes)
let prodInsights = appInsights {
    name "high-traffic-api-insights"
    production_sampling
}

// Development: 100% sampling (see all telemetry)
let devInsights = appInsights {
    name "dev-api-insights"
    development_sampling
}
```

> **Note**: Farmer will warn you if you set sampling to 100% for production workloads, as this can be expensive for high-traffic applications.
