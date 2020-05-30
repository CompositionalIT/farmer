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
| ip_address | Sets the IP addresss (default Public). |
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