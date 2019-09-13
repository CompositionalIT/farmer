#r @"..\Farmer\bin\Debug\netstandard2.0\Farmer.dll"

open Farmer

let makeTemplate (environment:string) theLocation storageSku webAppSku =
    let environment = environment.ToLower()
    let generateResourceName = sprintf "safe-%s-%s" environment 
    
    let myStorageAccount = storageAccount {
        name (sprintf "safe%sstorage" environment)
        sku storageSku
    }

    let myWebApp = webApp {
        name (generateResourceName "web")
        service_plan_name (generateResourceName "webhost")
        sku webAppSku

        use_app_insights (generateResourceName "insights")

        website_node_default_version "8.1.4"
        setting "public_path" "./public"
        setting "STORAGE_CONNECTIONSTRING" myStorageAccount.Key

        depends_on myStorageAccount
    }

    arm {
        resource myStorageAccount
        resource myWebApp
        location theLocation

        output "webAppName" myWebApp.Name
        output "webAppPassword" myWebApp.PublishingPassword        
    }

makeTemplate "dev" Helpers.Locations.NorthEurope Storage.Sku.StandardLRS WebApp.Sku.F1
|> Writer.toJson
|> Writer.toFile @"safe-template.json"