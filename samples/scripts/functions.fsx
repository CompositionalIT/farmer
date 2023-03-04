#r "nuget:Farmer"

open Farmer
open Farmer.Builders

let myFunctions = functions { name "isaacsuperfun" }

let deployment = arm {
    location Location.NorthEurope
    add_resource myFunctions
    output "functionsPassword" myFunctions.PublishingPassword
    output "functionsAIKey" (myFunctions.AppInsightsKey |> Option.defaultValue ArmExpression.Empty)
    output "storageAccountKey" myFunctions.StorageAccountKey
}

deployment |> Deploy.execute "my-resource-group-name" Deploy.NoParameters
