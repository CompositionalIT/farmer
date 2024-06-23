#r "nuget:Farmer"

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

deployment |> Deploy.execute "my-resource-group-name" Deploy.NoParameters