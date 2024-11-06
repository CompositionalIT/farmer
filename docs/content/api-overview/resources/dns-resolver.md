---
title: "DNS Resolver"
date: 2023-03-13T09:59:10-05:00
chapter: false
weight: 4
---

#### Overview

The DNS resolver resource provides a DNS endpoint for resources that have IP connectivity to a virtual network but
aren't directly attached to it such as VPN or ExpressRoute clients. It also provides outbound DNS resolution to enable
resources in the virtual network to resolve DNS using external DNS servers, such as an on-premise DNS.

* DNS Resolver (`Microsoft.Network/dnsResolvers`)
* DNS Resolver Inbound Endpoint (`Microsoft.Network/dnsResolvers/inboundEndpoints`)
* DNS Resolver Outbound Endpoint (`Microsoft.Network/dnsResolvers/outboundEndpoints`)
* DNS Forwarding Ruleset (`Microsoft.Network/dnsForwardingRulesets`)
* DNS Forwarding Rules (`Microsoft.Network/dnsForwardingRulesets/forwardingRules`)
* DNS Forwarding Virtual Network Links (`Microsoft.Network/dnsForwardingRulesets/virtualNetworkLinks`)

#### DNS Resolver Builder Keywords

| Applies To | Keyword | Purpose |
|-|-|-|
| dnsResolver | name | Sets the name of the DNS resolver. |
| dnsResolver | vnet | Sets the virtual network where the DNS resolver is attached. |
| dnsResolver | link_to_vnet | Links the resolver to an existing virtual network. |
| dnsResolver | inbound_subnet | If set, an inbound endpoint will be generated for this subnet with dynamic IP allocation. The subnet can only contain DNS resolver resources. |
| dnsResolver | add_inbound_endpoints | Add inbound endpoints to subnets and specify static or dynamic IP allocation. |
| dnsResolver | outbound_subnet | If set, an outbound endpoint will be generated for this subnet. The subnet can only contain DNS resolver resources. |
| dnsResolver | add_outbound_endpoints | Add outbound endpoints to additional subnets. |
| dnsResolver | depends_on | Deploy this DNS resolver after another resource is successfully deployed. |
| dnsResolver | add_tag | Adds a tag to this resource. |
| dnsResolver | add_tags | Adds a set of tags to this resource. |
| dnsInboundEndpoint | name | Sets the name of the DNS resolver inbound endpoint. |
| dnsInboundEndpoint | dns_resolver | Add the inbound endpoint to a DNS resolver in the same deployment. |
| dnsInboundEndpoint | link_to_dns_resolver | Links to an existing DNS resolver. |
| dnsInboundEndpoint | subnet | Specify a subnet in this deployment where the inbound endpoint will be added. |
| dnsInboundEndpoint | link_to_subnet | Create the inbound endpoint in an existing subnet. |
| dnsInboundEndpoint | add_dynamic_ip | Adds a dynamically assigned IP for the inbound endpoint in the subnet. |
| dnsInboundEndpoint | add_static_ip | Adds a statically assigned IP for the inbound endpoint in the subnet. |
| dnsInboundEndpoint | depends_on | Deploy this DNS inbound endpoint after another resource is successfully deployed. |
| dnsInboundEndpoint | add_tag | Adds a tag to this resource. |
| dnsInboundEndpoint | add_tags | Adds a set of tags to this resource. |
| dnsOutboundEndpoint | name | Sets the name of the DNS resolver outbound endpoint. |
| dnsOutboundEndpoint | dns_resolver | Add the outbound endpoint to a DNS resolver in the same deployment. |
| dnsOutboundEndpoint | link_to_dns_resolver | Links to an existing DNS resolver. |
| dnsOutboundEndpoint | subnet | Specify a subnet in this deployment where the outbound endpoint will be added. |
| dnsOutboundEndpoint | link_to_subnet | Create the outbound endpoint in an existing subnet. |
| dnsOutboundEndpoint | depends_on | Deploy this DNS outbound endpoint after another resource is successfully deployed. |
| dnsOutboundEndpoint | add_tag | Adds a tag to this resource. |
| dnsOutboundEndpoint | add_tags | Adds a set of tags to this resource. |
| dnsForwardingRuleset | name | Sets the name of the DNS forwarding ruleset. |
| dnsForwardingRuleset | add_resolver_outbound_endpoints | Applies this ruleset to one or more DNS resolver outbound endpoints in the same deployment. |
| dnsForwardingRuleset | add_rules | Adds one or more rules to forward DNS domain resolution to a DNS endpoint (IP and port). |
| dnsForwardingRuleset | add_vnet_links | Links this ruleset to one or more virtual networks to provide DNS resolution to resources in that virtual network. It does not need to be the same vnet where the resolver is created, but it must be in the same region. |
| dnsForwardingRuleset | depends_on | Deploy this DNS forwarding ruleset after another resource is successfully deployed. |
| dnsForwardingRuleset | add_tag | Adds a tag to this resource. |
| dnsForwardingRuleset | add_tags | Adds a set of tags to this resource. |
| dnsForwardingRule | name | Sets the name of the DNS forwarding rule. |
| dnsForwardingRule | forwarding_ruleset_id | Adds the rule an a forwarding ruleset defined in the same deployment. |
| dnsForwardingRule | domain_name | Specifies the domain to which the rules apply. A trailing dot '.' will be appended if not added since forwarding rules require it. |
| dnsForwardingRule | state | Enable or disable a rule. |
| dnsForwardingRule | add_target_dns_servers | Specify one or more DNS servers by IP and port as `System.Net.IPEndPoint` objects. These will be used to resolve requests for the `domain_name` in this rule. |

