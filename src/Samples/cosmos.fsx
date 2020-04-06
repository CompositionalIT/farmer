#r @"..\Farmer\bin\Debug\netstandard2.0\Farmer.dll"

open Farmer
open Farmer.Resources

let myCosmosDb = cosmosDb {    
    db_name "isaacsappdb"
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

let template =
    arm {
        location NorthEurope
        add_resource myCosmosDb
    }

template
|> Deploy.quick "my-resource-group-name"
