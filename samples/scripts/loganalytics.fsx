#r "./libs/Newtonsoft.Json.dll"
#r "../../src/Farmer/bin/Debug/netstandard2.0/Farmer.dll"

open Farmer
open Farmer.Builders

let myRegistry = logAnalytics {
    name "testmyFarmer2"
    sku LogAnalytics.PerGB2018
    enable_ingestion
    enable_query
    retention_period 30<Days>
}

let deployment = arm {
    location Location.CentralUS
    add_resource myRegistry
}

deployment
|> Deploy.execute "SPS_Integration_POC" Deploy.NoParameters
|> printfn "%A"