---
title: "SQL Azure"
date: 2020-02-05T08:53:46+01:00
chapter: false
weight: 18
---

#### Overview
The SQL Azure module contains two builders - `sqlServer`, used to create SQL Azure servers, and `sqlDb`, used to create individual databases. It supports features such as encryption, firewalls and automatic pool creation. Every SQL Azure server you create will automatically create a SecureString parameter for the admin account password.

* SQL Azure server (`Microsoft.Sql/servers`)

#### SQL Server Builder Keywords
| Keyword | Purpose |
|-|---------------------------------------------------------------------------------------------------------------------------------|
| name | Sets the name of the SQL server. |
| add_firewall_rule | Adds a custom firewall rule given a name, start and end IP address range. |
| add_firewall_rules | As add_firewall_rule but a list of rules |
| enable_azure_firewall | Adds a firewall rule that enables access to other Azure services. |
| admin_username | Sets the admin username of the server. The password is supplied as a secret parameter at runtime. |
| entra_id_admin | Activates Entra ID authentication using the supplied login name, associated objectId and principal type of the administrator account. |
| entra_id_admin_user | Activates Entra ID authentication for the User Principal Type using the supplied user's login name. You can determine the ObjectId using `Farmer.Builders.AccessPolicy.findUsers`. |
| entra_id_admin_group | Activates Entra ID authentication for the Group Principal Type using the supplied group's login name. You can determine the ObjectId using `Farmer.Builders.AccessPolicy.findGroups`. |
| elastic_pool_name | Sets the name of the elastic pool, if required. If not set, Farmer will generate a name for you. |
| elastic_pool_sku | Sets the sku of the elastic pool, if required. If not set, Farmer will default to Basic 50. |
| elastic_pool_database_min_max | Sets the optional minimum and maximum DTUs for the elastic pool for each database. |
| elastic_pool_capacity | Sets the optional disk size in MB for the elastic pool for each database. |
| min_tls_version | Sets the minimum TLS version for the SQL server |
| geo_replicate | Geo-replicate all the databases in this server to another location, having NameSuffix after the original server and database names. |

> You must set at least one of SQL user / pass (using `admin_username`) or Entra ID login (using one of the `entra_id_admin` variants).
> Setting both will leave both activated; setting only Entra ID will automatically explicitly deactivate user / pass authentication.

#### SQL Server Configuration Members
| Member | Purpose |
|-|-|
| ConnectionString | Gets a literal .NET connection string using the administrator username / password, given a database or database name. The password will be evaluated based on the contents of the password parameter supplied to the template at deploy time. |
| PasswordParameter | Gets a string that represents the parameter password required for deployment on the sql instance by Farmer e.g. "password-for-mysqlserver".

#### SQL Database Builder Keywords

| Keyword | Purpose |
|-|-|
| name | Sets the name of the database. |
| sku | Sets the sku of the database. If not set, the database is assumed to be part of an elastic pool, which will be automatically created. |
| hybrid_benefit | If a VCore-style SKU is selected, this allows you to use Azure Hybrid Benefit licensing. |
| db_size | Sets the maximum database size. |
| collation | Sets the collation of the database. |
| use_encryption | Enables transparent data encryption of the database. |

#### Serverless Gen5 SKU

The Serverless Gen5 SKU (`S_Gen5`) supports fractional VCores, allowing you to specify capacity as low as 0.5 or 0.75 VCores for cost-effective serverless databases. You can specify both minimum and maximum capacity:

```fsharp
// Serverless with fractional VCores
sqlDb {
    name "serverlessDb"
    sku (GeneralPurpose(S_Gen5(0.5, 2.0)))  // min: 0.5 VCores, max: 2.0 VCores
}

// Serverless with integer VCores (also supported)
sqlDb {
    name "serverlessDb2"
    sku (GeneralPurpose(S_Gen5(1, 4)))  // min: 1 VCore, max: 4 VCores
}
```

#### Example

##### AD auth not set
```fsharp
open Farmer
open Farmer.Builders
open Sql

let myDatabases = sqlServer {
    name "my_server"
    admin_username "admin_username"
    enable_azure_firewall

    elastic_pool_name "mypool"
    elastic_pool_sku PoolSku.Basic100

    add_databases [
        sqlDb { name "poolDb1" }
        sqlDb { name "poolDb2" }
        sqlDb { name "dtuDb"; sku Basic }
        sqlDb { name "memoryDb"; sku M_8 }
        sqlDb { name "cpuDb"; sku Fsv2_8 }
        sqlDb { name "businessCriticalDb"; sku (BusinessCritical Gen5_2) }
        sqlDb { name "hyperscaleDb"; sku (Hyperscale Gen5_2) }
        sqlDb {
            name "generalPurposeDb"
            sku (GeneralPurpose Gen5_8)
            db_size (1024<Mb> * 128)
            hybrid_benefit
        }
        sqlDb {
            name "serverlessDb"
            sku (GeneralPurpose(S_Gen5(0.5, 2.0)))  // Serverless with fractional VCores
        }
    ]
}

let template = arm {
    location Location.NorthEurope
    add_resource myDatabases
}

template
|> Writer.quickWrite "sql-example"

template
|> Deploy.execute "my-resource-group" [ "password-for-my_server", "*****" ]
```

##### AD auth set
```fsharp
open Farmer
open Farmer.Builders
open Sql
open Farmer.Arm.Sql

let activeDirectoryAdmin: ActiveDirectoryAdminSettings =
    {
        Login = "adadmin"
        Sid = "F9D49C34-01BA-4897-B7E2-3694BF3DE2CF"
        PrincipalType = ActiveDirectoryPrincipalType.User
        AdOnlyAuth = false  // when false, admin_username is required
                            // when true admin_username is ignored
    }

let myDatabases = sqlServer {
    name "my_server"
    active_directory_admin (Some(activeDirectoryAdmin))
    admin_username "admin_username"
    enable_azure_firewall

    elastic_pool_name "mypool"
    elastic_pool_sku PoolSku.Basic100

    add_databases [
        sqlDb { name "poolDb1" }
        sqlDb { name "poolDb2" }
        sqlDb { name "dtuDb"; sku Basic }
        sqlDb { name "memoryDb"; sku M_8 }
        sqlDb { name "cpuDb"; sku Fsv2_8 }
        sqlDb { name "businessCriticalDb"; sku (BusinessCritical Gen5_2) }
        sqlDb { name "hyperscaleDb"; sku (Hyperscale Gen5_2) }
        sqlDb {
            name "generalPurposeDb"
            sku (GeneralPurpose Gen5_8)
            db_size (1024<Mb> * 128)
            hybrid_benefit
        }
        sqlDb {
            name "serverlessDb"
            sku (GeneralPurpose(S_Gen5(0.5, 2.0)))  // Serverless with fractional VCores
        }
    ]
}

let template = arm {
    location Location.NorthEurope
    add_resource myDatabases
}

template
|> Writer.quickWrite "sql-example"

template
|> Deploy.execute "my-resource-group" [ "password-for-my_server", "*****" ]
```
