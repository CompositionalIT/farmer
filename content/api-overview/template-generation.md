---
title: "Generating templates"
date: 2020-02-05T09:13:36+01:00
draft: false
weight: 1
---

Farmer supports several ways to "output" ARM templates.

#### Generating JSON as a string
You can generate an ARM template as a plain string:

```fsharp
let json =
    deployment.Template
    |> Writer.toJson

 // prints out the JSON
 printfn "%s" json
```

#### Writing to a file
You can write out the ARM template directly to a file, from which you can then deploy to Azure using whichever mechanism you already use e.g. Azure CLI, Powershell, REST API etc.

```fsharp
deployment
|> Writer.quickWrite "myTemplate"
```

Notice how we use F#'s pipe operator to "pipe" data from the template configuration into json before writing to a file.

#### Integrated deployment to Azure
You can also turn over deployment of the template directly to Farmer. In this case, it orchestrates commands to the Azure CLI as required.

```fsharp
let response =
    deployment
    |> Deploy.tryExecute "myResourceGroup" Deploy.NoParameters

match response with
| Ok outputs -> printfn "Success! Outputs: %A" outputs
| Error error -> printfn "Failed! %s" error
```

As you can see, the response of calling `tryExecute` is a `Result` object which is either `Ok`, in which case any outputs returned from the template are made available as a `Map<string, string>`, or an `Error`, which is the error returned by the Azure CLI. Alternatively, you can call `execute` which will throw an exception rather than return a Result.

> You must have the Azure CLI installed on your machine in order for Farmer to perform deployments for you.

#### Authenticating to Azure
Azure CLI stores a login token on your machine, and Farmer will check for this. If you aren't logged in, Farmer will automatically start the interactive Azure CLI login process for you.

For automated deployments e.g. continuous deployment or through scripts etc., you'll want to use an unattended deployment mode. Some CI systems such as Azure Devops come with an pre-authenticated Azure CLI terminal from which you can run an application that uses Farmer. Alternatively, you can create a [service principal](../../deployment-guidance#how-do-i-create-a-service-principal), and supply them to the `Deploy.authenticate` function before calling `Deploy.execute`.

You should use a secure mechanism for storing and supplying the credentials to Farmer. **Do not commit them into source control!**