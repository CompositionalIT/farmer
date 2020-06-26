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

> Azure Storage Account names must be globally unique and between 3-24 alphanumeric lower-case characters:

#### Referencing the storage account in the web app
In this section, we will add an app setting to the web app and set the value to the storage account's connection string.

> In F#, you need to define a value *before* you reference it, so make sure that you define the storage account *above* the web app.

Add the storage account's connection key to the webapp as an app setting.

```fsharp
let myWebApp = webApp {
    ...
    setting "STORAGE_CONNECTION" myStorage.Key
}
```

> Settings can be strings or (as in this case) an ARM expression, which is evaluated at deployment time.

#### Setting a dependency on the storage account
In ARM templates, you need to explicitly set up **dependencies** between resources that refer to one another; this is still required in Farmer. This tells Azure to create the storage account *before* it creates the web app.

```fsharp
let myWebApp = webApp {
    ...
    depends_on myStorage
}
```

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
      "apiVersion": "2016-08-01",
      "dependsOn": [
        "yourfirststorage"
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
|> Writer.quickWrite  "myFirstTemplate"
```
