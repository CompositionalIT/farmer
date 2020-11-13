#r @"./libs/Newtonsoft.Json.dll"
#r @"../../src/Farmer/bin/Debug/netstandard2.0/Farmer.dll"

open Farmer
open Farmer.Builders
open Farmer.CoreTypes

let grp1Storage = storageAccount {
    name "codatgrp1storage"
}

let grp2Storage = storageAccount {
    name "codatgrp2storage"
}

let group1 = resourceGroup {
    name "grp1"
    add_resource grp1Storage
}

let group2 = resourceGroup {
    name "grp2"
    add_resource grp2Storage
}

let template = arm {
    scope Subscription
    add_resource group1
    add_resource group2
}

template
|> Deploy.executeSubscription Location.WestEurope Deploy.NoParameters