#r "nuget:Farmer"

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
        sqlDb {
            name "dtuDb"
            sku Basic
        }
        sqlDb {
            name "memoryDb"
            sku M_8
        }
        sqlDb {
            name "cpuDb"
            sku Fsv2_8
        }
        sqlDb {
            name "businessCriticalDb"
            sku (BusinessCritical Gen5_2)
        }
        sqlDb {
            name "hyperscaleDb"
            sku (Hyperscale Gen5_2)
        }
        sqlDb {
            name "generalPurposeDb"
            sku (GeneralPurpose Gen5_8)
            db_size (1024<Mb> * 128)
            hybrid_benefit
        }
        sqlDb {
            name "serverless4to8cpu"
            sku (GeneralPurpose(S_Gen5(4.0, 8.0)))
        }
        sqlDb {
            name "serverlessHalfCore"
            sku (GeneralPurpose(S_Gen5(0.5, 2.0)))
        }
    ]
}

let template = arm {
    location Location.NorthEurope
    add_resource myDatabases
}

template |> Writer.quickWrite "sql-example"