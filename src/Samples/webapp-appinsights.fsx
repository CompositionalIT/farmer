#r @"..\Farmer\bin\Debug\netstandard2.0\Farmer.dll"

open Farmer
open Helpers

let template =
    let myWebApp = webApp {
        name "mysuperwebapp"
        sku WebApp.Sku.F1
    }

    arm {
        location NorthEurope
        add_resource myWebApp
    }

template
|> Writer.quickDeploy "my-resource-group-name"