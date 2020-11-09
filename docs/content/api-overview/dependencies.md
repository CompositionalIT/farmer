---
title: "Dependencies"
date: 2020-08-22T09:13:36+01:00
draft: false
weight: 3
---
ARM resources can depend on one another, and Farmer caters for this as well. Dependencies guarantee that when resources are created, Azure will provision them in the correct order so that e.g. a storage account is created *before* Azure tries to grab the storage account key / connection string for your web app setting.

Much of this work is done for you:

* Farmer creates multiple resources for you at the builder level, and will ensure that the appropriate dependencies are set for you - for example, when creating a SQL Azure instance, Farmer will automatically ensure that the database depends on the server.
* Farmer will generally identify dependencies correctly when you have a relationship between builders, such as setting the key of a storage account on a web app.

#### Automatic dependency detection
In the sample below, the `web app { }` will automatically realise that it needs to depend on Storage Account based on the "owner" of `Key` expression that is supplied.

```fsharp
let myStorage = storageAccount {
    name "sampleaccount"
}

let myApp = webApp {
    name "myapp"
    setting "storage_key" myStorage.Key
}
```

#### Manually setting dependencies
Normally, Farmer will do everything you need. However, there are some times when you may need to *explicitly* set a dependency:

* Farmer has not automatically detected the dependencies (please [raise an issue](https://github.com/CompositionalIT/farmer/issues) if you notice this!).
* You're setting a dependency on a resource that you're creating yourself, outside of Farmer.
* You want to set a dependency even though there is no explicit coupling between two resources / builders.

Setting a dependency requires you to call the `depends_on` keyword on the target resource, providing a handle to the dependent resource.

```fsharp
let myStorage1 = storageAccount {
    name "sampleaccountFirst"
}

let myApp = webApp {
    name "myapp"
    depends_on myStorage1
}
```

Here, we set up an explicit dependency on `myApp` for `myStorage`.

#### Adding multiple dependencies at once
You can also supply multiple dependencies at once as a list; this is useful if you are programmatically creating multiple resources.

```fsharp
// Create five storage accounts
let storageAccounts : IBuilder list = [
    for letter in [ 'a' .. 'e' ] do
        storageAccount {
            name (sprintf "mystorage%c" letter)
        }
]

let myApp = webApp {
    name "myapp"
    depends_on storageAccounts // add them all to the web app as dependencies
}
```

Notice the extra type hint, `: IBuilder list`. This is required because F# does not, by default, allow you to implicitly treat a list of values as a supertype. In this case, a `StorageAccountConfig list` is not considered implicitly convertable to `: IBuilder list` (which is an interface that `StorageAccountConfig` implements). Therefore, we have to do it ourselves using the extra type declaration.

You can also use the `:>` (safe upcast) operator when declaring the StorageAccount:

```fsharp
let storageAccounts = [ // inferred as IBuilder list
    for letter in [ 'a' .. 'e' ] do
        storageAccount {
            name (sprintf "mystorage%c" letter)
        } :> IBuilder
]
```

> We're looking at improving this situation in the future using F#'s "flexible types" feature.

All builders that support dependencies support a number of `depends_on` overloads:

* A single, or a list of, Builders (as shown above)
* A single, or a list of, resources by their Name
* A single, or a list of, [IArmResources](../../contributing/adding-resources/2-iarm-resource/)