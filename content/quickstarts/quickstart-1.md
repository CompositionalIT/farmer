---
title: "Your First Template"
date: 2020-02-04T00:41:51+01:00
draft: false
weight: 1
---

#### Introduction
In this exercise, you'll:
* create a web application with a fully-configured Application Insights instance
* create an ARM deployment object and assign the web app to it
* generate an ARM template

#### Creating a web app
Create an F# console application using the .NET SDK: 

```cmd
dotnet new console -lang F# -n FarmerSample
```

Add a reference to the Farmer nuget package, modifying the `FarmerSample.fsproj` as follows and build the project to download the dependency.

```xml
<PackageReference Include="Farmer" Version="0.3.0"/>
```

#### Defining a Farmer web application
Open `Program.fs` and delete all the contents.

> In Farmer, resources are created using special code blocks in which you can quickly and easily configure a resource using special keywords.

Create a Farmer web application using the `webApp { }` block:

```fsharp
open Farmer
open Farmer.Resources

let myWebApp = webApp {
    name "yourFirstFarmerApp"
}
```

> You should pick something unique for the `name`. It must be **unique across Azure** i.e. someone else can't have another web app with the same name!

Create an ARM template deployment object, before setting the location for the overall resource group and adding the web app into it.
```fsharp
let deployment = arm {
    location Locations.NorthEurope
    add_resource myWebApp
}
```

#### Generating the ARM template 
Now you need to generate the ARM template from the deployment object to an ARM json file.

Add the following code:

```fsharp
let filename =
    Writer.toJson deployment.Template
    |> Writer.toFile "myFirstTemplate"
```

Run the application; you should notice that the file `myFirstTemplate.json` has been created.

The generated ARM template contains the following resources:

* A web application
* A server farm
* An application insights instance

The resources will be correctly configured with the appropriate dependencies set.

#### The full application

```fsharp
open Farmer
open Farmer.Resources

let myWebApp = webApp {
    name "yourFirstFarmerApp"
}

let deployment = arm {
    location Locations.NorthEurope
    add_resource myWebApp
}

let filename =
    Writer.toJson deployment.Template
    |> Writer.toFile "myFirstTemplate"
```

<!-- 1. Uncomment the last two lines in the application and run it again to deploy the template (see [here](#deploying-to-azure) if you want to learn more about this **and what prerequisites are required**).
1. Once it has deployed, find it in the Azure portal. You will see that *three* resources were created: the **app service**, the **app service plan** that the app service resides in and a linked **application insights** instance. -->