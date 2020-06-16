---
title: "Adding A Resource"
date: 2020-06-15T03:57:42+02:00
draft: false
chapter: false
---

This set of guided exercises shows the different steps required to create new ARM resources in Farmer. We'll create a basic resource that can hook into the Farmer pipeline, by adding support to Farmer for the `ContainerRegistry` Azure resource. This will involve:

* Defining an type that implements `IArmResource` that maps directly to the ARM template output.
* Defining any domain types required to capture details on the resource.
* Defining a type that implements `IBuilder` and an associated *computation expression* that will be easier for users to consume than an F# record.

This will end up allowing us to define a resource that looks like this:

```fsharp
let myRegistry = containerRegistry {
    name "devonRegistry"
    sku ContainerRegistrySku.Basic
    enable_admin_user
}

let deployment = arm {
    location NorthEurope
    add_resource myRegistry
}
```

which generates JSON looking something like this:

```json
{
  "name": "devonRegistry",
  "type": "Microsoft.ContainerRegistry/registries",
  "apiVersion": "2019-05-01",
  "location": "northeurope",
  "tags": {},
  "sku": {
    "name": "Basic"
  },
  "properties": {
    "adminUserEnabled": true,
  },
  "resources": []
}
```

### Useful terminology
* **Resource**: A resource is a single Azure service provided by ARM; in Farmer these resource models are created by implementing the `IArmResource` interface.
* **Template**: Represents an ARM template with parameters, outputs and zero, one or many resources.
* **Location**: An Azure Region where a service exists.
* **Deployment**: Represents the deployment of an ARM template to a specific Location and Resource Group name.
* **Builder**: In Farmer, an `IBuilder` represents provides the capability of creating a smart type that helps model a resource *or a collection of resources* into associated `IArmResource` objects required for constructing the ARM template. For example, Farmer's WebApp builder provides a logical abstraction on top of several ARM resources: Web App, Server Farm and Application Insights.

### Requirements
* Minimum version is 2.5.0 of Azure CLI
* Azure account to test against