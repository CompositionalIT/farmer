module ExpressRoute

open Expecto
open Farmer
open Farmer.Builders
open Farmer.ExpressRoute
open Microsoft.Azure.Management.Network
open Microsoft.Azure.Management.Network.Models
open Microsoft.Rest
open Newtonsoft.Json.Linq
open System

let client =
    new NetworkManagementClient(Uri "http://management.azure.com", TokenCredentials "NotNullOrWhiteSpace")

let tests =
    testList "ExpressRoute" [
        test "Can create a basic ExR" {
            let resource =
                let er = expressRoute {
                    name "my-circuit"
                    service_provider "My ISP"
                    peering_location "My ISP's Location"
                }

                arm { add_resource er }
                |> findAzureResources<ExpressRouteCircuit> client.SerializationSettings
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

        test "Can create an ExR with one private peering and one authorization" {
            let er = expressRoute {
                name "my-circuit"
                service_provider "My ISP"
                peering_location "My ISP's Location"

                add_peerings [
                    peering {
                        azure_asn 65412
                        peer_asn 39917L
                        vlan 199
                        primary_prefix (IPAddressCidr.parse "10.99.250.0/30")
                        secondary_prefix (IPAddressCidr.parse "10.99.250.4/30")
                    }
                ]

                add_authorizations [ "myauth" ]
                enable_global_reach
            }

            let deployment = arm {
                add_resource er
                output "auth-key" (er.AuthorizationKey "myauth")
            }

            let circuit =
                deployment
                |> findAzureResources<ExpressRouteCircuit> client.SerializationSettings
                |> List.head

            Expect.hasLength circuit.Peerings 1 "Circuit has incorrect number of peerings"
            Expect.equal circuit.Peerings.[0].AzureASN (Nullable 65412) ""
            Expect.equal circuit.Peerings.[0].PeerASN (Nullable 39917L) ""
            Expect.equal circuit.Peerings.[0].VlanId (Nullable 199) ""
            Expect.equal circuit.Peerings.[0].PrimaryPeerAddressPrefix "10.99.250.0/30" ""

            let auth =
                deployment
                |> findAzureResources<ExpressRouteCircuitAuthorization> client.SerializationSettings
                |> List.item 1

            Expect.equal auth.Name "my-circuit/myauth" "Missing authorization in request"

            Expect.hasLength deployment.Outputs 1 "Missing deployment output for authorization key"

            Expect.equal
                deployment.Outputs.["auth-key"]
                "[reference(resourceId('Microsoft.Network/expressRouteCircuits/authorizations', 'my-circuit', 'myauth')).authorizationKey]"
                "Incorrect auth key reference"
        }

        test "Can create an ExR with global reach, premium tier, unlimited data" {
            let resource =
                let er = expressRoute {
                    name "my-circuit"
                    service_provider "My ISP"
                    peering_location "My ISP's Location"
                    tier Premium
                    family UnlimitedData

                    add_peerings [
                        peering {
                            azure_asn 65412
                            peer_asn 39917L
                            vlan 199
                            primary_prefix (IPAddressCidr.parse "10.99.250.0/30")
                            secondary_prefix (IPAddressCidr.parse "10.99.250.4/30")
                        }
                    ]

                    enable_global_reach
                }

                arm { add_resource er }
                |> findAzureResources<ExpressRouteCircuit> client.SerializationSettings
                |> List.head

            Expect.equal resource.Sku.Name "Premium_UnlimitedData" ""
            Expect.equal resource.Sku.Family "UnlimitedData" ""
            Expect.equal resource.Sku.Tier "Premium" ""
            Expect.equal resource.GlobalReachEnabled (Nullable true) ""
        }

        test "ExR service key output expression" {
            let er = expressRoute {
                name "my-circuit"
                service_provider "My ISP"
                peering_location "My ISP's Location"
            }

            let deployment = arm {
                add_resource er
                output "er-service-key" er.ServiceKey
            }

            let json = deployment.Template |> Writer.toJson
            let jobj = JObject.Parse(json)
            let serviceKey = jobj.SelectToken("outputs.er-service-key.value")

            Expect.equal
                serviceKey
                (JValue.CreateString
                    "[reference(resourceId('Microsoft.Network/expressRouteCircuits', 'my-circuit')).serviceKey]"
                :> JToken)
                "Incorrect expression generated for serviceKey"
        }
    ]