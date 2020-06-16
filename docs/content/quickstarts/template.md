---
title: "The Farmer .NET Template"
date: 2020-02-04T00:41:51+01:00
draft: false
weight: 4
---

Farmer comes with a .NET template that makes getting started easy.

### Creating a basic Farmer app
The easiest way to create a Farmer app is to use the Farmer .NET Template.

```cmd
dotnet new -i Farmer.Template
dotnet new Farmer
```

> You only have to install the template once on your machine!

This creates a new dotnet application solution and project that looks by default as follows:

```fsharp
open Farmer
open Farmer.Builders

let deployment = arm {
    location Location.NorthEurope
}

printf "Generating ARM template..."
deployment |> Writer.quickWrite "output"
printfn "all done! Template written to output.json"
```

From here, you can add resources in the normal manner.

### Basic configuration options
You can configure the template using the following optional arguments.

#### ARM Template filename
The name of the ARM template JSON file e.g. `--armTemplate myTemplate`

#### Location
The location to create resources in e.g. `--location WestUS`

### Deploy Configuration
You can also configure the Farmer template to deploy to Azure out of the box using the `--ci` option. This has two modes of operation:

#### Azure DevOps deployment
This comes with a ready-made devops YAML file designed for simple CI/CD, using Farmer to generate ARM templates and Azdo to deploy using its own ARM Template deployment process. You should supply the following arguments:

* **--ci**: Tells the template to create a Farmer app for use with Azure Devops.
* **--azureSubscription**: Set the full name of the Azure Subscription that has been already configured in Azdo that has permission to deploy templates to Azure.
* **--resourceGroup**: Set the name of the resource group that you wish to deploy to.

#### Direct deployment
If you prefer a deployment process that is not coupled to Azure Devops, you can create a [service principle](../../deployment-guidance/#how-do-i-create-a-service-principal) in Azure and use the generated credentials in Farmer. Farmer will use its own wrapper around the Azure REST API to deploy to Azure, reporting progress to the console.