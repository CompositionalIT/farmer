#r @"..\Farmer\bin\Debug\netstandard2.0\Farmer.dll"

open Farmer

let myAppInsights = appInsights {    
    name "isaacsAi"
}

let myFunctions = functions {
    name "mysuperwebapp"
    service_plan_name "myserverfarm"
    app_insights_linked myAppInsights.Name
}

let template =
    arm {
        location NorthEurope
        add_resource myAppInsights
        add_resource myFunctions
    }

template
|> Writer.quickDeploy "deleteme"
