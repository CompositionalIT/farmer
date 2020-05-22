---
title: "PostgreSQL"
date: 2020-05-22T07:14:00+02:00
weight: 19
chapter: false
---

#### Overview
The PostreSQL builder is used to create Azure Database Service for PostreSQL servers
and databases. Every SQL PostgreSQL server you create will automatically create a SecureString parameter for the admin account password.
If you wish to create a PostgreSQL attached to an existing server, use the `link_to_server` keyword and supply the resource name of the existing server.

* PostgreSQL server (`Microsoft.DBforPostgreSQL/servers`)

#### Builder keywords
| Applies To | Keyword | Purpose |
|-|-|-|
| Server | server_name (string) | Sets the name of the SQL server. |
| Server | admin_username (string) | Sets the admin username of the server. |
| Server | geo_redundant_backup (bool) | Enables/disables geo-redundant backup |
| Server | enable_geo_redundant_backup | Enables geo-redundant backup |
| Server | disable_geo_redundant_backup | Disables geo-redundant backup |
| Server | storage_autogrow (bool) | Enables/disables auto-grow storage |
| Server | enable_storage_autogrow | Enables auto-grow storage |
| Server | disable_storage_autogrow | Disables auto-grow storage |
| Server | storage_size (int&lt;Gb>) | Sets the initial size of the storage available |
| Server | backup_retention (int&lt;Days>) | Sets the number of days to keep backups |
| Server | server_version (Version) | Selects the PoistgreSQL version of the server  |
| Server | capacity (int&lt;VCores>) | Sets the number of cores for the server |
| Server | tier (Sku) | Sets the service tier of the server |

#### Example

```fsharp
open Farmer
open Farmer.Builders
open Farmer.PostgreSQL

let myPostgres = postgreSQL {
    admin_username "adminallthethings"
    server_name "aserverformultitudes42"
    capacity 4<VCores>
    storage_size 50<Gb>
    tier GeneralPurpose
}

let template = arm {
    location Location.NorthEurope
    add_resource myPostgres
}

template |> Writer.quickWrite "postgres-example"
```

