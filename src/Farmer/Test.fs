module Test

open Farmer

let template =
    let withPostfix element = concat [ Variable "prefix"; Literal element ]
    let myStorageAccount = storageAccount {
        name (Variable "storage")
        sku Storage.Sku.StandardLRS
    }

    let web = webApp {
        name (Variable "web")
        service_plan_name (Variable "appServicePlan")
        sku (Parameter "pricingTier")

        use_app_insights (Variable "insights")

        website_node_default_version "8.1.4"
        setting "public_path" "./public"
        setting "STORAGE_CONNECTIONSTRING" myStorageAccount.Key

        depends_on myStorageAccount
    }

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
        resource web

        output "webAppName" web.Name
        output "webAppPassword" web.PublishingPassword        
    }

template
|> Writer.toJson
|> Writer.toFile @"safe-template.json"