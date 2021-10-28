module AppGateway

open Expecto
open Microsoft.Azure.Management.Network
open Microsoft.Rest
open System
open Farmer
open Farmer.ApplicationGateway
open Farmer.Builders
open Farmer.Network
open Farmer.NetworkSecurity

let client = new NetworkManagementClient(Uri "http://management.azure.com", TokenCredentials "NotNullOrWhiteSpace")

let tests = testList "Application Gateway Tests" [
    test "Empty basic app gateway" {
        let ag =
            appGateway {
                name "ag"
            }
        ()
        let resource =
             arm { add_resource ag }
                 |> findAzureResources<Microsoft.Azure.Management.Network.Models.ApplicationGateway> client.SerializationSettings
                 |> List.head
        Expect.equal resource.Name "ag" "Name did not match"
    }

    test "Complex App gateway" {
        let myNsg = nsg {
            name "agw-nsg"
            add_rules [
                securityRule {
                    name "app-gw"
                    description "GatewayManager"
                    services [ NetworkService ("GatewayManager", Range (65200us,65535us)) ]
                    add_source_tag NetworkSecurity.TCP "GatewayManager"
                    add_destination_any
                }
                securityRule {
                    name "inet-gw"
                    description "Internet to gateway"
                    services [ "http", 80 ]
                    add_source_tag NetworkSecurity.TCP "Internet"
                    add_destination_network "10.28.0.0/24"
                }
                securityRule {
                    name "app-servers"
                    description "Internal app server access"
                    services [ "http", 80 ]
                    add_source_network NetworkSecurity.TCP "10.28.0.0/24"
                    add_destination_network "10.28.1.0/24"
                }
            ]
        }

        let net = vnet {
            name "agw-vnet"
            build_address_spaces [
                addressSpace {
                    space "10.28.0.0/16"
                    subnets [
                        subnetSpec {
                            name "gw"
                            size 24
                            network_security_group myNsg
                        }
                        subnetSpec {
                            name "apps"
                            size 24
                            network_security_group myNsg
                            add_delegations [
                                SubnetDelegationService.ContainerGroups
                            ]
                        }
                    ]
                }
            ]
        }
        let msi = createUserAssignedIdentity "agw-msi"

        let backendPoolName = ResourceName "agw-be-pool"
        let myAppGateway =
            let gwIp =
                gatewayIp {
                    name "app-gw-ip"
                    link_to_subnet net.Name net.Subnets.[0].Name
                }
            let frontendIp =
                frontendIp {
                    name "app-gw-fe-ip"
                    public_ip "agp-gw-pip"
                }
            let frontendPort =
                frontendPort {
                    name "port-80"
                    port 80
                }
            let listener =
                httpListener {
                    name "http-listener"
                    frontend_ip frontendIp
                    frontend_port frontendPort
                    backend_pool backendPoolName.Value
                }
            let backendPool =
                appGatewayBackendAddressPool {
                    name backendPoolName.Value
                    add_backend_addresses [
                        backend_ip_address "10.28.1.4"
                        backend_ip_address "10.28.1.5"
                    ]
                }
            let healthProbe =
                appGatewayProbe {
                    name "agw-probe"
                    host "localhost"
                    path "/"
                    port 80
                    protocol Protocol.Http
                }
            let backendSettings =
                backendHttpSettings {
                    name "bp-default-web-80-web-80"
                    port 80
                    probe healthProbe
                    protocol Protocol.Http
                    request_timeout 10<Seconds>
                }
            let routingRule =
                basicRequestRoutingRule {
                    name "web-front-to-services-back"
                    http_listener listener
                    backend_address_pool backendPool
                    backend_http_settings backendSettings
                }
            
            appGateway {
                name "app-gw"
                sku_capacity 2
                add_identity msi
                add_ip_configs [ gwIp ]
                add_frontends [ frontendIp ]
                add_frontend_ports [ frontendPort ]
                add_http_listeners [ listener ]
                add_backend_address_pools [ backendPool ]
                add_backend_http_settings_collection [ backendSettings ]
                add_request_routing_rules [ routingRule ]
                add_probes [ healthProbe ]
                depends_on myNsg
                depends_on net
           }

        let deployment =
            arm {
                location Location.EastUS
                add_resources [
                    msi
                    net
                    myNsg
                    myAppGateway
                ]
            }
        deployment.Template |> Writer.toJson |> ignore
        let resource =
            deployment
            |> findAzureResources<Microsoft.Azure.Management.Network.Models.ApplicationGateway> client.SerializationSettings
            |> List.item 3
        Expect.equal resource.Name "app-gw" "Name did not match"
        Expect.hasLength resource.BackendAddressPools 1 "Expecting 1 backend address pool"
        let backendPool = resource.BackendAddressPools.[0]
        Expect.equal backendPool.Name "agw-be-pool" "Backend address pool name did not match"
        Expect.hasLength backendPool.BackendAddresses 2 "Incorrect number of addresses in backend pool"
        Expect.equal backendPool.BackendAddresses.[0].IpAddress "10.28.1.4" "Backend address pool has incorrect IP in pool - item 1"
        Expect.equal backendPool.BackendAddresses.[1].IpAddress "10.28.1.5" "Backend address pool has incorrect IP in pool - item 2"
        Expect.hasLength resource.BackendHttpSettingsCollection 1 "Expecting 1 backend http setting"
        let backendSettings = resource.BackendHttpSettingsCollection.[0]
        Expect.equal backendSettings.Name "bp-default-web-80-web-80" "Backend http settings name did not match"
        Expect.hasLength resource.FrontendPorts 1 "Expecting 1 frontend port"
        let feport = resource.FrontendPorts.[0]
        Expect.equal feport.Name "port-80" "Frontend port name did not match"
        Expect.equal feport.Port (Nullable 80) "Frontend port value did not match"
        Expect.hasLength resource.FrontendIPConfigurations 1 "Expecting 1 frontend IP config"
        let feipconf = resource.FrontendIPConfigurations.[0]
        Expect.equal feipconf.Name "app-gw-fe-ip" "Frontend IP config name did not match"
        Expect.hasLength resource.GatewayIPConfigurations 1 "Expecting 1 gateway IP"
        let gwipconf = resource.GatewayIPConfigurations.[0]
        Expect.equal gwipconf.Name "app-gw-ip" "Gateway IP subnet ID did not match"
        Expect.equal gwipconf.Subnet.Id "[resourceId('Microsoft.Network/virtualNetworks/subnets', 'agw-vnet', 'gw')]" "Gateway IP subnet ID did not match"
        Expect.hasLength resource.HttpListeners 1 "Expecting 1 http listener"
        let httpListener = resource.HttpListeners.[0]
        Expect.equal httpListener.Name "http-listener" "Listener name did not match"
        Expect.equal httpListener.FrontendPort.Id "[resourceId('Microsoft.Network/applicationGateways/frontendPorts', 'app-gw', 'port-80')]" "Listener port did not match"
        Expect.equal httpListener.FrontendIPConfiguration.Id "[resourceId('Microsoft.Network/applicationGateways/frontendIPConfigurations', 'app-gw', 'app-gw-fe-ip')]" "Listener ipconfig did not match"
        Expect.hasLength resource.Probes 1 "Expecting 1 health probe"
        let probe = resource.Probes.[0]
        Expect.equal probe.Name "agw-probe" "Probe name did not match"
        Expect.equal probe.Host "localhost" "Probe host did not match"
        Expect.equal probe.Port (Nullable 80) "Probe port did not match"
        Expect.equal probe.Path "/" "Probe path did not match"
        Expect.hasLength resource.RequestRoutingRules 1 "Expecting 1 request routing rule"
        let routingRule = resource.RequestRoutingRules.[0]
        Expect.equal routingRule.Name "web-front-to-services-back" "Routing rule name did not match"
        Expect.equal routingRule.HttpListener.Id "[resourceId('Microsoft.Network/applicationGateways/httpListeners', 'app-gw', 'http-listener')]" "Routing rule listener id did not match"
        Expect.equal routingRule.BackendAddressPool.Id "[resourceId('Microsoft.Network/applicationGateways/backendAddressPools', 'app-gw', 'agw-be-pool')]" "Routing rule pool did not match"
        Expect.equal routingRule.BackendHttpSettings.Id "[resourceId('Microsoft.Network/applicationGateways/backendHttpSettingsCollection', 'app-gw', 'bp-default-web-80-web-80')]" "Routing rule http settings did not match"
    }
]