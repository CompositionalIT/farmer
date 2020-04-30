---
title: "Cosmos DB"
date: 2020-02-05T08:53:46+01:00
weight: 4
chapter: false
---

#### Overview
The CosmosDb package containers two builders, used to create *databases* and *containers*.

* CosmosDB Account (`Microsoft.DocumentDb/databaseAccounts`)
* CosmosDB SQL (`"Microsoft.DocumentDB/databaseAccounts/sqlDatabases`)
* CosmosDB SQL Container (`Microsoft.DocumentDb/databaseAccounts/sqlDatabases/containers`)

> There is currently only support for document databases (the so-called "SQL API"), with support for Gremlin, Table and Cassandra data models planned.

#### Cosmos DB Builder
The CosmosDB builder abstracts the idea of server and database into one. If you wish to "re-use" an already-created Cosmos DB server, use `link_to_server` keyword - no server will be created and the database will be attached to the existing one.

| Applies To | Keyword | Purpose |
|-|-|-|
| Database | name | Sets the name of the database. |
| Database | link_to_server | Instructs Farmer to link this database to an existing Cosmos DB server rather than creating a new one. |
| Database | throughput | Sets the throughput of the server. |
| Database | add_containers | Adds a list of containers to the database. |
| Server | server_name | Sets the name of the CosmosDB server. |
| Server | enable_public_network_access | Enables public network access for the server. |
| Server | disable_public_network_access | Disables public network access for the server. |
| Server | consistency_policy | Sets the consistency policy of the database. |
| Server | failover_policy | Sets the failover policy of the database. |
| Server | free_tier | Registers this server with the free pricing tier, if supported and allowed by Azure. |

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