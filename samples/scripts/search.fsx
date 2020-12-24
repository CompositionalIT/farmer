#r @"./libs/Newtonsoft.Json.dll"
#r @"../../src/Farmer/bin/Debug/netstandard2.0/Farmer.dll"

open Farmer
open Farmer.Builders
open Farmer.Search

let mySearch = search {
    name "isaacsSearch"
    sku Basic
}

let deployment = arm {
    location Location.NorthEurope
    add_resource mySearch
    output "search-admin-key" mySearch.AdminKey
    output "search-query-key" mySearch.QueryKey
}

deployment.Deploy "my-resource-group-name"