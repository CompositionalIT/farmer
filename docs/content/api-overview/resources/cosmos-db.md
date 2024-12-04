---
title: "Cosmos DB"
date: 2020-02-05T08:53:46+01:00
chapter: false
weight: 3
---

#### Overview
The CosmosDb package containers two builders, used to create *databases* and *containers*.

* CosmosDB Account (`Microsoft.DocumentDb/databaseAccounts`)
* CosmosDB SQL (`Microsoft.DocumentDB/databaseAccounts/sqlDatabases`)
* CosmosDB MongoDB (`Microsoft.DocumentDB/databaseAccounts/mongodbDatabases`)
* CosmosDB SQL Container (`Microsoft.DocumentDb/databaseAccounts/sqlDatabases/containers`)
* CosmosDB Graph databases (`Microsoft.DocumentDb/databaseAccounts/gremlinDatabases`)
* CosmosDB Graph containers (`Microsoft.DocumentDb/databaseAccounts/gremlinDatabases/graphs`)

> There is currently support for document databases (the so-called "SQL API") and Gremlin graphs. Support for Table and Cassandra data models planned.

#### Cosmos DB Builder
The CosmosDB builder abstracts the idea of an account and database into one. If you wish to "re-use" an already-created Cosmos DB account, use the `link_to_account` keyword - no account will be created and the database will be attached to the existing one.

| Applies To | Keyword                       | Purpose |
|-|-------------------------------|-|
| Database | name                          | Sets the name of the database. |
| Database | link_to_account               | Instructs Farmer to link this database to an existing Cosmos DB account rather than creating a new one. |
| Database | throughput                    | Sets the throughput with either "provisioned throughput" or "serverless". |
| Database | add_containers                | Adds a list of containers to the database. |
| Account | account_name                  | Sets the name of the CosmosDB account. |
| Account | kind                          | Sets the API and data model to use -- currently defaults to "Core (SQL)". |
| Account | enable_public_network_access  | Enables public network access for the account. |
| Account | disable_public_network_access | Disables public network access for the account. |
| Account | consistency_policy            | Sets the consistency policy of the database. |
| Account | failover_policy               | Sets the failover policy of the database. |
| Account | free_tier                     | Registers this server with the free pricing tier, if supported and allowed by Azure. |

#### Cosmos Container Builder
The container builder allows you to create and configure a specific container that is attached to a cosmos database.

| Keyword | Purpose                                                                                    |
|-|--------------------------------------------------------------------------------------------|
| name | Sets the name of the container.                                                            |
| partition_key | Sets the partition key of the container.                                                   |
| add_index | Adds an index to the container.                                                            |
| exclude_path | Excludes a path from the container index.                                                  |
| gremlin_graph | Marks the container as graph (must be used with an account of `kind DatabaseKind.Gremlin`) |


#### Example
```fsharp
open Farmer
open Farmer.Builders

let myCosmosDb = cosmosDb {
    name "isaacsappdb"
    account_name "isaacscosmosdb"
    throughput 400<CosmosDb.RU> // or throughput Serverless
    failover_policy CosmosDb.NoFailover
    consistency_policy (CosmosDb.BoundedStaleness(500, 1000))
    //kind DatabaseKind.Gremlin //Create a gremlin enabled account
    add_containers [
        cosmosContainer {
            name "myContainer"
            partition_key [ "/id" ] CosmosDb.Hash
            add_index "/path" [ CosmosDb.Number, CosmosDb.Hash ]
            exclude_path "/excluded/*"
            //gremlin_graph //Mark this container to be a graph
        }
    ]
}
```
