#r @"..\Farmer\bin\Debug\netstandard2.0\Farmer.dll"

open Farmer

let myCosmosDb = cosmosDb {    
    name "isaacsappdb"
    server_name "isaacscosmosdb"
    throughput 400
    failover_policy NoFailover
    consistency_policy (BoundedStaleness(500, 1000))
    add_containers [
        container {
            name "myContainer"
            partition_key [ "/id" ] Hash
            include_index "/path" [ Number, Hash ]
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
|> Writer.quickDeploy "my-resource-group-name"
