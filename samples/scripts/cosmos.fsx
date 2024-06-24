#r "nuget:Farmer"

open Farmer
open Farmer.Builders
open Farmer.CosmosDb

let myCosmosDb = cosmosDb {
    name "isaacsappdb"
    account_name "isaacscosmosdb"
    throughput 400<CosmosDb.RU>
    failover_policy NoFailover
    consistency_policy (BoundedStaleness(500, 1000))

    add_containers [
        cosmosContainer {
            name "myContainer"
            partition_key [ "/id" ] Hash
            add_index "/path" [ Number, Hash ]
            exclude_path "/excluded/*"
        }
    ]
}

let deployment = arm {
    location Location.NorthEurope
    add_resource myCosmosDb
}

deployment |> Deploy.execute "my-resource-group-name" Deploy.NoParameters