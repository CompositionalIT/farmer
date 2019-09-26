#r @"..\Farmer\bin\Debug\netstandard2.0\Farmer.dll"

open Farmer

let myFunctions = functions {
    name "isaacsuperfun"
}

let template =
    arm {
        location NorthEurope
        add_resource myFunctions
        output "functionsPassword" myFunctions.PublishingPassword
        output "functionsAIKey" (myFunctions.AppInsightsKey |> Option.defaultValue "")
        output "storageAccountKey" myFunctions.StorageAccountKey
    }

template
|> Writer.quickDeploy "my-resource-group-name"