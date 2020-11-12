#r @"./libs/Newtonsoft.Json.dll"
#r @"../../src/Farmer/bin/Debug/netstandard2.0/Farmer.dll"

open Farmer
open Farmer.Builders
open Farmer.CoreTypes

let myStorage = storageAccount {
    name "farmerstg"
}

let myStorage2 = storageAccount {
    name "farmerstg2"
}

let rg = resourceGroup {
    name "deleteme2"
    add_resource myStorage2
}

let template = arm {
    location Location.NorthEurope
    scope DeploymentScope.Subscription
    add_resource myStorage
    add_resource rg
}

template
|> Deploy.execute "deleteme" Deploy.NoParameters