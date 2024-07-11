#r "nuget: Farmer"

open Farmer
open Farmer.Builders
open Farmer.PostgreSQL

let myPostgres = postgreSQL {
    name "flexibleserver"
    admin_username "adminallthethings"
    storage_size 64<Gb>
    storage_performance_tier Vm.DiskPerformanceTier.P10
    tier FlexibleTier.Burstable_B1ms
    enable_azure_firewall
    storage_autogrow true

    add_database (
        postgreSQLDb {
            name "thedatabase"
            collation "en_US.utf8"
        }
    )
}

let template = arm {
    location Location.NorthEurope
    add_resource myPostgres
}

// WARNING:
// since there is currently no free tier for PostgreSQL, actually deploying this
// *will* incur spending on your subscription.
template |> Writer.quickWrite "postgres-example"