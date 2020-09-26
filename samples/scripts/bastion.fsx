#r @"./libs/Newtonsoft.Json.dll"
#r @"../../src/Farmer/bin/Debug/netstandard2.0/Farmer.dll"

open Farmer
open Farmer.Builders

arm {
    location Location.EastUS
    add_resources [
        vnet {
            name "private-network"
            add_address_spaces [
                "10.1.0.0/16"
            ]
            add_subnets [
                subnet {
                    name "default"
                    prefix "10.1.0.0/24"
                }
                subnet {
                    name "AzureBastionSubnet"
                    prefix "10.1.250.0/27"
                }
            ]
        }
        bastion {
            name "my-bastion-host"
            vnet "private-network"
        }
    ]
} |> Writer.quickWrite "bastion"