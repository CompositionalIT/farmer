module Bastion

open System
open Expecto
open Farmer
open Farmer.ContainerGroup
open Farmer.Builders
open Microsoft.Azure.Management.Network
open Microsoft.Azure.Management.Network.Models
open Microsoft.Rest

/// Client instance needed to get the serializer settings.
let client = new NetworkManagementClient(Uri "http://management.azure.com", TokenCredentials "NotNullOrWhiteSpace")

let tests = testList "Bastion Host" [
    test "Bastion host is attached to subnet named AzureBastionSubnet" {

        let resources =
            arm {
                location Location.EastUS
                add_resources [
                    bastion {
                        name "my-bastion-host"
                        vnet "private-network"
                    }
                    vnet {
                        name "private-network"
                        add_address_spaces [
                            "10.1.0.0/16"
                        ]
                        add_subnets [
                            subnet {
                                name "default"
                                prefix "10.1.0.0/24"
                            }
                            subnet {
                                name "AzureBastionSubnet"
                                prefix "10.1.250.0/27"
                            }
                        ]
                    }
                ]
            } |> findAzureResources<BastionHost> client.SerializationSettings
            |> Array.ofList
            
        Expect.equal resources.[1].Name "my-bastion-host" "Account name is wrong"
        Expect.equal resources.[1].IpConfigurations.[0].Subnet.Id "[resourceId('Microsoft.Network/virtualNetworks/subnets', 'private-network', 'AzureBastionSubnet')]" "Subnet name must be 'AzureBastionSubnet'"
    }
]