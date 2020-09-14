---
title: "Dependencies"
date: 2020-08-22T09:13:36+01:00
draft: false
weight: 3
---
ARM resources can depend on one another, and Farmer caters for this as well. However, much of this work is done for you since Farmer creates multiple resources for you at the builder level, and will ensure that the appropriate dependencies are set for you - for example, when creating a SQL Azure instance, Farmer will automatically ensure that the database depends on the server.

However, you will still need to set dependencies when you have a relationship between builders, such as setting the key of a storage account on a web app.

#### Setting dependencies
Setting a dependency requires you to call the `depends_on` keyword on the target resource, providing a handle to the dependent resources.

```fsharp
let myStorage = storageAccount {
    name "sampleaccount"
}

let myApp = webApp {
    name "myapp"
    setting "storage_key" myStorage.Key
    depends_on [ myStorage ]
}
```

or resources if we have many of them.

```fsharp
let myStorage1 = storageAccount {
    name "sampleaccountFirst"
}

let myStorage2 = storageAccount {
    name "sampleaccountSecond"
}

let myApp = webApp {
    name "myapp"
    setting "storage_key" myStorage.Key
    depends_on [ myStorage1.Name.ResourceName; myStorage2.Name.ResourceName ]
}
```

Here, we set up a dependency on `myApp` for `myStorage`. This guarantees that the storage account is created *before* Azure tries to grab the storage account key / connection string.