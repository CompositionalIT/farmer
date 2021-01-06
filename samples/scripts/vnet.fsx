#r @"./libs/Newtonsoft.Json.dll"
#r @"../../src/Farmer/bin/Debug/netstandard2.0/Farmer.dll"

open Farmer
open Farmer.Builders

let privateNet = vnet {
    name "my-vnet"
    build_address_spaces [
        addressSpace {
            space "10.28.0.0/16"
            subnets [
                buildSubnet "vms" 26
                buildSubnet "services" 24
                buildSubnet "corporate-west" 18
                buildSubnet "corporate-east" 18
                buildSubnet "GatewaySubnet" 29
                buildSubnetDelegations "containers" 27 [ SubnetDelegationService.ContainerGroups ]
            ]
        }
        addressSpace {
            space "10.30.0.0/16"
            subnets [
                buildSubnet "stuff" 23
                buildSubnet "more-stuff" 28
            ]
        }
    ]
}

let deployment = arm {
    location Location.NorthEurope
    add_resource privateNet
}

deployment
|> Writer.quickWrite "output"

