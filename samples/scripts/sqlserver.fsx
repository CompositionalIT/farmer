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
        sqlDb { name "poolDb1" }
        sqlDb { name "poolDb2" }
        sqlDb { name "dtuDb"; sku SqlDtu.Basic }
        sqlDb { name "memoryDb"; sku MSeries.M_8 }
        sqlDb { name "cpuDb"; sku FSeries.Fsv2_8 }
        sqlDb { name "businessCriticalDb"; sku (SqlVCore.BusinessCritical Gen5Series.Gen5_2) }
        sqlDb { name "hyperscaleDb"; sku (SqlVCore.Hyperscale Gen5Series.Gen5_2) }
        sqlDb {
            name "generalPurposeDb"
            sku (SqlVCore.GeneralPurpose Gen5Series.Gen5_8)
            db_size (1024<Mb> * 128)
            hybrid_benefit
        }
    ]
}

let template = arm {
    location Location.NorthEurope
    add_resource myDatabases
}

template |> Writer.quickWrite "sql-example"