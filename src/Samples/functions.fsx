#r @"..\Farmer\bin\Debug\netstandard2.0\Farmer.dll"

open Farmer

let myFunctions = functions {
    name "isaacsuperfun"
    service_plan_name "isaacsuperfunhost"
    storage_account_name "isaacsuperstorage"
    auto_create_storage
    operating_system Windows
    use_runtime DotNet
    use_app_insights "isaacsuperai"
}

let template =
    arm {
        location Helpers.Locations.``North Europe``
        resource myFunctions
    }

template
|> Writer.toJson
|> Writer.toFile @"functions.json"
