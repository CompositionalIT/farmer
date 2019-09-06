#r @"..\Farmer\bin\Debug\netstandard2.0\Farmer.dll"

open Farmer
open Helpers

let template =
    let myStorage = {
        Name = Literal "mystorage"
        Sku = Storage.StandardLRS
    }

    let myWebApp = webApp {
        name (Literal "mysuperwebapp")
        service_plan_name (Literal "myserverfarm")
        sku WebApp.Skus.F1
        use_app_insights (Literal "myappinsights")
    }

    arm {
        resource myStorage
        resource myWebApp
    }

template
|> Writer.toJson
|> Writer.toFile @"webapp-storage.json"