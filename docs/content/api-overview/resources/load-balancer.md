---
title: "Load Balancer"
date: 2021-05-20T13:04:00-04:00
chapter: false
weight: 12
---

#### Overview
The Load Balancer builder (`loadBalancer`) creates load balancers that can distribute load amongst healthy services in a backend pool on public or private networks.

* Load balancers (`Microsoft.Network/loadBalancers`)
* Load balancer frontend IP configurations (`Microsoft.Network/loadBalancers/frontendIPConfigurations`)
* Load balancer backend address pools (`Microsoft.Network/loadBalancers/backendAddressPools`)
* Load balancer health probes (`Microsoft.Network/loadBalancers/probes`)

#### Builder Keywords

| Applies To | Keyword                      | Purpose                                                                                                                                                                                   |
|-|------------------------------|-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| loadBalancer | name                         | Specifies the name of the load balancer                                                                                                                                                   |
| loadBalancer | sku                          | Specifies the SKU of the load balancer - default is 'Basic'.                                                                                                                              |
| loadBalancer | tier                         | Specifies the tier of the load balancer - default is 'Regional'.                                                                                                                          |
| loadBalancer | add_frontends                | Adds frontend IP configurations as defined by the `frontend` builder.                                                                                                                     |
| loadBalancer | add_backend_pools            | Adds backend address pool configurations as defined by the `backendAddressPool` builder.                                                                                                  |
| loadBalancer | add_rules                    | Adds load balancing rules as defined by the `loadBalancingRule` builder.                                                                                                                  |
| loadBalancer | add_probes                   | Adds probes for the address pool as defined by the `loadBalancerProbe` builder.                                                                                                           |
| loadBalancer | add_dependencies             | Adds the resource ID's of additional dependencies that must be provisioned before the load balancer.                                                                                      |
| frontend | name                         | Name of the frontend IP configuration.                                                                                                                                                    |
| frontend | ip_v6                        | Generates an IPv6 IP address for the frontend.                                                                                                                                            |
| frontend | private_ip_allocation_method | Specifies how the private IP is assigned on an internal load balancer.                                                                                                                    |
| frontend | public_ip                    | Specifies the name of a public IP to generate. It will be generated with the same SKU as the load balancer.                                                                               |
| frontend | link_to_public_ip            | The name of an existing public IP to link to.                                                                                                                                             |
| backendAddressPool | name                         | The name of the backend address pool.                                                                                                                                                     |
| backendAddressPool | load_balancer                | The name of a load balancer these should be added to (used when adding to a pool for an existing load balancer).                                                                          |
| backendAddressPool | vnet                         | Specifies a virtual network in the same deployment where the backend services are connected.                                                                                              |
| backendAddressPool | link_to_vnet                 | Specifies an existing virtual network where the backend services are connected.                                                                                                           |
| backendAddressPool | add_ip_addresses             | Adds IP addresses to the backend pool.                                                                                                                                                    |
| loadBalancerProbe | name                         | The name of the load balancer probe.                                                                                                                                                      |
| loadBalancerProbe | protocol                     | The protocol to use for the probe - default is TCP.                                                                                                                                       |
| loadBalancerProbe | port                         | The port to probe on the backend service.                                                                                                                                                 |
| loadBalancerProbe | request_path                 | For HTTP(S) probes, the request path that returns a 200 when healthy.                                                                                                                     |
| loadBalancerProbe | interval                     | The interval between 4 and 30 seconds to probe the health of the services in the pool.                                                                                                    |
| loadBalancerProbe | number_of_probes             | The number of probe attempts before considering the service unhealthy.                                                                                                                    |
| loadBalancingRule | name                         | The name of the load balancing rule.                                                                                                                                                      |
| loadBalancingRule | frontend_ip_config           | The name of the frontend IP configuration to which this rule applies.                                                                                                                     |
| loadBalancingRule | backend_address_pool         | The name of the backend address pool to which this rule applies.                                                                                                                          |
| loadBalancingRule | probe                        | The name of the probe to use to check the health of services in the backend pool.                                                                                                         |
| loadBalancingRule | frontend_port                | The port on the frontend to forward to a backend service.                                                                                                                                 |
| loadBalancingRule | backend_port                 | The target port on the backend service.                                                                                                                                                   |
| loadBalancingRule | protocol                     | The protocol to forward, defaults to 'All'.                                                                                                                                               |
| loadBalancingRule | idle_timeout_minutes         | The time in minutes before a TCP connection is considered idle and disconnected.                                                                                                          |
| loadBalancingRule | load_distribution_policy     | The load distribution policy - 'Default' where a request can go to any backend, 'SourceIP' which is mapped on client IP, or 'SourceIPProtocol' which is mapped on client IP and protocol. |
| loadBalancingRule | enable_tcp_reset             | After an idle timeout, the TCP connection is reset - defaults to 'disabled'.                                                                                                              |
| loadBalancingRule | enable_outbound_snat         | Allows backend services to use this load balancer for outbound connections - defaults to 'disabled'.                                                                                      |

#### Example

```fsharp
open Farmer
open Farmer.Builders
open Farmer.LoadBalancer

arm {
    add_resources [
        vnet {
            name "my-vnet"
            add_address_spaces [ "10.0.1.0/24" ]
            add_subnets [
                subnet {
                    name "my-services"
                    prefix "10.0.1.0/24"
                    add_delegations [
                        SubnetDelegationService.ContainerGroups
                    ]
                }
            ]
        }
        loadBalancer {
            name "lb"
            sku Sku.Standard
            add_frontends [
                frontend {
                    name "lb-frontend"
                    public_ip "lb-pip"
                }
            ]
            add_backend_pools [
                backendAddressPool {
                    name "lb-backend"
                    vnet "my-vnet"
                    add_ip_addresses [
                        "10.0.1.4"
                        "10.0.1.5"
                    ]
                }
            ]
            add_probes [
                loadBalancerProbe {
                    name "httpGet"
                    protocol LoadBalancerProbeProtocol.HTTP
                    port 8080
                    request_path "/"
                }
            ]
            add_rules [
                loadBalancingRule {
                    name "rule1"
                    frontend_ip_config "lb-frontend"
                    backend_address_pool "lb-backend"
                    frontend_port 80
                    backend_port 8080
                    protocol TransmissionProtocol.TCP
                    probe "httpGet"
                }
            ]
        }
    ]
}
```
