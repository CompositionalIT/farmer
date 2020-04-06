#r @"..\Farmer\bin\Debug\netstandard2.0\Farmer.dll"

open Farmer
open Farmer.Resources.WebApp

let myAppInsights = appInsights {    
    name "isaacsAi"
}

let myFunctions = functions {
    name "mysuperwebapp"
    app_insights_manual myAppInsights.Name
}

let template =
    arm {
        location NorthEurope
        add_resource myAppInsights
        add_resource myFunctions
    }

template
|> Deploy.quick "deleteme"
