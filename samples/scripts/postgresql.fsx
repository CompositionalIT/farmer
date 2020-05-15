#r @"./libs/Newtonsoft.Json.dll"
#r @"../../src/Farmer/bin/Debug/netstandard2.0/Farmer.dll"

open Farmer
open Farmer.Builders
open Farmer.PostgreSQL

let myPostgres = postgreSQL {
    admin_username "adminallthethings"
    server_name "aserverformultitudes42"
    capacity 4<VCores>
    storage_size 50<Gb>
    tier GeneralPurpose
}

let template = arm {
    location Location.NorthEurope
    add_resource myPostgres
}

// WARNING:
// since there is currently no free tier for postgres, actually deploying this
// *will* incur spending on your subscription.
template
|> Writer.quickWrite "postgres-example"