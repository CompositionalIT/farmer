#r "./libs/Newtonsoft.Json.dll"
#r "../../src/Farmer/bin/Debug/netstandard2.0/Farmer.dll"

open Farmer
open Farmer.Builders

let myRegistry = logAnalytics {
    name "testmyFarmer2"
    sku (LogAnalytics.PerGb 30<Days>)
    enable_ingestion
    enable_query
}

let deployment = arm {
    location Location.CentralUS
    add_resource myRegistry
}

deployment
|> Deploy.execute "SPS_Integration_POC" Deploy.NoParameters
|> printfn "%A"