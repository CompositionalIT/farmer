---
title: "ARM Expressions"
date: 2020-02-05T09:13:36+01:00
draft: false
weight: 2
---
[ARM template expressions](https://docs.microsoft.com/en-us/azure/azure-resource-manager/templates/template-expressions) are a way of safely storing string values which contain expressions that are evaluated at *deployment time* by the Azure. ARM expressions can also contain a set of predefined [functions](https://docs.microsoft.com/en-us/azure/azure-resource-manager/templates/template-functions) supported by the ARM runtime. They can be passed back as outputs and used further downstream

Farmer understands how to use ARM expressions and provides functionality to correctly wrap and unwrap them as raw strings into a JSON template.

> For manipulation of literal values that are known in your Farmer applications, you will not need to use ARM expressions. To manipulate such values, you can use standard F# and .NET capabilities.

#### How do I use ARM expressions?
Many Farmer builders contain pre-defined ARM expression that can be used for common tasks, such as passing a connection string from a storage account as a KeyVault secret, or a web application setting.

As an example, a Storage Account config contains a `Key` member that you can supply this to a web app as a setting:

```fsharp
let storageConfig = storageAccount {
    name "myStorageAccount"
}

let webAppConfig = webApp {
    name "myWebApp"
    setting "storageKey" storageConfig.Key
}
```

This will be written to the ARM template file as follows:

```json
          "appSettings": [
            {
              "name": "storageKey",
              "value": "[concat('DefaultEndpointsProtocol=https;AccountName=myStorageAccount;AccountKey=', listKeys('myStorageAccount', '2017-10-01').keys[0].value)]"
            }
```

Using ARM expressions means that you can deploy an application which is automatically configured at deployment time. This means that you never need to store an application secret such as a storage account key in source control, or even e.g. as a secret variable in your build / deployment process.

#### Returning the value of ARM Expressions as outputs.
ARM Expressions can also be passed back as *outputs* and used further downstream once your deployment is complete:

```fsharp
let template = arm {
    location Location.WestEurope
    add_resource storageConfig

    // Mark the storage_key as an output in the ARM template.
    output "storage_key" storageConfig.Key
}

// Deploy the template.
let outputs =  template |> Deploy.execute template []

// Get the value of the storage_key.
let key = outputs.["storage_key"]
```

Be aware though, that the value of the storage_key is visible as a plain text value in the output - so anyone with access to, for example, the Azure portal will be able to see the values of the storage key if they look at the deployment history.