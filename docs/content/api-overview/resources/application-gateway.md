---
title: "Application Gateway"
date: 2021-10-23T10:47:02-05:00
chapter: false
weight: 1
---

#### Overview
The Application Gateway builder is used to create Application Gateways.

* Application Gateways (`Microsoft.Network/applicationGateways`)

#### Application Gateway Builder Keywords
The Application Gateway builder (`appGateway`) constructs Application Gateways.

| Keyword                              | Purpose                                                                                                                   |
|--------------------------------------|---------------------------------------------------------------------------------------------------------------------------|
| name                                 | Sets the name of the Application Gateway.                                                                                 |
| sku_capacity                         | Sets the capacity for this SKU of Application Gateway.                                                                    |
| add_identity                         | Assigns a managed identity to the Application Gateway.                                                                    |
| add_ip_configs                       | Assigns one or more gateway IP configuration for the subnet where it should be created.                                   |
| add_frontends                        | Assigns one or more frontend IP configuration for a public or private IP for the services accessible through the gateway. |
| add_frontend_ports                   | Assigns one or more frontend ports to listen                                                                              |
| add_http_listeners                   | Assigns one or more http listeners.                                                                                       |
| add_backend_address_pools            | Assigns one or more backend pools.                                                                                        |
| add_backend_http_settings_collection | Assigns HTTP settings for the listener.                                                                                   |
| add_request_routing_rules            | Assigns routing rules between frontend IP configurations and ports and services in the backend pool.                      |
| add_probes                           | Assigns health probes to ensure backend services are healthy or removed from the pool.                                    |
| add_ssl_certificates                 | Assigns one or more SSL certificates to the App Gateway for use in httpListeners.                                         |

#### Complete Example

This example creates an application gateway frontend with an NSG and a backend virtual network where application services would be running.

```fsharp
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
            services [ "https", 443 ]
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
            name "port-443"
            port 443
        }
    let listener =
        httpListener {
            name "https-listener"
            frontend_ip frontendIp
            frontend_port frontendPort
            backend_pool backendPoolName.Value
            protocol Protocol.Https
            ssl_certificate "my-tls-cert"
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
        add_ssl_certificates [
            sslCertificate {
                name "my-tls-cert"
                // Ensure App Gateway identity (MSI) has access to read this secret.
                key_vault_secret_id "https://my-kv.vault.azure.net/secrets/app-gw-cert"
            }
        ]
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
    ]
}
```