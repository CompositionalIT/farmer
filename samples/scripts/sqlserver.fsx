#r @"./libs/Newtonsoft.Json.dll"
#r @"../../src/Farmer/bin/Debug/netstandard2.0/Farmer.dll"

open Farmer
open Farmer.Builders

let myDatabases = sqlServer {
    name "isaac_super_server"
    admin_username "admin_username"
    enable_azure_firewall
    add_databases [
        sqlDb { name "poolDb1" }
        sqlDb { name "poolDb2" }
        sqlDb { name "standaloneDb1"; sku Sql.DbSku.Basic }
    ]
}

let template = arm {
    location Location.NorthEurope
    add_resource myDatabases
}

template
|> Writer.quickWrite "sql-example"