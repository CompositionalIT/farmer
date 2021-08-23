---
title: "Dashboard"
date: 2021-08-13T07:00:46+01:00
weight: 1
chapter: false
---

#### Overview

Dashboards are a focused and organized view of your cloud resources in the Azure portal. Use dashboards as a workspace where you can monitor resources and quickly launch tasks for day-to-day operations. 

* Microsoft.Portal dashboards (`Microsoft.Portal/dashboards`)

#### Builder Keywords

| Keyword | Purpose |
|-|-|
| name | Sets the name of the dashboard. |
| title | Sets the visible title of the dashboard. Default: Same as name |
| metadata | Sets the metadata for the dashboard. Pre-defined DashboardMetadata objects: EmptyMetadata, CustomMetadata and Cache24h |
| add_markdown_part | Create markdown lens part for the dashboard |
| add_video_part | Create video part for the dashboard |
| add_virtual_machine_icon | Create virtual machine status part for the dashboard |
| add_metrics_chart | Create metrics results part for the resource given in parameters |
| add_webtest_results_part |  Create webtest results part for the dashboard |
| add_monitor_chart | Create metrics results part for the resource given in parameters |
| add_custom_lens | Create your own lens part for the dashboard |

#### Example

This example generally follows the simple example of https://docs.microsoft.com/en-us/azure/azure-portal/azure-portal-dashboards-structure

```fsharp
open Farmer
open Farmer.Builders

let vm = vm { name "foo"; username "foo" }
let vmId = (vm :> IBuilder).ResourceId
let dash = dashboard { 
    name "myDashboard" 
    title "Monitoring"
    depends_on vm
    add_markdown_part (
        { x = 0; y = 0; rowSpan = 2; colSpan = 3 },
        { title = ""; subtitle = ""; content = "## Azure Virtual Machines Overview\r\nNew team members should watch this video to get familiar with Azure Virtual Machines." }
    )
    add_markdown_part (
        { x = 3; y = 0; rowSpan = 4; colSpan = 8 },
        { title = "Test VM Dashboard"; subtitle = "Contoso"; content = "This is the team dashboard for the test VM we use on our team. Here are some useful links:\r\n\r\n1. [Getting started](https://www.contoso.com/tsgs)\r\n1. [Troubleshooting guide](https://www.contoso.com/tsgs)\r\n1. [Architecture docs](https://www.contoso.com/tsgs)" }
    )
    add_video_part (
        { x = 3; y = 0; rowSpan = 4; colSpan = 8 },
        { title = ""; subtitle = ""; url = "https://www.youtube.com/watch?v=YcylDIiKaSU&list=PLLasX02E8BPCsnETz0XAMfpLR1LIBqpgs&index=4" }
    )
    add_metrics_chart (
        { x = 0; y = 4; rowSpan = 3; colSpan = 11 },
        { interval = (System.TimeSpan(1,0,0) |> IsoDateTime.OfTimeSpan); 
          metrics = [ Farmer.Arm.Dashboard.ChartResources.PercentageCPU ]; 
          resourceId = vmId }
    )
    add_virtual_machine_icon ({ x = 9; y = 7; rowSpan = 2; colSpan = 2 }, vmId)
}
```

