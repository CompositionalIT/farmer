---
title: "Route Tables"
date: 2022-09-08T22:26:00-04:00
chapter: false
weight: 5
---

#### Overview
The `routeTable` builder creates a route table to efficiently change default routing traffic between Azure subnets, virtual networks, and on-premises networks. To learn more about routeTables, reference the [Azure Docs](https://docs.microsoft.com/en-us/azure/virtual-network/manage-route-table)

* RouteTable (`Microsoft.Network/routeTables`)
* Route (`Microsoft.Network/routeTables/routes`)

#### Builder Keywords

| Applies To | Keyword | Purpose |
|-|-|-|
| routeTable | name | Name of the NAT Gateway resource |
| routeTable | disableBgpRoutePropagation | Whether to disable the routes learned by BGP on that route table |
| routeTable | add_routes | The routes to be added to this route table |
| route | name | Name of the route resource |
| route | addressPrefix | The destination CIDR to which the route applies |
| route | nextHopType | The type of Azure hop the packet should be sent to |
| route | nextHopIpAddress | The IP address packets should be forwarded to. Only allowed in routes where the next hop type is VirtualAppliance |
| route | hasBgpOverride | Whether the route overrides overalpping BGP routes regardless of LPM |

#### Example

```fsharp
#r "nuget:Farmer"

open Farmer
open Farmer.Builders

arm {
    location Location.EastUS

    add_resources
        [
            routeTable {
                name "myroutetable"

                add_routes
                    [
                        route {
                            name "myroute"
                            addressPrefix "10.10.90.0/24"
                            nextHopIpAddress "10.10.67.5"
                        }
                        route {
                            name "myroute2"
                            addressPrefix "10.10.80.0/24"
                        }
                        route {
                            name "myroute3"
                            addressPrefix "10.2.31.0/24"
                            nextHopType (Route.HopType.VirtualAppliance None)
                        }
                        route {
                            name "myroute4"
                            addressPrefix "10.2.31.0/24"

                            nextHopType (
                                Route.HopType.VirtualAppliance(
                                    Some(System.Net.IPAddress.Parse "10.2.31.2")
                                )
                            )
                        }
                    ]
            }
        ]
}
```