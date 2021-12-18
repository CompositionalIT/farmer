---
title: "Container Apps"
date: 2021-12-09T21:31:14-05:00
chapter: false
weight: 3
---

#### Overview
The Container Apps builder is used to create Azure Container Apps.

* Container Environment (`Microsoft.Web/kubeEnvironments`)
* Container App (`Microsoft.Web/containerApps`)

#### Container Environment Builder
The Container Environment builder (`containerEnvironment`) defines settings for the Kubernetes envirionment that hosts the container apps.

| Keyword | Purpose |
|-|-|
| name | Sets the name of the container environment. |
| add_container | Adds a single container app to the environment. |
| add_containers | Adds one or more container apps to the environment. |
| logAnalytics | Specifies a Log Analytics workspace where container logs should be sent. |
| internalLoadBalancerEnabled | Indicates if an internal load balancer should be used for load balancing traffic to container app replicas. |

#### Container Apps Builder
The Container Apps builder (`containerApp`) is used to define one or more container apps to add to the container environment.

| Keyword | Purpose |
|-|-|
| name | Sets the name of the container app. |
| add_scale_rule | Adds rules for how the container app should automatically scale. |
| ingress | Sets the ingress traffic allowed by port. |
| dapr | Configures the dapr settings for the app. |
| replicas | Minimum and maximum replicas to scale the container app. |
| activeRevisionsMode | Indicates whether multiple version of a container app can be active at once.|
| add_secret | Adds a single Kubernetes secret that can be to referenced in the container. |
| add_secrets | Adds multiple Kubernetes secrets that can be to referenced in the container. |
| secret_setting | Creates a setting for the Azure Container App whose value will be supplied as a secret parameter. |
| add_secretref_variable | Adds a secretRef to the Azure Container App environment variables. |
| setting | Adds a variable to the Azure Container App environment variables. |

#### Container Builder
The Container builder (`container`) is used to define one or more containers for a container app.

| Keyword | Purpose |
|-|-|
| name | Sets the name of the container. |
| public_docker_image | Sets a public container image. |
| private_docker_image | Sets a private container image. |
| cpu_cores | Specifies the CPU cores allocated to the container (maximum 2.0). |
| memory | Specifies the memory in gigabytes allocated to the container (maximum 4.0). |


#### Example

```fsharp
open Farmer
open Farmer.Builders

containerEnvironment {
    name "my-container-app"
    logAnalytics containerLogs
    add_containers [
        containerApp {
            name "httpservice"
            activeRevisionsMode ActiveRevisionsMode.Single
            reference_registry_credentials [
                Farmer.Arm.ContainerRegistry.registries.resourceId "myazurecontainerregistry"
            ]
            add_containers [
                container {
                    container_name "myservice1"
                    public_docker_image containerRegistryDomain containerRegistry "myimage1" version
                    memory 0.2<Gb>
                }
                container {
                    container_name "myservice2"
                    public_docker_image containerRegistryDomain containerRegistry "myimage2" version
                    cpu_cores 0.5<VCores>
                    memory 1.0<Gb>
                }
            ]
            replicas 1 5
            ingress { External = true; TargetPort = 80; Transport = "auto" }
            dapr { AppId = "httpservice" }
            add_scale_rule "http-rule" (ScaleRuleType.Http {| ConcurrentRequests = 100 |})
        }
    ]
}
```
