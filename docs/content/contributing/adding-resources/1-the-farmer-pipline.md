---
title: "1. The Farmer Pipeline"
draft: false
chapter: false
weight: 1
---

This step will get you up and running by incorporate something quickly and easily into the Farmer pipeline that emits a valid [Azure Container Registry](https://azure.microsoft.com/en-us/services/container-registry/).

### Step 1.1: Prototype and test using an fsx file
Technically this step is not necessary but it is the quickest way to get a working deployment from which you can iterate upon.

Start by looking at [this sample](https://github.com/Azure/azure-quickstart-templates/blob/master/101-container-registry-geo-replication/azuredeploy.json) and identifying the section of JSON that relates to the resource we want - in our case, the `Microsoft.ContainerRegistry/registries` resource.

> The use of `#r "nuget:..."` syntax can be enabled by going to `Settings > F# > Fsi Extra Parameters` and adding `--langversion:preview` to the `FSharp.fsiExtraParameters` list (.NET 5 only)
>
> If you are not using .NET 5, manually build Farmer and reference the dll manually - see the `samples` folder for examples.

```fsharp
// container-registry-prototype.fsx
#r "nuget: farmer"
#r "nuget: Newtonsoft.Json"

open Farmer
open Farmer.CoreTypes

// A function called "registries" that takes in a name, sku and boolean flag for whether to enable the admin user.
let registries name sku adminUserEnabled =
    sprintf """{
        "name": "%s",
        "type": "Microsoft.ContainerRegistry/registries",
        "apiVersion": "2019-05-01",
        "location": "westeurope",
        "tags": { },
        "sku": { "name": "%s" },
        "properties": { "adminUserEnabled": %b }
    }""" name sku adminUserEnabled
    |> Resource.ofJson

let deployment = arm {
    location Location.NorthEurope
    add_resource (registries "my-registry" "Basic" true)
}

deployment
|> Writer.quickWrite "test-output"

// or push out for real to Azure!

// deployment
// |> Deploy.execute "FarmerTest" Deploy.NoParameters
// |> printfn "%A"
```

Observe how we've pasted a minimal section of JSON and then tried to extract some of the candidates for parameterisation - in our case `name`, `sku` and `adminUserEnabled`, and how we've used the `Resource.ofJson` function to create an `IArmResource` for us to quickly allow us "into" the Farmer pipeline.

Test out the JSON model you created and make sure it creates the resources in Azure you would expect. You can deploy with `execute` or you can use `whatIf` to see what the expected state would be.

### Step 1.2: Convert from JSON to an F# anonymous record
For simple ARM resources, raw JSON may suffice, but normally you'll want a little more control in order to programmatically choose whether to add / remove fields etc. during the export phase. The best way to do this is to replace the raw string export with an anonymous record:

```fsharp
let registries name sku adminUserEnabled =
    {| name = name
       ``type`` = "Microsoft.ContainerRegistry/registries"
       apiVersion = "2019-05-01"
       location = "westeurope"
       tags = {| |}
       sku = {| name = sku |}
       properties = {| adminUserEnabled = adminUserEnabled |}
    |}
    |> Resource.ofObj
```

Notice how the structure is the same, but is now implemented directly in F#.