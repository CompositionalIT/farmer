---
title: "Your first Farmer template"
date: 2020-02-04T00:41:51+01:00
draft: false
weight: 1
---

#### Introduction
In this exercise, you'll:
* create a web application with a fully-configured Application Insights instance
* create an ARM deployment object and assign the web app to it
* generate an ARM template

#### Creating a Farmer app
Create an F# console application using the .NET SDK and add the Farmer package in an empty directory:

```cmd
dotnet new console -lang F#
dotnet add package Farmer
```

> Farmer also has a [.NET template](../template/) to get started even more quickly!

#### Defining a Farmer web application
Open `Program.fs` and delete all the contents.

> In Farmer, resources are defined using special code blocks that look somewhat json-esque, known as a "builder". In these builders you can quickly and easily configure a resource using special keywords, but unlike json you also have edit-time safety.

Create a Farmer web application using the `webApp { }` builder:

```fsharp
open Farmer
open Farmer.Builders

let myWebApp = webApp {
    name "yourFirstFarmerApp"
}
```

Create an ARM template deployment object, before setting the location for the overall resource group and adding the web app into it.

```fsharp
let deployment = arm {
    location Location.NorthEurope
    add_resource myWebApp
}
```

#### Generating the ARM template
Now you need to generate the ARM template from the deployment object to an ARM json file.

Add the following code:

```fsharp
deployment
|> Writer.quickWrite "myFirstTemplate"
```

Run the application:

```cmd
dotnet run
```

You should notice that the file `myFirstTemplate.json` has been created.

The generated ARM template contains the following resources:

* A web application
* A server farm
* An application insights instance

The resources will be correctly configured with the appropriate dependencies set.

#### The full application

```fsharp
open Farmer
open Farmer.Builders

let myWebApp = webApp {
    name "yourFirstFarmerApp"
}

let deployment = arm {
    location Location.NorthEurope
    add_resource myWebApp
}

deployment
|> Writer.quickWrite "myFirstTemplate"
```
