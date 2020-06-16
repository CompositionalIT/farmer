---
title: "Web App"
date: 2020-02-05T08:53:46+01:00
weight: 23
chapter: false
---

#### Overview
The Web App builder is used to create Azure App Service accounts. It abstracts the Service Plan into the same component, and will also create and configure a linked App Insights resource. If you wish to create a website that connects to an existing service plan, use the `link_to_service_plan` keyword and provide the resource name of the service plan to connect to.

* Web Site (`Microsoft.Web/sites`)
* Web Host (`Microsoft.Web/serverfarms`)
* Application Insights (`Microsoft.Insights/components`)
* Site Extension (`siteextensions`)

#### Web App Builder Keywords

| Applies To | Keyword | Purpose |
|-|-|-|
| Web App | name | Sets the name of the web app. |
| Web App | link_to_service_plan | Instructs Farmer to link this webapp to an existing service plan rather than creating a new one. |
| Web App | app_insights_auto_name | Sets the name of the automatically-created app insights instance. |
| Web App | app_insights_off | Removes any automatic app insights creation, configuration and settings for this webapp. |
| Web App | link_to_app_insights | Instead of creating a new AI instance, configure this webapp to point to another AI instance that you are managing yourself. |
| Web App | run_from_package | Sets the web app to use "run from package" deployment capabilities. |
| Web App | website_node_default_version | Sets the node version of the web app. |
| Web App | setting | Sets an app setting of the web app in the form "key" "value". |
| Web App | settings | Sets a list of app setting of the web app as tuples in the form of ("key", "value"). |
| Web App | https_only | Disables http for this webapp so that only HTTPS is used. |
| Web App | enable_http2 | Configures the webapp to allow clients to connect over http2.0. |
| Web App | disable_client_affinity | Stops the webapp from sending client affinity cookies. |
| Web App | enable_websockets | Configures the webapp to allow clients to connect via websockets. |
| Web App | depends_on | Sets a dependency for the web app. |
| Web App | docker_image | Sets the docker image to be pulled down from Docker Hub, and the command to execute as a second argument. Automatically sets the OS to Linux. |
| Web App | docker_ci | Turns on continuous integration of the web app from the Docker source repository using a webhook.
| Web App | docker_use_azure_registry | Uses the supplied Azure Container Registry name as the source of the Docker image, instead of Docker Hub. You do not need to specify the full url, but just the name of the registry itself.
| Web App | enable_managed_identity | Creates a system-assigned identity for the web app. |
| Web App | disable_managed_identity | Deletes the system-assigned identity for the web app. |
| Web App | enable_cors | Enables CORS support for the app. Either specify `WebApp.AllOrigins` or a list of valid URIs as strings. |
| Service Plan | service_plan_name | Sets the name of the service plan. If not set, uses the name of the web app postfixed with "-plan". |
| Service Plan | always_on | Sets "Always On" flag. |
| Service Plan | runtime_stack | Sets the runtime stack. |
| Service Plan | operating_system | Sets the operating system. If Linux, App Insights configuration settings will be omitted as they are not supported by Azure App Service. |
| Service Plan | sku | Sets the sku of the service plan. |
| Service Plan | worker_size | Sets the size of the service plan worker. |
| Service Plan | number_of_workers | Sets the number of instances on the service plan. |

> **Farmer also comes with a dedicated Service Plan builder** that contains all of the above keywords that apply to a Service Plan.
>
> Use this builder if you wish to have an explicit and clear separation between your web app and service plan. Otherwise, it is recommended to use the service plan keywords that exist directly in the web app builder, and let Farmer handle the connections between them.

#### Post-deployment Builder Keywords
The Web App builder contains special commands that are executed *after* the ARM deployment is completed.

| Keyword | Purpose |
|-|-|
| zip_deploy | Supplying a folder or zip file will instruct Farmer to upload the contents directly to the App Service once the ARM deployment is complete. |

#### Configuration Members

| Member | Purpose |
|-|-|
| PublishingPassword | Gets the ARM expression path to the publishing password of this web app. |
| ServicePlan | Gets the Resource Name of the service plan for this web app. |
| AppInsights | Gets the Resource Name of the service plan for the AI resource linked to this web app. |
| SystemIdentity | Gets the system-created managed principal for the web app. It must have been enabled using enable_managed_identity. |

#### Example

```fsharp
open Farmer
open Farmer.Builders

let myWebApp = webApp {
    name "myWebApp"
    service_plan_name "myServicePlan"
    setting "myKey" "aValue"
    sku WebApp.B1
    always_on
    app_insights_off
    worker_size Medium
    number_of_workers 3
    run_from_package
}
```