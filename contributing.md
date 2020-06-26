# Developer guide for contributing

Thanks for thinking about contributing! Azure is a giant beast and help supporting more use-cases is always appreciated. To make it easier to contribute, we put together this little guide. Please take a read through it before starting work on a Pull Request to **farmer**.

## The process (don't worry... this is not waterfall)

1. Open an issue, or comment on an existing open issue covering the resource you would like to work on. Basically, a PR from you should not come as a surprise.
1. Implement the 20% of features that cover 80% of the use cases.
1. PR against the *master* branch from your *fork*.
1. Add/update tests as required.
1. PRs need to pass build/test against both Linux & Windows build, and a review, before being merged in.
1. Submit a separate PR against the *docs* branch with the updated documentation for your feature.

## ARM template 101

> Skip this section if you are already familiar with ARM Templates

This won't be an introduction to [Azure Resource Manager](https://docs.microsoft.com/en-us/azure/azure-resource-manager/management/overview) or [ARM templates](https://docs.microsoft.com/en-us/azure/azure-resource-manager/templates/overview). Instead let's go through the main parts that are important for creating a new resource.

The main parts of ARM Templates can be broken into resources, outputs, variables, and parameters. Farmer has [limited support for parameters and no support for variables](https://compositionalit.github.io/farmer/api-overview/parameters/), so we will not cover them.

So a generated ARM Template from Farmer will have the following structure.

```json
{
  "$schema": "https://schema.management.azure.com/schemas/2019-04-01/deploymentTemplate.json#",
  "contentVersion": "1.0.0.0",
  "outputs": {},
  "parameters": {},
  "resources": []
}
```

When building a new resource in Farmer you are providing the means for a user of Farmer to generate a new resource type, or configure a new property on an existing resource. These resources are added to the `resources` array you can see above.

When building up a resource it will have a schema that looks something like this.

```json
{
  "name": "string",
  "type": "Microsoft.ContainerRegistry/registries",
  "apiVersion": "2019-05-01",
  "location": "string",
  "tags": {},
  "sku": {
    "name": "string"
  },
  "properties": {
    "adminUserEnabled": "boolean",
  },
  "resources": []
}
```

Your resource you create will have a [type of service](https://docs.microsoft.com/en-us/azure/templates/microsoft.containerregistry/allversions) that it represents. Each service has many versions, represented by a date.

Typically your builder will at first focus on adding properties to the `properties` field to configure a service to be deployed in a certain state.

## Terms that are useful

- **Resource**: Resources are a list of services added to an ARM template that define the state of said Azure services. In Farmer these resource models are created by implementing `IArmResource`.
- **Template**: Represents an ARM template with parameters, outputs and resources
- **Location**: An Azure Region where a service exists
- **Deployment**: Represents the deployment of an ARM template to a Location
- **Builder**: In Farmer an `IBuilder` represents provides the capability of creating a smart type that helps model a resource *or a collection of resources* into associated `IArmResource` objects required for constructing the ARM template. For example, Farmer's WebApp builder provides a logical abstraction on top of several ARM resources: Web App, Server Farm and Application Insights.

## Implementing a new resource

### Requirements

- Minimum version is 2.3.1 of Azure CLI
- Azure account to test against

### Steps

In the following steps we will go through the steps for creating the `ContainerRegistry` Builder and update Farmer shared components to allow for deploying a `ContainerRegistry` in Farmer.

This will end up allowing us to define a resource that looks like this:

```fsharp
let myRegistry = containerRegistry {
    name "devonRegistry"
    sku ContainerRegistrySku.Basic
    enable_admin_user
}

let deployment = arm {
    location NorthEurope
    add_resource myRegistry
}
```

#### Step 1: Look at existing ARM templates and reference docs
Often, you can find an existing ARM template that will give you a good starting point for a real-world template; meanwhile, the reference docs, whilst sometimes out of date, allow to see structure and glean possiblities for modelling into F# with, for example, discriminated unions or similar.

* Reference Docs: [https//docs.microsoft.com/en-us/azure/templates/microsoft.containerregistry/2017-10-01/registries](https://docs.microsoft.com/en-us/azure/templates/microsoft.containerregistry/2017-10-01/registries)
* Sample Template: https://github.com/Azure/azure-quickstart-templates/tree/master/101-container-registry-geo-replication

#### Step 2: Prototype and test using an fsx file

Technically this step is not necessary but it is the quickest way to get a working deployment.

```fsharp
// container-registry-prototype.fsx
// this can be enabled  by going to Settings > F# > Fsi Extra Parameters > and add "--langversion:preview" to the FSharp.fsiExtraParameters list
#r "nuget: farmer"
#r "nuget: Newtonsoft.Json"

open Farmer

let quickContainerRegistry (name, sku, enableAdmin) =
    {| name = name
       ``type`` = "Microsoft.ContainerRegistry/registries"
       apiVersion = "2019-05-01"
       sku = {| name = sku |}
       location = Location.NorthEurope
       tags = {||}
       properties = {| adminUserEnabled = enableAdmin |}
    |} |> Helpers.toArmResource

let deployment = arm {
    location NorthEurope
    add_resource (quickContainerRegistry "TestRegistry" "Basic" true)
}

deployment
|> Deploy.whatIf "FarmerTest" Deploy.NoParameters
|> printfn "%A"
```

Test out the JSON model you created and make sure it creates the resources in Azure you would expect. You can deploy with `execute` or you can use `whatIf` to see what the expected state would be.

#### Step 3: Create an IArmResource
Now that you know that your resource model produces the correct Json value when passed into an Arm deployment, you should now create a formal record to capture the data. We do this by implement `IArmResource` on the record with the required values.

This record should use types as required to capture e.g. SKUs or other elements that would benefit from typing. Try to avoid going too far though - the implementation of `JsonValue` should, more or less, be a copying of fields across and adding in some extra boilerplate around the `type` field etc. Feel free to use member properties to capture values that are "derived" from other ones though.

```fsharp
// src/Farmer/Arm/ContainerRegistry.fs
[<AutoOpen>]
module Farmer.Arm.ContainerRegistry

open Farmer
open Farmer.CoreTypes
open Farmer.ContainerRegistry

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
               sku = {| name = this.Sku.ToString().Replace("_", ".") |}
               location = this.Location.ArmValue
               tags = {||}
               properties = {| adminUserEnabled = this.AdminUserEnabled |}
            |} :> _
```
Notice how we perform simple "serialization" of elements such as the SKU, but otherwise most fields are copies across.

Types, such as the `Sku` type above, should be added to the `Farmer.<resource>` module, in `Common.fs` e.g.

```fsharp
// src/Farmer/Common.fs
namespace Farmer


module ContainerRegistry =
    type Sku =
    | Basic
    | Standard
    | Premium
```

You can test this again easily by passing an instance into an Arm deployment like we did in the previous step. Alternatively, you could now write a test to assert the Json structure. Most tests in the project though tend to test from the Farmer models, which we will get to soon.

#### Step 4: Create the builder
You can stop right here if you want - what you've done so far allows you to create `IArmResource` objects which can be added to the Farmer pipeline. However, we will go further and make it even easier to create Container Registries by creating an **IBuilder**.

An `IBuilder` can create *multiple* `IArmResource` objects at once. This is especially useful for more complex resources that tend to come in groups of two or three together - for example, Server Farm and Web Apps, or Cosmos DB Accounts, Databases and Containers. An IBuilder encapsulates the logic needed to create all the resources together, pre-configured to work together.

In this example, the Container Registry builder only creates a single resource.

```fsharp
// src/Farmer/Builders.ContainerRegistry.fs

type ContainerRegistryConfig =
    { Name : ResourceName
      Sku : Sku
      AdminUserEnabled : bool }
    interface IBuilder with
        member this.BuildResources location _ = [
            { Name = this.Name
              Location = location
              Sku = this.Sku
              AdminUserEnabled = this.AdminUserEnabled }
        ]
```

The `BuildResources` function takes in two arguments - the location the resources should be deployed to, and the existing resources that have been created so far. Normally, you can ignore the second argument.

#### Step 5: Define builder for your resources Computation Expression (CE)

If you need have not built your own computation expression before, here are some resources to brush up:
- [MS docs](https://docs.microsoft.com/en-us/dotnet/fsharp/language-reference/computation-expressions)
- [The "Computation Expressions" series](https://fsharpforfunandprofit.com/series/computation-expressions.html)
- [Workshop](https://github.com/panesofglass/computation-expressions-workshop)

We will not cover any details of the CE here. But to get started the only member you need to implement id `Yield`, which returns a minimal implementation of your resource.

```fsharp
// Builder.ContainerRegistry.fs
type ContainerRegistryBuilder() =
    member _.Yield _ =
        { Name = ResourceName.Empty
          Sku = Basic
          AdminUserEnabled = false }

    [<CustomOperation "name">]
    /// Sets the name of the Azure Container Registry instance.
    member _.Name (state:ContainerRegistryConfig, name) = { state with Name = ResourceName name }

    [<CustomOperation "sku">]
    /// Sets the name of the SKU/Tier for the Container Registry instance.
    member _.Sku (state:ContainerRegistryConfig, sku) = { state with Sku = sku }

    [<CustomOperation "enable_admin_user">]
    /// Enables the admin user on the Azure Container Registry.
    member _.EnableAdminUser (state:ContainerRegistryConfig) = { state with AdminUserEnabled = true }

let containerRegistry = ContainerRegistryBuilder()
```

Now you can create members on the builder that appear as custom operators in your resource CE. In each member you build up the state of the resource configuration you created in Step 4.

Don't forget to assign an instance of the builder to a value so it is available.

#### Step 6: Unit Tests + manual testing

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

## Updating the docs

1. Checkout the *docs* branch.
2. If detached, run `git checkout --track origin/docs`
3. Create a new **.md* file with the name of your resource in the folder **/content/api-overview/resources/**. Eg. **container-registry.md**
4. Add a description, keywords, and an example
5. Commit, push, PR

## Advanced

TODO
- validation
- outputs
- parameters
- multiple resource builders
- linking resources (one-to-many relationships)