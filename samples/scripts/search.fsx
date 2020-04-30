#r @"./libs/Newtonsoft.Json.dll"
#r @"../../src/Farmer/bin/Debug/netstandard2.0/Farmer.dll"

open Farmer
open Farmer.Resources

let mySearch = search {
    name "isaacsSearch"
    sku SearchSku.Basic
}

let deployment = arm {
    location NorthEurope
    add_resource mySearch
    output "search-admin-key" mySearch.AdminKey
    output "search-query-key" mySearch.QueryKey
}

deployment
|> Deploy.execute "my-resource-group-name" Deploy.NoParameters