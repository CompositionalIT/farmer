---
title: "Cosmos-backed Web App"
date: 2020-10-24
draft: false
weight: 5
---

#### Introduction
In this exercise, you'll:

* create a Cosmos DB account with a single database
* create a web application with an automatically configured app insights instance
* configure the web application to have connection settings required to connect to the cosmos DB

{{< figure src="../../images/quickstarts/webapp-cosmos.png" caption="[Full code available here](https://github.com/CompositionalIT/farmer/blob/master/samples/scripts/tutorials/cosmos-backed-webapp.fsx)">}}

#### Create the CosmosDB instance
Create a CosmosDB instance using the `cosmosDb` builder:

```fsharp
open Farmer
open Farmer.Builders
open Farmer.CosmosDb

let theDatabase = cosmosDb {
    name "Tasks"
    account_name "isaac-to-do-app-cosmos"
    consistency_policy Session
}
```

#### Create and link to a web app
Create a web application, and provide settings that are derived from the Cosmos DB instance that you just created.

> The API of the functions builder is virtually identical to that of the Web App builder. You can replace `webApp` with `functions` below, removing the `sku` keyword, and you will get a working Azure Functions instance instead.

```fsharp
let theApp = webApp {
    name "isaac-to-do-app"
    sku WebApp.Sku.B1
    setting "CosmosDb:Account" theDatabase.Endpoint
    setting "CosmosDb:Key" theDatabase.PrimaryKey
    setting "CosmosDb:DatabaseName" theDatabase.DbName
    setting "CosmosDb:ContainerName" "Items"
}
```

> You don't have to explicitly set a dependency between the two. Farmer will "pull out" the Cosmos DB details itself.

You don't have to be concerned about secrets of the CosmosDB instance leaking in your ARM template, because no secrets are supplied. Instead, your template will be populated with ARM *expressions* which will only be evaluated at runtime:

```json
{ "name": "CosmosDb:Account", "value": "[reference(resourceId('Microsoft.DocumentDb/databaseAccounts', 'isaac-to-do-app-cosmos'), '2020-03-01').documentEndpoint]" }
{ "name": "CosmosDb:ContainerName", "value": "Items" }
{ "name": "CosmosDb:DatabaseName", "value": "Tasks" }
{ "name": "CosmosDb:Key", "value": "[listKeys(resourceId('Microsoft.DocumentDb/databaseAccounts', 'isaac-to-do-app-cosmos'), providers('Microsoft.DocumentDb','databaseAccounts').apiVersions[0]).primaryMasterKey]" }
```

#### Add both resources to your ARM template

```fsharp
let template = arm {
    location Location.WestEurope
    add_resources [
        theDatabase
        theApp
    ]
}
```

You can now deploy the template and you'll have a web application which has all required secrets to communicate with the Cosmos DB instance.