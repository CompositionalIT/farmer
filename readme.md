# Farmer

An F# DSL for rapidly generating non-complex ARM templates.

[![Build Status](https://compositional-it.visualstudio.com/Farmer/_apis/build/status/CompositionalIT.farmer?branchName=master)](https://compositional-it.visualstudio.com/Farmer/_build/latest?definitionId=14&branchName=master)
[![Farmer on Nuget](https://buildstats.info/nuget/farmer)](https://www.nuget.org/packages/farmer/)

## Main features
* Create non-complex ARM templates through a simple, strongly-typed and pragmatic DSL.
* Create strongly-typed dependencies to resources.
* Just F# - use standard F# code to dynamically create ARM templates quickly and easily.

### Currently Supported Resources
* Storage
* App Service
* Application Insights
* Cosmos DB
* Azure SQL
* Functions
* Virtual Machines
* Azure Search

Jump to the [quickstart](#creating-your-first-template-using-farmer) or view the [API reference](#api-reference).

## FAQ
### Show me the code!
This is an example bit of Farmer F#:

```fsharp
open Farmer
open Farmer.Resources

// Create a storage resource with Premium LRS
let myStorage = storageAccount {
    name "mystorage"           // set account name
    sku Storage.Sku.PremiumLRS // use Premium LRS
}

// Create a web application resource
let myWebApp = webApp {
    name "mysuperwebapp"                // set web app name
    sku WebApp.Sku.S1                   // use S1 size
    setting "storage_key" myStorage.Key // set an app setting to the storage account key
    depends_on myStorage                // webapp is dependent on storage 
}

// Create the ARM template using those two resources
let deployment = arm {
    location Locations.NorthEurope // set location for all resources
    add_resource myStorage             // include storage into template
    add_resource myWebApp              // include web app into template

    // also output a couple of values generated at deployment-time
    output "storage_key" myStorage.Key
    output "web_password" myWebApp.PublishingPassword
}

/// Export the template to a file.
deployment.Template
|> Writer.toJson
|> Writer.toFile "webapp-deployment.json"
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
    "parameters": {
    },
    "resources": [
        {
            "apiVersion": "2016-08-01",
            "dependsOn": [
                "mysuperwebapp-plan",
                "mystorage",
                "mysuperwebapp-ai"
            ],
            "kind": "app",
            "location": "northeurope",
            "name": "mysuperwebapp",
            "properties": {
                "serverFarmId": "mysuperwebapp-plan",
                "siteConfig": {
                    "alwaysOn": false,
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
                    ],
                    "metadata": [
                        {
                            "name": "CURRENT_STACK",
                            "value": "dotnetcore"
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
                    "properties": {
                    },
                    "type": "siteextensions"
                }
            ],
            "type": "Microsoft.Web/sites"
        },
        {
            "apiVersion": "2018-02-01",
            "kind": "app",
            "location": "northeurope",
            "name": "mysuperwebapp-plan",
            "properties": {
                "name": "mysuperwebapp-plan",
                "perSiteScaling": false,
                "reserved": false
            },
            "sku": {
                "capacity": 1,
                "name": "S1",
                "size": "0",
                "tier": "Standard"
            },
            "type": "Microsoft.Web/serverfarms"
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
            "resources": [
            ],
            "sku": {
                "name": "Premium_LRS"
            },
            "type": "Microsoft.Storage/storageAccounts"
        }
    ]
}
```

### Getting started
1. Clone this repo
2. Build the Farmer project.
3. Try one of the sample scripts in the Samples folder.
4. Alternatively, use the SampleApp to generate your ARM templates from a console app.

### How can I help?
Try out the DSL and see what you think.

* Create as many issues as you can for both bugs, discussions and features
* Create suggestions for features and the most important elements you would like to see added

### I have an Azure subscription, but I'm not an expert. I like the look of this - how do I "use" it?
1. Create an [ARM template](https://docs.microsoft.com/en-us/azure/azure-resource-manager/template-deployment-overview) using the Farmer sample app.
1. Follow the steps [here](#deploying-arm-templates) to deploy the generated template into Azure.
1. Log any issues or ideas that you find [here](https://github.com/CompositionalIT/farmer/issues/new).

### I don't know F#. Would you consider writing a C# version of this?
I'm afraid not. F# isn't hard to learn (especially for simple DSLs such as this), and you can easily integrate F# applications as part of a dotnet solution, since F# is a first-class citizen of the dotnet core ecosystem.

### Are you trying to replace ARM templates?
No, we're not. Farmer *generates* ARM templates that can be used just as normal; Farmer can be used simply to make the process of getting started much simpler, or incorporated into your build pipeline as a way to avoid managing difficult-to-manage ARM templates and instead use them as the final part of your build / release pipeline.

### Are you trying to compete with Pulumi?
No, we're not. Farmer has (at least currently) a specific goal in mind, which is to lower the barrier to entry for creating and working with ARM templates that are non-complex. We're not looking to create a cross-platform DSL to also support things like Terraform etc. or support deployment of code along with infrastructure (or, at least, only to the extent that ARM templates do).

### There's no support for variables or parameters!
Farmer **does** support `securestring` parameters for e.g. SQL and Virtual Machine passwords - these are automatically generated based on the contents of the template rather than explicitly by yourself. However, we don't currently plan on providing *rich* support for either parameters or variables for several reasons:

* We want to keep the Farmer codebase simple for maintainers
* We want to keep the Farmer API simple for users
* We want to keep the generated ARM templates as readable as possible
* We feel that instead of trying to embed conditional logic and program flow directly inside ARM templates in JSON, if you wish to parameterise your template that you should use a real programming language to do that: in this case, F#.

You can read more on this issue [here](https://github.com/CompositionalIT/farmer/issues/8)

## Quickstarts

### Creating your first template using Farmer

#### 1. Creating a fully-configured web app.
1. Open `Program.fs` in the `SampleApp` folder.
1. Create a web application and give it a name. Pick something unique - the name of this web app
must be **unique across Azure** i.e. someone else can't have another web app with the same name!
```fsharp
let myWebApp = webApp {
    name "isaacssuperwebapp"
}
```
3. Assign the web app into the existing (empty) arm template definition:
```fsharp
let location, template = arm {
    ...
    add_resource myWebApp // add this line
}
```
4. Run the application.
1. Examine the `generated-template.json` file.
1. Uncomment the last two lines in the application and run it again to deploy the template (see [here](#deploying-to-azure) if you want to learn more about this **and what prerequisites are required**).
1. Once it has deployed, find it in the Azure portal. You will see that *three* resources were created: the **app service**, the **app service plan** that the app service resides in and a linked **application insights** instance.

#### 2. Creating and linking secondary resources.
8. *Above* the definition of `myWebApp`, create a storage account. The name must be globally unique and between 3-24 alphanumeric lower-case characters:
```fsharp
let myStorage = storageAccount {
    name "isaacsuperstorage"
}
```
9. Now add the storage account's connection key to the webapp as an app setting.
```fsharp
let myWebApp = webApp {
    ...
    setting "STORAGE_CONNECTION" myStorage.Key // add this line
}
```
10. Add another entry into the webapp definition that marks the storage account as a **dependency**. This tells Azure to create the storage account *before* it creates the web app.
```fsharp
let myWebApp = webApp {
    ...
    depends_on myStorage // add this line
}
```
11. Add it to the body of the `template` definition using the same `resource` keyword as you did with `myWebApp`.
1. Now regenerate and redeploy the template (don't worry about overwriting or duplicating the existing resources - Azure will simply create the "new" elements as required).
1. Check in the portal that the storage account has been created.
1. Navigate to the **app service** and then to the **configuration** section.
1. Observe that the setting `storage_connection` has been created and has the connection string of the storage account already in it.

### Deploying to Azure
There are two "recommended" ways of deploying to Azure: either "by hand", in which you use Farmer to generate an ARM template and then deploy that using any number of standard Azure patterns for deploying (one of which is shown [below](#deploying-by-hand)). The alternative is to use Farmer's own simple API for deploying Farmer templates without the hassle of needing to explicitly deal with ARM templates yourself.

#### Deploying through Farmer
Farmer has a simple API for deploying Farmer templates directly to Azure from your development machine, without you having to deal with ARM templates directly; you will need the [Azure CLI](https://docs.microsoft.com/en-gb/cli/azure/?view=azure-cli-latest) installed in order to use this.

1. Create a Farmer template as normal.
2. Pipe the result into the `quickDeploy` function.

```fsharp
let output = arm {
    location NorthEurope

    //TODO: Assign resources here using the add_resource keyword
}

// Deploy the template to my-resource-group in the default Azure subscription.
output |> Writer.quickDeploy "my-resource-group"
```
This will launch a simple batch file (**help needed to make this x-plat!**) which uses the Azure CLI to:

* Sign in to Azure
* Create the resource group
* Deploy the template into that resource group.

#### Deploying by hand
1. Install the [Azure CLI](https://docs.microsoft.com/en-gb/cli/azure/?view=azure-cli-latest).
1. Log in to Azure in the CLI: `az login`.
1. Create a [Resource Group](https://docs.microsoft.com/en-us/azure/azure-resource-manager/resource-group-overview#resource-groups) which will store the created Azure services: `az group create --location westus --name MyResourceGroup`.
1. Deploy the ARM template to the newly-created resource group: `az group deployment create --resource-group MyResourceGroup --template-file generated-arm-template.json`.
1. Log into the [Azure portal](https://portal.azure.com) to see the results.

## API Reference
(TBD).
