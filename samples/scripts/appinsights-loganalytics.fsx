#r @"nuget:Farmer"

open Farmer
open Farmer.Builders

let workspace = logAnalytics { name "loganalytics-workspace" }

let myAppInsights = appInsights {
    name "appInsights"
    log_analytics_workspace workspace
}

let myFunctions = functions {
    name "functions-app"
    link_to_app_insights myAppInsights.Name
}

let template = arm {
    location Location.NorthEurope
    add_resources [ workspace; myAppInsights; myFunctions ]
}

template |> Deploy.execute "deleteme" Deploy.NoParameters
