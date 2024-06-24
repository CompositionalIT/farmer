#r "nuget: Farmer"

open Farmer

open Farmer.NetworkSecurity
open Farmer.Identity
open Farmer.Network
open Farmer.Builders
open Farmer.ApplicationGateway
open Farmer.Arm.Network
open Farmer.Arm.ApplicationGateway


let gwPolicy = securityRule {
    name "app-gw"
    description "GatewayManager"
    services [ NetworkService("GatewayManager", Range(65200us, 65535us)) ]
    add_source_tag NetworkSecurity.TCP "GatewayManager"
    add_destination_any
}

let gwInternetPolicy = securityRule {
    name "inet-gw"
    description "Internet to gateway"
    services [ "http", 80 ]
    add_source_tag NetworkSecurity.TCP "Internet"
    add_destination_network "10.28.0.0/24"
}

let appPolicy = securityRule {
    name "app-servers"
    description "Internal app server access"
    services [ "http", 80 ]
    add_source_network NetworkSecurity.TCP "10.28.0.0/24"
    add_destination_network "10.28.1.0/24"
}

let myNsg = nsg {
    name "agw-nsg"
    add_rules [ gwPolicy; gwInternetPolicy; appPolicy ]
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
                    add_delegations [ SubnetDelegationService.ContainerGroups ]
                }
                subnetSpec {
                    name "no-nsg"
                    size 24
                }
            ]
        }
    ]
}

let msi = createUserAssignedIdentity "agw-msi"

let backendPoolName = ResourceName "agw-be-pool"

let myAppGateway =
    let gwIp = gatewayIp {
        name "app-gw-ip"
        link_to_subnet net.Name net.Subnets.[0].Name
    }

    let frontendIp = frontendIp {
        name "app-gw-fe-ip"
        public_ip "agp-gw-pip"
    }

    let frontendPort = frontendPort {
        name "port-80"
        port 80
    }

    let listener = httpListener {
        name "http-listener"
        frontend_ip frontendIp
        frontend_port frontendPort
        backend_pool backendPoolName.Value
    }

    let backendPool = appGatewayBackendAddressPool {
        name backendPoolName.Value
        add_backend_addresses [ backend_ip_address "10.28.1.4"; backend_ip_address "10.28.1.5" ]
    }

    let healthProbe = appGatewayProbe {
        name "agw-probe"
        host "localhost"
        path "/"
        port 80
        protocol Protocol.Http
    }

    let backendSettings = backendHttpSettings {
        name "bp-default-web-80-web-80"
        port 80
        probe healthProbe
        protocol Protocol.Http
        request_timeout 10<Seconds>
    }

    let routingRule = basicRequestRoutingRule {
        name "web-front-to-aci-back"
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

arm {
    location Location.EastUS

    add_resources [
        msi
        net
        myNsg
        myAppGateway
        networkProfile {
            name "hello-aci-netprofile"
            vnet net.Name.Value
            subnet net.Subnets.[1].Name.Value
        }
        containerGroup {
            name "hello-aci1"

            add_instances [
                containerInstance {
                    name "hello"
                    image "mcr.microsoft.com/azuredocs/aci-helloworld"
                    add_internal_ports [ 80us ]
                }
            ]

            network_profile "hello-aci-netprofile"
        }
        containerGroup {
            name "hello-aci2"

            add_instances [
                containerInstance {
                    name "hello"
                    image "mcr.microsoft.com/azuredocs/aci-helloworld"
                    add_internal_ports [ 80us ]
                }
            ]

            network_profile "hello-aci-netprofile"
        }
    ]
}
|> Writer.quickWrite "applicationGateway"