---
title: "Serverless ETL"
date: 2020-10-24
draft: false
weight: 5
---

#### Introduction
This tutorial shows how to create the infrastructure required to implement a common pattern for code-first "extract-transform-load" (ETL) processes. In this tutorial, imagine you wish to react to data being created in a blob in Storage, then parse it, and finally insert the "parsed" data into SQL in a relational database for use by e.g. a web application. We'll cover the following steps:

1. Creating an Azure Functions instance, with automatically configured storage and app insights instances.
1. Creating a SQL Azure database and server.
1. Configuring the Functions instance to have connection settings required to connect to both Storage and SQL instances.
1. Safely providing a SQL Server password to Farmer at deployment time.

{{< figure src="../../images/quickstarts/serverless-etl.png" caption="[Full code available here](https://github.com/CompositionalIT/farmer/blob/master/samples/scripts/tutorials/serverless-etl.fsx)">}}

#### Create the SQL instance
Create a SQL Server and database using the `sqlServer` builder. This server would be used for processed data at the end of the ETL pipeline.

```fsharp
open Farmer
open Farmer.Builders

let transactionalDb = sqlServer {
    name "etlserver"
    admin_username "theadministrator"
    add_databases [
        sqlDb { name "parseddata"; sku Sql.DtuSku.S1 }
    ]
}
```

> We explicitly set the SKU of the database. You don't *have* to do this; if you elect not to, Farmer will create an elastic pool and assign the database into that.

#### Create and configure a functions app
Create a functions instance which would contain the application that monitors the storage account for blobs, process each blob and then insert data into SQL. Also, provide the connection string that is derived from the SQL instance that you just created.

```fsharp
let etlProcessor = functions {
    name "etlprocessor"
    storage_account_name "mydata"
    setting "sql-conn" (transactionalDb.ConnectionString "parseddata")
}
```

If the mistype the database name for the connection string, Farmer will automatically fail and let you know. You can also, of course, bind the name to a symbol and use across both resources. Alternatively, bind the database itself to a value and provide that to both `add_databases` and as the value to the `ConnectionString` member.

> Functions instances require a storage account to operate, and will automatically create one for you. In this sample, we have explicitly provided the storage account name; you don't have to do this - Farmer will derive one based on the function instance name. If you prefer to manage the storage account yourself, you can create a storage account and use the `link_to_storage_account` keyword instead.
>
> Farmer will also automatically configure the functions instance with connection string settings for both the AzureWebJobsStorage and AzureWebJobsDashboard settings. You can use these to also configure your functions app to read from.

#### Add both resources to your ARM template

```fsharp
let template = arm {
    location Location.WestEurope
    add_resource transactionalDb
    add_resource etlProcessor
}
```

#### Deploy the template
When deploying the template, you'll need to provide the password for the SQL Server instance. This is captured as a secure parameter to the template; this guarantees that the password will not be stored in the ARM template as plain text.

```json
{
  "parameters": {
    "password-for-isaacetlserver": { "type": "securestring" }
  }
}
```

The parameter name is automatically generated based on the server name. A member on the sql configuration value can be used to quickly get to this and to set the password at deployment time:

```fsharp
template
|> Deploy.execute "my-resource-group" [ transactionalDb.PasswordParameter, "SQL PASSWORD GOES HERE" ]
|> printfn "%A"
```

> *Be sure to change the names of the Functions and SQL instances to be unique!*
>
> You should **never commit secrets into source control**. Instead, set environment variables or command line parameters to your Farmer program to read in the password and pass it into the `execute` function. For CI/CD tools such as Octopus or Azure DevOps, you can set secrets within such tools which will appear as environment variables.