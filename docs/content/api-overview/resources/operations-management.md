---
title: "Operations Management"
date: 2022-04-29T09:40:00-04:00
weight: 15
chapter: false
---

#### Overview
The Operations Management builder is used to create [Solutions](https://docs.microsoft.com/en-us/azure/templates/microsoft.operationsmanagement/solutions?tabs=bicep) for a [Log Analytics Workspace](https://docs.microsoft.com/en-us/azure/azure-monitor/logs/log-analytics-workspace-overview).

#### Builder Keywords

| Builder | Keyword | Purpose |
|-|-|-|
| omsPlan | name | The name of the plan, which can match the name of the overall Solution. |
| omsPlan | publisher | The publisher of the solution, usually "Microsoft" (the default value). |
| omsPlan | product | The specific solution being created, such as `OMGSGallery/SecurityInsights`. |
| omsProperties | workspace | The Log Analytics workspace this solution uses. |
| omsProperties | add_contained_resource | Adds a resource contained by this solution. |
| omsProperties | add_contained_resources | Adds multiple resources contained by this solution. |
| omsProperties | add_referenced_resource | Adds a resource referenced by this solution. |
| omsProperties | add_referenced_resources | Adds multiple resources referenced by this solution. |
| oms | name | The name of the solution. |
| oms | plan | The `OMSPlan` for the solution. |
| oms | properties | The `OMSProperties` for the solution. |
| oms | add_tag | Add a tag to the solution. |
| oms | add_tags | Add one or more tags to the solution. |

#### Example

This example creates an Azure Sentinel solution on an Log Analytics Workspace.

```fsharp
open Farmer
open Farmer.Builders

let sentinelWorkspace = logAnalytics {
    name "my-sentinel-workspace"
    retention_period 30<Days>
    enable_query
    daily_cap 5<Gb>
}

let solutionName = $"SecurityInsights({sentinelWorkspace.Name.Value})"

let sentinelSolution = oms {
    name solutionName
    plan (omsPlan {
        name solutionName
        publisher "Microsoft"
        product "OMSGallery/SecurityInsights"
    })
    properties(omsProperties {
        workspace sentinelWorkspace
    })
}

let deployment = arm {
  location Location.NorthCentralUS
  add_resource sentinelWorkspace
  add_resource sentinelSolution
}
```


