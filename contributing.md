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

When building up a resource is will have a schema that looks something like this.

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

- Resource: Resources are a list of services added to an ARM template that define the state of said Azure services
- Template: Represents an ARM template with parameters, outputs and resources
- Location: An Azure Region where a service exists
- Deployment: Represents the deployment of an ARM template to a Location
- Builders: In Farmer this is both the file where the bulk of the functionality is added, as well as the class that defines the computation expression for creating a resource & ARM deployment
- Converters: Functions used to convert from configuration types to ARM model types
- Outputters: Functions that help convert from a Farmer model to a JSON structure expected in an ARM template

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

#### Step 1: Find a good template to see structure and glean DU for options like SKUs

Example 1: [https//docs.microsoft.com/en-us/azure/templates/microsoft.containerregistry/2017-10-01/registries](https//docs.microsoft.com/en-us/azure/templates/microsoft.containerregistry/2017-10-01/registries)  
Example 2: https://github.com/Azure/azure-quickstart-templates/tree/master/101-container-registry-geo-replication  

#### Step 2: Create a Builder.X for the resource X you are creating

In the Farmer project, create a new Builder file **Builder.X.fs** and in it define the `module` `Farmer.Resources.X`.
Place the `[<AutoOpen>]` attribute on the `module`

```fsharp
// Builder.ContainerRegistry.fs
[<AutoOpen>]
module Farmer.Resources.ContainerRegistry

open Farmer
open Farmer.Models
```

#### Step 3: Define Discriminated Union (DU) types found in step 1

As an example, the Azure Container Registry supports `Basic`, `Standard`, and `Premium`.

```fsharp
// Builder.ContainerRegistry.fs
[<RequireQualifiedAccess>]
/// Container Registry SKU
type ContainerRegistrySku =
    | Basic
    | Standard
    | Premium
```

#### Step 4: Define config type

Create a record type that will hold the configuration for your new Azure resource. This will for the state for the computation expression builder we will create in the next step.

```fsharp
// Builder.ContainerRegistry.fs
type ContainerRegistryConfig =
    { Name : ResourceName
      Sku : ContainerRegistrySku
      AdminUserEnabled : bool }
```

#### Step 5: Define builder for your resources Computation Expression (CE)

If you need have not built your own computation expression before, here are some resources to brush up:
- [MS docs](https://docs.microsoft.com/en-us/dotnet/fsharp/language-reference/computation-expressions)
- [The "Computation Expressions" series](https://fsharpforfunandprofit.com/series/computation-expressions.html)
- [Workshop](https://github.com/panesofglass/computation-expressions-workshop)

We will not cover any details of CE here. But to get started the only member you need to implement id `Yield`, which returns a minimal implementation of your resource.

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

Don't forget to assign an instance of the builder to a value to it is available.

#### Step 6: Define Resource type in Farmer.fs. and add a case to SupportedResources

This next record type will like look very similar to the one you created in Step 4 but serves a slightly different purpose. Step 4's type was to define an F# model for your builder. Think of this new record as the ARM template model. You will strip out the DU types for strings. Think of this as your DTO. This of course is not the only reason, since there are other points where you could deal with this. The more **enforced** reason is simply that **Farmer.fs** should appear before your builder in the compile order. Lastly, this type needs a `Location` field to capture the location where the resource will be deployed. So where the first record type is configuring the resource in general, this type represents an instance that will be deployed to a resource group.

```fsharp
// Farmer.fs
type ContainerRegistry =
  {
    Name : ResourceName
    Location : Location
    Sku : string
    AdminUserEnabled : bool }
```

Next you add a case for your new resource to the `SupportedResource` type `of` the record type you just defined.

```fsharp
// Farmer.fs
type SupportedResource =
    | AppInsights of AppInsights
    | StorageAccount of StorageAccount
    //...
    | CognitiveService of CognitiveServices
    | ContainerRegistry of ContainerRegistry // <-- here we add our resource case
    member this.ResourceName =
        match this with
        | AppInsights x -> x.Name
        | StorageAccount x -> x.Name
        //...
        | CognitiveService c -> c.Name
        | ContainerRegistry r -> r.Name // <-- here we add a match case for fetching the name
```

#### Step 8: Converters + Outputters

At this point we have all but one of our types defined and the last one is going to be anonymous anyway.

Next we will create some functions for mapping between the types we have created so far. You may need to create more mapping functions depending on your needs but you will need at least one converter from your builders config type your deployment instance config.

```fsharp
// Builder.ContainerRegistry.fs
module Converters =
    let containerRegistry location (config:ContainerRegistryConfig) : ContainerRegistry =
        { Name = config.Name
          Location = location
          Sku = config.Sku.ToString().Replace("_", ".")
          AdminUserEnabled = config.AdminUserEnabled }

    module Outputters =
        let containerRegistry (service:Farmer.Models.ContainerRegistry) =
            {| name = service.Name.Value
               ``type`` = "Microsoft.ContainerRegistry/registries"
               apiVersion = "2019-05-01"
               sku = {| name = service.Sku |}
               location = service.Location.ArmValue
               tags = {||}
               properties = {| adminUserEnabled = service.AdminUserEnabled |}
            |}
```

The mapping function in `Outputter` is taking our deployment specific instance and transforming it into the structure required to serialize to JSON for the ARM template.

#### Step 9: Extend `ArmBuilder` to allow adding the resource

Next we need to extend `ArmBuilder` to allow calling `add_resource` on the `arm` CE.

```fsharp
// Builder.ContainerRegistry.fs
type Farmer.ArmBuilder.ArmBuilder with
    /// Add the Container Registry to the ARM template
    member this.AddResource(state:ArmConfig, config) =
        { state with
            Resources = ContainerRegistry (Converters.containerRegistry state.Location config) :: state.Resources
        }
    /// Add multiple Container Registries to the ARM template    
    member this.AddResources (state, configs) = addResources<ContainerRegistryConfig> this.AddResource state configs
```

#### Step 10: Add to Writer

Our penultimate step is calling our output function in *Writer.fs* which is where the ARM template is generated.

```fsharp
// Writer.fs
module TemplateGeneration =
    let processTemplate (template:ArmTemplate) = {|
        ``$schema`` = "https://schema.management.azure.com/schemas/2019-04-01/deploymentTemplate.json#"
        contentVersion = "1.0.0.0"
        resources =
            template.Resources
            |> List.map(function
                | StorageAccount s -> Converters.Outputters.storageAccount s |> box
                | AppInsights ai -> Converters.Outputters.appInsights ai |> box
                //...
                | CognitiveService service -> Converters.Outputters.cognitiveServices service |> box
                | ContainerRegistry registry -> Converters.Outputters.containerRegistry registry |> box // <-- add the new case
            )
```

#### Step 11: Unit Tests + manual testing

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