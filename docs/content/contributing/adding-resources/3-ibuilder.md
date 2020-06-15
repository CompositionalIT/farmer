---
title: "3. The IBuilder interface"
draft: false
chapter: false
weight: 3
---

Sometimes, ARM resources are captured at a level of abstraction that is too low for us to reason about. In such cases, we

An `IBuilder` is not only even easier to consume by users than the F# record above, but can create *multiple* `IArmResource` objects at once. This is especially useful for more complex resources that tend to come in groups of two or three together - for example, Server Farm and Web Apps, or Cosmos DB Accounts, Databases and Containers. An IBuilder encapsulates the logic needed to create and configure all the resources together.

> In this example, the Container Registry builder only creates a single resource.

### Step 3.1: The Configuration Record
The first step is to create a simple configuration record that contains any data that is required to be captured by the user. Often, this may map nearly 1:1 with the `IArmResource` - normally the main difference will be that you do not need to provide the `Location` here, as Farmer will automatically provide that for you.

```fsharp
// src/Farmer/Builders.ContainerRegistry.fs
type ContainerRegistryConfig =
    { Name : ResourceName
      Sku : Sku
      AdminUserEnabled : bool }
    interface IBuilder with
        member this.DependencyName = this.Name
        member this.BuildResources location _ = [
            { Name = this.Name
              Location = location
              Sku = this.Sku
              AdminUserEnabled = this.AdminUserEnabled }
```

The `IBuilder` interface has two methods:

1. `DependencyName` - this is the name of the resource. It's used by Farmer when setting dependencies between multiple resources.
2. `BuildResources` - this functions takes in two arguments: the location the resources should be deployed to, and a list of any existing resources that have been created so far. Normally, you can ignore the second argument. The method should return the list of `IArmResource` resources that must be created - as you can see, this is a relatively simple mapping. For more complex builders e.g. one which represent multiple IArmResources at once, your `BuildResources` function will emit a *list* of IArmResources.

> It's tempting to suggest simply applying `IBuilder` directly onto the `IArmResource`. You *could* probably do this, but the separation and clarity provided here is an important step, and gives freedom in the future to diverge the shapes of the builder and the resource.

### Step 3.2 Test out the IBuilder.

You can finish this exercise by confirming that your `IBuilder` works correctly:

```fsharp
open Farmer.Builders.ContainerRegistry

let registries =
    { Name = ResourceName "my-registry"
      Sku = ContainerRegistry.Basic
      AdminUserEnabled = true }

let deployment = arm {
    location Location.WestEurope
    add_resource registries
}

deployment
|> Writer.quickWrite "output"
```

> Ensure that the location of `WestEurope` has been correctly applied to the emitted ARM template json file!