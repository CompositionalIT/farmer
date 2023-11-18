module Bastion

open System
open Expecto
open Farmer
open Farmer.ContainerGroup
open Farmer.Builders
open Microsoft.Azure.Management.Network
open Microsoft.Azure.Management.Network.Models
open Microsoft.Rest
open Newtonsoft.Json.Linq

/// Client instance needed to get the serializer settings.
let client =
    new NetworkManagementClient(Uri "http://management.azure.com", TokenCredentials "NotNullOrWhiteSpace")

let tests =
    testList
        "Bastion Host"
        [
            test "Bastion host is attached to subnet named AzureBastionSubnet" {

                let resources =
                    arm {
                        location Location.EastUS

                        add_resources
                            [
                                bastion {
                                    name "my-bastion-host"
                                    vnet "private-network"
                                }
                                vnet {
                                    name "private-network"
                                    add_address_spaces [ "10.1.0.0/16" ]

                                    add_subnets
                                        [
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
                    }
                    |> findAzureResources<BastionHost> client.SerializationSettings
                    |> Array.ofList

                Expect.equal resources.[1].Name "my-bastion-host" "Account name is wrong"

                Expect.equal
                    resources.[1].IpConfigurations.[0].Subnet.Id
                    "[resourceId('Microsoft.Network/virtualNetworks/subnets', 'private-network', 'AzureBastionSubnet')]"
                    "Subnet name must be 'AzureBastionSubnet'"
            }
            test "Advanced settings upgrade to standard sku" {
                let deployment =
                    arm {
                        location Location.EastUS

                        add_resources
                            [
                                bastion {
                                    name "my-bastion"
                                    link_to_vnet "my-vnet"
                                    enable_shareable_link true
                                    enable_tunneling true
                                }
                            ]
                    }

                let jobj = deployment.Template |> Writer.toJson |> JToken.Parse
                let bastion = jobj.SelectToken "resources[?(@.name == 'my-bastion')]"
                Expect.isNotNull bastion "Bastion missing from template"

                Expect.equal
                    (bastion.SelectToken "sku.name" |> string)
                    "Standard"
                    "Bastion SKU should be upgraded to Standard"

                Expect.isTrue
                    ((bastion.SelectToken "properties.enableShareableLink").ToObject<bool>())
                    "Bastion should have shareable link enabled"

                Expect.isTrue
                    ((bastion.SelectToken "properties.enableTunneling").ToObject<bool>())
                    "Bastion should have tunneling enabled"

                Expect.isTrue
                    ((bastion.SelectToken "properties.enableTunneling").ToObject<bool>())
                    "Bastion should have tunneling enabled"

                Expect.equal
                    ((bastion.SelectToken "properties.scaleUnits").ToObject<int>())
                    2
                    "Bastion should specify 2 scale units by default"
            }
        ]
