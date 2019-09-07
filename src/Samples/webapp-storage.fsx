#r @"..\Farmer\bin\Debug\netstandard2.0\Farmer.dll"

open Farmer
open Helpers

let template =
    let myStorage = {
        Name = Literal "mystorage"
        Sku = Storage.Sku.StandardLRS
    }

    let myWebApp = webApp {
        name "mysuperwebapp"
        service_plan_name "myserverfarm"
        sku WebApp.Sku.F1
        use_app_insights "myappinsights"
    }

    arm {
        resource myStorage
        resource myWebApp
    }

template
|> Writer.toJson
|> Writer.toFile @"webapp-storage.json"