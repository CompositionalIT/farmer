#r "./libs/Newtonsoft.Json.dll"
#r "../../src/Farmer/bin/Debug/netstandard2.0/Farmer.dll"

open Farmer
open Farmer.Builders

let myAnalytics = logAnalytics {
    name "isaacla"
    retention_period 50<Days>
    enable_ingestion
    enable_query
    daily_cap 5<Gb>
}

let deployment = arm {
    location Location.WestEurope
    add_resource myAnalytics
}

deployment
|> Deploy.execute "test-resource-group" Deploy.NoParameters
|> printfn "%A"