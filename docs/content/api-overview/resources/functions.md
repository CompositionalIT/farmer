---
title: "Functions"
date: 2020-02-05T08:53:46+01:00
chapter: false
weight: 6
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
| link_to_unmanaged_service_plan | Instructs Farmer to link this Functions instance to an existing service plan that is externally managed, rather than creating a new one. |
| link_to_storage_account | Do not create an automatic storage account; instead, link to a storage account that is created outside of this Functions instance but within this Farmer template. |
| link_to_unmanaged_storage_account | Do not create an automatic storage account; instead, link to an existing storage account that was created external to Farmer. |
| https_only | Disables http for this functions app so that only HTTPS is used. |
| app_insights_name | Sets the name of the automatically-created app insights instance. |
| app_insights_off | Removes any automatic app insights creation, configuration and settings for this webapp. |
| link_to_app_insights | Instead of creating a new AI instance, configure this webapp to point to another AI instance that you are managing yourself. |
| link_to_unmanaged_app_insights | Instructs Farmer to link this functions instance to an existing app insights instance that is externally managed, rather than creating a new one. |
| use_runtime | Sets the runtime of the Functions host. |
| use_extension_version | Sets the extension version of the functions host. |
| operating_system | Sets the operating system of the Functions host. |
| setting | Sets an app setting of the web app in the form "key" "value". |
| secret_setting | Sets a "secret" app setting of the function. You must supply the "key", whilst the value will be supplied as a secure parameter or an ARM expression. |
| settings | Sets a list of app setting of the web app as tuples in the form of ("key", "value"). |
| depends_on | [Sets dependencies for the web app.](../../dependencies/) |
| enable_cors | Enables CORS support for the app. Either specify AllOrigins or a list of valid URIs. |
| enable_cors_credentials | Allows CORS requests with credentials. |
| add_identity | Adds a managed identity to the the Function App. |
| system_identity | Activates the system identity of the Function App. |
| always_on | Stops the app from sleeping if idle for a few minutes of inactivity. |
| worker_process | Specifies whether to set the app to 32 or 64 Bitness. |

#### Post-deployment Builder Keywords
The Functions builder contains special commands that are executed *after* the ARM deployment is completed.

| Keyword | Purpose |
|-|-|
| zip_deploy | Supplying a folder or zip file will instruct Farmer to upload the contents directly to the Azure Functions once the ARM deployment is complete. |

#### Key Vault integration
The Function builder comes with special integration into KeyVault. By activating KeyVault integration, the function builder can automatically link to, or even create, a full KeyVault instance. All Secret or ARM Expression-based Settings (e.g. a setting that links to the Key of a Storage Account) will automatically be redirected to KeyVault. The value will be stored in KeyVault and the system identity will be activated and provided into the KeyVault with GET permissions. Lastly, Function app settings will remain in place, using the Azure Functions built-in KeyVault redirection capabilities.

The following keywords exist on the function:

| Member | Purpose |
|-|-|
| use_keyvault | Tells the function app to create a brand new KeyVault for this Function's secrets. |
| link_to_keyvault | Tells the function to use an existing Farmer-managed KeyVault which you have defined elsewhere. All secret settings will automatically be mapped into KeyVault. |
| link_to_unmanaged_keyvault | Tells the web app to use an existing non-Farmer managed KeyVault which you have defined elsewhere.  All secret settings will automatically be mapped into KeyVault. |


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
}
```
