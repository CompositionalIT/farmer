#r @"..\Farmer\bin\Debug\netstandard2.0\Farmer.dll"

open Farmer

let myStorage = storageAccount {
    name "mystorage"
    sku Storage.Sku.PremiumLRS
}

let myWebApp = webApp {
    name "mysuperwebapp"
    sku WebApp.Sku.S1
    setting "storage_key" myStorage.Key
    depends_on myStorage
}

let template = arm {
    location Locations.NorthEurope
    resource myStorage
    resource myWebApp
    output "storage_key" myStorage.Key
    output "web_password" myWebApp.PublishingPassword
}

template
|> Writer.toJson
|> Writer.toFile "webapp-storage.json"