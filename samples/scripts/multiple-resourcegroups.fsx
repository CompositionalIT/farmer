#r @"./libs/Newtonsoft.Json.dll"
#r @"../../src/Farmer/bin/Debug/netstandard2.0/Farmer.dll"

open Farmer
open Farmer.Builders


let storageNested = storageAccount{ name "storagenested" }
let storage1 = storageAccount{ name "storage1" }
let storage2 = storageAccount{ name "storage2" }

let nestedResourceGroup = 
    resourceGroup {
        name "nested"
        add_resource storageNested
    } 
    
let resGroup1 = resourceGroup {
    name "resgroup-1"
    location Location.WestEurope
    add_resource storage1
    add_resource nestedResourceGroup // This can't create the resource group but can deploy to it
}
let resGroup2 = resourceGroup {
    location Location.EastUS
    name "resgroup-2"
    add_resource storage2
}

let template = subscriptionDeployment {
    location Location.NorthEurope // Store deployment metadata in North Eurpoe
    add_resource_group resGroup1
    add_resource_group resGroup2
}

template
|> Deploy.execute "deleteme" Deploy.NoParameters