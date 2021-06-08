---
title: "Virtual WAN"
date: 2021-05-03T11:22:17-05:00
chapter: false
weight: 21
---

#### Overview

The Virtual WAN builder (`vwan`) is used to create Azure Virtual WAN instances.

- Virtual WAN (`Microsoft.Network/virtualWans`)

#### Builder Keywords

| Resource       | Keyword              | Purpose                                                                |
| -------------- | -------------------- | -----------------------------------------------------------------------|
| vwan           | name | Sets the name of the virtual wan |
| vwan           | standard_vwan | Sets the virtual wan type to "standard" instead of the default "basic" |
| vwan           | allow_branch_to_branch_traffic | Specifies branch to branch traffic is allowed |
| vwan           | disable_vpn_encryption | Specifies Vpn encryption is disabled |
| vwan           | office_365_local_breakout_category | Sets the office local breakout category |


### Example

```fsharp
open Farmer
open Farmer.Builders

let myVwan = vwan {
    name "my-vwan"
    disable_vpn_encryption
    allow_branch_to_branch_traffic
    office_365_local_breakout_category Office365LocalBreakoutCategory.None
    standard_vwan
}
let deployment = arm {
    location Location.NorthEurope
    add_resource myVwan
}
```