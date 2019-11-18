#r @"..\Farmer\bin\Debug\netstandard2.0\Farmer.dll"

open Farmer
open Farmer.Resources.Search

let mySearch = search {
    name "isaacsSearch"
    sku Sku.Basic
}

let template = arm {
    location NorthEurope
    add_resource mySearch
    output "search-admin-key" mySearch.AdminKey
    output "search-query-key" mySearch.QueryKey
}

template
|> Writer.quickDeploy "my-resource-group-name"