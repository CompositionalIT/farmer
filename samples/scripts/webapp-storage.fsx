#r "nuget:Farmer"

open Farmer
open Farmer.Builders

let myStorage = storageAccount {
    name "mystorage"
    sku Storage.Sku.Standard_LRS
    add_lifecycle_rule "cleanup" [ Storage.DeleteAfter 7<Days> ] Storage.NoRuleFilters
    add_lifecycle_rule "test" [ Storage.DeleteAfter 1<Days>; Storage.DeleteAfter 2<Days>; Storage.ArchiveAfter 1<Days>; ] [ "foo/bar" ]
}

let myWebApp = webApp {
    name "mysuperwebapp"
    sku WebApp.Sku.S1
    app_insights_off
    setting "storage_key" myStorage.Key
    add_allowed_ip_restriction "allow everything" "0.0.0.0/0"
    add_denied_ip_restriction "deny" "1.2.3.4/31"
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
