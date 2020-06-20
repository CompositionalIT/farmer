open Farmer
open Farmer.Builders

//TODO: Create resources here!
let privateNet = vnet {
    name "private-vnet"
    add_address_spaces [
        "10.30.0.0/16"
    ]
    add_subnets [
        (*
        create_subnet "containersubnet" "10.30.12.0/24"
        carve_subnet "containersubnet" "/24"
        carve_subnet "vnet" "/24"
        carve_subnet "dbs" "/24"
        carve_subnet "gateways" "/29"
        *)
        subnet { // vnet __.Run - this runs when assigning the private net so could do this validation
            name "ContainerSubnet"
            prefix "10.30.19.0/28"
            add_delegations [
                SubnetDelegationService.ContainerGroups
            ]
        }
        subnet { // vnet __.Run - this runs when assigning the private net so could do this validation
            name "vms"
            prefix "10.30.18.0/24"
        }
        subnet { // vnet __.Run - this runs when assigning the private net so could do this validation
            name "databases"
            prefix "10.30.22.0/24"
        }
    ]
}

let aciProfile = networkProfile {
    name "vnet-aci-profile"
    //vnet privateNet <- inside the CE, we could check that the subnets are within this vnet
    vnet "private-vnet"
    //subnet containerSubnet <- can be referenced instead of going by name
    subnet "ContainerSubnet"
}

let myContainer = container {
    name "helloworld"
    image "microsoft/aci-helloworld"
    network_profile "vnet-aci-profile" // same thing here, could reference object instead of string
    //link_to_vnet privateNet containerSubnet
    ports [ 80us ]
    private_static_ip "10.30.19.4" [TCP, 80us]
}

open Farmer.VirtualNetworkGateway

let gw = gateway {
    name "er-gateway"
    //gateway_vpn VpnGatewaySku.VpnGw1
    //gateway_er ErGatewaySku.HighPerformance
    vpn_gateway_sku VpnGatewaySku.VpnGw1
    er_gateway_sku ErGatewaySku.Standard
    vpn_type VpnType.RouteBased
    vnet "my-vnet"
    gateway_ip_config DynamicPrivateIp "gw-pip"
    // active_active_ip_config DynamicPrivateIp "gw-pip2"
}


let deployment = arm {
    location Location.WestEurope

    //TODO: Assign resources here using the add_resource keyword
    add_resource privateNet
    add_resource aciProfile
    add_resource myContainer
    add_resource gw
}

// Generate the ARM template here...
deployment
|> Writer.quickWrite @"generated-template"

// Or deploy it directly to Azure here... (required Azure CLI installed!)
// deployment
// |> Deploy.execute "my-resource-group" Deploy.NoParameters