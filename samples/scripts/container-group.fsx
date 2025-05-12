#r @"C:\Users\isaac\code\farmer\src\Farmer\bin\Debug\net5.0\Farmer.dll"

open Farmer
open Farmer.Builders
open Farmer.ContainerGroup

let nginx = containerInstance {
    name "nginx"
    image "nginx:1.17.6-alpine"
    add_ports PublicPort [ 80us; 443us ]
    add_ports InternalPort [ 9090us ]
    memory 0.5<Gb>
    cpu_cores 1
}

let profile = networkProfile {
    name "netprofile"
    vnet "containernet"
    subnet "ContainerSubnet"
}

let g = containerGroup {
    name "appWithHttpFrontend"
    operating_system Linux
    restart_policy AlwaysRestart
    add_udp_port 123us
    add_instances [ nginx ]
    network_profile profile
}