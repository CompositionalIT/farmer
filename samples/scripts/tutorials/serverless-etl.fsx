#r @"../libs/Newtonsoft.Json.dll"
#r @"../../../src/Farmer/bin/Debug/netstandard2.0/Farmer.dll"

open Farmer
open Farmer.Builders

let database = sqlDb {
    name "isaacparseddata"
    sku Sql.DtuSku.S1
}

let transactionalDb = sqlServer {
    name "isaacetlserver"
    admin_username "theadministrator"
    add_databases [ database ]
}

let etlProcessor = functions {
    name "isaacetlprocessor"
    storage_account_name "isaacmydata"
    setting "sql-conn" (transactionalDb.ConnectionString database)
}

let template = arm {
    location Location.WestEurope
    add_resources [
        transactionalDb
        etlProcessor
    ]
}

// Generate the ARM template here...
template
|> Writer.quickWrite @"generated-template"

// Or deploy it directly to Azure here... (required Azure CLI installed!)
// template
// |> Deploy.execute "my-resource-group" [ transactionalDb.PasswordParameter, "SQL PASSWORD GOES HERE" ]
// |> printfn "%A"