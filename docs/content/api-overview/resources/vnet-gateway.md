---
title: "Virtual Network Gateway"
date: 2020-02-05T08:53:46+01:00
weight: 19
chapter: false
---

#### Overview
The Virtual Network Gateway builder creates virtual network gateways for ExpressRoute or VPN connections to a virtual network.

* Virtual Network Gateways (`Microsoft.Network/virtualNetworkGatways`)

#### Builder Keywords

| Keyword | Purpose |
|-|-|
| name | Specifies the name of the virtual network gateway |
| gateway_type | Sets the type of gateway to create (ExpressRoute or VPN) and its SKU |
| gateway_ip_config | Specifies the gateway public and private IP addresses |
| vnet | The name of the virtual network to which the gateway connects |
| active_active_ip_config | Specifies the second public and private IP configuration for an redundant gateway |
| disable_bgp | BGP is enabled by default, but this can disable it |

#### Example

```fsharp
open Farmer
open Farmer.Builders
open Farmer.VirtualNetworkGateway

let gw = gateway {
    name "er-gateway"
    gateway_type (GatewayType.ExpressRoute ErGatewaySku.Standard)
    vnet "my-vnet"
    gateway_ip_config DynamicPrivateIp "gw-pip"
}
```
