---
title: "Log Analytics"
date: 2020-10-7T19:10:46+02:00
chapter: false
weight: 1
---

#### Overview
The Log Analytics builder is used to create Work space instances.

* Log Analytics (`Microsoft.OperationalInsights/workspaces`)

#### Builder Keywords
| Keyword | Purpose |
|-|-|
| name | Sets the name of the log analytics instance. |
| sku | Sets the SKU of the instance. Defaults to PerGB. |
| retentionInDays | Sets the value of the retention Days. |
| publicNetworkAccessForIngestion |Sets The network access type for accessing Log Analytics ingestion.|
|publicNetworkAccessForQuery |Sets the network access type for accessing Log Analytics query.|
#### Example
```fsharp
open Farmer
open Farmer.Builders
open Farmer.LogAnalytics

let myLogAnalytics = logAnalytics {
    name "myLogAnalytics"
    sku PerGB2018
    publicNetworkAccessForIngestion
    publicNetworkAccessForQuery
    retentionInDays 30 
}
```

