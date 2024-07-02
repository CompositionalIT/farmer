module Network

open System
open Expecto
open Newtonsoft.Json.Linq
open Farmer
open Farmer.Arm
open Farmer.Builders
open Farmer.Builders.NetworkInterface
open Farmer.Network
open Microsoft.Rest

let netClient =
    new Microsoft.Azure.Management.Network.NetworkManagementClient(
        Uri "http://management.azure.com",
        TokenCredentials "NotNullOrWhiteSpace"
    )

let getVnetResource =
    findAzureResources<Microsoft.Azure.Management.Network.Models.VirtualNetwork> netClient.SerializationSettings
    >> List.head

let getPeeringResource =
    findAzureResources<Microsoft.Azure.Management.Network.Models.VirtualNetworkPeering> netClient.SerializationSettings

let tests =
    testList "Network Tests" [
        test "Basic vnet with subnets" {
            let vnetName = "my-vnet"
            let webServerSubnet = "web"
            let databaseSubnet = "db"

            let myNet = vnet {
                name vnetName
                add_address_spaces [ "10.100.200.0/22" ]

                add_subnets [
                    subnet {
                        name webServerSubnet
                        prefix "10.100.200.0/24"
                        add_prefixes [ "10.100.202.0/24" ]
                    }
                    subnet {
                        name databaseSubnet
                        prefix "10.100.201.0/24"
                    }
                ]
            }

            let builtVnet = arm { add_resource myNet } |> getVnetResource
            Expect.hasLength builtVnet.AddressSpace.AddressPrefixes 1 "Incorrect number of address spaces"
            Expect.hasLength builtVnet.Subnets 2 "Incorrect number of subnets"

            Expect.containsAll
                (builtVnet.Subnets |> Seq.map _.Name)
                [ webServerSubnet; databaseSubnet ]
                "Incorrect set of subnets"

            Expect.equal builtVnet.Subnets.[0].Name webServerSubnet "Incorrect name for web server subnet"

            Expect.containsAll
                builtVnet.Subnets.[0].AddressPrefixes
                [ "10.100.200.0/24"; "10.100.202.0/24" ]
                "Incorrect prefix for web server subnet (multiple address prefixes)"

            Expect.equal builtVnet.Subnets.[1].Name databaseSubnet "Incorrect name for database server subnet"

            Expect.equal
                builtVnet.Subnets.[1].AddressPrefix
                "10.100.201.0/24"
                "Incorrect prefix for database server subnet (single address prefix)"

            Expect.isNull
                builtVnet.Subnets.[1].PrivateEndpointNetworkPolicies
                "Incorrect PrivateEndpointNetworkPolicies"
        }
        test "Manually defined subnets with service endpoints" {
            let vnetName = "my-vnet"
            let servicesSubnet = "services"
            let containerSubnet = "containers"

            let myNet = vnet {
                name vnetName
                add_address_spaces [ "10.28.0.0/16" ]

                add_subnets [
                    subnet {
                        name servicesSubnet
                        prefix "10.28.0.0/24"

                        add_service_endpoints [
                            EndpointServiceType.Storage, [ Location.EastUS; Location.EastUS2; Location.WestUS ]
                        ]
                    }
                    subnet {
                        name containerSubnet
                        prefix "10.28.1.0/24"

                        add_service_endpoints [
                            EndpointServiceType.Storage, [ Location.EastUS; Location.EastUS2; Location.WestUS ]
                        ]

                        add_delegations [ SubnetDelegationService.ContainerGroups ]
                    }
                ]
            }

            let builtVnet = arm { add_resource myNet } |> getVnetResource
            Expect.hasLength builtVnet.AddressSpace.AddressPrefixes 1 "Incorrect number of address spaces"
            Expect.hasLength builtVnet.Subnets 2 "Incorrect number of subnets"

            Expect.containsAll
                (builtVnet.Subnets |> Seq.map _.Name)
                [ servicesSubnet; containerSubnet ]
                "Incorrect set of subnets"

            Expect.equal
                builtVnet.Subnets.[0].ServiceEndpoints.[0].Service
                "Microsoft.Storage"
                "Incorrect MS.Storage service endpoint for services subnet"

            Expect.equal
                builtVnet.Subnets.[1].ServiceEndpoints.[0].Service
                "Microsoft.Storage"
                "Incorrect MS.Storage service endpoint for containers subnet"

            Expect.equal
                builtVnet.Subnets.[1].Delegations.[0].ServiceName
                "Microsoft.ContainerInstance/containerGroups"
                "Incorrect MS.ContainerGroups subnet delegation"
        }
        test "Automatically carved subnets with service endpoints" {
            let vnetName = "my-vnet"
            let servicesSubnet = "services"
            let containerSubnet = "containers"

            let myNet = vnet {
                name vnetName

                build_address_spaces [
                    addressSpace {
                        space "10.28.0.0/16"

                        subnets [
                            subnetSpec {
                                name servicesSubnet
                                size 24

                                add_service_endpoints [ EndpointServiceType.Storage, [ Location.EastUS ] ]
                            }
                            subnetSpec {
                                name containerSubnet
                                size 24
                                add_delegations [ SubnetDelegationService.ContainerGroups ]

                                add_service_endpoints [ EndpointServiceType.Storage, [ Location.EastUS ] ]
                            }
                        ]
                    }
                ]
            }

            let generatedVNet = arm { add_resource myNet } |> getVnetResource

            Expect.containsAll
                (generatedVNet.Subnets |> Seq.map _.Name)
                [ servicesSubnet; containerSubnet ]
                "Incorrect set of subnets"

            Expect.equal generatedVNet.Subnets.[0].Name servicesSubnet "Incorrect name for services subnet"

            Expect.equal generatedVNet.Subnets.[0].AddressPrefix "10.28.0.0/24" "Incorrect prefix for services subnet"

            Expect.equal
                generatedVNet.Subnets.[0].ServiceEndpoints.[0].Service
                "Microsoft.Storage"
                "Incorrect MS.Storage service endpoint for services subnet"

            Expect.equal generatedVNet.Subnets.[1].Name containerSubnet "Incorrect name for containers subnet"

            Expect.equal generatedVNet.Subnets.[1].AddressPrefix "10.28.1.0/24" "Incorrect prefix for containers subnet"

            Expect.equal
                generatedVNet.Subnets.[1].ServiceEndpoints.[0].Service
                "Microsoft.Storage"
                "Incorrect MS.Storage service endpoint for containers subnet"

            Expect.equal
                generatedVNet.Subnets.[1].Delegations.[0].ServiceName
                "Microsoft.ContainerInstance/containerGroups"
                "Incorrect MS.ContainerGroups subnet delegation"

            Expect.isNull
                generatedVNet.Subnets.[1].PrivateEndpointNetworkPolicies
                "Incorrect PrivateEndpointNetworkPolicies"
        }


        test "Manually defined subnets with private endpoint support" {
            let vnetName = "my-vnet"
            let servicesSubnet = "services"
            let containerSubnet = "containers"

            let myNet = vnet {
                name vnetName
                add_address_spaces [ "10.28.0.0/16" ]

                add_subnets [
                    subnet {
                        name servicesSubnet
                        prefix "10.28.0.0/24"
                        allow_private_endpoints Enabled
                        private_link_service_network_policies Disabled
                    }
                ]
            }

            let builtVnet = arm { add_resource myNet } |> getVnetResource
            Expect.hasLength builtVnet.AddressSpace.AddressPrefixes 1 "Incorrect number of address spaces"
            Expect.hasLength builtVnet.Subnets 1 "Incorrect number of subnets"

            Expect.equal
                builtVnet.Subnets.[0].PrivateEndpointNetworkPolicies
                "Disabled"
                "Incorrect PrivateEndpointNetworkPolicies"

            Expect.equal
                builtVnet.Subnets.[0].PrivateLinkServiceNetworkPolicies
                "Disabled"
                "PrivateLinkServiceNetworkPolicies should be enabled"
        }
        test "Automatically carved subnets with private endpoint support" {
            let vnetName = "my-vnet"
            let servicesSubnet = "services"
            let containerSubnet = "containers"

            let myNet = vnet {
                name vnetName

                build_address_spaces [
                    addressSpace {
                        space "10.28.0.0/16"

                        subnets [
                            subnetSpec {
                                name servicesSubnet
                                size 24
                                allow_private_endpoints Enabled
                                private_link_service_network_policies Disabled
                            }
                        ]
                    }
                ]
            }

            let generatedVNet = arm { add_resource myNet } |> getVnetResource
            Expect.equal generatedVNet.Subnets.[0].Name servicesSubnet "Incorrect name for services subnet"

            Expect.equal generatedVNet.Subnets.[0].AddressPrefix "10.28.0.0/24" "Incorrect prefix for services subnet"

            Expect.equal
                generatedVNet.Subnets.[0].PrivateEndpointNetworkPolicies
                "Disabled"
                "Incorrect PrivateEndpointNetworkPolicies"

            Expect.equal
                generatedVNet.Subnets.[0].PrivateLinkServiceNetworkPolicies
                "Disabled"
                "Incorrect PrivateEndpointNetworkPolicies"
        }
        test "Two VNets with bidirectional peering" {
            let vnet1 = vnet { name "vnet1" }

            let vnet2 = vnet {
                name "vnet2"
                add_peering vnet1
            }

            let peerings =
                arm { add_resources [ vnet1; vnet2 ] }
                |> getPeeringResource
                |> List.filter (fun x -> x.Name.Contains("/peering-"))

            Expect.hasLength peerings 2 "Incorrect peering count"

            Expect.equal
                peerings.[0].RemoteVirtualNetwork.Id
                ((virtualNetworks.resourceId (ResourceName "vnet1")).ArmExpression.Eval())
                "remote VNet incorrect"

            Expect.equal
                peerings.[1].RemoteVirtualNetwork.Id
                ((virtualNetworks.resourceId (ResourceName "vnet2")).ArmExpression.Eval())
                "remote VNet incorrect"

            Expect.equal
                (Nullable false)
                peerings.[0].AllowGatewayTransit
                "Gateway transit should be disabled by default"

            Expect.equal
                (Nullable false)
                peerings.[1].AllowGatewayTransit
                "Gateway transit should be disabled by default"
        }
        test "Two VNets with one-directional peering" {
            let vnet1 = vnet { name "vnet1" }

            let peering = vnetPeering {
                remote_vnet vnet1
                direction OneWayToRemote
                access AccessOnly
                transit UseRemoteGateway
            }

            let vnet2 = vnet {
                name "vnet2"
                add_peering peering
            }

            let foundPeerings =
                arm { add_resources [ vnet1; vnet2 ] }
                |> getPeeringResource
                |> List.filter (fun x -> x.Name.Contains("/peering-"))

            Expect.hasLength foundPeerings 1 "Incorrect peering count"

            Expect.equal
                foundPeerings.[0].RemoteVirtualNetwork.Id
                ((virtualNetworks.resourceId (ResourceName "vnet1")).ArmExpression.Eval())
                "remote VNet incorrect"

            Expect.equal foundPeerings.[0].AllowVirtualNetworkAccess (Nullable true) "incorrect network access"
            Expect.equal foundPeerings.[0].AllowForwardedTraffic (Nullable false) "incorrect forwarding"
            Expect.equal foundPeerings.[0].AllowGatewayTransit (Nullable true) "incorrect transit"
            Expect.equal foundPeerings.[0].UseRemoteGateways (Nullable true) "incorrect gateway"
        }
        test "Two VNets with one-directional reverse peering" {
            let vnet1 = vnet { name "vnet1" }

            let peering = vnetPeering {
                remote_vnet vnet1
                direction OneWayFromRemote
                access AccessOnly
                transit UseRemoteGateway
            }

            let vnet2 = vnet {
                name "vnet2"
                add_peering peering
            }

            let foundPeerings =
                arm { add_resources [ vnet1; vnet2 ] }
                |> getPeeringResource
                |> List.filter (fun x -> x.Name.Contains("/peering-"))

            Expect.hasLength foundPeerings 1 "Incorrect peering count"

            Expect.equal
                foundPeerings.[0].RemoteVirtualNetwork.Id
                ((virtualNetworks.resourceId (ResourceName "vnet2")).ArmExpression.Eval())
                "remote VNet incorrect"

            Expect.equal foundPeerings.[0].AllowVirtualNetworkAccess (Nullable true) "incorrect network access"
            Expect.equal foundPeerings.[0].AllowForwardedTraffic (Nullable false) "incorrect forwarding"
            Expect.equal foundPeerings.[0].AllowGatewayTransit (Nullable true) "incorrect transit"
            Expect.equal foundPeerings.[0].UseRemoteGateways (Nullable false) "incorrect gateway"
        }
        test "Automatically carved subnets with network security group support" {
            let webPolicy = securityRule {
                name "web-servers"
                description "Public web server access"
                services [ "http", 80; "https", 443 ]
                add_source_tag NetworkSecurity.TCP "Internet"
                add_destination_network "10.28.0.0/24"
            }

            let appPolicy = securityRule {
                name "app-servers"
                description "Internal app server access"
                services [ "http", 8080 ]
                add_source_network NetworkSecurity.TCP "10.28.0.0/24"
                add_destination_network "10.28.1.0/24"
            }

            let myNsg = nsg {
                name "my-nsg"
                add_rules [ webPolicy; appPolicy ]
            }

            let vnetName = "my-vnet"
            let webSubnet = "web"
            let appsSubnet = "apps"
            let noNsgSubnet = "no-nsg"

            let myNet = vnet {
                name vnetName

                build_address_spaces [
                    addressSpace {
                        space "10.28.0.0/16"

                        subnets [
                            subnetSpec {
                                name webSubnet
                                size 24
                                network_security_group myNsg
                            }
                            subnetSpec {
                                name appsSubnet
                                size 24
                                network_security_group myNsg
                            }
                            subnetSpec {
                                name noNsgSubnet
                                size 24
                            }
                        ]
                    }
                ]
            }

            let template = arm { add_resources [ myNet; myNsg ] }
            let jobj = template.Template |> Writer.toJson |> Newtonsoft.Json.Linq.JObject.Parse

            let dependencies =
                jobj.SelectToken "resources[?(@.type=='Microsoft.Network/virtualNetworks')].dependsOn"
                :?> Newtonsoft.Json.Linq.JArray

            Expect.isNotNull dependencies "vnet missing dependency for nsg"
            Expect.hasLength dependencies 1 "Incorrect number of dependencies for vnet"

            Expect.equal
                (dependencies.[0].ToString())
                "[resourceId('Microsoft.Network/networkSecurityGroups', 'my-nsg')]"
                "Incorrect vnet dependencies"

            let vnet = template |> getVnetResource
            Expect.isNotNull vnet.Subnets.[0].NetworkSecurityGroup "First subnet missing NSG"

            Expect.equal
                vnet.Subnets.[0].NetworkSecurityGroup.Id
                "[resourceId('Microsoft.Network/networkSecurityGroups', 'my-nsg')]"
                "Incorrect security group for first subnet"

            Expect.isNotNull vnet.Subnets.[0].NetworkSecurityGroup "Second subnet missing NSG"

            Expect.equal
                vnet.Subnets.[1].NetworkSecurityGroup.Id
                "[resourceId('Microsoft.Network/networkSecurityGroups', 'my-nsg')]"
                "Incorrect security group for second subnet"

            Expect.isNull vnet.Subnets.[2].NetworkSecurityGroup "Third subnet should not have NSG"
        }
        test "Vnet with linked network security group doesn't add dependsOn" {
            let vnetName = "my-vnet"
            let webSubnet = "web"

            let myNet = vnet {
                name vnetName

                build_address_spaces [
                    addressSpace {
                        space "10.28.0.0/16"

                        subnets [
                            subnetSpec {
                                name webSubnet
                                size 24

                                link_to_network_security_group (networkSecurityGroups.resourceId "my-nsg")
                            }
                        ]
                    }
                ]
            }

            let template = arm { add_resources [ myNet ] }
            let jobj = template.Template |> Writer.toJson |> Newtonsoft.Json.Linq.JObject.Parse

            let dependencies =
                jobj.SelectToken "resources[?(@.type=='Microsoft.Network/virtualNetworks')].dependsOn"
                :?> Newtonsoft.Json.Linq.JArray

            Expect.hasLength dependencies 0 "Should be no vnet dependencies when linking to nsg"

            let vnet = template |> getVnetResource
            Expect.isNotNull vnet.Subnets.[0].NetworkSecurityGroup "Subnet missing NSG"

            Expect.equal
                vnet.Subnets.[0].NetworkSecurityGroup.Id
                "[resourceId('Microsoft.Network/networkSecurityGroups', 'my-nsg')]"
                "Incorrect security group for subnet"
        }
        test "Add subnet linked to managed vnet" {
            let vnetName = "my-vnet"
            let servicesSubnet = "services"

            let subnetResource = subnet {
                name servicesSubnet
                link_to_vnet (virtualNetworks.resourceId vnetName)
                prefix "10.28.0.0/24"
            }

            Expect.equal
                ((subnetResource :> IBuilder).ResourceId.Eval())
                "[resourceId('Microsoft.Network/virtualNetworks/subnets', 'my-vnet', 'services')]"
                "Incorrect resourceId on subnet"

            let template = arm { add_resources [ subnetResource ] }
            let jobj = template.Template |> Writer.toJson |> Newtonsoft.Json.Linq.JObject.Parse

            let dependsOn =
                jobj.SelectToken "resources[?(@.type=='Microsoft.Network/virtualNetworks/subnets')].dependsOn"
                :?> Newtonsoft.Json.Linq.JArray

            Expect.hasLength dependsOn 1 "Linking to managed vnet should have dependency on the vnet"

            let subnet =
                jobj.SelectToken "resources[?(@.type=='Microsoft.Network/virtualNetworks/subnets')].name"

            Expect.equal (string subnet) "my-vnet/services" "Incorrect name on subnet"
        }
        test "Add subnet linked to existing (unmanaged) vnet" {
            let vnetName = "my-vnet"
            let servicesSubnet = "services"

            let subnetResource = subnet {
                name servicesSubnet
                link_to_unmanaged_vnet (virtualNetworks.resourceId vnetName)
                prefix "10.28.0.0/24"
            }

            Expect.equal
                ((subnetResource :> IBuilder).ResourceId.Eval())
                "[resourceId('Microsoft.Network/virtualNetworks/subnets', 'my-vnet', 'services')]"
                "Incorrect resourceId on subnet"

            let template = arm { add_resources [ subnetResource ] }
            let jobj = template.Template |> Writer.toJson |> Newtonsoft.Json.Linq.JObject.Parse

            let dependsOn =
                jobj.SelectToken "resources[?(@.type=='Microsoft.Network/virtualNetworks/subnets')].dependsOn"
                :?> Newtonsoft.Json.Linq.JArray

            Expect.isEmpty dependsOn "Linking to unmanaged vnet should have no dependencies"

            let subnet =
                jobj.SelectToken "resources[?(@.type=='Microsoft.Network/virtualNetworks/subnets')].name"

            Expect.equal (string subnet) "my-vnet/services" "Incorrect name on subnet"
        }
        test "Add multiple subnets linked to existing (unmanaged) vnet with dependencies" {
            let vnetName = "my-vnet"

            let template = arm {
                add_resources [
                    subnet {
                        name "subnet1"
                        link_to_unmanaged_vnet (virtualNetworks.resourceId vnetName)
                        prefix "10.28.0.0/24"
                    }
                    subnet {
                        name "subnet2"
                        link_to_unmanaged_vnet (virtualNetworks.resourceId vnetName)
                        prefix "10.28.1.0/24"
                        depends_on (subnets.resourceId (ResourceName vnetName / ResourceName "subnet1"))
                    }
                ]
            }

            let jobj = template.Template |> Writer.toJson |> Newtonsoft.Json.Linq.JObject.Parse

            let dependsOn =
                jobj.SelectToken "resources[?(@.name=='my-vnet/subnet2')].dependsOn" :?> Newtonsoft.Json.Linq.JArray

            Expect.hasLength dependsOn 1 "subnet2 should have a single dependency"

            Expect.equal
                (string dependsOn[0])
                "[resourceId('Microsoft.Network/virtualNetworks/subnets', 'my-vnet', 'subnet1')]"
                "subnet2 should have a dependency on subnet1"
        }
        test "Standalone subnet without linked vnet not allowed" {
            Expect.throws
                (fun _ ->
                    let template = arm {
                        add_resources [
                            subnet {
                                name "foo"
                                prefix "10.28.0.0/24"
                            }
                        ]
                    }

                    template.Template |> Writer.toJson |> ignore)
                "Adding a subnet resource without linking to a vnet is not allowed"
        }
        test "Creates basic NAT gateway" {
            let deployment = arm {
                location Location.EastUS

                add_resources [
                    natGateway { name "my-nat-gateway" }
                    vnet {
                        name "my-net"
                        add_address_spaces [ "10.100.0.0/16" ]

                        add_subnets [
                            subnet {
                                name "my-services"
                                prefix "10.100.12.0/24"
                                nat_gateway (natGateways.resourceId "my-nat-gateway")
                            }
                        ]
                    }
                ]
            }

            let jobj = deployment.Template |> Writer.toJson |> JObject.Parse

            let natGateway =
                jobj.SelectToken "resources[?(@.type=='Microsoft.Network/natGateways')]"

            let dependencies = natGateway.["dependsOn"] :?> JArray

            Expect.contains
                dependencies
                (JValue "[resourceId('Microsoft.Network/publicIPAddresses', 'my-nat-gateway-publicip-1')]")
                "Missing dependency for public IP"

            let natGwProps = natGateway.["properties"]
            let idleTimeout = natGwProps.["idleTimeoutInMinutes"]
            Expect.equal (int idleTimeout) 4 "Incorrect default value for idle timeout"
            let ipRefs = natGwProps.["publicIpAddresses"]

            Expect.equal
                (string ipRefs.[0].["id"])
                "[resourceId('Microsoft.Network/publicIPAddresses', 'my-nat-gateway-publicip-1')]"
                "IP Addresses did not match"

            let publicIp =
                jobj.SelectToken "resources[?(@.type=='Microsoft.Network/publicIPAddresses')]"

            Expect.isNotNull publicIp "Public IP should have been generated for the NAT gateway."
        }
        test "Creates route table with two routes" {
            let deployment = arm {
                location Location.EastUS

                add_resources [
                    routeTable {
                        name "myroutetable"

                        add_routes [
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
                                    Route.HopType.VirtualAppliance(Some(System.Net.IPAddress.Parse "10.2.31.2"))
                                )
                            }
                        ]
                    }
                ]
            }

            let jobj = deployment.Template |> Writer.toJson |> JObject.Parse
            let a = jobj.ToString()

            let routeTable =
                jobj.SelectToken "resources[?(@.type=='Microsoft.Network/routeTables')]"

            let routeTableProps = routeTable.["properties"]

            let disableBgp: bool =
                JToken.op_Explicit routeTableProps.["disableBgpRoutePropagation"]

            Expect.equal disableBgp false "Incorrect default value for disableBgpRoutePropagation"
            let routes = routeTableProps.["routes"] :?> JArray
            Expect.isNotNull routes "Routes should have been generated for the route table"
            Expect.equal (string routes.[0].["name"]) "myroute" "route 1 should be named 'myroute'"
            Expect.equal (string routes.[1].["name"]) "myroute2" "route 2 should be named 'myroute2'"
            Expect.isNull routes.[0].["apiVersion"] "Embedded routes should not have 'apiVersion'."
            let routeProps = routes.[0].["properties"]
            let route2Props = routes.[1].["properties"]
            let route3Props = routes.[2].["properties"]
            let route4Props = routes.[3].["properties"]

            Expect.equal
                (string routeProps.["nextHopType"])
                "VirtualAppliance"
                "route 1 should have a hop type of 'VirtualAppliance'"

            Expect.equal
                (string routeProps.["addressPrefix"])
                "10.10.90.0/24"
                "route 1 should have an address prefix of '10.10.90.0/24'"

            Expect.isNull route2Props.["nextHopIpAddress"] "route 2 should not have a next hop ip address"
            Expect.isNull route3Props.["nextHopIpAddress"] "route 3 should not have a next hop ip address"

            Expect.equal
                (string route2Props.["nextHopType"])
                "None"
                "route 2 should have the default set to None for nextHopType"

            Expect.equal
                (string route4Props.["nextHopIpAddress"])
                "10.2.31.2"
                "route 4 should have the next hop ip address set to 10.2.31.2"
        }
        test "Create private endpoint" {
            let myNet = vnet {
                name "my-net"
                add_address_spaces [ "10.40.0.0/16" ]

                add_subnets [
                    subnet {
                        name "priv-endpoints"
                        prefix "10.40.255.0/24"
                        allow_private_endpoints Enabled
                    }
                ]
            }

            let existingPrivateLinkId = {
                PrivateLink.privateLinkServices.resourceId "pls" with
                    ResourceGroup = Some "farmer-pls"
            }

            let pe1 = privateEndpoint {
                name "pe1"
                custom_nic_name "pe1-nic"
                link_to_subnet (subnets.resourceId (ResourceName "my-net", ResourceName "priv-endpoints"))
                resource (Unmanaged existingPrivateLinkId)
            }

            let myDnsZone = dnsZone {
                name "farmer.com"
                zone_type Dns.Public

                add_records [
                    Farmer.Builders.Dns.aRecord {
                        name "pe1"
                        ttl 600

                        add_ipv4_addresses [
                            pe1.CustomNicFirstEndpointIP |> Option.map ArmExpression.Eval |> Option.toObj
                        ]
                    }
                ]
            }

            let deployment = arm {
                add_resources [
                    myNet
                    pe1
                    resourceGroup {
                        name "[resourceGroup().name]"
                        depends_on pe1
                        add_resource myDnsZone
                    }
                ]
            }

            let jobj = deployment.Template |> Writer.toJson |> JObject.Parse
            let peProps = jobj.SelectToken "resources[?(@.name=='pe1')].properties"
            Expect.equal (string peProps.["customNetworkInterfaceName"]) "pe1-nic" "Incorrect custom nic name"

            Expect.equal
                (string peProps.["subnet"].["id"])
                "[resourceId('Microsoft.Network/virtualNetworks/subnets', 'my-net', 'priv-endpoints')]"
                "Incorrect subnet id"

            Expect.equal
                (string peProps.["privateLinkServiceConnections"].[0].["properties"].["privateLinkServiceId"])
                "[resourceId('farmer-pls', 'Microsoft.Network/privateLinkServices', 'pls')]"
                "Incorrect private link service ID"
        }

        test "Creates basic route server" {
            let deployment = arm {
                location Location.EastUS

                add_resources [
                    vnet {
                        name "test-vnet"
                        add_address_spaces [ "10.0.0.0/16" ]
                    }
                    routeServer {
                        name "my-route-server"
                        sku RouteServer.Sku.Standard
                        subnet_prefix "10.0.12.0/24"
                        link_to_vnet (virtualNetworks.resourceId "test-vnet")
                        public_ip_name "my-route-server-public-ip-name"

                        add_bgp_connections [
                            routeServerBGPConnection {
                                name "my-bgp-conn"
                                peer_ip "10.0.1.85"
                                peer_asn 65000
                            }
                            routeServerBGPConnection {
                                name "my-bgp-conn-2"
                                peer_ip "10.0.1.86"
                                peer_asn 4110002310L

                                depends_on (
                                    ResourceId.create (
                                        routeServerBGPConnections,
                                        ResourceName "my-route-server",
                                        ResourceName "my-bgp-conn"
                                    )
                                )
                            }
                        ]
                    }
                ]
            }

            let jobj = deployment.Template |> Writer.toJson |> JObject.Parse
            let templateStr = jobj.ToString()

            //validate vnet generated
            let vnet =
                jobj.SelectToken "resources[?(@.type=='Microsoft.Network/virtualNetworks')]"

            Expect.isNotNull vnet "vnet should be generated"

            //validate publicIPAddresses generated
            let publicip =
                jobj.SelectToken "resources[?(@.type=='Microsoft.Network/publicIPAddresses')]"

            Expect.isNotNull publicip "public ip should be generated"

            let publicipName = publicip.["name"]

            Expect.equal
                publicipName
                (JToken.op_Implicit "my-route-server-public-ip-name")
                "Incorrect default value for public ip name"

            let publicipProps = publicip.["properties"]

            let allocationMethod: string =
                JToken.op_Explicit publicipProps.["publicIPAllocationMethod"]

            Expect.equal allocationMethod "Static" "Incorrect default value for public ip allocation method"

            //validate route server subnet generated
            let subnet =
                jobj.SelectToken "resources[?(@.type=='Microsoft.Network/virtualNetworks/subnets')]"

            Expect.isNotNull subnet "subnet should be generated"

            let subnetName = subnet.["name"]

            Expect.equal
                subnetName
                (JToken.op_Implicit "test-vnet/RouteServerSubnet")
                "Incorrect default value for subnet name"

            let subnetProps = subnet.["properties"]
            let addressPrefix: string = JToken.op_Explicit subnetProps.["addressPrefix"]
            Expect.equal addressPrefix "10.0.12.0/24" "Incorrect addressPrefix for subnet"

            //validate ip configuration generated
            let ipConfig =
                jobj.SelectToken "resources[?(@.type=='Microsoft.Network/virtualHubs/ipConfigurations')]"

            Expect.isNotNull ipConfig "ipConfig should be generated"

            let ipConfigName = ipConfig.["name"]

            Expect.equal
                ipConfigName
                (JToken.op_Implicit "my-route-server/my-route-server-ipconfig")
                "Incorrect default value for ipConfig name"

            let ipConfigDependencies =
                jobj.SelectToken "resources[?(@.type=='Microsoft.Network/virtualHubs/ipConfigurations')].dependsOn"
                :?> JArray

            Expect.isNotNull ipConfigDependencies "Missing dependency for ipConfig"
            Expect.hasLength ipConfigDependencies 3 "Incorrect number of dependencies for ipConfig"

            Expect.equal
                (ipConfigDependencies.[0].ToString())
                "[resourceId(\u0027Microsoft.Network/publicIPAddresses\u0027, \u0027my-route-server-public-ip-name\u0027)]"
                "Incorrect ipConfig dependencies"

            Expect.equal
                (ipConfigDependencies.[1].ToString())
                "[resourceId(\u0027Microsoft.Network/virtualHubs\u0027, \u0027my-route-server\u0027)]"
                "Incorrect ipConfig dependencies"

            Expect.equal
                (ipConfigDependencies.[2].ToString())
                "[resourceId(\u0027Microsoft.Network/virtualNetworks/subnets\u0027, \u0027test-vnet\u0027, \u0027RouteServerSubnet\u0027)]"
                "Incorrect ipConfig dependencies"

            let ipConfigPip = ipConfig.SelectToken("properties.publicIPAddress.id").ToString()

            Expect.equal
                ipConfigPip
                "[resourceId(\u0027Microsoft.Network/publicIPAddresses\u0027, \u0027my-route-server-public-ip-name\u0027)]"
                "Incorrect publicIPAddress id for ipConfig"

            let ipConfigSubnet = ipConfig.SelectToken("properties.subnet.id").ToString()

            Expect.equal
                ipConfigSubnet
                "[resourceId(\u0027Microsoft.Network/virtualNetworks/subnets\u0027, \u0027test-vnet\u0027, \u0027RouteServerSubnet\u0027)]"
                "Incorrect subnet id for ipConfig"

            //validate route server generated
            let routeServer =
                jobj.SelectToken "resources[?(@.type=='Microsoft.Network/virtualHubs')]"

            Expect.isNotNull routeServer "route server should be generated"

            let routeServerName = routeServer.["name"]

            Expect.equal
                routeServerName
                (JToken.op_Implicit "my-route-server")
                "Incorrect default value for route server name"

            let routeServerProps = routeServer.["properties"]

            let allowBranchToBranchTraffic: bool =
                JToken.op_Explicit routeServerProps.["allowBranchToBranchTraffic"]

            Expect.equal allowBranchToBranchTraffic false "Incorrect default value for allowBranchToBranchTraffic"

            let hubRoutingPreference: string =
                JToken.op_Explicit routeServerProps.["hubRoutingPreference"]

            Expect.equal hubRoutingPreference "ExpressRoute" "Incorrect default value for HubRoutingPreference"
            let sku: string = JToken.op_Explicit routeServerProps.["sku"]
            Expect.equal sku "Standard" "Incorrect default value for sku"

            //validate bgp connection generated
            let bgpConn = jobj.SelectToken "resources[?(@.name=='my-route-server/my-bgp-conn')]"

            Expect.isNotNull bgpConn "bgp connection should be generated"

            let bgpConnName = bgpConn.["name"]

            Expect.equal
                bgpConnName
                (JToken.op_Implicit "my-route-server/my-bgp-conn")
                "Incorrect default value for bgp connection name"

            let bgpConnDependencies = bgpConn.SelectToken "dependsOn" :?> JArray

            Expect.isNotNull bgpConnDependencies "Missing dependency for bgp connection"
            Expect.hasLength bgpConnDependencies 2 "Incorrect number of dependencies for bgp connection"

            Expect.equal
                (bgpConnDependencies.[0].ToString())
                "[resourceId(\u0027Microsoft.Network/virtualHubs\u0027, \u0027my-route-server\u0027)]"
                "Incorrect bgp connection dependencies"

            Expect.equal
                (bgpConnDependencies.[1].ToString())
                "[resourceId(\u0027Microsoft.Network/virtualHubs/ipConfigurations\u0027, \u0027my-route-server\u0027, \u0027my-route-server-ipconfig\u0027)]"
                "Incorrect bgp connection dependencies"

            let bgpConnProps = bgpConn.["properties"]
            let peerAsn: int = JToken.op_Explicit bgpConnProps.["peerAsn"]
            Expect.equal peerAsn 65000 "Incorrect peer Asn for bgp connection"
            let peerIp: string = JToken.op_Explicit bgpConnProps.["peerIp"]
            Expect.equal peerIp "10.0.1.85" "Incorrect peer ip for bgp connection"

            //validate bgp connection generated
            let bgpConnWithDep =
                jobj.SelectToken "resources[?(@.name=='my-route-server/my-bgp-conn-2')]"

            Expect.isNotNull bgpConnWithDep "bgp connection with dependency should be generated"

            Expect.equal
                (bgpConnWithDep.SelectToken("properties.peerAsn"))
                (JValue(4110002310L))
                "peer_asn long value incorrect did not match"

            let bgpConnWithDepName = bgpConnWithDep.["name"]

            Expect.equal
                bgpConnWithDepName
                (JToken.op_Implicit "my-route-server/my-bgp-conn-2")
                "Incorrect default value for bgp connection with dependency name"

            let bgpConnWithDepDependencies = bgpConnWithDep.SelectToken "dependsOn" :?> JArray

            Expect.isNotNull bgpConnWithDepDependencies "Missing dependency for bgp connection with explicit dependency"

            Expect.hasLength
                bgpConnWithDepDependencies
                3
                "Incorrect number of dependencies for bgp connection with explicit 'depends_on'"

            Expect.contains
                (bgpConnWithDepDependencies)
                (JValue "[resourceId('Microsoft.Network/virtualHubs/bgpConnections', 'my-route-server', 'my-bgp-conn')]")
                "Incorrect bgp connection dependencies with explicit 'depends_on'"

        }

        test "Creates basic network interface with static ip" {
            let deployment = arm {
                location Location.EastUS

                add_resources [
                    vnet {
                        name "test-vnet"
                        add_address_spaces [ "10.0.0.0/16" ]
                    }
                    networkInterface {
                        name "my-network-interface"
                        subnet_name "my-subnet"
                        subnet_prefix "10.0.100.0/24"
                        link_to_vnet (virtualNetworks.resourceId "test-vnet")
                        add_static_ip "10.0.100.10"
                        accelerated_networking_flag false
                        ip_forwarding_flag false
                    }
                ]
            }

            let jobj = deployment.Template |> Writer.toJson |> JObject.Parse
            let templateStr = jobj.ToString()

            //validate vnet generated
            let vnet =
                jobj.SelectToken "resources[?(@.type=='Microsoft.Network/virtualNetworks')]"

            Expect.isNotNull vnet "vnet should be generated"

            //validate subnet generated
            let subnet =
                jobj.SelectToken "resources[?(@.type=='Microsoft.Network/virtualNetworks/subnets')]"

            Expect.isNotNull subnet "subnet should be generated"

            let subnetName = subnet.["name"]

            Expect.equal subnetName (JToken.op_Implicit "test-vnet/my-subnet") "Incorrect default value for subnet name"

            let subnetProps = subnet.["properties"]
            let addressPrefix: string = JToken.op_Explicit subnetProps.["addressPrefix"]
            Expect.equal addressPrefix "10.0.100.0/24" "Incorrect addressPrefix for subnet"

            //validate network interface generated
            let networkInterface =
                jobj.SelectToken "resources[?(@.type=='Microsoft.Network/networkInterfaces')]"

            Expect.isNotNull networkInterface "network interface should be generated"

            let networkInterfaceName = networkInterface.["name"]

            Expect.equal
                networkInterfaceName
                (JToken.op_Implicit "my-network-interface")
                "Incorrect default value for network interface name"

            let networkInterfaceDependencies =
                jobj.SelectToken "resources[?(@.type=='Microsoft.Network/networkInterfaces')].dependsOn" :?> JArray

            Expect.isNotNull networkInterfaceDependencies "Missing dependency for networkInterface"
            Expect.hasLength networkInterfaceDependencies 1 "Incorrect number of dependencies for networkInterface"

            Expect.equal
                (networkInterfaceDependencies.[0].ToString())
                "[resourceId(\u0027Microsoft.Network/virtualNetworks\u0027, \u0027test-vnet\u0027)]"
                "Incorrect networkInterface dependencies"

            let networkInterfaceProps = networkInterface.["properties"]

            let enableAcceleratedNetworking: bool =
                JToken.op_Explicit networkInterfaceProps.["enableAcceleratedNetworking"]

            Expect.equal enableAcceleratedNetworking false "Incorrect default value for enableAcceleratedNetworking"

            let enableIPForwarding: bool =
                JToken.op_Explicit networkInterfaceProps.["enableIPForwarding"]

            Expect.equal enableIPForwarding false "Incorrect default value for enableIPForwarding"

            //validate ip config generated
            let ipConfig = networkInterfaceProps.["ipConfigurations"].[0]
            Expect.isNotNull ipConfig "network interface ip config should be generated"

            let ipConfigName = ipConfig.["name"]

            Expect.equal
                ipConfigName
                (JToken.op_Implicit "ipconfig1")
                "Incorrect default value for network interface ip config name"

            let ipConfigProps = ipConfig.["properties"]

            let privateIPAddress: string = JToken.op_Explicit ipConfigProps.["privateIPAddress"]
            Expect.equal privateIPAddress "10.0.100.10" "Incorrect default value for privateIPAddress"

            let privateIPAllocationMethod: string =
                JToken.op_Explicit ipConfigProps.["privateIPAllocationMethod"]

            Expect.equal privateIPAllocationMethod "Static" "Incorrect default value for privateIPAllocationMethod"

            let subnetId = ipConfigProps.SelectToken("subnet.id").ToString()

            Expect.equal
                subnetId
                "[resourceId(\u0027Microsoft.Network/virtualNetworks/subnets\u0027, \u0027test-vnet\u0027, \u0027my-subnet\u0027)]"
                "Incorrect subnet id for ipConfig"
        }

        test "Creates basic network interface with existing vnet subnet and dynamic ip" {
            let deployment = arm {
                location Location.EastUS

                add_resources [
                    networkInterface {
                        name "my-network-interface"
                        link_to_subnet "test-subnet"
                        link_to_vnet "test-vnet"
                    }
                ]
            }

            let jobj = deployment.Template |> Writer.toJson |> JObject.Parse
            let templateStr = jobj.ToString()

            let networkInterface =
                jobj.SelectToken "resources[?(@.type=='Microsoft.Network/networkInterfaces')]"

            Expect.isNotNull networkInterface "network interface should be generated"

            let ipConfig = networkInterface.["properties"].["ipConfigurations"].[0]
            Expect.isNotNull ipConfig "network interface ip config should be generated"

            let ipConfigProps = ipConfig.["properties"]

            let privateIPAllocationMethod: string =
                JToken.op_Explicit ipConfigProps.["privateIPAllocationMethod"]

            Expect.equal privateIPAllocationMethod "Dynamic" "Incorrect default value for privateIPAllocationMethod"

            let subnetId = ipConfigProps.SelectToken("subnet.id").ToString()

            Expect.equal
                subnetId
                "[resourceId(\u0027Microsoft.Network/virtualNetworks/subnets\u0027, \u0027test-vnet\u0027, \u0027test-subnet\u0027)]"
                "Incorrect subnet id for ipConfig"
        }
    ]