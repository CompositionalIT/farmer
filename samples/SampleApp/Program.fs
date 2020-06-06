open Farmer
open Farmer.Builders

//TODO: Create resources here!

//TODO: Testing out the user experience

(*
// Question - would it make sense to add a separate builder for vnets?
let privateNetwork = vnet {
    name "private-vnet"
    address_spaces [
        address_space { prefix "10.30.0.0/16" }
    ]
    subnets [
        subnet {
            name "ContainerSubnet"
            prefix "10.30.19.0/24"
            delegations [
                delegation { serviceName "Microsoft.ContainerInstance/containerGroups" }
            ]
        }
    ]
}
*)

let aciProfile = networkProfile {
    name "vnet-aci-profile"
    vnet "private-vnet"
    subnet "ContainerSubnet"
    // Typically just one subnet is needed
    // but instead they might want to add many, like this
    (* add_interface_configs [
        interface_config {
            add_ipconfigs [
                ip_config { subnet "ContainerSubnet" }
            ]
        }
    ]*)
}

let myContainer = container {
    name "container1"
    image "aci-hello-world"
    network_profile "vnet-aci-profile"
}

let deployment = arm {
    location Location.NorthEurope

    //TODO: Assign resources here using the add_resource keyword
}

// Generate the ARM template here...
deployment
|> Writer.quickWrite @"generated-template"

// Or deploy it directly to Azure here... (required Azure CLI installed!)
// deployment
// |> Deploy.execute "my-resource-group" Deploy.NoParameters