module Network

open System
open Expecto
open Newtonsoft.Json.Linq
open Farmer
open Farmer.Arm
open Farmer.Builders
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
    testList
        "Network Tests"
        [
            test "Basic vnet with subnets" {
                let vnetName = "my-vnet"
                let webServerSubnet = "web"
                let databaseSubnet = "db"

                let myNet =
                    vnet {
                        name vnetName
                        add_address_spaces [ "10.100.200.0/23" ]

                        add_subnets
                            [
                                subnet {
                                    name webServerSubnet
                                    prefix "10.100.200.0/24"
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
                    (builtVnet.Subnets |> Seq.map (fun s -> s.Name))
                    [ webServerSubnet; databaseSubnet ]
                    "Incorrect set of subnets"

                Expect.equal builtVnet.Subnets.[0].Name webServerSubnet "Incorrect name for web server subnet"

                Expect.equal
                    builtVnet.Subnets.[0].AddressPrefix
                    "10.100.200.0/24"
                    "Incorrect prefix for web server subnet"

                Expect.equal builtVnet.Subnets.[1].Name databaseSubnet "Incorrect name for database server subnet"

                Expect.equal
                    builtVnet.Subnets.[1].AddressPrefix
                    "10.100.201.0/24"
                    "Incorrect prefix for database server subnet"

                Expect.isNull
                    builtVnet.Subnets.[1].PrivateEndpointNetworkPolicies
                    "Incorrect PrivateEndpointNetworkPolicies"
            }
            test "Manually defined subnets with service endpoints" {
                let vnetName = "my-vnet"
                let servicesSubnet = "services"
                let containerSubnet = "containers"

                let myNet =
                    vnet {
                        name vnetName
                        add_address_spaces [ "10.28.0.0/16" ]

                        add_subnets
                            [
                                subnet {
                                    name servicesSubnet
                                    prefix "10.28.0.0/24"

                                    add_service_endpoints
                                        [
                                            EndpointServiceType.Storage,
                                            [ Location.EastUS; Location.EastUS2; Location.WestUS ]
                                        ]
                                }
                                subnet {
                                    name containerSubnet
                                    prefix "10.28.1.0/24"

                                    add_service_endpoints
                                        [
                                            EndpointServiceType.Storage,
                                            [ Location.EastUS; Location.EastUS2; Location.WestUS ]
                                        ]

                                    add_delegations [ SubnetDelegationService.ContainerGroups ]
                                }
                            ]
                    }

                let builtVnet = arm { add_resource myNet } |> getVnetResource
                Expect.hasLength builtVnet.AddressSpace.AddressPrefixes 1 "Incorrect number of address spaces"
                Expect.hasLength builtVnet.Subnets 2 "Incorrect number of subnets"

                Expect.containsAll
                    (builtVnet.Subnets |> Seq.map (fun s -> s.Name))
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

                let myNet =
                    vnet {
                        name vnetName

                        build_address_spaces
                            [
                                addressSpace {
                                    space "10.28.0.0/16"

                                    subnets
                                        [
                                            subnetSpec {
                                                name servicesSubnet
                                                size 24

                                                add_service_endpoints
                                                    [ EndpointServiceType.Storage, [ Location.EastUS ] ]
                                            }
                                            subnetSpec {
                                                name containerSubnet
                                                size 24
                                                add_delegations [ SubnetDelegationService.ContainerGroups ]

                                                add_service_endpoints
                                                    [ EndpointServiceType.Storage, [ Location.EastUS ] ]
                                            }
                                        ]
                                }
                            ]
                    }

                let generatedVNet = arm { add_resource myNet } |> getVnetResource

                Expect.containsAll
                    (generatedVNet.Subnets |> Seq.map (fun s -> s.Name))
                    [ servicesSubnet; containerSubnet ]
                    "Incorrect set of subnets"

                Expect.equal generatedVNet.Subnets.[0].Name servicesSubnet "Incorrect name for services subnet"

                Expect.equal
                    generatedVNet.Subnets.[0].AddressPrefix
                    "10.28.0.0/24"
                    "Incorrect prefix for services subnet"

                Expect.equal
                    generatedVNet.Subnets.[0].ServiceEndpoints.[0].Service
                    "Microsoft.Storage"
                    "Incorrect MS.Storage service endpoint for services subnet"

                Expect.equal generatedVNet.Subnets.[1].Name containerSubnet "Incorrect name for containers subnet"

                Expect.equal
                    generatedVNet.Subnets.[1].AddressPrefix
                    "10.28.1.0/24"
                    "Incorrect prefix for containers subnet"

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

                let myNet =
                    vnet {
                        name vnetName
                        add_address_spaces [ "10.28.0.0/16" ]

                        add_subnets
                            [
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

                let myNet =
                    vnet {
                        name vnetName

                        build_address_spaces
                            [
                                addressSpace {
                                    space "10.28.0.0/16"

                                    subnets
                                        [
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

                Expect.equal
                    generatedVNet.Subnets.[0].AddressPrefix
                    "10.28.0.0/24"
                    "Incorrect prefix for services subnet"

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

                let vnet2 =
                    vnet {
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

                let peering =
                    vnetPeering {
                        remote_vnet vnet1
                        direction OneWayToRemote
                        access AccessOnly
                        transit UseRemoteGateway
                    }

                let vnet2 =
                    vnet {
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

                let peering =
                    vnetPeering {
                        remote_vnet vnet1
                        direction OneWayFromRemote
                        access AccessOnly
                        transit UseRemoteGateway
                    }

                let vnet2 =
                    vnet {
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
                let webPolicy =
                    securityRule {
                        name "web-servers"
                        description "Public web server access"
                        services [ "http", 80; "https", 443 ]
                        add_source_tag NetworkSecurity.TCP "Internet"
                        add_destination_network "10.28.0.0/24"
                    }

                let appPolicy =
                    securityRule {
                        name "app-servers"
                        description "Internal app server access"
                        services [ "http", 8080 ]
                        add_source_network NetworkSecurity.TCP "10.28.0.0/24"
                        add_destination_network "10.28.1.0/24"
                    }

                let myNsg =
                    nsg {
                        name "my-nsg"
                        add_rules [ webPolicy; appPolicy ]
                    }

                let vnetName = "my-vnet"
                let webSubnet = "web"
                let appsSubnet = "apps"
                let noNsgSubnet = "no-nsg"

                let myNet =
                    vnet {
                        name vnetName

                        build_address_spaces
                            [
                                addressSpace {
                                    space "10.28.0.0/16"

                                    subnets
                                        [
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

                let myNet =
                    vnet {
                        name vnetName

                        build_address_spaces
                            [
                                addressSpace {
                                    space "10.28.0.0/16"

                                    subnets
                                        [
                                            subnetSpec {
                                                name webSubnet
                                                size 24

                                                link_to_network_security_group (
                                                    networkSecurityGroups.resourceId "my-nsg"
                                                )
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

                let subnetResource =
                    subnet {
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

                let subnetResource =
                    subnet {
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

                Expect.isNull dependsOn "Linking to unmanaged vnet should have no dependencies"

                let subnet =
                    jobj.SelectToken "resources[?(@.type=='Microsoft.Network/virtualNetworks/subnets')].name"

                Expect.equal (string subnet) "my-vnet/services" "Incorrect name on subnet"
            }
            test "Standalone subnet without linked vnet not allowed" {
                Expect.throws
                    (fun _ ->
                        let template =
                            arm {
                                add_resources
                                    [
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
                let deployment =
                    arm {
                        location Location.EastUS

                        add_resources
                            [
                                natGateway { name "my-nat-gateway" }
                                vnet {
                                    name "my-net"
                                    add_address_spaces [ "10.100.0.0/16" ]

                                    add_subnets
                                        [
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
                let deployment =
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
                let myNet =
                    vnet {
                        name "my-net"
                        add_address_spaces [ "10.40.0.0/16" ]

                        add_subnets
                            [
                                subnet {
                                    name "priv-endpoints"
                                    prefix "10.40.255.0/24"
                                    allow_private_endpoints Enabled
                                }
                            ]
                    }

                let existingPrivateLinkId =
                    { PrivateLink.privateLinkServices.resourceId "pls" with
                        ResourceGroup = Some "farmer-pls"
                    }

                let pe1 =
                    privateEndpoint {
                        name "pe1"
                        custom_nic_name "pe1-nic"
                        link_to_subnet (subnets.resourceId (ResourceName "my-net", ResourceName "priv-endpoints"))
                        resource (Unmanaged existingPrivateLinkId)
                    }

                let myDnsZone =
                    dnsZone {
                        name "farmer.com"
                        zone_type Dns.Public

                        add_records
                            [
                                Farmer.Builders.Dns.aRecord {
                                    name "pe1"
                                    ttl 600

                                    add_ipv4_addresses
                                        [
                                            pe1.CustomNicFirstEndpointIP
                                            |> Option.map ArmExpression.Eval
                                            |> Option.toObj
                                        ]
                                }
                            ]
                    }

                let deployment =
                    arm {
                        add_resources
                            [
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
        ]
