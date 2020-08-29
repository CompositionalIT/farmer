#r @"./libs/Newtonsoft.Json.dll"
#r @"../../src/Farmer/bin/Debug/netstandard2.0/Farmer.dll"

open Farmer
open Farmer.Builders
open Farmer.PostgreSQL

let myPostgres = postgreSQL {
    name "aserverformultitudes42"
    admin_username "adminallthethings"
    capacity 4<VCores>
    storage_size 50<Gb>
    tier GeneralPurpose
    add_database "my_db"
    enable_azure_firewall
}

let template = arm {
    location Location.NorthEurope
    add_resource myPostgres
}

// WARNING:
// since there is currently no free tier for PostgreSQL, actually deploying this
// *will* incur spending on your subscription.
template
|> Writer.quickWrite "postgres-example"