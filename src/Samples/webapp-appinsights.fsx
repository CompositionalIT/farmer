#r @"..\Farmer\bin\Debug\netstandard2.0\Farmer.dll"

open Farmer
open Farmer.Resources.WebApp

let template =
    let myWebApp = webApp {
        name "mysuperwebapp"
        sku Sku.F1
    }

    arm {
        location NorthEurope
        add_resource myWebApp
    }

template
|> Deploy.quick "my-resource-group-name"