---
title: "Virtual Network Gateway"
date: 2020-06-19T10:40:00-04:00
weight: 5
chapter: false
---

#### Overview
The Virtual Network Gateway builder creates virtual network gateways for ExpressRoute or VPN connections to a virtual network.

* Virtual Network Gateways (`Microsoft.Network/virtualNetworkGatways`)

#### Builder Keywords

| Keyword | Purpose |
|-|-|
| name | Specifies the name of the virtual network gateway |
| vnet | The name of the virtual network to which the gateway connects |
| er_gateway_sku | SKU for an ExpressRoute gateway |
| vpn_gateway_sku | SKU for a VPN gateway |
| vpn_type | Sets the VPN type to route-based (default) or policy-based. |
| gateway_ip_config | Specifies the gateway public and private IP addresses |
| active_active_ip_config | Specifies the second public and private IP configuration for a redundant gateway |
| disable_bgp | BGP is enabled by default, but this can disable it |

#### Example

```fsharp
open Farmer
open Farmer.Builders
open Farmer.VirtualNetworkGateway

let gw = gateway {
    name "er-gateway"
    vnet "my-vnet" // Must contain a subnet named 'GatewaySubnet'
    er_gateway_sku ErGatewaySku.Standard
    gateway_ip_config DynamicPrivateIp "gw-pip"
}

let privateNet = vnet {
    name "my-vnet"
    add_address_spaces [
        "10.30.0.0/16"
    ]
    add_subnets [
        subnet {
            name "GatewaySubnet"
            prefix "10.30.254.0/28"
        }
    ]
}
```
