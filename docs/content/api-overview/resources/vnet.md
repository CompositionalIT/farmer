---
title: "Virtual Network"
date: 2021-01-09T11:22:17-05:00
chapter: false
weight: 25
---

#### Overview

The Virtual Network builder (`vnet`) is used to create Azure Virtual Network instances.

- Virtual Network (`Microsoft.Network/virtualNetworks`)
- Subnets (`Microsoft.Network/virtualNetworks/subnets`)

#### Builder Keywords

| Resource       | Keyword              | Purpose                                                            |
| -------------- | -------------------- | ------------------------------------------------------------------ |
| vnet           | name                 | Sets the name of the virtual network.                              |
| vnet           | add_address_spaces   | Adds address spaces to the virtual network.                        |
| vnet           | add_subnets          | Adds subnets to the virtual network.                               |
| vnet           | build_address_spaces | Automatically builds address spaces for subnet names and sizes.    |
| vnet           | add_tags             | Adds a set of tags to the resource                                 |
| vnet           | add_tag              | Adds a tag to the resource                                         |
| subnet         | name                 | Name of the subnet resource                                        |
| subnet         | prefix               | Subnet prefix in CIDR notation (e.g. 192.168.100.0/24)             |
| subnet         | add_delegations      | Adds one or more delegations to this subnet.                       |
| addressSpace   | space                | When using `build_address_space` this specifies the address space. |
| addressSpace   | subnets              | Specifies the subnets to build automatically.                      |
| addressSpace   | build_subnet         | Specifies the name, size, and service delegations for the subnet.  |

#### Example - Manual Subnets

A virtual network is defined with the `vnet` builder. Address spaces and 
subnets should be added, taking care to ensure the subnets are contained
within an address space on the virtual network.

```fsharp
open Farmer
open Farmer.Builders

let myVnet = vnet {
    name "my-vnet"
    add_address_spaces [ "192.168.200.0/22" ]
    add_subnets [
        subnet {
            name "vms"
            prefix "192.168.200.0/24"
        }
        subnet {
            name "containers"
            prefix "192.168.201.0/24"
            add_delegations [
                SubnetDelegationService.ContainerGroups
            ]
        }
        subnet {
            name "databases"
            prefix "192.168.202.0/24"
            add_delegations [
                SubnetDelegationService.SqlManagedInstances
            ]
        }
    ]
}

let deployment = arm {
    location Location.NorthEurope
    add_resource myVnet
}
```

#### Example - Automatically Build Subnets

Address spaces and subnets can be built out automatically to ensure the subnets
are contained within the address spaces. This reduces the need for "IP math"
to determine the start addresses for contiguous networks of different sizes.

```fsharp
open Farmer
open Farmer.Builders

let myVnet = vnet {
    name "my-vnet"
    build_address_spaces [
        addressSpace {
            space "10.28.0.0/16"
            subnets [
                buildSubnet "vms" 26
                buildSubnet "services" 24
                buildSubnet "corporate-west" 18
                buildSubnet "corporate-east" 18
                buildSubnet "GatewaySubnet" 28
                buildSubnetDelegations "containers" 27 [ SubnetDelegationService.ContainerGroups ]
            ]
        }
        addressSpace {
            space "10.30.0.0/16"
            subnets [
                buildSubnet "remote-office" 23
                buildSubnet "reserved" 24
                buildSubnet "GatewaySubnet" 28
            ]
        }
    ]
}

let deployment = arm {
    location Location.NorthEurope
    add_resource myVnet
}
```
