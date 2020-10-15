#r "./libs/Newtonsoft.Json.dll"
#r "../../src/Farmer/bin/Debug/netstandard2.0/Farmer.dll"

open Farmer
open Farmer.Builders

let myRegistry = logAnalytics {
    name "isaacla"
    retention_period 30<Days>
    enable_ingestion
    enable_query
}

let deployment = arm {
    location Location.WestEurope
    add_resource myRegistry
}

deployment
|> Deploy.execute "test-resource-group" Deploy.NoParameters
|> printfn "%A"