---
title: "PostgreSQL"
date: 2020-05-22T07:14:00+02:00
chapter: false
weight: 16
---

#### Overview
The PostgreSQL module contains two builders - `postgreSQL`, used to create
PostgreSQL Azure servers, and `postgreSQLDb`, used to create individual databases.
It supports features such as firewall, autogrow and version selection.
Every PostgreSQL Azure server you create will automatically create a SecureString
parameter for the admin account password.

> There is support for newer "Flexible" as well as the original "Single Server" server type.
> Several keywords are overloaded to cater for both server types.
> By default, the builder uses the Flexible Server model.

* PostgreSQL server (`Microsoft.DBforPostgreSQL/servers`)
* PostgreSQL server (`Microsoft.DBforPostgreSQL/flexibleServers`)

#### PostgreSQL Builder keywords
| Applies To | Keyword | Purpose |
|-|-|-|
| Server | name (string) | Sets the name of the PostgreSQL server. |
| Server | admin_username (string) | Sets the admin username of the server. |
| Server | geo_redundant_backup (bool) | Enables/disables geo-redundant backup |
| Server | enable_geo_redundant_backup | Enables geo-redundant backup |
| Server | disable_geo_redundant_backup | Disables geo-redundant backup |
| Server | storage_autogrow (bool) | Enables/disables auto-grow storage |
| Server | enable_storage_autogrow | Enables auto-grow storage |
| Server | disable_storage_autogrow | Disables auto-grow storage |
| Server | storage_size (int&lt;Gb>) | Sets the initial size of the storage available |
| Server | storage_performance_tier (Vm.DiskPerformanceTier) | Sets the storage performance tier of the server. |
| Server | backup_retention (int&lt;Days>) | Sets the number of days to keep backups |
| Server | server_version (Version) | Selects the PostgreSQL version of the server  |
| Server | capacity (int&lt;VCores>) | Sets the number of cores for the server |
| Server | tier (Sku) | Sets the service tier of the server |
| Server | add_database (database:Database) | Adds a database from the result of a postgreSQLDb builder expression |
| Server | add_database (name:string) | Adds a database with name of `name` |
| Server | enable_azure_firewall | Enables firewall access to all Azure services |
| Server | add_firewall_rule (name:string, start ip:string, end ip:string) | Adds a firewall rule to the server |
| Server | add_firewall_rules (rules:(string*string*sting)list) | As add_firewall_rule but a list of rules |
| Server | add_vnet_rule (name:string, virtualNetworkSubnetId:ResourceId) | Adds a vnet rule to the server |
| Server | add_vnet_rules (rules:(string*ResourceId)list) | As add_vnet_rule but a list of rules |

##### Configuration Members

| Member | Purpose |
|-|-|
| FullyQualifiedDomainName | The fully qualified domain name for the server endpoint. |

#### PostgreSQLDb Builder keywords
| Applies To | Keyword | Purpose |
|-|-|-|
| Database | name (string) | Sets the name of the PostgreSQL database |
| Database | collation (string) | Sets the collation of the postgreSQL database |
| Database | charset (string) | Sets the charset of the postgreSQL database |

#### Example

* Original "Single Server" model

```fsharp
open Farmer
open Farmer.Builders
open Farmer.PostgreSQL

let myPostgres = postgreSQL {
    admin_username "adminallthethings"
    name "aserverformultitudes42"
    capacity 4<VCores>
    storage_size 50<Gb>
    add_database "my_db"
    enable_azure_firewall

    // overloaded or single-instance-specific keywords
    tier GeneralPurpose
    server_version Version.VS_11
    capacity 1<VCores>
}

let template = arm {
    location Location.NorthEurope
    add_resource myPostgres
    output "fqdn" myPostgres.FullyQualifiedDomainName
}
```

* "Flexible Server" model

```fsharp
open Farmer
open Farmer.Builders
open Farmer.PostgreSQL

let myPostgres = postgreSQL {
    name "aserverformultitudes42"
    admin_username "adminallthethings"
    storage_size 64<Gb>
    add_database "my_db"
    enable_azure_firewall
    storage_autogrow true

    // overloaded or model-specific keywords
    tier FlexibleTier.Burstable_B1ms
    server_version FlexibleVersion.V_16
    storage_performance_tier Vm.DiskPerformanceTier.P10
}
```
