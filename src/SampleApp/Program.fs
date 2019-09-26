open Farmer

let mySearch = search {
    name "isaacsSearch"
    sku Search.Sku.BasicSearch
}

let myWebApp = webApp {
    name "isaacswebapp"
    sku WebApp.Sku.F1
    setting "search_key" mySearch.QueryKey
    depends_on mySearch
}

let template = arm {
    location NorthEurope
    add_resource mySearch
    add_resource myWebApp
    output "publishing-password" myWebApp.PublishingPassword
    output "search-admin-key" mySearch.AdminKey
    output "search-query-key" mySearch.QueryKey
}

template
|> Writer.quickDeploy "deleteme"