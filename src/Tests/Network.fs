module Network

open Expecto
open Farmer
open Farmer.Builders
open Farmer.Network
open Microsoft.Rest
open System
open Farmer.Arm

let netClient = new Microsoft.Azure.Management.Network.NetworkManagementClient(Uri "http://management.azure.com", TokenCredentials "NotNullOrWhiteSpace")
let getVnetResource = findAzureResources<Microsoft.Azure.Management.Network.Models.VirtualNetwork> netClient.SerializationSettings >> List.head
let getPeeringResource = findAzureResources<Microsoft.Azure.Management.Network.Models.VirtualNetworkPeering> netClient.SerializationSettings

let tests = testList "Network Tests" [
    test "Basic vnet with subnets" {
        let vnetName = "my-vnet"
        let webServerSubnet = "web"
        let databaseSubnet = "db"
        let myNet = vnet {
            name vnetName
            add_address_spaces [ "10.100.200.0/23" ]
            add_subnets [
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
        let builtVnet = arm { add_resource myNet; } |> getVnetResource
        Expect.hasLength builtVnet.AddressSpace.AddressPrefixes 1 "Incorrect number of address spaces"
        Expect.hasLength builtVnet.Subnets 2 "Incorrect number of subnets"
        Expect.containsAll (builtVnet.Subnets |> Seq.map (fun s -> s.Name)) [webServerSubnet; databaseSubnet] "Incorrect set of subnets"
        Expect.equal builtVnet.Subnets.[0].Name webServerSubnet "Incorrect name for web server subnet"
        Expect.equal builtVnet.Subnets.[0].AddressPrefix "10.100.200.0/24" "Incorrect prefix for web server subnet"
        Expect.equal builtVnet.Subnets.[1].Name databaseSubnet "Incorrect name for database server subnet"
        Expect.equal builtVnet.Subnets.[1].AddressPrefix "10.100.201.0/24" "Incorrect prefix for database server subnet"
        Expect.isNull builtVnet.Subnets.[1].PrivateEndpointNetworkPolicies "Incorrect PrivateEndpointNetworkPolicies"
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
                    add_service_endpoints [ EndpointServiceType.Storage, [Location.EastUS; Location.EastUS2; Location.WestUS] ]
                }
                subnet {
                    name containerSubnet
                    prefix "10.28.1.0/24"
                    add_service_endpoints [ EndpointServiceType.Storage, [Location.EastUS; Location.EastUS2; Location.WestUS] ]
                    add_delegations [ SubnetDelegationService.ContainerGroups ]
                }
            ]
        }
        let builtVnet = arm { add_resource myNet; } |> getVnetResource
        Expect.hasLength builtVnet.AddressSpace.AddressPrefixes 1 "Incorrect number of address spaces"
        Expect.hasLength builtVnet.Subnets 2 "Incorrect number of subnets"
        Expect.containsAll (builtVnet.Subnets |> Seq.map (fun s -> s.Name)) [servicesSubnet; containerSubnet] "Incorrect set of subnets"
        Expect.equal builtVnet.Subnets.[0].ServiceEndpoints.[0].Service "Microsoft.Storage" "Incorrect MS.Storage service endpoint for services subnet"
        Expect.equal builtVnet.Subnets.[1].ServiceEndpoints.[0].Service "Microsoft.Storage" "Incorrect MS.Storage service endpoint for containers subnet"
        Expect.equal builtVnet.Subnets.[1].Delegations.[0].ServiceName "Microsoft.ContainerInstance/containerGroups" "Incorrect MS.ContainerGroups subnet delegation"
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
                            add_service_endpoints [
                                EndpointServiceType.Storage, [Location.EastUS]
                            ]
                        }
                        subnetSpec {
                            name containerSubnet
                            size 24
                            add_delegations [SubnetDelegationService.ContainerGroups]
                            add_service_endpoints [
                                EndpointServiceType.Storage, [Location.EastUS]
                            ]
                        }
                    ]
                }
            ]
        }
        let generatedVNet = arm { add_resource myNet; } |> getVnetResource
        Expect.containsAll (generatedVNet.Subnets |> Seq.map (fun s -> s.Name)) [servicesSubnet; containerSubnet] "Incorrect set of subnets"
        Expect.equal generatedVNet.Subnets.[0].Name servicesSubnet "Incorrect name for services subnet"
        Expect.equal generatedVNet.Subnets.[0].AddressPrefix "10.28.0.0/24" "Incorrect prefix for services subnet"
        Expect.equal generatedVNet.Subnets.[0].ServiceEndpoints.[0].Service "Microsoft.Storage" "Incorrect MS.Storage service endpoint for services subnet"
        Expect.equal generatedVNet.Subnets.[1].Name containerSubnet "Incorrect name for containers subnet"
        Expect.equal generatedVNet.Subnets.[1].AddressPrefix "10.28.1.0/24" "Incorrect prefix for containers subnet"
        Expect.equal generatedVNet.Subnets.[1].ServiceEndpoints.[0].Service "Microsoft.Storage" "Incorrect MS.Storage service endpoint for containers subnet"
        Expect.equal generatedVNet.Subnets.[1].Delegations.[0].ServiceName "Microsoft.ContainerInstance/containerGroups" "Incorrect MS.ContainerGroups subnet delegation"
        Expect.isNull generatedVNet.Subnets.[1].PrivateEndpointNetworkPolicies "Incorrect PrivateEndpointNetworkPolicies"
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
        let builtVnet = arm { add_resource myNet; } |> getVnetResource
        Expect.hasLength builtVnet.AddressSpace.AddressPrefixes 1 "Incorrect number of address spaces"
        Expect.hasLength builtVnet.Subnets 1 "Incorrect number of subnets"
        Expect.equal builtVnet.Subnets.[0].PrivateEndpointNetworkPolicies "Disabled" "Incorrect PrivateEndpointNetworkPolicies"
        Expect.equal builtVnet.Subnets.[0].PrivateLinkServiceNetworkPolicies "Disabled" "PrivateLinkServiceNetworkPolicies should be enabled"
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
        let generatedVNet = arm { add_resource myNet; } |> getVnetResource
        Expect.equal generatedVNet.Subnets.[0].Name servicesSubnet "Incorrect name for services subnet"
        Expect.equal generatedVNet.Subnets.[0].AddressPrefix "10.28.0.0/24" "Incorrect prefix for services subnet"
        Expect.equal generatedVNet.Subnets.[0].PrivateEndpointNetworkPolicies "Disabled" "Incorrect PrivateEndpointNetworkPolicies"
        Expect.equal generatedVNet.Subnets.[0].PrivateLinkServiceNetworkPolicies "Disabled" "Incorrect PrivateEndpointNetworkPolicies"
    }
    test "Two VNets with bidirectional peering" {
        let vnet1 = vnet { name "vnet1" }
        let vnet2 = vnet { name "vnet2"; add_peering vnet1 }
        let peerings = arm { add_resources [vnet1;vnet2] } |> getPeeringResource |> List.filter (fun x -> x.Name.Contains("/peering-"))

        Expect.hasLength peerings 2 "Incorrect peering count"
        Expect.equal peerings.[0].RemoteVirtualNetwork.Id ((virtualNetworks.resourceId (ResourceName "vnet1")).ArmExpression.Eval()) "remote VNet incorrect"
        Expect.equal peerings.[1].RemoteVirtualNetwork.Id ((virtualNetworks.resourceId (ResourceName "vnet2")).ArmExpression.Eval()) "remote VNet incorrect"

        Expect.equal (Nullable false) peerings.[0].AllowGatewayTransit "Gateway transit should be disabled by default"
        Expect.equal (Nullable false) peerings.[1].AllowGatewayTransit "Gateway transit should be disabled by default"
    }
    test "Two VNets with one-directional peering" {
        let vnet1 = vnet { name "vnet1" }
        let peering = vnetPeering {
            remote_vnet vnet1
            direction OneWayToRemote
            access AccessOnly
            transit UseRemoteGateway
        }
        let vnet2 = vnet { name "vnet2"; add_peering peering }
        let foundPeerings = arm { add_resources [vnet1;vnet2] } |> getPeeringResource |> List.filter (fun x -> x.Name.Contains("/peering-"))

        Expect.hasLength foundPeerings 1 "Incorrect peering count"
        Expect.equal foundPeerings.[0].RemoteVirtualNetwork.Id ((virtualNetworks.resourceId (ResourceName "vnet1")).ArmExpression.Eval()) "remote VNet incorrect"
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
        let vnet2 = vnet { name "vnet2"; add_peering peering }
        let foundPeerings = arm { add_resources [vnet1;vnet2] } |> getPeeringResource |> List.filter (fun x -> x.Name.Contains("/peering-"))

        Expect.hasLength foundPeerings 1 "Incorrect peering count"
        Expect.equal foundPeerings.[0].RemoteVirtualNetwork.Id ((virtualNetworks.resourceId (ResourceName "vnet2")).ArmExpression.Eval()) "remote VNet incorrect"
        Expect.equal foundPeerings.[0].AllowVirtualNetworkAccess (Nullable true) "incorrect network access"
        Expect.equal foundPeerings.[0].AllowForwardedTraffic (Nullable false) "incorrect forwarding"
        Expect.equal foundPeerings.[0].AllowGatewayTransit (Nullable true) "incorrect transit"
        Expect.equal foundPeerings.[0].UseRemoteGateways (Nullable false) "incorrect gateway"
    }
    test "Automatically carved subnets with network security group support" {
        let webPolicy = securityRule {
            name "web-servers"
            description "Public web server access"
            services [ "http", 80
                       "https", 443 ]
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
                            network_security_group myNsg.Name
                        }
                        subnetSpec {
                            name appsSubnet
                            size 24
                            network_security_group myNsg.Name
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
        let dependencies = jobj.SelectToken "resources[?(@.type=='Microsoft.Network/virtualNetworks')].dependsOn" :?> Newtonsoft.Json.Linq.JArray
        Expect.isNotNull dependencies "vnet missing dependency for nsg"
        Expect.hasLength dependencies 1 "Incorrect number of dependencies for vnet"
        Expect.equal (dependencies.[0].ToString()) "[resourceId('Microsoft.Network/networkSecurityGroups', 'my-nsg')]" "Incorrect vnet dependencies"

        let vnet = template |> getVnetResource
        Expect.isNotNull vnet.Subnets.[0].NetworkSecurityGroup "First subnet missing NSG"
        Expect.equal vnet.Subnets.[0].NetworkSecurityGroup.Id "[resourceId('Microsoft.Network/networkSecurityGroups', 'my-nsg')]" "Incorrect security group for first subnet"
        Expect.isNotNull vnet.Subnets.[0].NetworkSecurityGroup "Second subnet missing NSG"
        Expect.equal vnet.Subnets.[1].NetworkSecurityGroup.Id "[resourceId('Microsoft.Network/networkSecurityGroups', 'my-nsg')]" "Incorrect security group for second subnet"
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
                            link_to_network_security_group "my-nsg"
                        }
                    ]
                }
            ]
        }
        let template = arm { add_resources [ myNet ] }
        let jobj = template.Template |> Writer.toJson |> Newtonsoft.Json.Linq.JObject.Parse
        let dependencies = jobj.SelectToken "resources[?(@.type=='Microsoft.Network/virtualNetworks')].dependsOn" :?> Newtonsoft.Json.Linq.JArray
        Expect.hasLength dependencies 0 "Should be no vnet dependencies when linking to nsg"

        let vnet = template |> getVnetResource
        Expect.isNotNull vnet.Subnets.[0].NetworkSecurityGroup "Subnet missing NSG"
        Expect.equal vnet.Subnets.[0].NetworkSecurityGroup.Id "[resourceId('Microsoft.Network/networkSecurityGroups', 'my-nsg')]" "Incorrect security group for subnet"
    }
]