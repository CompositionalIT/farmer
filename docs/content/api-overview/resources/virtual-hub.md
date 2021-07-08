---
title: "Virtual Hub"
date: 2021-07-07T08:53:46+01:00
chapter: false
weight: 21
---

#### Overview
The Virtual WAN builder (`vhub`) is used to create Azure Virtual Hub instances.

- Virtual Hub (`Microsoft.Network/virtualHubs`)

#### Builder Keywords

| Resource       | Keyword              | Purpose                                                                |
| -------------- | -------------------- | -----------------------------------------------------------------------|
| vhub           | name | Sets the name of the virtual hub |
| vhub           | sku | Sets the sku of the virtual hub |
| vhub           | address_prefix | Sets the address prefix of the virtual hub |
| vhub           | link_to_vwan | Sets the virtual wan to which the virtual hub belongs |
| vhub           | link_to_vwan | Sets the virtual wan deployed by farmer to which the virtual hub belongs |
| vhub           | link_to_unmanaged_vwan | Sets the existing virtual wan to which the virtual hub belongs |

### Example

```fsharp
open Farmer
open Farmer.Builders

let vhub = vhub {
    name "my-vhub"
    address_prefix (IPAddressCidr.parse "10.0.0.0/24")
}

let deployment = arm {
    location Location.NorthEurope
    add_resource vhub
}
```