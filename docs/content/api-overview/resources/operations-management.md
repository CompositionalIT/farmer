---
title: "Operations Management"
date: 2022-04-29T09:40:00-04:00
weight: 18
chapter: false
---

#### Overview
The Operations Management builder is used to create [Solutions](https://docs.microsoft.com/en-us/azure/templates/microsoft.operationsmanagement/solutions?tabs=bicep) for a [Log Analytics Workspace](https://docs.microsoft.com/en-us/azure/azure-monitor/logs/log-analytics-workspace-overview).

### Builder Keywords

| Builder | Keyword | Purpose |
|-|-|-|
| solutionPlan | name | The name of the plan, which can match the name of the overall Solution. |
| solutionPlan | publisher | The publisher of the solution, usually "Microsoft" (the default value). |
| solutionPlan | product | The specific solution being created, such as `OMGSGallery/SecurityInsights`. |
| solutionProperties | workspace | The Log Analytics workspace this solution uses. |
| solutionProperties | contains | Resources contained by this solution. |
| solutionProperties | references | Resources referenced by this solution. |
| solution | name | The name of the solution. |
| solution | plan | The `SolutionPlan` for the solution. |
| solution | properties | The `SolutionProperties` for the solution. |
| solution | add_tag | Add a tag to the solution. |
| solution | add_tags | Add one or more tags to the solution. |

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

let sentinelSolution = solution {
    name solutionName
    plan (solutionPlan {
        name solutionName
        publisher "Microsoft"
        product "OMSGallery/SecurityInsights"
    })
    properties(solutionProperties {
        workspace sentinelWorkspace
    })
}

let deployment = arm {
  location Location.NorthCentralUS
  add_resource sentinelWorkspace
  add_resource sentinelSolution
}
```


