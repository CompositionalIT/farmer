---
title: "API Overview"
date: 2020-02-05T08:53:46+01:00
weight: 3
chapter: false
---

#### API aims
The key guiding principles of the Farmer API are (in order):

* **Simplicity**: Make it as easy as possible to do the most common tasks.
* **Type safety**: Where possible, use F#'s type system to make it impossible to create invalid templates.
* **Flexibility**: Provide users with the ability to override the defaults where needed.

#### Farmer Resources
Farmer works on a simple, consistent process:

1. You create **Farmer resources**, such as Storage Accounts and Web Apps.
1. Each Farmer resource can represent *one or many* ARM resources. For example, a Farmer `webApp` resource represents both the `Microsoft.Web/sites` and `Microsoft.Web/serverfarms` resources. In addition, it optionally also provides simplified access to an `Microsoft.Insights/components`.
1. You configure each resource using simple, human-readable custom keywords in a strongly-typed environment.
1. You link together resources as required.
1. Once you have created all resources, you bundle them up together into an **ARM deployment resource**.
1. You then generate (and optionally deploy) an ARM template.
1. The rest of your deployment pipeline stays the same.

The diagram below illustrates how Farmer resources map to ARM ones:

{{<mermaid align="left">}}
graph TD

subgraph ARM Template
classDef danger fill:orange;

C(Microsoft.Web/serverfarms) -. dependency .->F
D(Microsoft.Insights/components) -. dependency .->F
E(Microsoft.Storage/storageAccounts) -. dependency .->F
E -. storage key .-> F
E -. storage key .-> G
F(Microsoft.Web/sites)
G(blobServices/containers)

class C danger
class D danger
class E danger
class F danger
class G danger

end

subgraph Farmer
A(webApp)-. depends on .->B
B(storageAccount)-. key .->A
end

{{< /mermaid >}}

In this example, we create a storage account and web app in Farmer, which maps five different ARM template resources. As you can see, resources in Farmer are declared at a higher level of abstraction than ARM template resources. This makes things much simpler to reason about, and quicker to author.

#### An example Farmer Resource
All Farmer resources follow the same approach:

1. You define a resource using a special "builder" that allows the use of custom keywords. This is known as an F# [computation expression](https://docs.microsoft.com/en-us/dotnet/fsharp/language-reference/computation-expressions). Each builder is designed for a specific type of Azure resource e.g. websites, functions, virtual machines etc., and each has defaults set for the most common scenario.
2. This resource is validated and converted into a Farmer configuration object, which contains the configuration for that resource including any defaults.
3. This configuration is then added to an overarching Farmer ARM deployment object.
4. The ARM deployment object is converted into an ARM template json file.

{{<mermaid align="left">}}
graph LR

subgraph JSON
C(ARM Template)
end

subgraph .NET
A(Farmer Builder)--validation and defaults -->B
B(Farmer Configuration) --emitted to --> C
end

{{< /mermaid >}}

Here's an example web application.

```fsharp
let myWebApp = webApp {
    name "mystorage"
    setting "myKey" "aValue"
    sku Sku.B1
    always_on
    app_insights_off
    worker_size WorkerSize.Medium
    number_of_workers 3
    run_from_package
}
```

* The `webApp { }` builder defines the start and end of the definition of the web application.
* Within this builder, you use custom keywords to configure the web app, such as `name` and `setting`.
* Some keywords take arguments, but others e.g. `always_on` are simple declarative markers.

> You can view details of all farmer resources in the [resource guide](../api-overview/resources/).

#### Putting it all together

The diagram above can be shown in code as follows:

```fsharp
/// An Azure Storage account with a container.
let storage = storageAccount {
    name "astorageaccount"
    add_public_container "myContainer"
}

/// An Azure App Service with built-in App Insights.
let app = webApp {
    name "awebapp"
    setting "storageKey" storage.Key // pull in the storage key to an app setting
    depends_on storage // state that this web app depends on the storage account
}

/// An ARM deployment with both of the above resources attached
let deployment = arm {
    location Location.NorthEurope
    add_resource storage
    add_resource app
}

// Write the ARM template out to myTemplate.json
let filename =
    deployment.Template
    |> Writer.toJson
    |> Writer.toFile "myTemplate"
```