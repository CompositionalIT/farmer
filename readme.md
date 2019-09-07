# Farmer

An F# DSL for rapidly generating non-complex ARM templates. This isn't a replacement for ARM templates,
nor some sort of compete offering with Pulumi or similar. It's designed mostly as an experiment at this stage -
things will change rapidly and there will be lots of breaking changes both in terms of namespace and API design.

**THIS IS PROTOTYPE CODE. USE AT YOUR OWN RISK.**

## Main features

* Create simple web apps, app insight and storage accounts in a simple DSL.
* Create strongly-typed dependencies to resources.
* Just F# - use standard F# code to dynamically create ARM templates quickly and easily.

## Plans
* Support for more common ARM resource types
* Clean up lots of code
* Make DSL nicer
* Support more use-cases
* Look to simplify the DSL even more e.g. auto-detect parameters and dependencies.

## What does it look like?
This is an example Farmer value:

```fsharp
/// Create a web application resource
let myWebApp = webApp {
    name "mysuperwebapp"
    service_plan_name "myserverfarm"
    sku WebApp.Sku.F1
    use_app_insights "myappinsights"
}

/// The overall ARM template which has the webapp as a resource.
let template = arm {
    location Locations.``North Europe``
    resource myWebApp
}

/// Export the template to a file.
template
|> Writer.toJson
|> Writer.toFile "webapp-appinsights.json"
```

It does the following:

1. Creates a Web Application called `mysuperwebapp`.
2. Creates and links a service plan called `myserverfarm` with the F1 service tier.
4. Creates and links a fully configured Application Insights resource called `myappinsights`, and adds an app setting with the instrumentation key.
5. Embeds the web app into an ARM Template with location set to North Europe.
6. Converts the template into JSON and then writes it to disk.

This ends up looking like this, expanding from around 15 lines of more-or-less strongly type code to around 100 lines of more-or-less weakly typed JSON:

```json
{
    "$schema": "https://schema.management.azure.com/schemas/2015-01-01/deploymentTemplate.json#",
    "contentVersion": "1.0.0.0",
    "outputs": {},
    "parameters": {},
    "resources": [
        {
            "apiVersion": "2016-09-01",
            "location": "northeurope",
            "name": "myserverfarm",
            "properties": {
                "name": "myserverfarm",
                "perSiteScaling": false,
                "reserved": false
            },
            "sku": {
                "name": "F1"
            },
            "type": "Microsoft.Web/serverfarms"
        },
        {
            "apiVersion": "2016-08-01",
            "dependsOn": [
                "[resourceId('Microsoft.Web/serverfarms', 'myserverfarm')]",
                "[resourceId('Microsoft.Insights/components/', 'myappinsights')]"
            ],
            "location": "northeurope",
            "name": "mysuperwebapp",
            "properties": {
                "serverFarmId": "[resourceId('Microsoft.Web/serverfarms', 'myserverfarm')]",
                "siteConfig": {
                    "appSettings": [
                        {
                            "name": "APPINSIGHTS_INSTRUMENTATIONKEY",
                            "value": "[reference(concat('Microsoft.Insights/components/', 'myappinsights')).InstrumentationKey]"
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
                        "[resourceId('Microsoft.Web/sites/', 'mysuperwebapp')]"
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
            "name": "myappinsights",
            "properties": {
                "name": "myappinsights"
            },
            "tags": {
                "[concat('hidden-link:', resourceGroup().id, '/providers/Microsoft.Web/sites/', 'mysuperwebapp')]": "Resource",
                "displayName": "AppInsightsComponent"
            },
            "type": "Microsoft.Insights/components"
        }
    ],
    "variables": {}
}
```

## How can I help?
Try out the DSL and see what you think.

* Create as many issues as you can for both bugs
* Create suggestions for features and the most important elements you would like to see added

The is prototype code. There **will** be massive breaking changes on a regular basis.

## Getting started
1. Clone this repo
2. Build the Farmer project.
3. Try one of the sample scripts in the Samples folder.
4. Alternatively, use the SampleApp to generate your ARM templates from a console app.