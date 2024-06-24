#r "nuget:Farmer"

open Farmer
open Farmer.Builders

let template =
    let myWebApp = webApp {
        name "mysuperwebapp"
        sku WebApp.Sku.F1
    }

    arm {
        location Location.NorthEurope
        add_resource myWebApp
    }

template |> Deploy.execute "my-resource-group-name" Deploy.NoParameters