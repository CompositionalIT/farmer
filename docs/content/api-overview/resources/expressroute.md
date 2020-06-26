---
title: "ExpressRoute"
date: 2020-06-07T21:57:00+01:00
weight: 5
chapter: false
---

#### Overview
An ExpressRoute circuit is a dedicated link to Azure to provide communication with Azure services without traversing the Internet. It requires some coordination with a networking provider for these circuits, so some information, such as the service provider and peering location must be obtained from [Azure reference documentation](https://docs.microsoft.com/en-us/azure/expressroute/expressroute-locations). The ExpressRoute builder creates an ExpressRoute circuit and enables Azure private peering and Microsoft peering.

* ExpressRoute Namespace (`Microsoft.Network/expressRouteCircuits`)

#### Builder Keywords
| Applies To | Keyword | Purpose |
|-|-|-|
| ExpressRoute | service_provider | Connectivity service provider from [Azure reference documentation](https://docs.microsoft.com/en-us/azure/expressroute/expressroute-locations) |
| ExpressRoute | peering_location | Connectivity peering location from [Azure reference documentation](https://docs.microsoft.com/en-us/azure/expressroute/expressroute-locations) |
| ExpressRoute | tier | Standard or Premium |
| ExpressRoute | family | Metered or Unlimited data |
| ExpressRoute | bandwidth | Bandwidth in Mbps for the circuit |
| ExpressRoute | add_peering | Peering details for the circuit - can add Azure Private and Microsoft peerings |
| Peering | peering_type | A network CIDR block of 4 IP addresses (/30) for the ExpressRoute primary circuit |
| Peering | peer_asn | Peer Autonomous System Number - this is a uniquely assigned number for the peer network, typically provided by the service provider in agreement with Microsoft |
| Peering | azure_asn | Azure Autonomous System Number - Microsoft oftent uses AS 12076 for Azure public, Azure private and Microsoft peering |
| Peering | primary_prefix | A network CIDR block of 4 IP addresses (/30) for the ExpressRoute primary circuit |
| Peering | secondary_prefix | A network CIDR block of 4 IP addresses (/30) for the ExpressRoute secondary circuit |
| Peering | vlan | A unique VLAN ID for the peering |
| Peering | shared_key | An optional shared key the service provider may specify for the peering |

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
   add_peering (
       peering {
           peering_type AzurePrivatePeering
           peer_asn 55277L
           azure_asn 12076
           primary_prefix (IPAddressCidr.parse "10.254.12.0/30")
           secondary_prefix (IPAddressCidr.parse "10.254.12.4/30")
           vlan 2406
       }
   )
}
```
