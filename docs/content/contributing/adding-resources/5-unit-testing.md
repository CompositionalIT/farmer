---
title: "5. Unit Testing"
draft: false
chapter: false
weight: 5
---

Usually I would be pro writing the tests before you implement all this but it is important to get a feel for the moving parts. At this point you may want to write some tests so you can iterate quickly on getting the structure of your ARM template correct.

The tests you will find in the project are black-box style tests that focus on the input of a resource and the output of the ARM template. If you want to create tests for your mapping functions that is fine but remember between the strong type system and making it difficult to have `null` values, those kind of tests seldom yield much benefit in F#.

Of course, unit tests can only tell you so much when dealing with something as complex as Azure. Create a **fsx** file to run to check that your resource is deploying as expected.

```fsharp
// container-registry.fsx
#r "Newtonsoft.Json.dll"
#r @"../Farmer/bin/Debug/netstandard2.0/Farmer.dll"

open Farmer
open Farmer.Resources.ContainerRegistry

let myRegistry = containerRegistry {
    name "devonRegistry"
    sku ContainerRegistrySku.Basic
    enable_admin_user
}

let deployment = arm {
    location NorthEurope
    add_resource myRegistry
    output "registry" myRegistry.Name
    output "loginServer" myRegistry.LoginServer
}

deployment
|> Deploy.execute "FarmerTest" Deploy.NoParameters
|> printfn "%A"
```

Create a Resource Group to run it, here I called it "FarmerTest".

Run `dotnet fsi container-registry.fsx`
