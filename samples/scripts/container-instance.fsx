#r "nuget:Farmer"

open Farmer
open Farmer.Builders

let containerGroupUser = userAssignedIdentity {
    name "aciUser"
}

let template = arm {
    location Location.WestEurope
    add_resources [
        containerGroupUser
        containerGroup {
            name "isaac-container-group"
            operating_system Linux
            restart_policy ContainerGroup.AlwaysRestart
            add_identity containerGroupUser
            add_instances [
                containerInstance {
                    name "nginx"
                    image "nginx:1.17.6-alpine"

                    add_public_ports [ 80us; 443us ]
                    add_internal_ports [ 123us ]

                    memory 0.5<Gb>
                    cpu_cores 1
                }
            ]
        }
    ]
}

Writer.quickWrite "generated-template" template

Deploy.execute "my-resource-group" [] template

