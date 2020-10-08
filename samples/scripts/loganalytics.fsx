#r @"./libs/Newtonsoft.Json.dll"
#r @"../../src/Farmer/bin/Debug/netstandard2.0/Farmer.dll"

open Farmer
open Farmer.Builders
open Farmer.LogAnalytics

let myRegistry = logAnalytics {
    name "testmyFarmer2"
    sku PerGB2018
    retentionInDays 30
    publicNetworkAccessForIngestion
    publicNetworkAccessForQuery
      }

let deployment = arm {
    location Location.CentralUS
    add_resource myRegistry
         }

deployment
|> Deploy.execute "SPS_Integration_POC" Deploy.NoParameters
|> printfn "%A"