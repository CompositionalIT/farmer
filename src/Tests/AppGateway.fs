module AppGateway

open Expecto
open Microsoft.Azure.Management.Network
open Microsoft.Rest
open System
open Farmer
open Farmer.ApplicationGateway
open Farmer.Builders

let client = new NetworkManagementClient(Uri "http://management.azure.com", TokenCredentials "NotNullOrWhiteSpace")

let tests = testList "Application Gateway Tests" [
    test "Empty basic app gateway" {
        let ag =
            appGateway {
                name "ag"
            }
        ()
        // let resource =
        //     arm { add_resource ag }
        //         |> findAzureResources<Microsoft.Azure.Management.Network.Models.LoadBalancer> client.SerializationSettings
        //         |> List.head
        // Expect.equal resource.Name "ag" "Name did not match"
    }

    test "Complex App gateway" {
        let ag =
            appGateway {
                name "ag"
                sku Sku.Standard_v2
                tier Tier.Standard_v2
                sku_capacity 1
                add_ip_configs [
                    gatewayIp {
                        name "gatewayIpConfig"
                        link_to_subnet "my-subnet"
                    }
                ]
                add_frontends [
                    frontendIp {
                        name "my-frontend-ip"
                        private_ip_allocation_method (PrivateIpAddress.StaticPrivateIp (System.Net.IPAddress.Parse "10.0.0.1")) // ??
                        link_to_public_ip "my-pip"
                    }
                ]
                add_backend_address_pools [
                    appGatewayBackendAddressPool {
                        name "my-backend-address-pool"
                        application_gateway "ag"
                        add_backend_addresses [
                            backendAddress {
                                fqdn "test"
                                ip_address "10.0.0.1"
                            }
                        ]
                    }
                ]
                add_backend_https_settings_collection [
                    backendHttpSettings {
                        name "my-backend-http-settings"
                        affinity_cookie_name "my-cookie"
                        add_auth_certs [
                            "my-auth-cert"
                        ]
                        connection_draining (connectionDraining {
                            drain_timeout 500<Seconds>
                            enabled true
                        })
                        cookie_based_affinity FeatureFlag.Enabled
                        host_name "my-host-name"
                        path "my-path"
                        port (uint16 80)
                        protocol Protocol.Https
                        pick_host_name_from_backend_address true
                        request_timeout 500<Seconds>
                        probe "my-probe"
                        probe_enabled true
                        trusted_root_certs [ 
                            "my-root-cert" 
                        ]
                    }
                ]
            }
        ()
        // let resource =
        //     arm { add_resource ag }
        //         |> findAzureResources<Microsoft.Azure.Management.Network.Models.LoadBalancer> client.SerializationSettings
        //         |> List.head
        // Expect.equal resource.Name "ag" "Name did not match"
    }

]