#r @"./libs/Newtonsoft.Json.dll"
#r @"../../src/Farmer/bin/Debug/netstandard2.0/Farmer.dll"

open Farmer
open Farmer.Builders

let myStorage = storageAccount {
    name "mystorage"
    sku Storage.Standard_LRS
    add_lifecycle_rule "cleanup" [ Storage.DeleteAfter 7<Days> ] Storage.NoRuleFilters
    add_lifecycle_rule "test" [ Storage.DeleteAfter 1<Days>; Storage.DeleteAfter 2<Days>; Storage.ArchiveAfter 1<Days>; ] [ "foo/bar" ]
}

let myWebApp = webApp {
    name "mysuperwebapp"
    sku WebApp.Sku.S1
    app_insights_off
    setting "storage_key" myStorage.Key
}

let deployment = arm {
    location Location.NorthEurope
    add_resource myStorage
    add_resource myWebApp
    output "storage_key" myStorage.Key
    output "web_password" myWebApp.PublishingPassword
}

deployment
|> Deploy.execute "my-resource-group-name" Deploy.NoParameters
