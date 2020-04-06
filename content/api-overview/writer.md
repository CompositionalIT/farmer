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
You can also write out the JSON directly to a file:

```fsharp
deployment
|> Writer.quickWrite "myTemplate"
```

> Notice how we use F#'s pipe operator to "pipe" data from the template configuration into json before writing to a file.

#### Generating a deployment batch file
You can create a batch file that will deploy the template to Azure when run. It uses an interactive login prompt via the Azure CLI.

```fsharp
// farmer-deploy.bat
let filename =
    deployment
    |> Deploy.AzureCli.generateDeployScript "myResourceGroup"
```

> This assumes that you have the Azure CLI installed on your machine.

#### Interactive deploy to Azure
You can also create the template itself and the associated batch file, *and* the launch it as a single command.

```fsharp
deployment
|> Deploy.quick "myResourceGroup"
```

#### Non-interactive deploy to Azure
For automated deployments e.g. continuous deployment or through scripts etc., you'll want to use an unattended deployment mode. You can opt to generate the ARM template and then use another existing mechanism for deploying ARM templates e.g. Powershell, or you can use Farmer's built-in Deployment API, which wraps around the Azure ARM REST API.

```fsharp
let credentials =
    { ClientId = // set app id here...
      ClientSecret = // set secret here...
      TenantId = } // set tenant id here...
let subscriptionId = // set subscription id here...
let parameters = [] // list of string * string parameters

/// Deploy to ARM, and get the response.
let response =
    deployment
    |> fullDeploy credentials subscriptionId "myResourceGroup" parameters

match response.Result with
| Ok outputs -> printfn "Success! Outputs: %A" outputs
| Error (DeploymentRejected error) -> printfn "Rejected! %A" error
| Error (DeploymentFailed error) -> printfn "Failed! %A" error
```

As you can see, the response of calling `fullDeploy` is a `Result` object which is either `Ok`, in which case any outputs returned from the template are made available, or an `Error`, which is either one of several `DeploymentRejected` or `DeploymentFailed` errors.