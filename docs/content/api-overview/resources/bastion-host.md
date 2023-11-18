---
title: "Bastion Host"
date: 2020-08-26T17:55:00-04:00
chapter: false
weight: 2
---

#### Overview
The Bastion Host builder creates a bastion host to access resources inside a virtual network. It also creates a static public IP for the bastion host.

* BastionHosts (`Microsoft.Network/bastionHosts`)

#### Builder Keywords

| Applies To | Keyword               | Purpose                                                                                                              |
|------------|-----------------------|----------------------------------------------------------------------------------------------------------------------|
| `bastion`  | vnet                  | Name of the virtual network the bastion host can access.                                                             |
| `bastion`  | link_to_vnet          | Link to an existing virtual network (no dependsOn emitted.                                                           |
| `bastion`  | scale_units           | Number of scale units when more connections are needed. Default is 2 and more scale units will use the Standard SKU. |
| `bastion`  | disable_copy_paste    | Disables copy and paste to and from the bastion - enabling this upgrades to the Standard SKU.                        |
| `bastion`  | dns_name              | Set the DNS name for accessing the bastion host.                                                                     |
| `bastion`  | enable_file_copy      | Upload and download files to the target VM.                                                                          |
| `bastion`  | enable_ip_connect     | Connect to virtual machines by IP address instead of using their target resource Id.                                 |
| `bastion`  | enable_kerberos       | Enable kerberos authentication support for supporting scenarios such as Windows Single Sign On.                      |
| `bastion`  | enable_shareable_link | lets users connect to a target resource using Azure Bastion without accessing the Azure portal.                      |
| `bastion`  | enable_tunneling      | Set up tunnels through the bastion host so native client tools can be used.                                          |

#### Example

```fsharp
#r "nuget:Farmer"

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
            enable_shareable_link true
            enable_tunneling true
            scale_units 2
        }
    ]
}
```
