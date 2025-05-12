---
title: "F# Script in a Container Group"
date: 2021-02-10
draft: false
---

#### Introduction

In this tutorial, you will deploy an F# script directly to an Azure Container Group. This is useful when you need to fill a gap in your solution with some quick application logic or to test scenarios on Azure before building a more complex application. We'll cover the following steps:

1. Create a brief F# script.
1. Create a container instance.
1. Include the script on a volume that will be attached to the container when started.

#### Create the F# Script

Scripts are often useful for quick automation or very simple application logic. Our goal here is to create a small HTTP service - this could be used for a health check service or to bootstrap a larger application, but we'll keep it very simple for illustrative purposes. Let's name it `main.fsx`.

```fsharp
#r "nuget: Suave, Version=2.6.0"
open Suave
let config = { defaultConfig with bindings = [ HttpBinding.createSimple HTTP "0.0.0.0" 8080 ] }
startWebServer config (Successful.OK "Hello Farmers!")
```

#### Create the Container Group

A container group consists of one or more containers that will run together. The container instances in the group can communicate with each other and share files over volume mounts.

Our script relies on the dotnet 5 SDK to run the script with the `dotnet fsi` command, so we will run it on the `dotnet/sdk:5.0` docker image which includes this SDK. Since our script creates a web listener on port 8080, we will add that as a public port and give it a DNS name so we can reach it.

```fsharp
open Farmer
open Farmer.Builders

let containers = containerGroup {
    name "my-app"
    add_instances [
        containerInstance {
            name "fsi"
            image "mcr.microsoft.com/dotnet/sdk:5.0"
            add_public_ports [ 8080us ]
        }
    ]
    public_dns "my-fsi-app" [ TCP, 8080us ]
}
```

#### Include the script in the deployment

The F# script will be embedded in the template as a `secret_string`. This creates a file in the container group that can be mounted into the file system on any of the container instances in the group. In our case, we will mount the F# script as a file named `main.fsx` in the container group and mount the directory containing that file as `/src`. With this in place we can also set the `dotnet fsi` command to run on container start, executing the script.

```fsharp
let containers = containerGroup {
    name "my-app"
    add_instances [
        containerInstance {
            name "fsi"
            image "mcr.microsoft.com/dotnet/sdk:5.0"
            add_public_ports [ 8080us ]
            // Add a volume mount with the script source.
            add_volume_mount "script-source" "/src"
            // Set the command line to run 'dotnet fsi /src/main.fsx' on startup
            command_line ("dotnet fsi /src/main.fsx".Split null |> List.ofArray)
        }
    ]
    public_dns "my-fsi-app" [ TCP, 8080us ]
    // Read our script source when building the ARM template and embed it into the template as a secret string volume mount to attach to the container group.
    add_volumes [
        volume_mount.secret_string "script-source" "main.fsx" (System.IO.File.ReadAllText "main.fsx")
    ]
}
```

The resulting template contains the contents of the F# script embedded as base64 so we have a standalone template that can be deployed to ARM.

```fsharp
arm {
    location Location.EastUS
    add_resources [
        containers
    ]
}
```

When the container group starts, it will execute the F# script, starting the service. This is very useful for gathering source and configuration from your local or internal environment and including it in a deployment. We are able to use this technique due to two unique features:

* Farmer is an "embedded" DSL - rather than simply a friendly version of the ARM template language, it brings the full feature set of .NET when building a template. This makes reading a script from the file system, converting it to base64, and embedding it in the template a simple process.
* ARM templates are not executed locally like Azure CLI scripts - they are a specification for ARM to deploy the infrastructure on your behalf. Once the F# script on your local machine is embedded into the template, ARM is able to pass it to your infrastructure securely over Azure's control plane.
