#r @"./libs/Newtonsoft.Json.dll"
#r @"../../src/Farmer/bin/Debug/netstandard2.0/Farmer.dll"

open Farmer
open Farmer.Builders

let makeSafeApp (environment:string) theLocation storageSku webAppSku =
    let environment = environment.ToLower()
    let generateResourceName = sprintf "safe-%s-%s" environment

    let myStorageAccount = storageAccount {
        name (sprintf "safe%sstorage" environment)
        sku storageSku
    }

    let myWebApp = webApp {
        name (generateResourceName "web")
        sku webAppSku

        website_node_default_version "8.1.4"
        setting "public_path" "./public"
        setting "STORAGE_CONNECTIONSTRING" myStorageAccount.Key

        depends_on [ myStorageAccount.Name ]
    }

    arm {
        location theLocation

        add_resource myStorageAccount
        add_resource myWebApp

        output "webAppName" myWebApp.Name
        output "webAppPassword" myWebApp.PublishingPassword
    }

makeSafeApp "dev" Location.NorthEurope Storage.Standard_LRS WebApp.Sku.F1
|> Deploy.execute "my-resource-group-name" Deploy.NoParameters
