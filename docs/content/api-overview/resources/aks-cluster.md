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

| Keyword                             | Purpose                                                                                                                                                     |
|-------------------------------------|-------------------------------------------------------------------------------------------------------------------------------------------------------------|
| name                                | Sets the name of the AKS cluster.                                                                                                                           |
| sku                                 | Specifies the SKU of the AKS cluster - default is 'Base'.                                                                                                   |
| tier                                | Specifies the tier of the AKS cluster - default is 'Free'.                                                                                                  | 
| dns_prefix                          | Sets the DNS prefix of the AKS cluster.                                                                                                                     |
| enable_defender                     | Enables Defender for the containers running in the cluster.                                                                                                 |
| enable_image_cleaner                | Enables a service to periodically purge images that are no longer used.                                                                                     |
| enable_private_cluster              | Restricts the cluster's Kubernetes API to only be accessible from private networks.                                                                         |
| enable_rbac                         | Enable Kubernetes Role-Based Access Control.                                                                                                                |
| enable_workload_identity            | Enables workload identity to assign a pod to a managed identity. Requires OIDC, so enables that as well.                                                    |
| oidc_issuer                         | Enables or disables the OIDC issuer service for issuing tokens for federated identity.                                                                      |
| add_agent_pools                     | Adds agent pools to the AKS cluster.                                                                                                                        |
| add_agent_pool                      | Adds an agent pool to the AKS cluster.                                                                                                                      |
| add_identity                        | Adds a managed identity to the the AKS cluster.                                                                                                             |
| system_identity                     | Activates the system identity of the AKS cluster.                                                                                                           |
| kubelet_identity                    | Assigns a user assigned identity to the kubelet user that pulls container images.                                                                           |
| network_profile                     | Sets the network profile for the AKS cluster.                                                                                                               |
| linux_profile                       | Sets the linux profile for the AKS cluster.                                                                                                                 |
| service_principal_client_id         | Sets the client id of the service principal for the AKS cluster.                                                                                            |
| service_principal_use_msi           | Enables the AKS cluster to use the managed identity service principal instead of an external client secret.                                                 |
| windows_username                    | Sets the windows admin username for the AKS cluster.                                                                                                        |
| add_api_server_authorized_ip_ranges | Adds IP address CIDR ranges to be allowed Kubernetes API access.                                                                                            |
| addon                               | A list with the configuration of all addons on the cluster (AciConnectorLinux, HttpApplicationRouting, KubeDashboard, IngressApplicationGateway, OmsAgent). |

##### Configuration Members

* `OidcIssuerUrl` - the configuration built by the `aks` builder uses this property to provide an ARM expression to reference the OIDC Issuer URL on the managed cluster, if enabled.

#### Agent Pool Builder keywords
The Agent Pool builder (`agentPool`) constructs agent pools in the AKS cluster.

| Keyword            | Purpose                                                                                          |
|--------------------|--------------------------------------------------------------------------------------------------|
| name               | Sets the name of the agent pool.                                                                 |
| count              | Sets the count of VM's in the agent pool.                                                        |
| user_mode          | Sets the agent pool to user mode.                                                                |
| disk_size          | Sets the disk size for the VM's in the agent pool.                                               |
| enable_fips        | Uses a FIPS compliant OS image for VM's in the agent pool.                                       |
| max_pods           | Sets the maximum number of pods in the agent pool.                                               |
| os_type            | Sets the OS type of the VM's in the agent pool.                                                  |
| pod_subnet         | Sets the name of a virtual network subnet where this AKS cluster should be attached.             |
| subnet             | Sets the name of a virtual network subnet where this AKS cluster should be attached.             |
| vm_size            | Sets the size of the VM's in the agent pool.                                                     |
| vnet               | Sets the name of a virtual network in the same region where this AKS cluster should be attached. |
| enable_autoscaling | Enables node pool autoscale                                                                      |
| scaleDownMode      | Optional. Use with enable_autoscaling. Options are Delete and Deallocate                         |
| min_count          | Use with enable_autoscaling. Minimum node count in node pool                                     |
| max_count          | Use with enable_autoscaling. Maximum node count in node pool                                     |

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

#### Basic Example

The simplest cluster uses a system assigned managed identity and
default settings for the node pool (size of 3).
The pricing tier is 'Free'.

```fsharp
open Farmer
open Farmer.Builders
open Farmer.ContainerService

let myAks = aks {
    name "aks-cluster"
    service_principal_use_msi
}
```

#### Standard pricing tier. Customizing agent pool and network profile
```fsharp
let myAks = aks {
    name "k8s-cluster"
    tier Tier.Standard
    dns_prefix "testaks"
    add_agent_pools [
        agentPool {
            name "linuxPool"
            count 3
        }
    ]
    linux_profile "aksuser" "public-key-here"
    service_principal_use_msi
    network_profile (
        azureCniNetworkProfile {
            service_cidr "10.250.0.0/16"
        }
    )
}
```
#### Using user assigned identities and connecting to the container registry
```fsharp
// Create an identity for kubelet (used to connect to container registry)
let kubeletMsi = createUserAssignedIdentity "kubeletIdentity"
// Create an identity for the AKS cluster
let clusterMsi = createUserAssignedIdentity "clusterIdentity"
// Give the AKS cluster's identity rights to assign a the kubelet MSI
let assignMsiRoleNameExpr = ArmExpression.create($"guid(concat(resourceGroup().id, '{clusterMsi.ResourceId.Name.Value}', '{Roles.ManagedIdentityOperator.Id}'))")
let assignMsiRole =
    { Name =
        assignMsiRoleNameExpr.Eval()
        |> ResourceName
        RoleDefinitionId = Roles.ManagedIdentityOperator
        PrincipalId = clusterMsi.PrincipalId
        PrincipalType = PrincipalType.ServicePrincipal
        Scope = ResourceGroup
        Dependencies = Set [ clusterMsi.ResourceId ] }
// Create a container image registry
let myAcr = containerRegistry { name "mycontainerregistry" }
let myAcrResId = (myAcr :> IBuilder).ResourceId
// Assign the AcrPull role on that registry to the identity used for kubelet.
let acrPullRoleNameExpr = ArmExpression.create($"guid(concat(resourceGroup().id, '{kubeletMsi.ResourceId.Name.Value}', '{Roles.AcrPull.Id}'))")
let acrPullRole =
    { Name = acrPullRoleNameExpr.Eval() |> ResourceName
        RoleDefinitionId = Roles.AcrPull
        PrincipalId = kubeletMsi.PrincipalId
        PrincipalType = PrincipalType.ServicePrincipal
        Scope = AssignmentScope.SpecificResource myAcrResId
        Dependencies = Set [ kubeletMsi.ResourceId ] }

// Create the cluster and assign the cluster and kubelet identities.
let myAks = aks {
    name "aks-cluster"
    add_identity clusterMsi
    service_principal_use_msi
    kubelet_identity kubeletMsi
    depends_on clusterMsi
    depends_on myAcr
    depends_on_expression assignMsiRoleNameExpr
    depends_on_expression acrPullRoleNameExpr
}
// A template to deploy the MSI's, role assignemnts, container registry and AKS.
let template =
    arm {
        add_resource kubeletMsi
        add_resource clusterMsi
        add_resource myAcr
        add_resource myAks
        add_resource assignMsiRole
        add_resource acrPullRole
    }
```
