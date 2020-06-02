#r @"./libs/Newtonsoft.Json.dll"
#r @"../../src/Farmer/bin/Debug/netstandard2.0/Farmer.dll"

open Farmer
open Farmer.Builders

let sqlDb = sql {
    name "my_db"
    server_name "my_server"
    admin_username "admin_username"
    sku Sql.Free
    enable_azure_firewall
}

let template = arm {
    location Location.NorthEurope
    add_resource sqlDb
}

template
|> Writer.quickWrite "sql-example"