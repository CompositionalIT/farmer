open Farmer
open Farmer.Builders
open Farmer.VirtualNetworkGateway

let devVn = vnet {
    name "dev-isaac-vnet"
    add_address_spaces [ "10.0.0.0/16" ]
    add_subnets [
        subnet { name "GatewaySubnet"; prefix "10.0.0.0/24" }
        subnet { name "Frontend"; prefix "10.0.2.0/24" }
    ]
}

let developmentEnvironment = vm {
    name "test-isaac"
    username "isaac"
    no_public_ip
    // link_to_vnet devVn
    // vnet_subnet_name "Frontend"
}

// let gw = gateway {
//     name "dev-isaac-gateway"
//     vnet devVn
//     vpn_gateway_sku VpnGatewaySku.Basic
// }

let deployment = arm {
    add_resource developmentEnvironment
    // add_resource gw
    // add_resource devVn
}

deployment
|> Writer.quickWrite "generated-template"

deployment
|> Deploy.execute "my-resource-group-test" [ "password-for-test-isaac", "PQow01**"]
|> printfn "%A"
