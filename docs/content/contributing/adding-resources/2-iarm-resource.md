---
title: "2. The IArmResource"
draft: false
chapter: false
weight: 2
---

In this exercise, we'll migrate our code from the previous step directly into the Farmer codebase.

### Step 2.1: Migrating to IArmResource

Now that you know that your resource model produces the correct Json value when passed into Farmer, we can now create a proper type that contains the "parameterised" parts of the above function, such as `name`, `sku` and `adminUserEnabled`, and properly takes part in the Farmer pipeline, by implementing the `IArmResource` interface. This type is normally a full [Record](https://docs.microsoft.com/en-us/dotnet/fsharp/language-reference/records) and should use types as required to capture e.g. SKUs or other elements that would benefit from typing (in the example above, `sku` is a string, but we will shortly replace that with a union type).

```fsharp
// src/Farmer/Arm/ContainerRegistry.fs
[<AutoOpen>]
module Farmer.Arm.ContainerRegistry

open Farmer

// Create a reference to the full ARM registries type and version.
let registries = ResourceType ("Microsoft.ContainerRegistry/registries", "2019-05-01")

// Temporarily define the SKU and other types alongside the IArmResource.
type Sku =
    | Basic
    | Standard
    | Premium

type Registries =
    { Name : ResourceName
      Location : Location
      Sku : Sku
      AdminUserEnabled : bool }
    interface IArmResource with
        member this.ResourceId = registries.resourceId this.Name
        member this.JsonModel =
            {| name = this.Name.Value
               ``type`` = "Microsoft.ContainerRegistry/registries"
               apiVersion = "2019-05-01"
               sku = {| name = this.Sku.ToString() |}
               location = this.Location.ArmValue
               tags = {||}
               properties = {| adminUserEnabled = this.AdminUserEnabled |}
            |} :> _ // upcast to obj
```

Notice how we perform simple "serialization" of elements such as the SKU, but otherwise most fields are just copied across.

The biggest difference is that we have now also introduced the notion of `ResourceType` and `ResourceId`. A `ResourceType` allows you to specify the versioned ARM type that your resource implements; a `ResourceId` represents the qualified path to a specific, named resource. It contains at least the resource's type and its name, but can also optionally include e.g. the resource group that the resource belongs to.

### Step 2.2: Generating the ARM values more quickly
Because most ARM resources have a set of fields that are commonly used e.g. name, location etc., Farmer comes with a helper factory function to construct ARM JSON objects quickly and easily. Here's a shortened version of `JsonModel` above:

```fsharp
{| registries.Create(this.Name, this.Location) with
        sku = {| name = this.Sku.ToString() |}
        properties = {| adminUserEnabled = this.AdminUserEnabled |}
|} :> _ // upcast to obj
```

Now, the common fields will be generated for us through the `registies.Create` function; any custom fields (such as `sku` and `properties`) are then applied on top.

### Step 2.3: Move domain types out of the IArmBuilder.

> You can skip this step if you're just experimenting in e.g. a script.

For now, we've created any associated types such as `Sku` directly above the file, but you'll want to migrate these to a `Farmer.ContainerRegistry` module in `Common.fs` afterwards e.g.

```fsharp
// src/Farmer/Common.fs
namespace Farmer

module ContainerRegistry =
    type Sku =
    | Basic
    | Standard
    | Premium
```

### Step 2.4: Test out the new Registries record in Farmer.

You can test this again easily by passing an instance into a Farmer deployment like we did in the previous step:

```fsharp
open Farmer.Arm.ContainerRegistry
let registries =
    { Name = ResourceName "my-registry"
      Location = Location.WestEurope
      Sku = ContainerRegistry.Basic
      AdminUserEnabled = true }

let deployment = arm {
    location Location.NorthEurope
    add_resource registries
}

deployment
|> Writer.quickWrite "output"
```

> Note that F# records must be completely filled at creation, so you must provide values for all four fields.

You could now write a test to assert the Json structure. Most tests in the project though tend to test from the Farmer builders, which we will get to soon. You can stop right here if you want - what you've done so far allows you to create `IArmResource` objects which can be added to the Farmer pipeline. However, we will go further in the next exericse and make it even easier to create Container Registries by creating an **IBuilder**.