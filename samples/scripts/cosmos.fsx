#r @"./libs/Newtonsoft.Json.dll"
#r @"../../src/Farmer/bin/Debug/netstandard2.0/Farmer.dll"

open Farmer
open Farmer.Resources

let myCosmosDb = cosmosDb {
    name "isaacsappdb"
    server_name "isaacscosmosdb"
    throughput 400
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

let deployment =
    arm {
        location NorthEurope
        add_resource myCosmosDb
    }

deployment
|> Deploy.execute "my-resource-group-name" Deploy.NoParameters
