#r @"..\..\src\Farmer\bin\Debug\net5.0\Farmer.dll"

open Farmer
open Farmer.Builders

let hub =
    vnet {
        name "vnet-hub"

        build_address_spaces [ addressSpace {
                                   space "10.0.0.0/16"
                                   build_subnet "management" 24
                                   build_subnet "workload" 24
                                   build_subnet "AzureBastionSubnet" 20
                                   build_subnet "GatewaySubnet" 20
                               } ]
    }

let spokes =
    let spoke i vnetName =
        vnet {
            name $"vnet-%s{vnetName}"

            build_address_spaces [ addressSpace {
                                       space $"10.%i{i}.0.0/16"
                                       build_subnet "management" 24
                                       build_subnet "workload" 24
                                   } ]

            add_peering hub
        }

    {| BuildAgents = spoke 1 "buildagents"
       InternalServices = spoke 2 "internal"
       PublicServices = spoke 3 "public" |}

let jumpBoxes =
    [ spokes.BuildAgents
      spokes.InternalServices
      spokes.PublicServices ]
    |> List.map
        (fun vnet ->
            vm {
                name (vnet.Name.Value.Replace("vnet-","vm-"))
                link_to_vnet vnet
                subnet_name "management"
                username "testadmin"
                password_parameter "jump-password"
                allocate_public_ip Disabled
            }
            :> IBuilder)

let gateway =
    gateway {
        name "vnet-gateway"
        vnet hub.ResourceId.Name.Value
    }

let bastion =
    bastion {
        name "bastion"
        vnet hub.Name.Value
    }

// TODO: Azure Firewall, Azure Application Gateway, Route Tables, NSGs

arm {
    add_resources [ hub
                    spokes.BuildAgents
                    spokes.InternalServices
                    spokes.PublicServices ]

    add_resources jumpBoxes

    add_resource gateway
    //add_resource bastion
}
|> Deploy.execute "codat-nspocf-test" [ "jump-password", "Password1234!" ]
