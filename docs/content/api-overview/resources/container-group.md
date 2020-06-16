---
title: "Container Group"
date: 2020-04-30T19:30:59+02:00
weight: 3
chapter: false
---

#### Overview
The Container Group builder is used to create Azure Container Group instances.

* Container Group (`Microsoft.ContainerInstance/containerGroups`)

#### Builder Keywords
| Keyword | Purpose |
|-|-|
| name | Sets the name of the Container Group instance. |
| image | Sets the container image. |
| ports | Sets the ports the container exposes. |
| cpu_cores | Sets the maximum CPU cores the container may use. |
| memory | Sets the maximum gigabytes of memory the container may use. |
| group_name | Sets the name of the container group. |
| link_to_container_group | Links this container to an already-created container group. |
| os_type | Sets the OS type (default Linux). |
| restart_policy | Sets the restart policy (default Always) |
| public_dns | Sets the DNS host label when using a public IP. |
| private_ip | Indicates the container should use a system-assigned private IP address for use in a virtual network. |
| private_static_ip | Sets a static assigned IP address for use in a virtual network |
| ip_address | _(Deprecated)_ Sets the IP addresss (default Public). |
| network_profile | Name of a network profile resource for the subnet in a virtual network where the container group will attach. |
| add_tcp_port | Adds a TCP port to be externally accessible. |
| add_udp_port | Adds a UDP port to be externally accessible. |

#### Example
```fsharp
open Farmer
open Farmer.Builders

let nginx = container {
    group_name "appWithHttpFrontend"
    os_type Linux
    add_tcp_port 80us
    add_tcp_port 443us
    restart_policy ContainerGroup.RestartPolicy.Always

    name "nginx"
    image "nginx:1.17.6-alpine"
    ports [ 80us; 443us ]
    memory 0.5<Gb>
    cpu_cores 1
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
    network_profile "vnet-aci-profile"
    ports [ 80us ]
    private_static_ip "10.30.19.4" [TCP, 80us]
}
```
