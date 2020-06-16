---
title: "2. The IArmResource"
draft: false
chapter: false
weight: 2
---

In this exercise, we'll migrate our code from the previous step directly into the Farmer codebase.

### Step 2.1: Migrating to IArmResource

Now that you know that your resource model produces the correct Json value when passed into Farmer, we can now create a formal `IArmResource` statically that contains the "parameterised" parts of the above function, such as `name`, `sku` and `adminUserEnabled` and properly take part in the Farmer pipeline. This record should use types as required to capture e.g. SKUs or other elements that would benefit from typing (in the example above, `sku` is a string, but we will shortly replace that with a union type).

> Try to avoid going too far - the implementation of `JsonModel` should, more or less, be a copying of fields across and adding in some extra boilerplate around the `type` field etc. Feel free to use member properties to capture values that are "derived" from other ones.

```fsharp
// src/Farmer/Arm/ContainerRegistry.fs
[<AutoOpen>]
module Farmer.Arm.ContainerRegistry

open Farmer

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
        member this.ResourceName = this.Name
        member this.JsonModel =
            {| name = this.Name.Value
               ``type`` = "Microsoft.ContainerRegistry/registries"
               apiVersion = "2019-05-01"
               sku = {| name = this.Sku.ToString() |}
               location = this.Location.ArmValue
               tags = {||}
               properties = {| adminUserEnabled = this.AdminUserEnabled |}
            |} :> _
```

Notice how we perform simple "serialization" of elements such as the SKU, but otherwise most fields are copies across.

### Step 2.2: Move domain types out of the IArmBuilder.

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

Note that F# records must be completely filled, so you must provide values for all four fields.

 Alternatively, you could now write a test to assert the Json structure. Most tests in the project though tend to test from the Farmer builders, which we will get to soon.  You can stop right here if you want - what you've done so far allows you to create `IArmResource` objects which can be added to the Farmer pipeline. However, we will go further in the next exericse and make it even easier to create Container Registries by creating an **IBuilder**.