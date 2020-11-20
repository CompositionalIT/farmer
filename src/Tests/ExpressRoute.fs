module ExpressRoute

open Expecto
open Farmer
open Farmer.Builders
open Farmer.ExpressRoute
open Microsoft.Azure.Management.Network
open Microsoft.Azure.Management.Network.Models
open Microsoft.Rest
open System

let client = new NetworkManagementClient(Uri "http://management.azure.com", TokenCredentials "NotNullOrWhiteSpace")
let toAzResource (er:ExpressRouteConfig) =
    arm { add_resource er }
    |> findAzureResourcesByType<ExpressRouteCircuit> Arm.Network.expressRouteCircuits client.SerializationSettings
let tests = testList "ExpressRoute" [
    test "Can create a basic ExR" {
        let resource =
            expressRoute {
                name "my-circuit"
                service_provider "My ISP"
                peering_location "My ISP's Location"
            }
            |> toAzResource
            |> List.head

        Expect.equal resource.Name "my-circuit" ""
        Expect.equal resource.Sku.Name "Standard_MeteredData" ""
        Expect.equal resource.Sku.Family "MeteredData" ""
        Expect.equal resource.Sku.Tier "Standard" ""
        Expect.equal resource.ServiceProviderProperties.BandwidthInMbps (Nullable 50) ""
        Expect.equal resource.ServiceProviderProperties.ServiceProviderName "My ISP" ""
        Expect.equal resource.ServiceProviderProperties.PeeringLocation "My ISP's Location" ""
        Expect.equal resource.GlobalReachEnabled (Nullable false) ""
    }

    test "Can create an ExR with one private peering" {
        let resource =
            expressRoute {
                name "my-circuit"
                service_provider "My ISP"
                peering_location "My ISP's Location"
                add_peering (
                    peering {
                        azure_asn 65412
                        peer_asn 39917L
                        vlan 199
                        primary_prefix (IPAddressCidr.parse "10.99.250.0/30")
                        secondary_prefix (IPAddressCidr.parse "10.99.250.4/30")
                    }
                )
                enable_global_reach
            }
            |> toAzResource
            |> List.head

        Expect.equal resource.Peerings.[0].AzureASN (Nullable 65412) ""
        Expect.equal resource.Peerings.[0].PeerASN (Nullable 39917L) ""
        Expect.equal resource.Peerings.[0].VlanId (Nullable 199) ""
        Expect.equal resource.Peerings.[0].PrimaryPeerAddressPrefix "10.99.250.0/30" ""
    }

    test "Can create an ExR with global reach, premium tier, unlimited data" {
        let resource =
            expressRoute {
                name "my-circuit"
                service_provider "My ISP"
                peering_location "My ISP's Location"
                tier Premium
                family UnlimitedData
                add_peering (
                    peering {
                        azure_asn 65412
                        peer_asn 39917L
                        vlan 199
                        primary_prefix (IPAddressCidr.parse "10.99.250.0/30")
                        secondary_prefix (IPAddressCidr.parse "10.99.250.4/30")
                    }
                )
                enable_global_reach
            }
            |> toAzResource
            |> List.head

        Expect.equal resource.Sku.Name "Premium_UnlimitedData" ""
        Expect.equal resource.Sku.Family "UnlimitedData" ""
        Expect.equal resource.Sku.Tier "Premium" ""
        Expect.equal resource.GlobalReachEnabled (Nullable true) ""
    }
]
