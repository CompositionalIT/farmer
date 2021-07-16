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
            name "container-group-with-init"
            operating_system Linux
            restart_policy ContainerGroup.AlwaysRestart
            add_volumes [
                volume_mount.empty_dir "html"
            ]
            add_init_containers [
                initContainer {
                    name "init-stuff"
                    image "debian"
                    add_volume_mount "html" "/usr/share/nginx/html"
                    command_line [
                        "/bin/sh"
                        "-c"
                        "mkdir -p /usr/share/nginx/html && echo 'hello there' >> /usr/share/nginx/html/index.html"
                    ]
                }
            ]
            add_identity containerGroupUser
            add_instances [
                containerInstance {
                    name "nginx"
                    image "nginx:alpine"
                    add_volume_mount "html" "/usr/share/nginx/html"

                    add_public_ports [ 80us; 443us ]
                    add_internal_ports [ 123us ]

                    memory 0.5<Gb>
                    cpu_cores 0.2
                    probes [
                        liveness {
                            http "http://localhost:80/index.html"
                            initial_delay_seconds 15
                        }
                    ]
                }
            ]
        }
    ]
}

Writer.quickWrite "container-instance" template

