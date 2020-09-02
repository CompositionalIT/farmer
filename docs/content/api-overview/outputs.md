---
title: "Outputs"
date: 2020-08-22T09:13:36+01:00
draft: false
weight: 3
---
ARM templates also support the notion of *[outputs](https://docs.microsoft.com/en-us/azure/azure-resource-manager/templates/template-outputs)*. Outputs can be used to provide your Farmer applications with values which were generated during the deployment process, to be used further downstream.

For example, you may wish to prime an Azure storage account with data post-creation. In this case, one way is to return back out the connection string of the storage account and use that to connect and upload your data.

#### Creating and Consuming outputs
Outputs are applied onto the `arm { }` builder using the [output keyword](../resources/arm/#builder-keywords).

```fsharp
let myStorage = storageAccount {
    name "sampleaccount"
}

let template = arm {
    add_resource myStorage
    output "storage_key" myStorage.Key
}

let outputs = template |> Deploy.execute "my-resource-group" []

let connectionString = outputs.["storage_key"]
```

Outputs are returned back from the deployment as a simple `Map<string, string>`.

Any [ARM expression](../expressions) can be returned as an output, and you can create as many outputs as you wish.
