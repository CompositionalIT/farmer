---
title: "Cosmos DB"
date: 2020-02-05T08:53:46+01:00
weight: 4
chapter: false
---

#### Overview
The CosmosDb package containers two builders, used to create CosmosDB databases and containers. There is only support document databases (that support so-called "SQL" queries), with support for Graph, Table and Cassandra data models planned.

#### Cosmos DB Builder
The CosmosDB builder abstracts the idea of server and database into one. This simplfication means that at present there is only support for a single database for each Cosmos server that you create.

| Keyword | Purpose |
|-|-|
| name | Sets the name of the database. |
| server_name | Sets the name of the CosmosDB server. |
| consistency_policy | Sets the consistency policy of the database. |
| failover_policy | Sets the failover policy of the database. |
| throughput | Sets the throughput of the server. |
| add_containers | Adds a list of containers to the database. |

#### Cosmos Container Builder
The container builder allows you to create and configure a specific container that is attached to a cosmos database.

| Keyword | Purpose |
|-|-|
| name | Sets the name of the container. |
| partition_key | Sets the partition key of the container. |
| add_index | Adds an index to the container. |
| exclude_path | Excludes a path from the container index. |

#### Example
```fsharp
open Farmer
open Farmer.Resources

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
            add_index "/path" [ Number, Hash ]
            exclude_path "/excluded/*"
        }
    ]
}
```