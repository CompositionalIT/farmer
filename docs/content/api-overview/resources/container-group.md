---
title: "Container Group"
date: 2020-04-30T19:30:59+02:00
chapter: false
weight: 5
---

#### Overview
The Container Group builder is used to create Azure Container Group instances.

* Container Group (`Microsoft.ContainerInstance/containerGroups`)

#### Builder Keywords
| Applies To | Keyword | Purpose |
|-|-|-|
| containerInstance | name | Sets the name of the Container Group instance. |
| containerInstance | image | Sets the container image. |
| containerInstance | add_ports | Sets the ports the container exposes. |
| containerInstance | cpu_cores | Sets the maximum CPU cores the container may use. |
| containerInstance | memory | Sets the maximum gigabytes of memory the container may use. |
| containerInstance | env_vars | Sets a list of environment variables for the container. |
| containerInstance | add_volume_mount | Adds a volume mount on a container from a volume in the container group. |
| containerGroup | add_instances | Adds container instances to the group. |
| containerGroup | operating_system | Sets the OS type (default Linux). |
| containerGroup | restart_policy | Sets the restart policy (default Always) |
| containerGroup | public_dns | Sets the DNS host label when using a public IP. |
| containerGroup | private_ip | Indicates the container should use a system-assigned private IP address for use in a virtual network. |
| containerGroup | network_profile | Name of a network profile resource for the subnet in a virtual network where the container group will attach. |
| containerGroup | add_registry_credentials | Adds a container image registry credential with a secure parameter for the password. |
| containerGroup | add_tcp_port | Adds a TCP port to be externally accessible. |
| containerGroup | add_udp_port | Adds a UDP port to be externally accessible. |
| containerGroup | add_volumes | Adds volumes to a container group so they are accessible to containers. |

#### Example
```fsharp
open Farmer
open Farmer.Builders
open Farmer.ContainerGroup

let nginx = containerInstance {
    name "nginx"
    image "nginx:1.17.6-alpine"
    add_ports PublicPort [ 80us; 443us ]
    add_ports InternalPort [ 9090us; ]
    memory 0.5<Gb>
    cpu_cores 1
    env_vars [
        env_var "CONTENT_PATH" "/www"
        secure_env_var "SECRET_PASSWORD" "shhhhhh!"
    ]
    add_volume_mount "secret-files" "/config/secrets"
    add_volume_mount "source-code" "/src/farmer"
}

let group = containerGroup {
    name "webApp"
    operating_system Linux
    restart_policy AlwaysRestart
    add_udp_port 123us
    add_instances [ nginx ]
    add_registry_credentials [
        registry "mygregistry.azurecr.io" "registryuser"
    ]
    add_volumes [
        volume_mount.secret_string "secret-files" "secret1" "abcdefg"
        volume_mount.git_repo "source-code" (Uri "https://github.com/CompositionalIT/farmer")
    ]
}
```

#### Private Virtual Network Example

Attaching a container group to a virtual network requires adding a service
delegation on a subnet indicating it is for container groups, adding a
network profile to bind the container group interface to that subnet, and
finally adding the container group itself with a private IP address.

```fsharp
open Farmer
open Farmer.Builders

let privateNetwork = vnet {
    name "private-vnet"
    add_address_spaces [
        "10.30.0.0/16"
    ]
    add_subnets [
        subnet {
            name "ContainerSubnet"
            prefix "10.30.19.0/24"
            add_delegations [
                SubnetDelegationService.ContainerGroups
            ]
        }
    ]
}

let aciProfile = networkProfile {
    name "vnet-aci-profile"
    vnet "private-vnet"
    subnet "ContainerSubnet"
}

let myContainer = container {
    name "helloworld"
    image "microsoft/aci-helloworld"
    add_ports PublicPort [ 80us ]
}

let group = containerGroup {
    name "webApp"
    operating_system Linux
    restart_policy AlwaysRestart
    add_instances [ myContainer ]
    network_profile "vnet-aci-profile"
    private_ip [TCP, 80us]
}
```
