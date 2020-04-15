#r @"..\Farmer\bin\Debug\netstandard2.0\Farmer.dll"

open Farmer
open Farmer.Resources

let myAppInsights = appInsights {
    name "isaacsAi"
}

let myFunctions = functions {
    name "mysuperwebapp"
    link_to_app_insights myAppInsights.Name
}

let template = arm {
    location NorthEurope
    add_resource myAppInsights
    add_resource myFunctions
}

template
|> Deploy.execute "deleteme"
