---
title: "4. Providing Builder syntax"
draft: false
chapter: false
weight: 4
---

If you want to get the nice json-like syntax for your configuration record, you need to implement a separate class which contains a set of methods that act on the Configuration Record that you created previously - one for each keyword that you want.

> If you need have not built your own computation expression before, here are some resources to brush up:
> * [Office microsoft docs](https://docs.microsoft.com/en-us/dotnet/fsharp/language-reference/computation-expressions)
> * [The "Computation Expressions" series](https://fsharpforfunandprofit.com/series/computation-expressions.html)
> * [Workshop](https://github.com/panesofglass/computation-expressions-workshop)

### Step 4.1: Creating basic keywords

We will not cover the inner details of creating a CE here. But to get started the only member you need to implement id `Yield`, which returns a minimal implementation of your resource.

```fsharp
// Builder.ContainerRegistry.fs
type ContainerRegistryBuilder() =
    /// Required - creates default "starting" values
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

Each keyword has a similar set of steps required:

1. Create a member which takes in at least one argument, the current `state` object. The implement should perform some modification and return back the newly-updated state.
2. Decorate the method with the `CustomOperation` attribute; the string value passed to it will become the keyword name. Use `_` to separate words of the keyword e.g. `enable_admin_user`.
3. Put a `///` comment on the method for intellisense to guide users.

Now you can create members on the builder that appear as custom operators in your resource CE. In each member you build up the state of the resource configuration you created in the previous step.

> Don't forget to assign an instance of the builder to a value so it is available for consumers!

#### Parameterless keywords
You can create parameterless keywords by simply only taking in the state argument e.g. `enable_admin_user` above.

#### Keywords with multiple arguments
You can take in multiple arguments by simply putting a comma after each additional argument. They will be consumed by the user with spaces.

#### Overloaded keywords
You can provide multiple overloads for a keyword. However, each overload must take in the same number of arguments. Do not re-apply the `CustomOperation` attribute - simply provide multiple methods with the same name.