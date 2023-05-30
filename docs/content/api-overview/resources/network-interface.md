---
title: "Network Interface"
chapter: false
weight: 5
---

#### Overview
The `networkInterface` builder allows you to create network interfaces (NIC) so that Azure virtual machine (VM) can 
communicate with internet, Azure, and on-premises resources. To learn more about routeServer, reference to 
[Azure Docs](https://learn.microsoft.com/en-us/azure/virtual-network/virtual-network-network-interface?tabs=azure-portal)

* NetworkInterface (`Microsoft.Network/networkInterfaces`)

#### Builder Keywords

| Applies To | Keyword          | Purpose                                                                                    |
|-|------------------|--------------------------------------------------------------------------------------------|
| networkInterface | name             | Name of the network interface resource                                                     |
| networkInterface | subnet_prefix     | Sets the subnet prefix of the vnet for network interface                                   |
| networkInterface | link_to_vnet       | Link to existing vnet or to vnet managed by Farmer                                         |
| networkInterface | add_static_ip       | Use static ip for the network interface. If not provided, ip will be dynamically allocated |
| networkInterface | accelerated_networking_flag    | The accelerated networking flag for the network interface. Default is false  |
| networkInterface | ip_forwarding_flag    | The ip forwarding flag for the network interface. Default is false                         |

#### Example

```fsharp
#r "nuget:Farmer"
open Farmer
open Farmer.Builders
open Farmer.Builders.NetworkInterface

arm {
    location Location.EastUS

    add_resources
        [
            vnet {
                name "test-vnet"
                add_address_spaces [ "10.0.0.0/16" ]
            }
            networkInterface {
                name "my-network-interface"
                subnet_prefix "10.0.100.0/24"
                link_to_vnet (virtualNetworks.resourceId "test-vnet")
                add_static_ip "10.0.100.10"
                accelerated_networking_flag false
                ip_forwarding_flag false
            }
        ]
}
```