#### Example - Inbound Endpoint

To provide a private resolver for resources in a virtual network, add a subnet that is delegated to DNS resolvers and
specify that as the `inbound_subnet` on a `dnsResolver` resource.

```fsharp
#r "nuget: Farmer"

open Farmer
open Farmer.Builders
open Farmer.Network

arm {
    add_resources
        [
            vnet {
                name "mynet"
                add_address_spaces [ "100.72.2.0/24" ]

                add_subnets
                    [
                        subnet {
                            name "resolver-subnet"
                            prefix "100.72.2.240/28"
                            add_delegations [ SubnetDelegationService.DnsResolvers ]
                        }
                    ]
            }
            dnsResolver {
                name "my-dns-resolver"
                vnet "mynet"
                inbound_subnet "resolver-subnet"
            }
        ]
}
```

#### Example - Outbound Endpoint and Ruleset

To resolve DNS in a virtual network with a route to an on-premise DNS server (e.g. a vNet with a VPN gateway to
on-premise), add a subnet that is delegated to DNS resolvers and specify that as the `outbound_subnet` on
a `dnsResolver` resource. Define rules for the domains that should be forwarded to the on-premise DNS servers.

```fsharp
#r "nuget: Farmer"

open Farmer
open Farmer.Builders
open Farmer.Network

arm {
    add_resources
        [
            vnet {
                name "mynet"
                add_address_spaces [ "100.72.2.0/24" ]

                add_subnets
                    [
                        subnet {
                            name "resolver-subnet"
                            prefix "100.72.2.240/28"
                            add_delegations [ SubnetDelegationService.DnsResolvers ]
                        }
                    ]
            }
            dnsResolver {
                name "my-dns-resolver"
                vnet "mynet"

                add_outbound_endpoints
                    [
                        dnsOutboundEndpoint {
                            name "outbound-dns"

                            link_to_subnet (
                                Farmer.Arm.Network.subnets.resourceId (
                                    ResourceName "mynet",
                                    ResourceName "resolver-subnet"
                                )
                            )
                        }
                    ]
            }
            dnsForwardingRuleset {
                name "route-dns-requests"
                depends_on [ Farmer.Arm.Network.virtualNetworks.resourceId (ResourceName "mynet") ]

                add_resolver_outbound_endpoints
                    [
                        // list of outbound endpoint IDs. These must be in a subnet that
                        // can reach the endpoint IPs for rules in this ruleset.
                        Farmer.Arm.Dns.dnsResolverOutboundEndpoints.resourceId (
                            ResourceName "my-dns-resolver",
                            ResourceName "outbound-dns"
                        )
                    ]

                add_vnet_links
                    [
                        // List of vnet IDs that will resolve domains using this ruleset.
                        Farmer.Arm.Network.virtualNetworks.resourceId (ResourceName "mynet")
                    ]

                add_rules
                    [
                        // List of rule sets for domains in the on-premise network.
                        dnsForwardingRule {
                            name "rule-1"
                            domain_name "example.com"
                            state Enabled

                            add_target_dns_servers
                                [
                                    // On-premise DNS servers IP addresses and ports.
                                    System.Net.IPEndPoint.Parse("192.168.100.74:53")
                                    System.Net.IPEndPoint.Parse("192.168.100.75:53")
                                ]
                        }
                    ]
            }
        ]
}
```
