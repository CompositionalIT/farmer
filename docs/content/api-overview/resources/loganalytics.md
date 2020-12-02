---
title: "Log Analytics"
date: 2020-10-7T19:10:46+02:00
chapter: false
weight: 14
---

#### Overview

The Log Analytics builder is used to create Work space instances.

- Log Analytics (`Microsoft.OperationalInsights/workspaces`)

#### Builder Keywords

| Keyword          | Purpose                                                         |
| ---------------- | --------------------------------------------------------------- |
| name             | Sets the name of the log analytics instance.                    |
| retention_period | Sets the retention period for logs in days.                     |
| enable_ingestion | Enables ingestion network traffic.                              |
| enable_query     | Enables query network traffic.                                  |
| daily_cap        | Specifies an upper limit on the amount of data to ingest daily. |
| add_tags         | Adds a set of tags to the resource                              |
| add_tag          | Adds a tag to the resource                                      |

#### Example

```fsharp
open Farmer
open Farmer.Builders

let myAnalytics = logAnalytics {
    name "myloganalytics"
    retention_period 30<Days>
    enable_ingestion
    enable_query
    daily_cap 5<Gb>
    add_tag "tag1" "myTestResourceFarmer"
}

let deployment = arm {
    location Location.WestEurope
    add_resource myRegistry
}
```
