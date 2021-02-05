#r @"./libs/Newtonsoft.Json.dll"
#r @"../../src/Farmer/bin/Debug/netstandard2.0/Farmer.dll"

open Farmer
open Farmer.Builders

let storage1 = storageAccount{ name "farmerstorage1" }
let storage2 = storageAccount{ name "farmerstorage2" }
let storageNested = storageAccount{ name "farmerstorage3" }
    
let resGroupNested = resourceGroup {
    name "deleteme-nested"
    location Location.WestEurope
    add_resource storageNested
    output "storageNested_key" storageNested.Key
}

let resGroup1 = resourceGroup {
    name "deleteme-1"
    location Location.WestEurope
    add_resource storage1
    add_resource resGroupNested
    output "storage1_key" storage1.Key
}

let resGroup2 = resourceGroup {
    location Location.EastUS
    name "deleteme-2"
    add_resource storage2

    add_tag "Project" "farmer-test"
}

let template = subscriptionDeployment {
    location Location.NorthEurope // Store deployment metadata in North Eurpoe
    add_resource resGroup1
    add_resource resGroup2
}

template
|> Deploy.whatIf "deleteme" Deploy.NoParameters |> printf "%A"
//|> Writer.quickWrite "deleteme"
