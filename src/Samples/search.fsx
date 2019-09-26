#r @"..\Farmer\bin\Debug\netstandard2.0\Farmer.dll"

open Farmer

let mySearch = search {
    name "isaacsSearch"
    sku Sku.BasicSearch
}

let template = arm {
    location NorthEurope
    add_resource mySearch
    output "search-admin-key" mySearch.AdminKey
    output "search-query-key" mySearch.QueryKey
}

template
|> Writer.quickDeploy "my-resource-group-name"