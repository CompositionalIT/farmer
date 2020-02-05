---
title: "Web App"
date: 2020-02-05T08:53:46+01:00
weight: 2
chapter: false
---

#### Overview
The Web App builder is used to create Azure App Service accounts. It abstracts the Service Plan into the same component, and will also create and configure a linked App Insights resource.

* Web Site (`Microsoft.Web/sites`)
* Web Host (`Microsoft.Web/serverfarms`)
* Application Insights (`Microsoft.Insights/components`)
* Site Extension (`siteextensions`)

#### Builder Keywords

| Keyword | Purpose |
|-|-|
| name | Sets the name of the web app. |
| service_plan_name | Sets the name of the service plan. If not set, uses the name of the web app postfixed with "-plan". |
| sku | Sets the sku of the service plan. |
| worker_size | Sets the size of the service plan worker. |
| number_of_workers | Sets the number of instances on the service plan. |
| app_insights_auto_name | Sets the name of the automatically-created app insights instance. |
| app_insights_off | Removes any automatic app insights creation, configuration and settings for this webapp. |
| app_insights_manual | Instead of creating a new AI instance, configure this webapp to point to another AI instance that you are managing yourself. |
| run_from_package | Sets the web app to use "run from package" deployment capabilities. |
| website_node_default_version | Sets the node version of the web app. |
| setting | Sets an app setting of the web app in the form "key" "value". |
| depends_on | Sets a dependency for the web app. |
| always_on | Sets "Always On" flag. |
| runtime_stack | Sets the runtime stack. |
| operating_system | Sets the operating system. |

#### Configuration Members

| Member | Purpose |
|-|-|
| PublishingPassword | Gets the ARM expression path to the publishing password of this web app. |

#### Example

```fsharp
open Farmer
open Farmer.Resources

let myWebApp = webApp {
    name "myWebApp"
    service_plan_name "myServicePlan"
    setting "myKey" "aValue"
    sku Sku.B1
    always_on
    app_insights_off
    worker_size Medium
    number_of_workers 3
    run_from_package
}
```