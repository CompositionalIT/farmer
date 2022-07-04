---
title: "Private Link Service"
date: 2022-06-16T17:40:05-04:00
chapter: false
weight: 12
---

#### Overview
The Private Link Service builder (`privateLink`) creates a private link service to access resources behind a load balancer privately from a private endpoint instead of traversing the internet.

* Private Link Services (`Microsoft.Network/privateLinkServices`)

#### Builder Keywords

| Applies To | Keyword | Purpose |
|-|-|-|
| privateLink | name | Specifies the name of the private link service |
| privateLink | depends_on | Specify any dependencies that must be deployed before this. |
| privateLink | add_auto_approved_subscriptions | To auto-approve private endpoints, the subscription for those must be added here. |
| privateLink | add_visible_to_subscriptions | To allow subscription to request access for their private endpoints, they must be added here. |
| privateLink | add_load_balancer_frontend_ids | Adds the resource ID for the load balancer frontend IP configurations that are accessible through this private link service. |
| privateLink | add_ip_configs | Adds the subnet where the private endpoints will connect to this service. |
| privateLinkIpConfig | name | Optionally name the IP config |
| privateLinkIpConfig | private_ip_allocation | Specifies static or dynamic allocation within the subnet - defaults to Dyanmic |
| privateLinkIpConfig | private_ip_address_version | Specifies IPv4 or IPv6 - defaults to IPv4. |
| privateLinkIpConfig | primary | Specify this is the primary IP config if connecting to multiple subnets - defaults to false |
| privateLinkIpConfig | link_to_subnet | Required - the resource ID of the subnet where private link endpoints will be connected to this service. |

#### Example

```fsharp
open Farmer
open Farmer.Builders

let vnet = vnet {
    name "private-net"
    add_address_spaces [
        "10.100.0.0/16"
    ]
    add_subnets [
        subnet {
            name "default"
            prefix "10.100.0.0/24"
        }
        subnet {
            name "backend-services"
            prefix "10.100.1.0/24"
        }
        subnet {
            name "private-endpoints"
            prefix "10.100.255.0/24"
            private_link_service_network_policies Disabled
        }
    ]
}
let lb =
    loadBalancer {
        name "lb"
        sku LoadBalancer.Sku.Standard
        depends_on vnet
        add_frontends [
            frontend {
                name "lb-frontend"
                private_ip_allocation_method AllocationMethod.DynamicPrivateIp
                link_to_subnet (ResourceId.create(Farmer.Arm.Network.subnets, vnet.Name, ResourceName "default"))
            }
        ]
        add_backend_pools [
            backendAddressPool {
                name "lb-backend"
            }
        ]
        add_probes [
            loadBalancerProbe {
                name "httpGet"
                protocol Farmer.LoadBalancer.LoadBalancerProbeProtocol.HTTP
                port 80
                request_path "/"
            }
        ]
        add_rules [
            loadBalancingRule {
                name "rule1"
                frontend_ip_config "lb-frontend"
                backend_address_pool "lb-backend"
                frontend_port 80
                backend_port 80
                protocol TransmissionProtocol.TCP
                probe "httpGet"
            }
        ]
    }
    privateLink {
        name "pls"
        depends_on lb
        add_auto_approved_subscriptions [ System.Guid.NewGuid() ]
        add_load_balancer_frontend_ids [
            ResourceId.create(Farmer.Arm.LoadBalancer.loadBalancerFrontendIPConfigurations, lb.Name, ResourceName "lb-frontend")
        ]
        add_ip_configs [
            privateLinkIpConfig {
                link_to_subnet (ResourceId.create(Farmer.Arm.Network.subnets, vnet.Name, ResourceName "private-endpoints"))
            }
        ]
    }
arm {
    add_resources [
        vnet
        lb
        pls
    ]
}
```
