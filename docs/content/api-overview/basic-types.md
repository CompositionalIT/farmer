---
title: "Basic Types"
date: 2022-05-25T13:32:12+01:00
draft: false
weight: 3
---
Farmer often uses references to link resouces, when this is not possible, Farmer uses certain types to define links.

#### ResourceName

A ResourceName represents a name of an ARM resource.


```fsharp
ResourceName "myapp"
```

#### ResourceId

A ResourceId identifies an ARM resource.

This is used when you want to link unmanaged resources to a deployment or have multiple deployment parts.

A ResourceId can be created with the resourceId member of a ResourceType: 

```fsharp
let subnet = Arm.Network.virtualNetworks.resourceId(ResourceName "")
```

or directly as a ResourceId:

```fsharp
let mySubnet = {
    Type = Arm.Network.subnets
    Name = ResourceName "my-vnet"
    ResourceGroup = Some("myResourceGroup")
    Subscription = None
    Segments = [ResourceName "mySubnet"]
}
```

These resources can be used i.e. in referencing a `web app { }` to a subnet.

```fsharp
let webApp = webApp {
    name "myWebApp"
    service_plan_name "myServicePlan"
    sku WebApp.Sku.B1
    link_to_unmanaged_vnet mySubnet
}
```
