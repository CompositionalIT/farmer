---
title: "Custom Output with ARM Expressions"
date: 2021-04-23
draft: false
---

#### Introduction

Many resources have properties that are only set once the resource is created, such as a public IP's address or an ExpressRoute's circuit service key. It is often helpful to have these as output from the deployment so they are available to any downstream automation tasks.

In this tutorial, you will deploy an ExpressRoute circuit, create a reference to the `serviceKey` property on the newly deployed circuit, and provide that as the ARM deployment output.

#### Define the ExpressRoute circuit to deploy

An ExpressRoute circuit provides direct connectivity into Azure over a telecommunication provider's network rather than traversing the Internet or a VPN. Once the circuit is created, the typical flow is to take the circuit's service key to the telecommunications provider so they can enable it for your business connecivity.

```fsharp
open Farmer
open Farmer.Builders

let er = expressRoute {
    name "my-test-circuit"
    service_provider "Equinix"
    peering_location "Frankfurt"
}
```

#### Reference the `serviceKey` Property

ARM templates support expressions that are evaluated when the template is executed by ARM. These have many different capabilities, but in this case, we want to reference a newly deployed resource - the ExpressRoute circuit.

First, you can use the type and name of the resource to create a `ResourceId`. Then, that `ResourceId` can be used to build a `reference` expression and retrieve a property of the resource.

```fsharp
// Build an ARM resourceId type for the circuit.
let erId = ResourceId.create(Arm.Network.expressRouteCircuits, er.Name)

// Use that ID to build a reference expression and get a property of the referenced resource.
let serviceKeyRef = ArmExpression.create ($"reference({erId.ArmExpression.Value}).serviceKey")
```

#### Adding the Deployment Output

One or more outputs can be added to an `arm` computation expression to generate outputs from the deployment. An output is created using the name for the output an an `ArmExpression`, such as `serviceKeyRef` created above.

```fsharp
arm {
    location Location.WestEurope
    add_resource er
    output "er-service-key" serviceKeyRef
} |> Writer.quickWrite "custom-output"
```

This results in a template with an output named "er-service-key" that will contain the value of the `serviceKey` property on the newly deployed ExpressRoute circuit.
