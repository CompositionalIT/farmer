---
title: "Deploying to Azure"
date: 2020-02-04T00:41:51+01:00
draft: false
weight: 3
---

#### Introduction
In this exercise, you'll update the application to deploy the generated ARM template to Azure directly from Farmer.

> Farmer generates normal ARM templates. You use all of the standard mechanisms for deploying ARM templates such as through the portal, Powershell, .NET or Azure CLI etc. This tutorial shows you a simple way to deploy templates from your development machine directly from within F#.

#### Install the Azure CLI
If you haven't done so already, install the [Azure CLI](https://docs.microsoft.com/en-us/cli/azure/install-azure-cli?view=azure-cli-latest) on your machine.

#### Deploy the template
As Farmer emits standard ARM templates, you can use all the standard ARM template deployment tools at your disposal - whether that's Visual Studio, Powershell, or the Azure CLI. In our case, we can also use Farmer's API which wraps around the Azure CLI.

Modify the application you created at the end of [quickstart #2](quickstart-2/#the-full-application) so that instead of writing the template to a file, we deploy it directly:

```fsharp
deployment
|> Deploy.execute "myResourceGroup" Deploy.NoParameters
```

> Note that the Web Application and Storage Account names should be *globally* unique; they must be **unique across Azure** i.e. someone else can't have another web app or storage account with the same name!

Farmer will now create the ARM template, and also generate a batch / shell script which calls the Azure CLI. You'll be prompted to login to Azure by the CLI, after which point it will create the named resource group in the location specified in the `arm { }` builder.

Wait until the process is completed and log into the [Azure Portal](https://portal.azure.com/). Navigate to the newly-created resource group and inspect the overview record list e.g.

![](../../images/deploy.jpg)

Congratulations - you've now created and deployed an ARM template entirely from F#!

#### The full application

```fsharp
open Farmer
open Farmer.Builders

let myStorageAccount = storageAccount {
    name "yourfirststorageaccount"
}

let myWebApp = webApp {
    name "yourFirstFarmerApp"
    setting "storageKey" myStorageAccount.Key
    depends_on myStorageAccount.Name
}

let deployment = arm {
    location Location.NorthEurope
    add_resource myStorageAccount
    add_resource myWebApp
}

deployment
|> Deploy.execute "myResourceGroup" Deploy.NoParameters
```
