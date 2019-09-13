#r @"..\Farmer\bin\Debug\netstandard2.0\Farmer.dll"

open Farmer
open Helpers

let myStorage = storageAccount {
    name "mystorage"
    sku Storage.Sku.StandardLRS
}

let myWebApp = webApp {
    name "mysuperwebapp"
    service_plan_name "myserverfarm"
    sku WebApp.Sku.F1
    use_app_insights "myappinsights"
    depends_on myStorage
}

let template =
    arm {
        location Helpers.Locations.``North Europe``
        resource myStorage
        resource myWebApp
    }

template
|> Writer.toJson
|> Writer.toFile @"webapp-storage.json"