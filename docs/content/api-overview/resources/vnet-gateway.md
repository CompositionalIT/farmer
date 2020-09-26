---
title: "Virtual Network Gateway"
date: 2020-06-19T10:40:00-04:00
chapter: false
weight: 25
---

#### Overview
The Virtual Network Gateway builder creates virtual network gateways for ExpressRoute or VPN connections to a virtual network.

* Virtual Network Gateways (`Microsoft.Network/virtualNetworkGatways`)
* Connections (`Microsoft.Network/connections`)

#### Builder Keywords

| Applies To | Keyword | Purpose |
|-|-|-|
| Gateway | name | Specifies the name of the virtual network gateway |
| Gateway | vnet | The name of the virtual network to which the gateway connects |
| Gateway | er_gateway_sku | SKU for an ExpressRoute gateway |
| Gateway | vpn_gateway_sku | SKU for a VPN gateway |
| Gateway | vpn_type | Sets the VPN type to route-based (default) or policy-based. |
| Gateway | gateway_ip_config | Specifies the gateway public and private IP addresses |
| Gateway | active_active_ip_config | Specifies the second public and private IP configuration for a redundant gateway |
| Gateway | disable_bgp | BGP is enabled by default, but this can disable it |
| Gateway | vpn_client | Specifies the VPN client configuration using the vpnclient builder (optional) |
| VPNClient | add_address_pool | The reference of the address space resource which represents Address space for P2S VpnClient |
| VPNClient | add_root_certificate | Adds the name and the public data of a root certificate to validate client certificates used for VPN Client connexion. This can be either just the data of the base64 content of the certificate or a multiline string starting with -----BEGIN CERTIFICATE----- and ending with -----END CERTIFICATE----- |
| VPNClient | add_revoked_certificate | Adds the name and the thumbprint of a revoked client certificate |
| VPNClient | protocols | Sets the protocols for the VPN client. SSTP (default), IkeV2 or OpenVPN |
| Connection | name | Specifies the name of the connection |
| Connection | vnet_gateway1 | Name of the first vnet gateway this is connecting |
| Connection | vnet_gateway2 | Name of the second vnet gateway this is connecting, for use when connecting two vnets |
| Connection | local_gateway | Name of the local gateway connection for a VPN |
| Connection | peer_id | Id of the peer, typically an ExpressRoute circuit Id |
| Connection | auth_key | Authorization key used when peering across subscriptions |

#### Example

```fsharp
open Farmer
open Farmer.Builders
open Farmer.VirtualNetworkGateway

let gw = gateway {
    name "er-gateway"
    vnet "my-vnet" // Must contain a subnet named 'GatewaySubnet'
    er_gateway_sku ErGatewaySku.Standard

     vpn_client 
        (vpnclient {
               add_address_pool "10.31.0.0/16"
               add_root_certificate "rootcert" "" })
}
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
