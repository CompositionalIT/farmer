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

### Turn on Resource Provider
Before you deploy your container app, you need to turn on the Container Apps resource provider in your Azure subscription.

Get sure you have the following providers registered: `Microsoft.Kubernetes` and `Microsoft.ContainerService`.

#### Container Environment Builder
The Container Environment builder (`containerEnvironment`) defines settings for the Kubernetes envirionment that hosts the container apps.

| Keyword | Purpose |
|-|-|
| name | Sets the name of the container environment. |
| log_analytics_instance | Specifies a Log Analytics workspace where container logs should be sent. If none is provided, one will automatically be created. |
| internal_load_balancer_state | Sets whether an internal load balancer should be used for load balancing traffic to container app replicas. |
| add_container | Adds a single container app to the environment. |
| add_containers | Adds one or more container apps to the environment. |

> Also supports Tagging and Dependencies.

#### Container Apps Builder
The Container Apps builder (`containerApp`) is used to define one or more container apps to add to the container environment.

| Keyword | Purpose |
|-|-|
| name | Sets the name of the container app. |
| add_identity | Adds a managed identity to the the container app. |
| ingress_state | Activates or deactivates the ingress of the Azure Container App. |
| ingress_target_port | Activates the ingress of the Azure Container App and sets the target port. |
| ingress_transport | Activates the ingress of the Azure Container App and sets the transport mode. |
| system_identity | Activates the system identity of the Azure Container App. |
| dapr_app_id | Sets the dapr app id for the app. |
| replicas | Sets the minimum and maximum replicas to scale the container app. |
| active_revision_mode | Indicates whether multiple version of a container app can be active at once.|
| add_registry_credentials | Adds container image registry credentials for images in this container app, which are a list of server and usernames. Passwords are supplied as secure parameters. |
| reference_registry_credentials | Adds container image registry credentials for images in this container app in the form of a list of Azure resource ids. |
| add_managed_identity_registry_credentials | Adds container app registry managed identity credentials for images in this container app, which are a list of server and identities. |
| add_containers | Adds a list of containers to this container app. All containers in the app share resources and scaling. |
| add_simple_container | Adds a single container that references a public docker image and version. |
| add_secret_parameter | Adds an application secret to the entire container app. This is passed as a secure parameter to the template, and an environment variable is automatically created which references the secret. |
| add_secret_parameters | Adds application secrets to the entire container app. This is passed as secure parameters to the template, and environment variables are automatically created which reference the secret. |
| add_secret_expression | As per `add_secret_parameter`, but the value is sourced from an ARM expression instead of as a parameter. Useful for e.g. storage keys etc. |
| add_secret_expressions | As per `add_secret_parameters`, but the values are sourced from an ARM expressions instead of as parameters. Useful for e.g. storage keys etc. |
| add_env_variable | Adds a static, plain text environment variable. |
| add_env_variables | Adds static, plain text environment variables. |
| add_volumes | Adds volumes to a container app so they are accessible to containers. |

##### Scale Rules

The Container App Builder supports a number of KEDA scale rules out of the box:

| Scale Rule Keyword | Purpose |
|-|-|
| add_http_scale_rule | Adds a scale rule for HTTP concurrent requests. |
| add_cpu_scale_rule | Adds a scale rule for CPU usage, either utilisation or average value. |
| add_memory_scale_rule | Adds a scale rule for Memory usage, either utilisation or average value. |
| add_servicebus_scale_rule | Adds a scale rule for service bus queues message count. |
| add_eventhub_scale_rule | Adds a scale rule for event hub events. |
| add_queue_scale_rule | Adds a scale rule for Azure Storage Queue length. |
| add_custom_scale_rule | Adds a custom scale rule. Provide an object that matches the KEDA specification. |

> The Azure Storage Queue Scale Rule integration is "smart" - provide a reference to the storage account, queue name and length threshold; all appropriate settings and secrets will be automatically configured for you.

#### Container Builder
The Container builder (`container`) is used to define one or more containers for a container app.

| Keyword | Purpose |
|-|-|
| name | Sets the name of the container. |
| public_docker_image | Sets a public container image. |
| private_docker_image | Sets a private container image. |
| cpu_cores | Specifies the CPU cores allocated to the container (maximum 2.0). |
| memory | Specifies the memory in gigabytes allocated to the container (maximum 4.0). |
| add_volume_mount | Adds a volume mount on a container from a volume in the container app. |

#### Example

```fsharp
open Farmer
open Farmer.Builders
open Farmer.Arm

let storageName = $"{Guid.NewGuid().ToString().[0..5]}containerqueue"
let myStorageAccount = storageAccount {
    name storageName
    add_queue queueName
    add_file_share "certs"
}

containerEnvironment {
    name "my-container-app"
    add_containers [
        containerApp {
            name "httpservice"
            activeRevisionsMode ActiveRevisionsMode.Single
            reference_registry_credentials [
                ContainerRegistry.registries.resourceId "myazurecontainerregistry"
            ]
            add_volumes [ Volume.emptyDir "empty-v"
                          Volume.azureFile "certs-v" (ResourceName "certs") myStorageAccount.Name StorageAccessMode.ReadOnly ]
            add_containers [
                container {
                    name "myservice1"
                    public_docker_image containerRegistryDomain containerRegistry "myimage1" version
                    memory 0.2<Gb>
                    add_volume_mounts [ "empty-v", "/tmp" ]
               }
                container {
                    name "myservice2"
                    public_docker_image containerRegistryDomain containerRegistry "myimage2" version
                    cpu_cores 0.5<VCores>
                    memory 1.0<Gb>
                    add_volume_mounts [ "certs-v", "/certs" ]
                }
            ]
            replicas 1 5
            ingress_target_port 80us
            ingress_transport Auto
            dapr_app_id "httpservice"
            add_http_scale_rule "http-rule" { ConcurrentRequests = 100 }
        }
    ]
}
```
