#r "nuget:Farmer"

open Farmer
open Farmer.Builders

let myAppInsights = appInsights {
    name "isaacsAi"
}

let myFunctions = functions {
    name "mysuperwebapp"
    link_to_app_insights myAppInsights.Name
}

let template = arm {
    location Location.NorthEurope
    add_resource myAppInsights
    add_resource myFunctions
}

template
|> Deploy.execute "deleteme" Deploy.NoParameters