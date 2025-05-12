module PrivateLink

open Expecto
open Farmer
open Farmer.Builders
open Newtonsoft.Json.Linq

let tests =
    testList "Private Link" [
        test "Creates a private link service for a load balancer" {
            let vnet = vnet {
                name "private-net"
                add_address_spaces [ "10.100.0.0/16" ]

                add_subnets [
                    subnet {
                        name "default"
                        prefix "10.100.0.0/24"
                    }
                    subnet {
                        name "backend-services"
                        prefix "10.100.1.0/24"
                    }
                    subnet {
                        name "private-endpoints"
                        prefix "10.100.255.0/24"
                        private_link_service_network_policies Disabled
                    }
                ]
            }

            let lb = loadBalancer {
                name "lb"
                sku LoadBalancer.Sku.Standard
                depends_on vnet

                add_frontends [
                    frontend {
                        name "lb-frontend"
                        private_ip_allocation_method AllocationMethod.DynamicPrivateIp

                        link_to_subnet (
                            ResourceId.create (Farmer.Arm.Network.subnets, vnet.Name, ResourceName "default")
                        )
                    }
                ]

                add_backend_pools [ backendAddressPool { name "lb-backend" } ]

                add_probes [
                    loadBalancerProbe {
                        name "httpGet"
                        protocol Farmer.LoadBalancer.LoadBalancerProbeProtocol.HTTP
                        port 80
                        request_path "/"
                    }
                ]

                add_rules [
                    loadBalancingRule {
                        name "rule1"
                        frontend_ip_config "lb-frontend"
                        backend_address_pool "lb-backend"
                        frontend_port 80
                        backend_port 80
                        protocol TransmissionProtocol.TCP
                        probe "httpGet"
                    }
                ]
            }

            let pls = privateLink {
                name "pls"
                depends_on lb
                add_auto_approved_subscriptions [ System.Guid.NewGuid() ]

                add_load_balancer_frontend_ids [
                    ResourceId.create (
                        Farmer.Arm.LoadBalancer.loadBalancerFrontendIPConfigurations,
                        lb.Name,
                        ResourceName "lb-frontend"
                    )
                ]

                add_ip_configs [
                    privateLinkIpConfig {
                        link_to_subnet (
                            ResourceId.create (Farmer.Arm.Network.subnets, vnet.Name, ResourceName "private-endpoints")
                        )
                    }
                ]
            }

            let deployment = arm { add_resources [ lb; vnet; pls ] }
            let json = deployment.Template |> Writer.toJson |> JToken.Parse
            let privateLinkProps = json.SelectToken("resources[?(@.name=='pls')].properties")
            let ipconfigs = privateLinkProps.SelectToken("ipConfigurations") :?> JArray
            let ipconfig = ipconfigs.[0]
            Expect.equal (string ipconfig.["name"]) "private-net-private-endpoints" "Incorrect name for ipconfig"
            let ipconfigProps = ipconfig.["properties"]
            Expect.equal ipconfigProps.["primary"] (JValue false) "Incorrect value for ipconfig.properties.primary"

            Expect.equal
                ipconfigProps.["privateIPAddressVersion"]
                (JValue "IPv4")
                "Incorrect value for ipconfig.properties.privateIPAddressVersion"

            Expect.equal
                ipconfigProps.["privateIPAllocationMethod"]
                (JValue "Dynamic")
                "Incorrect value for ipconfig.properties.privateIPAllocationMethod"

            Expect.equal
                ipconfigProps.["subnet"].["id"]
                (JValue "[resourceId('Microsoft.Network/virtualNetworks/subnets', 'private-net', 'private-endpoints')]")
                "Incorrect value for ipconfig.properties.subnet.id"

            Expect.hasLength ipconfigs 1 "Incorrect number of ip configurations"

            let frontendIpConfigs =
                privateLinkProps.SelectToken("loadBalancerFrontendIpConfigurations") :?> JArray

            Expect.hasLength frontendIpConfigs 1 "Incorrect number of lb frontend ip configurations"
            let frontendIpConfigId = frontendIpConfigs.[0].["id"]

            Expect.equal
                frontendIpConfigId
                (JValue "[resourceId('Microsoft.Network/loadBalancers/frontendIPConfigurations', 'lb', 'lb-frontend')]")
                "Incorrect expression for frontend IP config"
        }
    ]