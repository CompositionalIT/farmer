#r @"..\Farmer\bin\Debug\netstandard2.0\Farmer.dll"

open Farmer
open Farmer.Resources

let myFunctions = functions {
    name "isaacsuperfun"
}

let deployment =
    arm {
        location NorthEurope
        add_resource myFunctions
        output "functionsPassword" myFunctions.PublishingPassword
        output "functionsAIKey" (myFunctions.AppInsightsKey |> Option.defaultValue ArmExpression.Empty)
        output "storageAccountKey" myFunctions.StorageAccountKey
    }

deployment
|> Deploy.execute "my-resource-group-name" Deploy.NoParameters