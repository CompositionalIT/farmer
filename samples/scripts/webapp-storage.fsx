#r @"..\..\src\Farmer\bin\Debug\netstandard2.0\Farmer.dll"

open Farmer
open Farmer.Resources

let myStorage = storageAccount {
    name "mystorage"
    sku StorageSku.Premium_LRS
}

let myWebApp = webApp {
    name "mysuperwebapp"
    sku Sku.S1
    app_insights_off
    setting "storage_key" myStorage.Key
    depends_on myStorage
}

let deployment = arm {
    location NorthEurope
    add_resource myStorage
    add_resource myWebApp
    output "storage_key" myStorage.Key
    output "web_password" myWebApp.PublishingPassword
}

deployment
|> Deploy.execute "my-resource-group-name" Deploy.NoParameters