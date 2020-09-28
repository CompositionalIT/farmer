---
title: "Bastion HOst"
date: 2020-08-26T17:55:00-04:00
chapter: false
weight: 26
---

#### Overview
The Bastion Host builder creates a bastion host to access resources inside a virtual network. It also creates a static public IP for the bastion host.

* BastionHosts (`Microsoft.Network/bastionHosts`)

#### Builder Keywords

| Applies To | Keyword | Purpose |
|-|-|-|
| BastionHost | vnet | Name of the virtual network the bastion host can access |

#### Example

```fsharp
#r @"./libs/Newtonsoft.Json.dll"
#r @"../../src/Farmer/bin/Debug/netstandard2.0/Farmer.dll"

open Farmer
open Farmer.Builders

arm {
    location Location.EastUS
    add_resources [
        vnet {
            name "private-network"
            add_address_spaces [
                "10.1.0.0/16"
            ]
            add_subnets [
                subnet {
                    name "default"
                    prefix "10.1.0.0/24"
                }
                subnet {
                    name "AzureBastionSubnet"
                    prefix "10.1.250.0/27"
                }
            ]
        }
        bastion {
            name "my-bastion-host"
            vnet "private-network"
        }
    ]
}
```
