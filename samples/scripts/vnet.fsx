#r @"./libs/Newtonsoft.Json.dll"
#r @"../../src/Farmer/bin/Debug/netstandard2.0/Farmer.dll"

open Farmer
open Farmer.Builders

let privateNet = vnet {
    name "my-vnet"
    build_address_spaces [
        address_space {
            space "10.28.0.0/16"
            subnets [
                build_subnet "vms" 26
                build_subnet "services" 24
                build_subnet "corporate-west" 18
                build_subnet "corporate-east" 18
                build_subnet "GatewaySubnet" 29
                build_subnet_delegations "containers" 27 [ SubnetDelegationService.ContainerGroups ]
            ]                
        }
        address_space {
            space "10.30.0.0/16"
            subnets [
                build_subnet "stuff" 23
                build_subnet "more-stuff" 28
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

