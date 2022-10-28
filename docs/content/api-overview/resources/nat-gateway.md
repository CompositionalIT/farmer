---
title: "NAT Gateway"
date: 2022-08-02T22:26:00-04:00
chapter: false
weight: 5
---

#### Overview
The `natGateway` builder creates a NAT Gateway to efficiently manage the SNAT traffic used by resources
in a virtual network. By default, it creates a single static public IP for the NAT Gateway, but more IP
addresses or prefixes of groups of addresses can be specified.

* NatGateway (`Microsoft.Network/natGateways`)

#### Builder Keywords

| Applies To | Keyword | Purpose |
|-|-|-|
| natGateway | name | Name of the NAT Gateway resource |
| natGateway | idle_timeout | Timeout after which connections that have seen no traffic will be disconnected to free SNAT ports. |

#### Example

```fsharp
#r "nuget:Farmer"

open Farmer
open Farmer.Builders

arm {
    location Location.EastUS
    add_resources [
        natGateway {
            name "my-nat-gateway"
        }
        vnet {
            name "my-net"
            add_address_spaces [ "10.100.0.0/16" ]
            add_subnets [
                subnet {
                    name "my-services"
                    prefix "10.100.12.0/24"
                    nat_gateway (Farmer.Arm.Network.natGateways.resourceId "my-nat-gateway")
                }
            ]
        }
    ]
}
```
