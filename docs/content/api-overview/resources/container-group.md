---
title: "Container Group"
date: 2020-04-30T19:30:59+02:00
chapter: false
weight: 3
---

#### Overview
The Container Group builder is used to create Azure Container Group instances.

* Container Group (`Microsoft.ContainerInstance/containerGroups`)
* Network Profile (`Microsoft.Network/networkProfiles`)

#### Builder Keywords
| Applies To        | Keyword | Purpose |
|-------------------|---------|------------------------------------------|
| containerInstance | name    | Sets the name of the container instance. |
| containerInstance | image   | Sets the container image. |
| containerInstance | command | Sets the commands to execute within the container instance in exec form. |
| containerInstance | add_ports | Sets the ports the container exposes. |
| containerInstance | cpu_cores | Sets the maximum CPU cores the container may use. |
| containerInstance | memory | Sets the maximum gigabytes of memory the container may use. |
| containerInstance | env_vars | Sets a list of environment variables for the container. |
| containerInstance | add_volume_mount | Adds a volume mount on a container from a volume in the container group. |
| containerInstance | probes | Adds liveliness and readiness probes to a container. |
| initContainer | name | Sets the name of the init container. |
| initContainer | image | Sets the init container image. |
| initContainer | command | Sets the commands to execute within the init container in exec form. |
| initContainer | env_vars | Sets a list of environment variables for the init container. |
| initContainer | add_volume_mount | Adds a volume mount on an init container from a volume in the container group. |
| containerGroup | name | Sets the name of the container group. |
| containerGroup | add_instances | Adds container instances to the group. |
| containerGroup | operating_system | Sets the OS type (default Linux). |
| containerGroup | restart_policy | Sets the restart policy (default Always) |
| containerGroup | public_dns | Sets the DNS host label when using a public IP. |
| containerGroup | private_ip | Indicates the container should use a system-assigned private IP address for use in a virtual network. |
| containerGroup | network_profile | Name of a network profile resource for the subnet in a virtual network where the container group will attach. |
| containerGroup | add_identity | Adds a managed identity to the the container group. |
| containerGroup | system_identity | Turns the system identity of the container group on or off (on by default). |
| containerGroup | add_registry_credentials | Adds a container image registry credential with a secure parameter for the password. |
| containerGroup | add_tcp_port | Adds a TCP port to be externally accessible. |
| containerGroup | add_udp_port | Adds a UDP port to be externally accessible. |
| containerGroup | add_volumes | Adds volumes to a container group so they are accessible to containers. |
| liveliness | http | Sets the http GET URI on a container liveliness check. |
| liveliness | exec | Sets a command to execute on a container liveliness check. |
| liveliness | initial_delay_seconds | Sets a delay after container startup before the first check - default is 0 seconds. |
| liveliness | period_seconds | Sets the period between running checks - default is 10 seconds. |
| liveliness | failure_threshold | Sets the number of times a check can fail before the container is considered unhealthy and will be restarted - default is 3. |
| liveliness | success_threshold | Sets the number of times a check must succeed before the container is considered healthy - default is 1. |
| liveliness | timeout_seconds | Sets the number of seconds a check is allowed to run before considering the check a failure - default is 1 second. |
| readiness | http | Sets the http GET URI on a container readiness check. |
| readiness | exec | Sets a command to execute on a container readiness check. |
| readiness | initial_delay_seconds | Sets a delay after container startup before the readiness check - default is 0 seconds. |
| readiness | period_seconds | Sets the period between running checks - default is 10 seconds. |
| readiness | failure_threshold | Sets the number of times a check can fail before the container is considered unhealthy and will be restarted - default is 3. |
| readiness | success_threshold | Sets the number of times a check must succeed before the container is considered healthy - default is 1. |
| readiness | timeout_seconds | Sets the number of seconds a check is allowed to run before considering the check a failure - default is 1 second. |
| networkProfile | name | Name of the container network profile for connecting a container group to a virtual network. |
| networkProfile | vnet | Resource name of the virtual network to connect (if created in the same deployment). |
| networkProfile | link_to_vnet | Resource name of an existing virtual network to connect. |
| networkProfile | subnet | Name of the subnet in the virtual network where the container group should attach. |

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
    probes [
        liveliness {
            http "http://localhost:80/"
            initial_delay_seconds 15
        }
    ]
}

let containerGroupUser = userAssignedIdentity {
    name "aciUser"
}

let group = containerGroup {
    name "webApp"
    operating_system Linux
    restart_policy AlwaysRestart
    add_identity containerGroupUser
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

#### Execute container command example

Modified from azure-cli example here: https://docs.microsoft.com/en-us/azure/container-instances/container-instances-start-command

```fsharp
open Farmer
open Farmer.Builders
open Farmer.ContainerGroup

let wordcount = containerInstance {
    name "mycontainer1"
    image "mcr.microsoft.com/azuredocs/aci-wordcount:latest"
    memory 0.5<Gb>
    cpu_cores 1
    env_vars [
        env_var "NumWords" "3"
        env_var "MinLength" "5"
    ]
    command_line [ "python"; "wordcount.py"; "http://shakespeare.mit.edu/romeo_juliet/full.html" ]
}

let group = containerGroup {
    name "wordcount"
    operating_system Linux
    restart_policy RestartOnFailure
    add_instances [ wordcount ]
}
```
#### Using an initContainer on startup

An initContainer will run on container group startup before any of the containers are executed.

If there are any issues with the initContainer, it will remain in a 'Creating' state indefinitely. Check
for issues by viewing the logs for the init container(s):

`az container logs -g resource-group-name -n container-group-name --container-name init-container-name`

The example below creates a volume mount that is shared between the initContainer and the container
instances. It writes to a file so that the nginx container can serve that file once the group is running.

```fsharp
arm {
    location Location.WestEurope
    add_resources [
        containerGroup {
            name "container-group-with-init"
            operating_system Linux
            restart_policy ContainerGroup.AlwaysRestart
            add_volumes [
                volume_mount.empty_dir "html"
            ]
            add_init_containers [
                initContainer {
                    name "write-index-file"
                    image "debian"
                    add_volume_mount "html" "/usr/share/nginx/html"
                    command_line [
                        "/bin/sh"
                        "-c"
                        "mkdir -p /usr/share/nginx/html && echo 'hello there' >> /usr/share/nginx/html/index.html"
                    ]
                }
            ]
            add_instances [
                containerInstance {
                    name "nginx"
                    image "nginx:alpine"
                    add_volume_mount "html" "/usr/share/nginx/html"
                    add_public_ports [ 80us; 443us ]
                    memory 0.5<Gb>
                    cpu_cores 0.2
                }
            ]
        }
    ]
}
```
