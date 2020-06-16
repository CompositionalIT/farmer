---
title: "Functions"
date: 2020-02-05T08:53:46+01:00
weight: 6
chapter: false
---

#### Overview
The Functions builder is used to create Azure Functions accounts. It abstracts the App Host and Service Plan into the same component, and will also create and configure a linked App Insights resource. In addition, it will automatically create a backing storage account required by the functions runtime.

* Web Site (`Microsoft.Web/sites`)
* Web Host (`Microsoft.Web/serverfarms`)
* Application Insights (`Microsoft.Insights/components`)
* Storage Accounts (`Microsoft.Storage/storageAccounts`)

#### Builder Keywords

| Keyword | Purpose |
|-|-|
| name | Sets the name of the functions instance. |
| service_plan_name | Sets the name of the service plan hosting the function instance. |
| link_to_service_plan | Instructs Farmer to link this webapp to an existing service plan rather than creating a new one. |
| link_to_storage_account | Do not create an automatic storage account; instead, link to a storage account that is created outside of this Functions instance. |
| https_only | Disables http for this functions app so that only HTTPS is used. |
| app_insights_auto_name | Sets the name of the automatically-created app insights instance. |
| app_insights_off | Removes any automatic app insights creation, configuration and settings for this webapp. |
| link_to_app_insights | Instead of creating a new AI instance, configure this webapp to point to another AI instance that you are managing yourself. |
| use_runtime | Sets the runtime of the Functions host. |
| use_extension_version | Sets the extension version of the functions host. |
| operating_system | Sets the operating system of the Functions host. |
| setting | Sets an app setting of the web app in the form "key" "value". |
| depends_on | Sets a dependency for the web app. |
| enable_cors | Enables CORS support for the app. Either specify AllOrigins or a list of valid URIs. |

#### Configuration Members

| Member | Purpose |
|-|-|
| PublishingPassword | Gets the ARM expression path to the publishing password of this functions app. |
| StorageAccountKey | Gets the ARM expression path to the storage account key of this functions app. |
| AppInsightsKey | Gets the ARM expression path to the app insights key of this functions app, if it exists. |
| DefaultKey | Gets the ARM expression path for the default key of this functions app. |
| MasterKey | Gets the ARM expression path for the master key of this functions app. |

#### Example
```fsharp
open Farmer
open Farmer.Builders

let myFunctions = functions {
    name "myWebApp"
    service_plan_name "myServicePlan"
    setting "myKey" "aValue"
    app_insights_off
}```