---
title: "Deploy an ASP.NET app"
date: 2020-10-24
draft: false
weight: 5
---

#### Introduction
This tutorial shows how to create the infrastructure required to host a ASP.NET web app, and how to automatically deploy that application with Farmer. We'll cover the following steps:

1. Creating and configuring a basic ASP.NET web application.
1. Creating a web app in Farmer.
1. Deploying the web app through Farmer.

{{< figure src="../../images/tutorials/webapp.png" caption="[Full code available here](https://github.com/CompositionalIT/farmer/blob/master/samples/scripts/tutorials/webapp.fsx)">}}

> Note: Your web application can be a C# web application - it does not need to be written in F#!

#### Creating the ASP.NET web application
Create a brand new ASP.NET web application:

1. Create a directory for your new application and enter it.
2. Using the dotnet SDK, create a new application: `dotnet new mvc`.
3. Notice that inside the project file (either `csproj` or `fsproj`), the Project SDK is already set to `Microsoft.NET.Sdk.Web`. This is more-or-less required for hosting in Azure.
4. Locally publish the application to a directory called `deploy`: `dotnet publish -c Release -o deploy`.

> dotnet publish puts all built files and outputs into a single folder, and adds a `web.config` as required for e.g. Azure, as long as your Project SDK is set correctly.

#### Create the Web App
Create a new Farmer application which contains a web app.

```fsharp
open Farmer
open Farmer.Builders

let webapplication = webApp {
    name "<web app name goes here>"
}
```

#### Configure Web App to deploy your ASP.NET application
```fsharp
let webapplication = webApp {
    ...
    zip_deploy @"<path_to_your_deploy_folder>"
}
```

#### That's it!
Deploy the web app by adding it to an ARM builder and deploy it to a resource group of your choosing. During the deployment process, you will notice the following:

`Running ZIP deploy for <path_to_your_deploy_folder>`

> Farmer will automatically zip up the contents of the folder for you.
