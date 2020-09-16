#r @"./libs/Newtonsoft.Json.dll"
#r @"../../src/Farmer/bin/Debug/netstandard2.0/Farmer.dll"

open Farmer
open Farmer.Builders
open Sql

let myDatabases = sqlServer {
    name "isaac_super_server"
    admin_username "admin_username"
    enable_azure_firewall

    add_databases [
        sqlDb { name "standaloneDb1"; sku MSeries_12 }
        sqlDb { name "standaloneDb1"; sku Fsv2_12 }
        sqlDb { name "standaloneDb1"; sku (GeneralPurpose.Provisioned (Gen5 Gen5_10)) }
        sqlDb { name "standaloneDb1"; sku (BusinessCritical.Gen5 Gen5_10) }
    ]
}

let template = arm {
    location Location.NorthEurope
    add_resource myDatabases
}

template |> Writer.quickWrite "sql-example"

// template |> Deploy.execute "delete-me-too" [ "password-for-isaac_super_server", "qweasdQWEASD123***" ]

