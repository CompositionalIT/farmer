# Farmer

An F# DSL for rapidly generating non-complex ARM templates. This isn't a replacement for ARM templates,
nor some sort of compete offering with Pulumi or similar. It's designed mostly as an experiment at this stage -
things will change rapidly and there will be lots of breaking changes both in terms of namespace and API design.

**THIS IS PROTOTYPE CODE. USE AT YOUR OWN RISK.**

## Main features

* Create non-complex ARM templates through a simple, strongly-typed and pragmatic DSL.
* Create strongly-typed dependencies to resources.
* Just F# - use standard F# code to dynamically create ARM templates quickly and easily.

## Plans
* Support for more common ARM resource types
* Support more complex interactions with resources
* Look to simplify the DSL even more e.g. auto-detect parameters and dependencies.

### Currently Supported Resources
* Storage
* App Service
* Application Insights
* Cosmos DB
* Azure SQL
* Functions

## What does it look like?
This is an example Farmer value:

```fsharp
open Farmer

// Create a storage resource with Premium LRS
let myStorage = storageAccount {
    name "mystorage"                        // set account name
    sku Storage.Sku.PremiumLRS              // use Premium LRS
}

// Create a web application resource
let myWebApp = webApp {
    name "mysuperwebapp"                    // set web app name
    sku WebApp.Sku.S1                       // use S1 size
    setting "storage_key" myStorage.Key     // set an app setting to the storage account key
    depends_on myStorage                    // set the dependency
}

// Create the ARM template using those two resources
let template = arm {
    location Locations.NorthEurope    
    resource myStorage
    resource myWebApp

    // also output a couple of values generated at deployment-time
    output "storage_key" myStorage.Key
    output "web_password" myWebApp.PublishingPassword
}

/// Export the template to a file.
template
|> Writer.toJson
|> Writer.toFile "webapp-appinsights.json"
```

This ends up looking like this, expanding from around 15 lines of more-or-less strongly type code to around 100 lines of more-or-less weakly typed JSON:

```json
{
    "$schema": "https://schema.management.azure.com/schemas/2015-01-01/deploymentTemplate.json#",
    "contentVersion": "1.0.0.0",
    "outputs": {
        "storage_key": {
            "type": "string",
            "value": "[concat('DefaultEndpointsProtocol=https;AccountName=mystorage;AccountKey=', listKeys('mystorage', '2017-10-01').keys[0].value)]"
        },
        "web_password": {
            "type": "string",
            "value": "[list(resourceId('Microsoft.Web/sites/config', 'mysuperwebapp', 'publishingcredentials'), '2014-06-01').properties.publishingPassword]"
        }
    },
    "parameters": {},
    "resources": [
        {
            "apiVersion": "2016-09-01",
            "location": "northeurope",
            "name": "mysuperwebapp-plan",
            "properties": {
                "name": "mysuperwebapp-plan",
                "perSiteScaling": false,
                "reserved": false
            },
            "sku": {
                "name": "S1",
                "numberOfWorkers": 1,
                "size": "0",
                "tier": "Standard"
            },
            "type": "Microsoft.Web/serverfarms"
        },
        {
            "apiVersion": "2016-08-01",
            "dependsOn": [
                "mysuperwebapp-plan",
                "mystorage",
                "mysuperwebapp-ai"
            ],
            "location": "northeurope",
            "name": "mysuperwebapp",
            "properties": {
                "serverFarmId": "mysuperwebapp-plan",
                "siteConfig": {
                    "appSettings": [
                        {
                            "name": "storage_key",
                            "value": "[concat('DefaultEndpointsProtocol=https;AccountName=mystorage;AccountKey=', listKeys('mystorage', '2017-10-01').keys[0].value)]"
                        },
                        {
                            "name": "APPINSIGHTS_INSTRUMENTATIONKEY",
                            "value": "[reference('Microsoft.Insights/components/mysuperwebapp-ai').InstrumentationKey]"
                        },
                        {
                            "name": "APPINSIGHTS_PROFILERFEATURE_VERSION",
                            "value": "1.0.0"
                        },
                        {
                            "name": "APPINSIGHTS_SNAPSHOTFEATURE_VERSION",
                            "value": "1.0.0"
                        },
                        {
                            "name": "ApplicationInsightsAgent_EXTENSION_VERSION",
                            "value": "~2"
                        },
                        {
                            "name": "DiagnosticServices_EXTENSION_VERSION",
                            "value": "~3"
                        },
                        {
                            "name": "InstrumentationEngine_EXTENSION_VERSION",
                            "value": "~1"
                        },
                        {
                            "name": "SnapshotDebugger_EXTENSION_VERSION",
                            "value": "~1"
                        },
                        {
                            "name": "XDT_MicrosoftApplicationInsights_BaseExtensions",
                            "value": "~1"
                        },
                        {
                            "name": "XDT_MicrosoftApplicationInsights_Mode",
                            "value": "recommended"
                        }
                    ]
                }
            },
            "resources": [
                {
                    "apiVersion": "2016-08-01",
                    "dependsOn": [
                        "mysuperwebapp"
                    ],
                    "name": "Microsoft.ApplicationInsights.AzureWebSites",
                    "properties": {},
                    "type": "siteextensions"
                }
            ],
            "type": "Microsoft.Web/sites"
        },
        {
            "apiVersion": "2014-04-01",
            "kind": "web",
            "location": "northeurope",
            "name": "mysuperwebapp-ai",
            "properties": {
                "ApplicationId": "mysuperwebapp",
                "Application_Type": "web",
                "name": "mysuperwebapp-ai"
            },
            "tags": {
                "[concat('hidden-link:', resourceGroup().id, '/providers/Microsoft.Web/sites/', 'mysuperwebapp')]": "Resource",
                "displayName": "AppInsightsComponent"
            },
            "type": "Microsoft.Insights/components"
        },
        {
            "apiVersion": "2018-07-01",
            "kind": "StorageV2",
            "location": "northeurope",
            "name": "mystorage",
            "sku": {
                "name": "Premium_LRS"
            },
            "type": "Microsoft.Storage/storageAccounts"
        }
    ]
}```

## How can I help?
Try out the DSL and see what you think.

* Create as many issues as you can for both bugs, discussions and features
* Create suggestions for features and the most important elements you would like to see added

The is prototype code. There **will** be massive breaking changes on a regular basis.

## Getting started
1. Clone this repo
2. Build the Farmer project.
3. Try one of the sample scripts in the Samples folder.
4. Alternatively, use the SampleApp to generate your ARM templates from a console app.