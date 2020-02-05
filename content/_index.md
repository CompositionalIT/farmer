---
title: "Farmer"
date: 2020-02-04T00:36:21+01:00
draft: false
---

# Farmer

Farmer is a DSL for rapidly generating non-complex ARM templates in a type-safe manner.

### Main Features

* Create non-complex ARM templates through a simple, strongly-typed and pragmatic DSL.
* Create strongly-typed dependencies to resources.
* Runs on .NET Core.
* Use standard F# code to dynamically create ARM templates quickly and easily.

```fsharp
// Create a storage account
let myStorageAccount = storageAccount {
    name "myTestStorage"
    sku Sku.PremiumLRS
    add_public_container "myContainer"
}

// Create a web app with a pre-configured application insights service
let myWebApp = webApp {
    name "myTestWebApp"
    setting "storageKey" myStorageAccount.Key
    sku Sku.B1
    always_on
    depends_on myStorageAccount.Name
}

// Create an ARM template
let deployment = arm {
    location NorthEurope
    add_resource myStorageAccount
    add_resource myWebApp
}
```