#r @"./libs/Newtonsoft.Json.dll"
#r @"../../src/Farmer/bin/Debug/netstandard2.0/Farmer.dll"

open Farmer
open Farmer.Builders

let storage1 = storageAccount{ name "codatdevstorage1" }
let storage2 = storageAccount{ name "codatdevstorage2" }
    
let resGroup1 = resourceGroup {
    name "deleteme-1"
    location Location.WestEurope
    add_resource storage1
    output "storage1_key" storage1.Key
}

let resGroup2 = resourceGroup {
    location Location.EastUS
    name "deleteme-2"
    add_resource storage2
}

let template = subscriptionDeployment {
    location Location.NorthEurope // Store deployment metadata in North Eurpoe
    add_resource_group resGroup1
    add_resource_group resGroup2
}

template
|> Deploy.execute "deleteme" Deploy.NoParameters |> printf "%A"
//|> Writer.quickWrite "deleteme"
