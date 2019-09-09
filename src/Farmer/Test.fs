module Test

open Farmer

let template =
    let myStorageAccount = storageAccount {
        name (Variable "storage")
        sku Storage.Sku.StandardLRS
    }

    let myCosmosDb = cosmosDb {
        name "isaacsappdb"
        server_name "isaacscosmosdb"
        throughput 400
        write_model (AutoFailover Helpers.Locations.``North Europe``.Command)
        consistency_policy (BoundedStaleness(500, 1000))
    }

    let myWebApp = webApp {
        name (Variable "web")
        service_plan_name (Variable "appServicePlan")
        sku (Parameter "pricingTier")

        use_app_insights (Variable "insights")

        website_node_default_version "8.1.4"
        setting "public_path" "./public"
        setting "STORAGE_CONNECTIONSTRING" myStorageAccount.Key

        depends_on myStorageAccount
        depends_on myCosmosDb
    }

    let withPostfix element = concat [ Variable "prefix"; Literal element ]
    arm {
        parameters [ "environment"; "location"; "pricingTier" ]

        variable "environment" (toLower (Parameter "environment"))
        variable "prefix" (concat [ Literal "safe-"; Variable "environment" ])
        variable "appServicePlan" (withPostfix "-web-host")
        variable "web" (withPostfix "-web")
        variable "storage" (concat [ Literal "safe"; Variable "environment"; Literal "storage" ])
        variable "insights" (withPostfix "-insights")

        location (Parameter "location")

        resource myStorageAccount
        resource cosmosDb
        resource myWebApp

        output "webAppName" myWebApp.Name
        output "webAppPassword" myWebApp.PublishingPassword        
    }

template
|> Writer.toJson
|> Writer.toFile @"safe-template.json"