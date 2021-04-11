---
title: " Diagnostic Settings "
date: 2020-12-02T12:10:03 +00:00
chapter: false
weight: 4
---

#### Overview

The Diagnostic Settings builder is used to create diagnostic settings instances to send platform logs and metrics to different destinations (storage, event hub and log analytics). Support for Farmer builders and external resources is supported.

- Diagnostic Settings (`providers/diagnosticSettings`)

#### Builder Keywords

| Keyword | Purpose|
|-|-|
| name | Sets the name of the Diagnostic Settings resource. |
| metrics_source | The resource that will be used for the source of logging and metrics information. Can be any Builder, or you can supply a ResourceId for an external resource. |
| capture_metrics | Specifies the list of Metrics to capture from the source resource. |
| capture_logs | Specifies the list of Log Categories to capture from the source resource. |
| add_destination | Adds a destination for all logs and metrics, either a storage account, log analytics workspacce, event hub or a Resource ID pointing to any valid Resource for those three resource types. |
| event_hub_destination_name | Allows you to override the event hub name to use. |
| loganalytics_output_type | If a Log Analytics Workspace is specified as output, specifies whether to use the default Azure Diagnostics grouping or a dedicated grouping for logging and metrics. |

#### Example
The example below illustrates how to create a web application and set up a diagnostics setting against it,
whilst setting up three destinations for the diagnostics (storage, event hub and log analytics). Also notice
the using of the `Logging.` namespace, which contains all documented Logging categories.

```fsharp
open Farmer
open Farmer.Builders
open Farmer.DiagnosticSettings

let data = storageAccount { name "isaacsuperdata" }
let hub = eventHub { name "isaacsuperhub" }
let logs = logAnalytics { name "isaacsuperlogs" }
let web = webApp { name "isaacdiagsuperweb"; app_insights_off }

let mydiagnosticSetting = diagnosticSettings {
    name "myDiagnosticSetting"
    metrics_source web

    add_destination data
    add_destination logs
    add_destination hub
    loganalytics_output_type Dedicated
    capture_metrics [ "AllMetrics" ]
    capture_logs [
        Logging.Web.Sites.AppServicePlatformLogs
        Logging.Web.Sites.AppServiceAntivirusScanAuditLogs
    ]
}

let deployment = arm {
    add_resources [ data; web; hub; logs; mydiagnosticSetting ]
}
```