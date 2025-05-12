---
title: "3. The IBuilder interface"
draft: false
chapter: false
weight: 3
---

Sometimes, ARM resources are captured at a level of abstraction that is too low for us to reason about. In such cases, we may wish to use the `IBuilder` interface. An `IBuilder` is not only even easier to consume by users than the F# record we looked at in the previous exercise, but can create *multiple* `IArmResource` objects at once. This is especially useful for more complex resources that tend to come in groups of two or three together - for example, Server Farm and Web Apps, or Cosmos DB Accounts, Databases and Containers. An IBuilder encapsulates the logic needed to create and configure all the resources together.

> In this example, the Container Registry builder only creates a single resource.

### Step 3.1: The Configuration Record
The first step is to create a simple configuration record that contains any data that is required to be captured by the user. Often, this may map nearly 1:1 with the `IArmResource` - normally the main difference will be that you do not need to provide the `Location` here, as Farmer will automatically provide that for you.

```fsharp

// src/Farmer/Arm/ContainerRegistry.fs
let registries = ResourceType ("Microsoft.ContainerRegistry/registries", "2019-05-01")

// src/Farmer/Builders.ContainerRegistry.fs
type ContainerRegistryConfig =
    { Name : ResourceName
      Sku : Sku
      AdminUserEnabled : bool }
    interface IBuilder with
        member this.ResourceId = registries.resourceId this.Name
        member this.BuildResources location = [
            { Name = this.Name
              Location = location
              Sku = this.Sku
              AdminUserEnabled = this.AdminUserEnabled }
        ]
```

The `IBuilder` interface has two members:

1. `ResourceId` - a field that is the *identifying path* of the resource (including resource type, version, and name). It's used by Farmer when setting dependencies between multiple resources and for emitting the appropriate JSON. If your builder has several ARM resources, it should return the "main" resource in the builder that others would depend upon.
2. `BuildResources` - a function takes the location that the resources should be deployed to, and should return the list of `IArmResource` resources that must be created - this is normally a relatively simple mapping. For more complex builders e.g. one which represents multiple `IArmResource` objects, your `BuildResources` function will emit a *list* of IArmResources.

> It's tempting to suggest simply applying `IBuilder` directly onto the `IArmResource`. You *could* probably do this, but the separation and clarity provided here is an important step, and gives freedom in the future to diverge the shapes of the builder and the underlying resource.

### Step 3.2 Test out the IBuilder.
You can finish this exercise by confirming that your `IBuilder` works correctly:

```fsharp
open Farmer.Builders.ContainerRegistry

let registries =
    { Name = ResourceName "my-registry"
      Sku = ContainerRegistry.Basic
      AdminUserEnabled = true }

let deployment = arm {
    location Location.WestCentralUS
    add_resource registries
}

deployment
|> Writer.quickWrite "output"
```

> Ensure that the location of `WestCentralUS` has been correctly applied to the emitted ARM template json file!
