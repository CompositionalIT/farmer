---
title: "Route Server"
chapter: false
weight: 5
---

#### Overview
The `routeServer` builder creates a route server to simplify dynamic routing between your network virtual appliance (NVA) and your virtual network. To learn more about routeServer, reference the [Azure Docs](https://learn.microsoft.com/en-us/azure/route-server/overview)

* RouteServer (`Microsoft.Network/virtualHubs`)
* BGPConnection (`Microsoft.Network/virtualHubs/bgpConnections`)

#### Builder Keywords

| Applies To | Keyword                        | Purpose                                                                    |
|-|--------------------------------|----------------------------------------------------------------------------|
| routeServer | name                           | Name of the route server resource                                          |
| routeServer | sku                            | Sets the tier of the route server                                          |
| routeServer | allow_branch_to_branch_traffic | The allowBranchToBranchTraffic flag for the route server. Default is false |
| routeServer | routing_preference             | The routingPreference for the route server. Default is ExpressRoute        |
| routeServer | link_to_vnet                   | Link to existing vnet or to vnet managed by Farmer                         |
| routeServer | subnet_prefix                  | Sets the subnetPrefix of the vnet for route server                         |
| routeServer | add_bgp_connections             | The BGP connections to be added to the route server                        |
| routeServerBGPConnection | name                           | Name of the BGP connection                                                 |
| routeServerBGPConnection | peer_ip                         | The peer IP of the BGP connection                                          |
| routeServerBGPConnection | peer_asn                        | The peer Asn of the BGP connection                                         |
| routeServerBGPConnection | depends_on                      | Depend on another resource before deploying this bgp connection            |

#### Example

```fsharp
#r "nuget:Farmer"

open Farmer
open Farmer.Builders

arm {
    location Location.EastUS
    add_resources
        [
            vnet {
                name "test-vnet"
                add_address_spaces [ "10.0.0.0/16" ]
            }
            routeServer {
                name "my-route-server"
                sku RouteServer.Sku.Standard
                subnet_prefix "10.0.12.0/24"
                link_to_vnet (virtualNetworks.resourceId "test-vnet")

                add_bgp_connections
                    [
                        routeServerBGPConnection {
                            name "my-bgp-conn"
                            peer_ip "10.0.1.85"
                            peer_asn 65000
                        }
                    ]
            }
        ]
}
```