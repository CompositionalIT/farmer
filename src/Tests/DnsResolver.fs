module DnsResolver

open Expecto
open Farmer
open Farmer.Builders
open Farmer.Network
open Newtonsoft.Json.Linq

let tests =
    testList
        "DNS Resolver Tests"
        [
            test "Basic resolver with single inbound endpoint" {
                let deployment =
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

                let jobj = JObject.Parse(deployment.Template |> Writer.toJson)
                let dnsResolver = jobj.SelectToken "resources[?(@name=='my-dns-resolver')]"
                Expect.isNotNull dnsResolver "DNS resolver resource missing from template"
                let dnsResolverDependencies = dnsResolver.SelectToken "dependsOn"
                Expect.hasLength dnsResolverDependencies 1 "Incorrect number of dnsResolver dependencies"
                let dnsResolverVnetId = dnsResolver.SelectToken "properties.virtualNetwork.id"

                Expect.equal
                    (dnsResolverVnetId |> string)
                    "[resourceId('Microsoft.Network/virtualNetworks', 'mynet')]"
                    "Incorrect vnet ID"

                let dnsResolverInbound =
                    jobj.SelectToken "resources[?(@name=='my-dns-resolver/resolver-subnet')]"

                Expect.isNotNull dnsResolverInbound "Generated DNS resolver inbound resource missing from template"
                let ipAllocation = dnsResolverInbound.SelectToken "properties.ipConfigurations[0]"
                let ipAllocationMethod = ipAllocation.["privateIpAllocationMethod"]
                Expect.equal ipAllocationMethod (JValue "Dynamic") "Incorrect generated IP allocation method"
                let ipAllocationSubnet = ipAllocation.SelectToken "subnet.id"

                Expect.equal
                    (ipAllocationSubnet |> string)
                    "[resourceId('Microsoft.Network/virtualNetworks/subnets', 'mynet', 'resolver-subnet')]"
                    "Incorrect subnet id on generated resolver inbound"
            }
            test "Adding an inbound to an existing DNS resolver" {
                let deployment =
                    arm {
                        add_resources
                            [
                                dnsInboundEndpoint {
                                    name "another-inbound"

                                    link_to_subnet (
                                        Farmer.Arm.Network.subnets.resourceId (
                                            ResourceName "mynet",
                                            ResourceName "another-resolver-subnet"
                                        )
                                    )

                                    link_to_dns_resolver "my-dns-resolver"
                                }
                            ]
                    }

                let jobj = JObject.Parse(deployment.Template |> Writer.toJson)

                let dnsResolverInbound =
                    jobj.SelectToken "resources[?(@name=='my-dns-resolver/another-inbound')]"

                Expect.isNotNull dnsResolverInbound "DNS resolver inbound resource missing from template"

                Expect.isEmpty
                    dnsResolverInbound.["dependsOn"]
                    "Adding to existing resolver should have no dependencies."

                let ipAllocation = dnsResolverInbound.SelectToken "properties.ipConfigurations[0]"
                let ipAllocationSubnet = ipAllocation.SelectToken "subnet.id"

                Expect.equal
                    (ipAllocationSubnet |> string)
                    "[resourceId('Microsoft.Network/virtualNetworks/subnets', 'mynet', 'another-resolver-subnet')]"
                    "Incorrect subnet id on generated resolver inbound"
            }
            test "Use external DNS servers for a domain" {
                let deployment =
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
                                            // list of outbound endpoint IDs
                                            Farmer.Arm.Dns.dnsResolverOutboundEndpoints.resourceId (
                                                ResourceName "my-dns-resolver",
                                                ResourceName "outbound-dns"
                                            )
                                        ]

                                    add_vnet_links
                                        [
                                            // List of vnet IDs
                                            Farmer.Arm.Network.virtualNetworks.resourceId (ResourceName "mynet")
                                        ]

                                    add_rules
                                        [
                                            // List of rule sets
                                            dnsForwardingRule {
                                                name "rule-1"
                                                domain_name "example.com"
                                                state Enabled

                                                add_target_dns_servers
                                                    [
                                                        System.Net.IPEndPoint.Parse("192.168.100.74:53")
                                                        System.Net.IPEndPoint.Parse("192.168.100.75:53")
                                                    ]
                                            }
                                        ]
                                }
                            ]
                    }

                let jobj = JObject.Parse(deployment.Template |> Writer.toJson)
                // RuleSet
                let dnsRuleset = jobj.SelectToken "resources[?(@name=='route-dns-requests')]"
                Expect.isNotNull dnsRuleset "DNS forwarding ruleset missing from template"
                Expect.hasLength dnsRuleset.["dependsOn"] 2 "Incorrect number of dependencies on ruleset."

                let expectedRulesetDeps =
                    JArray(
                        "[resourceId('Microsoft.Network/dnsResolvers/outboundEndpoints', 'my-dns-resolver', 'outbound-dns')]",
                        "[resourceId('Microsoft.Network/virtualNetworks', 'mynet')]"
                    )

                Expect.containsAll
                    dnsRuleset.["dependsOn"]
                    expectedRulesetDeps
                    "Incorrect number of dependencies on ruleset."

                let dnsOutboundEndpointId =
                    dnsRuleset.SelectToken "properties.dnsResolverOutboundEndpoints[0].id"

                Expect.equal
                    (dnsOutboundEndpointId |> string)
                    "[resourceId('Microsoft.Network/dnsResolvers/outboundEndpoints', 'my-dns-resolver', 'outbound-dns')]"
                    "Incorrect resolver outbound id on ruleset"

                // Rule
                let rule1 = jobj.SelectToken "resources[?(@name=='route-dns-requests/rule-1')]"
                Expect.isNotNull rule1 "DNS forwarding rule 'rule-1' missing from template"

                let expectedRuleDeps =
                    JArray("[resourceId('Microsoft.Network/dnsForwardingRulesets', 'route-dns-requests')]")

                Expect.containsAll rule1.["dependsOn"] expectedRuleDeps "Missing dependencies for rule 'rule-1'."

                Expect.equal
                    (rule1.SelectToken "properties.domainName" |> string)
                    "example.com."
                    "Incorrect domain name on 'rule-1'"

                Expect.equal
                    (rule1.SelectToken "properties.targetDnsServers[0].ipAddress" |> string)
                    "192.168.100.74"
                    "Incorrect targetDnsServer ipAddress on 'rule-1'"

                Expect.equal
                    (rule1.SelectToken "properties.targetDnsServers[0].port" |> int)
                    53
                    "Incorrect targetDnsServer port on 'rule-1'"

                // vNet link
                let mynetLink = jobj.SelectToken "resources[?(@name=='route-dns-requests/mynet')]"
                Expect.isNotNull mynetLink "DNS ruleset vnet link 'mynet' missing from template"

                let mynetLinkDeps =
                    JArray(JValue "[resourceId('Microsoft.Network/dnsForwardingRulesets', 'route-dns-requests')]")

                Expect.containsAll mynetLink.["dependsOn"] mynetLinkDeps "Missing dependencies for vnetLink 'mynet'."

                Expect.equal
                    (mynetLink.SelectToken "properties.virtualNetwork.id" |> string)
                    "[resourceId('Microsoft.Network/virtualNetworks', 'mynet')]"
                    "Incorrect id on vnet link"
            }
        ]
