#r @"..\Farmer\bin\Debug\netstandard2.0\Farmer.dll"

open Farmer
open Helpers

let template =
    let myWebApp = webApp {
        name "mysuperwebapp"
        service_plan_name "myserverfarm"
        sku WebApp.Sku.F1
        app_insights_auto_name "myappinsights"
    }

    arm {
        location NorthEurope
        add_resource myWebApp
    }

template
|> Writer.quickDeploy "my-resource-group-name"