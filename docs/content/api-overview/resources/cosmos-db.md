---
title: "Cosmos DB"
date: 2020-02-05T08:53:46+01:00
chapter: false
weight: 7
---

#### Overview
The CosmosDb package containers two builders, used to create *databases* and *containers*.

* CosmosDB Account (`Microsoft.DocumentDb/databaseAccounts`)
* CosmosDB SQL (`"Microsoft.DocumentDB/databaseAccounts/sqlDatabases`)
* CosmosDB SQL Container (`Microsoft.DocumentDb/databaseAccounts/sqlDatabases/containers`)

> There is currently only support for document databases (the so-called "SQL API"), with support for Gremlin, Table and Cassandra data models planned.

#### Cosmos DB Builder
The CosmosDB builder abstracts the idea of account and database into one. If you wish to "re-use" an already-created Cosmos DB account, use `link_to_account` keyword - no account will be created and the database will be attached to the existing one.

| Applies To | Keyword | Purpose |
|-|-|-|
| Database | name | Sets the name of the database. |
| Database | link_to_account | Instructs Farmer to link this database to an existing Cosmos DB account rather than creating a new one. |
| Database | throughput | Sets the throughput of the account. |
| Database | add_containers | Adds a list of containers to the database. |
| Account | account_name | Sets the name of the CosmosDB account. |
| Account | api (not yet implemented) | Sets the API and data model to use -- currently defaults to "Core (SQL)". |
| Account | enable_public_network_access | Enables public network access for the account. |
| Account | disable_public_network_access | Disables public network access for the account. |
| Account | consistency_policy | Sets the consistency policy of the database. |
| Account | failover_policy | Sets the failover policy of the database. |
| Account | free_tier | Registers this server with the free pricing tier, if supported and allowed by Azure. |

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
open Farmer.Builders

let myCosmosDb = cosmosDb {
    name "isaacsappdb"
    account_name "isaacscosmosdb"
    throughput 400<CosmosDb.RU>
    failover_policy CosmosDb.NoFailover
    consistency_policy (CosmosDb.BoundedStaleness(500, 1000))
    add_containers [
        cosmosContainer {
            name "myContainer"
            partition_key [ "/id" ] CosmosDb.Hash
            add_index "/path" [ CosmosDb.Number, CosmosDb.Hash ]
            exclude_path "/excluded/*"
        }
    ]
}
```
