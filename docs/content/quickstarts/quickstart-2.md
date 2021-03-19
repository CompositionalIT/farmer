---
title: "Working with multiple resources"
date: 2020-02-04T00:41:51+01:00
draft: false
weight: 2
---

#### Introduction
In this quickstart, you'll expand on the deployment authored in the [previous quickstart](../quickstart-1/#the-full-application) as follows:

* add an Azure storage account
* add an application setting to the web app that references the storage account's key
* set a dependency between the two resources

#### Creating a storage account
Create a storage account by using the `storageAccount` builder.

```fsharp
let myStorage = storageAccount {
    name "yourfirststorage"
}
```

> Azure Storage Account names must be globally unique and between 3-24 alphanumeric lower-case characters!

#### Referencing the storage account in the web app
In this section, we will add an app setting to the web app and set the value to the storage account's connection string.

> In F#, you need to define a value *before* you reference it, so make sure that you define the storage account *above* the web app.

Add the storage account's connection key to the webapp as an app setting.

```fsharp
let myWebApp = webApp {
    ...
    setting "storageKey" myStorage.Key
}
```

If you're coming from a raw ARM template background, don't worry about the need to set dependencies between the Storage Account and Web App - Farmer will automatically do this for you!

> Settings can be strings or (as in this case) an ARM expression, which is evaluated at deployment time.

#### Adding the storage account to the deployment
Add the storage account to the deployment using the same `add_resource` keyword as you did with `myWebApp`.

#### Analysing the ARM template

Run the application:

```cmd
dotnet run
```

You should notice that the template now contains a storage account. Also observe the dependency that has been created:

```json
{
  "resources": [
    {
      "apiVersion": "2020-06-01",
      "dependsOn": [
        "[resourceId('Microsoft.Storage/storageAccounts', 'yourfirststorage')]"
      ],
      "type": "Microsoft.Web/sites"
    }
  ]
}
```

Also observe the application setting that has been created:

```json
{
    "appSettings": [
        {
            "name": "storageKey",
            "value": "[concat('DefaultEndpointsProtocol=https;AccountName=yourfirststorage;AccountKey=', listKeys('yourfirststorage', '2017-10-01').keys[0].value)]"
        }
    ]
}
```

#### The full application

```fsharp
open Farmer
open Farmer.Builders

let myStorageAccount = storageAccount {
    name "yourfirststorage"
}

let myWebApp = webApp {
    name "yourFirstFarmerApp"
    setting "storageKey" myStorageAccount.Key
}

let deployment = arm {
    location Location.NorthEurope
    add_resource myStorageAccount
    add_resource myWebApp
}

deployment
|> Writer.quickWrite  "myFirstTemplate"
```
