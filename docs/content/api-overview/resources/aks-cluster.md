---
title: "AKS Cluster"
date: 2020-09-16T19:30:59+02:00
chapter: false
weight: 1
---

#### Overview
The AKS Cluster builder is used to create AKS clusters.

* Container Service (`Microsoft.ContainerService/managedClusters`)

#### AKS Builder Keywords
The AKS builder (`aks`) constructs AKS clusters.

| Keyword | Purpose |
|-|-|
| name | Sets the name of the AKS cluster. |
| dns_prefix | Sets the DNS prefix of the AKS cluster. |
| enable_private_cluster | Restricts the cluster's Kubernetes API to only be accessible from private networks. |
| enable_rbac | Enable Kubernetes Role-Based Access Control. |
| add_agent_pools | Adds agent pools to the AKS cluster. |
| add_agent_pool | Adds an agent pool to the AKS cluster. |
| add_identity | Adds a managed identity to the the AKS cluster. |
| system_identity | Activates the system identity of the AKS cluster. |
| network_profile | Sets the network profile for the AKS cluster. |
| linux_profile | Sets the linux profile for the AKS cluster. |
| service_principal_client_id | Sets the client id of the service principal for the AKS cluster. |
| service_principal_use_msi | Enables the AKS cluster to use the managed identity service principal instead of an external client secret. |
| windows_username | Sets the windows admin username for the AKS cluster. |
| add_api_server_authorized_ip_ranges | Adds IP address CIDR ranges to be allowed Kubernetes API access. |

#### Agent Pool Builder keywords
The Agent Pool builder (`agentPool`) constructs agent pools in the AKS cluster.

| Keyword | Purpose |
|-|-|
| name | Sets the name of the agent pool. |
| count | Sets the count of VM's in the agent pool. |
| user_mode | Sets the agent pool to user mode. |
| disk_size | Sets the disk size for the VM's in the agent pool. |
| max_pods | Sets the maximum number of pods in the agent pool. |
| os_type | Sets the OS type of the VM's in the agent pool. |
| subnet | Sets the name of a virtual network subnet where this AKS cluster should be attached. |
| vm_size | Sets the size of the VM's in the agent pool. |
| vnet | Sets the name of a virtual network in the same region where this AKS cluster should be attached. |

#### Kubenet Builder
The Kubenet builder (`kubenetNetworkProfile`) creates Kubenet network profiles on the AKS cluster.

| Keyword | Purpose |
|-|-|
| load_balancer_sku | SKU for the Load Balancer - defaults to 'Standard' |

#### CNI Builder
The CNI builder (`azureCniNetworkProfile`) creates Azure CNI network profiles on the AKS cluster.

| Keyword | Purpose |
|-|-|
| docker_bridge | Sets the docker bridge CIDR to a network other than the default 17.17.0.1/16. |
| dns_service | Sets the DNS service IP - must be within the service CIDR, default is the second address in the service CIDR. |
| service_cidr | Sets the service cidr to a network other than the default 10.224.0.0/16. |
| load_balancer_sku | SKU for the Load Balancer - defaults to 'Standard' |

#### Example
```fsharp
open Farmer
open Farmer.Builders
open Farmer.ContainerService

let myAks = aks {
    name "k8s-cluster"
    dns_prefix "testaks"
    add_agent_pools [
        agentPool {
            name "linuxPool"
            count 3
        }
    ]
    linux_profile "aksuser" "public-key-here"
    service_principal_client_id "some-spn-client-id"
    network_profile (
        azureCniNetworkProfile {
            service_cidr "10.250.0.0/16"
        }
    )
}

```
