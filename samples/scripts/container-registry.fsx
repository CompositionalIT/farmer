#r "nuget:Farmer"

open Farmer
open Farmer.Builders
open Farmer.ContainerRegistry

let myRegistry = containerRegistry {
    name "devonRegistry"
    sku Basic
    enable_admin_user
}

let deployment = arm {
    location Location.NorthEurope
    add_resource myRegistry
    output "registry" myRegistry.Name
    output "loginServer" myRegistry.LoginServer
}

deployment
|> Deploy.whatIf "FarmerTest" Deploy.NoParameters
|> printfn "%A"