#r @"./libs/Newtonsoft.Json.dll"
#r @"../../src/Farmer/bin/Debug/netstandard2.0/Farmer.dll"

open Farmer
open Farmer.Arm.ContainerInstance
open Farmer.Builders

let template =
    createTemplate
        Location.WestEurope [
            containerGroup {
                name "isaac-container-group"
                operating_system Linux
                restart_policy ContainerGroup.AlwaysRestart
                add_instances [
                    containerInstance {
                        name "nginx"
                        image "nginx:1.17.6-alpine"

                        public_ports [ 80us; 443us ]
                        internal_ports [ 123us ]

                        ports PublicPort [ 80us; 443us ]
                        ports InternalPort [ 123us ]

                        memory 0.5<Gb>
                        cpu_cores 1
                    }
                ]
            }
        ]

Writer.quickWrite "generated-template" template

Deploy.execute "my-resource-group" [] template

