module Network

open Expecto
open Farmer
open Farmer.Builders
open Farmer.Network
open Microsoft.Rest
open System

let netClient = new Microsoft.Azure.Management.Network.NetworkManagementClient(Uri "http://management.azure.com", TokenCredentials "NotNullOrWhiteSpace")
let getVnetResource = findAzureResources<Microsoft.Azure.Management.Network.Models.VirtualNetwork> netClient.SerializationSettings >> List.head

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
    }
]