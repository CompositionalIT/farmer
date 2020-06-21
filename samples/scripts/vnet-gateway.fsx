
#r @"./libs/Newtonsoft.Json.dll"
#r @"../../src/Farmer/bin/Debug/netstandard2.0/Farmer.dll"

open Farmer
open Farmer.Builders
open Farmer.VirtualNetworkGateway

let privateNet = vnet {
    name "my-vnet"
    add_address_spaces [
        "10.30.0.0/16"
    ]
    add_subnets [
        subnet {
            name "GatewaySubnet"
            prefix "10.30.254.0/28"
        }
    ]
}

let gatewayIp = {
    Name = ResourceName "gw-pip"
    Location = Location.NorthEurope
    DomainNameLabel = None
}

let myGateway = gateway {
    name "er-gateway"
    er_gateway_sku ErGatewaySku.Standard
    vpn_type VpnType.RouteBased
    vnet "my-vnet"
    gateway_ip_config DynamicPrivateIp gatewayIp.Name.Value
}

let deployment = arm {
    location Location.NorthEurope
    add_resource privateNet
    add_resource myGateway
    add_resource gatewayIp
}

deployment
|> Deploy.whatIf "FarmerTest" Deploy.NoParameters
|> printfn "%A"
