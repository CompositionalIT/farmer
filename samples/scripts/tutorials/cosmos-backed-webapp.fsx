#r @"../libs/Newtonsoft.Json.dll"
#r @"../../../src/Farmer/bin/Debug/netstandard2.0/Farmer.dll"

open Farmer
open Farmer.Builders
open Farmer.CosmosDb

let theDatabase = cosmosDb {
    name "Tasks"
    account_name "isaac-to-do-app-cosmos"
    consistency_policy Session
}

let theWebApp = webApp {
    name "isaac-to-do-app"
    sku WebApp.Sku.B1
    setting "CosmosDb:Account" theDatabase.Endpoint
    setting "CosmosDb:Key" theDatabase.PrimaryKey
    setting "CosmosDb:DatabaseName" theDatabase.DbName
    setting "CosmosDb:ContainerName" "Items"
}

let template = arm {
    location Location.WestEurope
    add_resources [
        theDatabase
        theWebApp
    ]
}

template.ToFile @"generated-template"