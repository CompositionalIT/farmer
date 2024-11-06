---
title: "Azure Firewall"
date: 2021-07-07T11:22:17-05:00
chapter: false
weight: 1
---

#### Overview

The Azure Firewall builder (`azureFirewall`) is used to create Azure Firewall instances.

- Azure Firewall (`Microsoft.Network/azureFirewalls`)

#### Builder Keywords

| Resource       | Keyword              | Purpose                                                                |
| -------------- | -------------------- | -----------------------------------------------------------------------|
| azureFirewall           | name | Sets the name of the azure firewall |
| azureFirewall           | sku | Sets the name and tier of the Azure firewall sku |
| azureFirewall           | link_to_unmanaged_firewall_policy | Configure the azure firewall to use an existing firewall policy |
| azureFirewall           | link_to_firewall_policy | Configure the Azure firewall to use a firewall policy deployed by Farmer |
| azureFirewall           | link_to_unmanaged_vhub | Specify the existing virtual hub to which the azure firewall belongs |
| azureFirewall           | link_to_vhub | Specify the virtual hub deployed by Farmer to which the azure firewall belongs |
| azureFirewall           | public_ip_reservation_count | Specify the number of Public IP addresses associated with the azure firewall |
| azureFirewall           | depends_on | Specify resources deployed by Farmer the azure firewall depends on |

### Example

```fsharp
open Farmer
open Farmer.Builders

let firewall = azureFirewall {
    name "farmer_firewall"
    sku SkuName.AZFW_Hub SkuTier.Standard
    public_ip_reservation_count 2
    link_to_unmanaged_vhub (virtualHubs.resourceId "unmanaged-vhub")
}

let deployment = arm {
    location Location.NorthEurope
    add_resource firewall
}
```
