---
title: "Generating ARM templates"
date: 2020-02-05T09:13:36+01:00
draft: false
weight: 2
---

Farmer supports several ways to "output" ARM templates.

#### Generating JSON as string
You can generate an ARM template as a plain string:

```fsharp
let json = deployment.Template |> Writer.toJson
printfn "%s" json // prints out the JSON
```

#### Writing to a file
You can also write out the JSON directly to a file:

```fsharp
let filename = // myTemplate.json
    deployment.Template
    |> Writer.toJson
    |> Writer.toFile "myTemplate"
```

> Notice how we use F#'s pipe operator to "pipe" data from the template configuration into json before writing to a file.

#### Generating a deployment batch file
You can create a batch file that will deploy the template to Azure when run. It uses an interactive
login prompt via the Azure CLI.

```fsharp
let filename = // farmer-deploy.bat
    deployment
    |> Writer.generateDeployScript "myResourceGroup"
```

> This assumes that you have the Azure CLI installed on your machine.

#### Deploying directly to Azure
You can also create the template itself and the associated batch file, *and* the launch it as a single command.

```fsharp
deployment
|> Writer.quickDeploy "myResourceGroup"
```
