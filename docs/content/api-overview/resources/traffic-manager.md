---
title: "Traffic Manager"
date: 2021-05-235T13:01:00+01:00
chapter: false
weight: 20
---

#### Overview
The Traffic Manager builder creates traffic manager profiles and their associated endpoints.

* Traffic Manager Profiles (`Microsoft.Network/trafficManagerProfiles`)

#### Builder Keywords

#### Configuration Members

#### Example

```fsharp
open Farmer
open Farmer.Builders
open Farmer.ServiceBus

let myTrafficManager = trafficManager {
    name "my-trafficmanager"
}

let deployment = arm {
    location Location.NorthEurope
    add_resource myTrafficManager
}
```
