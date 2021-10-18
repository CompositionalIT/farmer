---
title: "ExpressRoute"
date: 2020-06-07T21:57:00+01:00
chapter: false
weight: 5
---

#### Overview
An ExpressRoute circuit is a dedicated link to Azure to provide communication with Azure services without traversing the Internet. It requires some coordination with a networking provider for these circuits, so some information, such as the service provider and peering location must be obtained from [Azure reference documentation](https://docs.microsoft.com/en-us/azure/expressroute/expressroute-locations). The ExpressRoute builder creates an ExpressRoute circuit and enables Azure private peering and Microsoft peering.

* ExpressRoute Namespace (`Microsoft.Network/expressRouteCircuits`)

#### ExpressRoute Builder (`expressRoute`)
| Keyword | Purpose |
|-|-|
| service_provider | Connectivity service provider from [Azure reference documentation](https://docs.microsoft.com/en-us/azure/expressroute/expressroute-locations) |
| peering_location | Connectivity peering location from [Azure reference documentation](https://docs.microsoft.com/en-us/azure/expressroute/expressroute-locations) |
| tier | Standard or Premium |
| family | Metered or Unlimited data |
| bandwidth | Bandwidth in Mbps for the circuit |
| add_authorizations | Adds names of authorization keys to be created on the new circuit. |
| add_peerings | Adds peering details for the circuit - can add Azure Private and Microsoft peerings |

#### ExpressRoute Peering Builder (`peering`)
| Applies To | Keyword | Purpose |
|-|-|
| peering_type | A network CIDR block of 4 IP addresses (/30) for the ExpressRoute primary circuit |
| peer_asn | Peer Autonomous System Number - this is a uniquely assigned number for the peer network, typically provided by the service provider in agreement with Microsoft |
| azure_asn | Azure Autonomous System Number - Microsoft oftent uses AS 12076 for Azure public, Azure private and Microsoft peering |
| primary_prefix | A network CIDR block of 4 IP addresses (/30) for the ExpressRoute primary circuit |
| secondary_prefix | A network CIDR block of 4 IP addresses (/30) for the ExpressRoute secondary circuit |
| vlan | A unique VLAN ID for the peering |
| shared_key | An optional shared key the service provider may specify for the peering |

#### Configuration Members

| Member | Purpose |
|-|-|
| ServiceKey | An ARM expression path to get the service key on the newly created circuit. |

#### Example

```fsharp
open Farmer
open Farmer.Builders
open Farmer.ExpressRoute

let circuit = expressRoute {
    name "my-express-route"
    service_provider "Equinix"
    peering_location "New York"
    tier Premium
    family MeteredData
    bandwidth 1000<Mbps>
    add_authorizations [
        "authkey1"
    ]
    add_peerings [
        peering {
            peering_type AzurePrivatePeering
            peer_asn 55277L
            azure_asn 12076
            primary_prefix (IPAddressCidr.parse "10.254.12.0/30")
            secondary_prefix (IPAddressCidr.parse "10.254.12.4/30")
            vlan 2406
        }
    ]
}

arm {
    add_resource circuit
    output "er-service-key" circuit.ServiceKey
}
```
