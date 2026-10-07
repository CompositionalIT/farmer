---
title: "Log Analytics"
date: 2020-10-7T19:10:46+02:00
chapter: false
weight: 12
---

#### Overview

The Log Analytics builder is used to create Work space instances.

- Log Analytics (`Microsoft.OperationalInsights/workspaces`)
- Tables (`Microsoft.OperationalInsights/workspaces/tables`)

#### Log Analytics Builder Keywords

| Keyword          | Purpose                                                         |
| ---------------- | --------------------------------------------------------------- |
| name             | Sets the name of the log analytics instance.                    |
| retention_period | Sets the retention period for logs in days.                     |
| enable_ingestion | Enables ingestion network traffic.                              |
| enable_query     | Enables query network traffic.                                  |
| daily_cap        | Specifies an upper limit on the amount of data to ingest daily. |
| tables           | Defines tables to be created in the workspace.                  |
| add_tags         | Adds a set of tags to the resource                              |
| add_tag          | Adds a tag to the resource                                      |

#### Table Builder Keywords

| Keyword               | Purpose                                                                                                                               |
| --------------------- | --------------------------------------------------------------------------------------------------------------------------------------|
| Name                  | Sets the name of the table.                                                                                                           |
| Plan                  | Sets the table plan. Analytics plans can set a retention period between 4 and 730. If ommited will default to the workspace retention.|
| Columns               | Sets the columns of the table. Each column has a name and type.                                                                       |
| TotalRetentionInDays  | Sets the total retention period for the table in days, between 4 and 4383. If ommited will default to the Plan retention.             |

#### Configuration Members

| Member | Purpose |
|-|-|
| CustomerID | Gets the ARM expression path to the customer ID of this LogAnalytics instance. |
| CustomerID | Gets the ARM expression path to the primary shared key of this LogAnalytics instance. |

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
    tables [
        {
            Name = ResourceName "Serilog"
            Plan = Analytics (Some 30<Days>)
            Columns = [
                { Name = "TimeGenerated"; Type = "datetime" }
                { Name = "Event"; Type = "dynamic" }
            ]
            TotalRetentionInDays = None
        }
    ]
    add_tag "tag1" "myTestResourceFarmer"
}

let deployment = arm {
    location Location.WestEurope
    add_resource myAnalytics
}
```
