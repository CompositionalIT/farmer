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
The Container Environment builder (`containerEnvironment`) defines settings for the Kubernetes environment that hosts the container apps.

| Keyword | Purpose |
|-|-|
| name | Sets the name of the container environment. |
| app_insights_instance | Specifies an App Insights instance. All apps will receive the instrumentation key as a setting, and Dapr will be configured to send logs there. |
| log_analytics_instance | Specifies a Log Analytics workspace where container logs should be sent. If none is provided, one will automatically be created. |
| internal_load_balancer_state | Sets whether an internal load balancer should be used for load balancing traffic to container app replicas. |
| add_container | Adds a single container app to the environment. |
| add_containers | Adds one or more container apps to the environment. |
| add_dapr_component | Adds a dapr component to the environment. |
| add_dapr_components | Adds one or more dapr components to the environment. |
| app_insights_instance | Links an App Insights instance to this environment. All containers will be configured to use this AI instance, as well as DAPR. |

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
| dapr_app_port | Sets the dapr app port for the app. |
| dapr_app_protocol | Sets the dapr app protocol for the app. |
| replicas | Sets the minimum and maximum replicas to scale the container app. |
| active_revision_mode | Indicates whether multiple versions of a container app can be active at once.|
| add_registry_credentials | Adds container image registry credentials for images in this container app, which are a list of server and usernames. Passwords are supplied as secure parameters. |
| reference_registry_credentials | Adds container image registry credentials for images in this container app in the form of a list of Azure resource ids. |
| add_managed_identity_registry_credentials | Adds container app registry managed identity credentials for images in this container app, which are a list of server and identities. |
| add_containers | Adds a list of containers to this container app. All containers in the app share resources and scaling. |
| add_simple_container | Adds a single container that references a public docker image and version. |
| add_secret_parameter | Adds an application secret to the entire container app. This is passed as a secure parameter to the template, and an environment variable is automatically created which references the secret. |
| add_secret_parameters | Adds application secrets to the entire container app. This is passed as secure parameters to the template, and environment variables are automatically created which reference the secret. |
| add_secret_expression | As per `add_secret_parameter`, but the value is sourced from an ARM expression instead of as a parameter. Useful for e.g. storage keys etc. |
| add_secret_expressions | As per `add_secret_parameters`, but the values are sourced from ARM expressions instead of as parameters. Useful for e.g. storage keys etc. |
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
| resources | Sets both CPU and memory for the container using a valid consumption plan resource allocation. Use this for consumption plans to ensure only supported combinations are used. |
| cpu_cores | Specifies the CPU cores allocated to the container (maximum 2.0). Use this for dedicated plans where any combination is valid. |
| memory | Specifies the memory in gigabytes allocated to the container (maximum 4.0). Use this for dedicated plans where any combination is valid. |
| add_volume_mount | Adds a volume mount on a container from a volume in the container app. |
| set_probe | Adds a health probe of the given type, with the given protocol, at the given route, with the given port |

##### Consumption Plan Resource Allocations

For containers on consumption plans, use the `resources` operation with a `ContainerApp.ConsumptionPlanResources` value to select a valid CPU and memory combination:

| Case | CPU (vCores) | Memory (Gi) |
|-|-|-|
| `ConsumptionPlanResources.Cores0_25` | 0.25 | 0.5 |
| `ConsumptionPlanResources.Cores0_5` | 0.5 | 1.0 |
| `ConsumptionPlanResources.Cores0_75` | 0.75 | 1.5 |
| `ConsumptionPlanResources.Cores1_0` | 1.0 | 2.0 |
| `ConsumptionPlanResources.Cores1_25` | 1.25 | 2.5 |
| `ConsumptionPlanResources.Cores1_5` | 1.5 | 3.0 |
| `ConsumptionPlanResources.Cores1_75` | 1.75 | 3.5 |
| `ConsumptionPlanResources.Cores2_0` | 2.0 | 4.0 |
| `ConsumptionPlanResources.Cores2_25` | 2.25 | 4.5 |
| `ConsumptionPlanResources.Cores2_5` | 2.5 | 5.0 |
| `ConsumptionPlanResources.Cores2_75` | 2.75 | 5.5 |
| `ConsumptionPlanResources.Cores3_0` | 3.0 | 6.0 |
| `ConsumptionPlanResources.Cores3_25` | 3.25 | 6.5 |
| `ConsumptionPlanResources.Cores3_5` | 3.5 | 7.0 |
| `ConsumptionPlanResources.Cores3_75` | 3.75 | 7.5 |
| `ConsumptionPlanResources.Cores4_0` | 4.0 | 8.0 |

> See [Azure Container Apps documentation](https://learn.microsoft.com/en-us/azure/container-apps/containers#allocations) for more details on supported allocations.

#### Dapr Component Builder
The Dapr Component builder (`daprComponent`) is used to define one or more dapr components for a container environment.

| Keyword | Purpose |
|-|-|
| name | Sets the name of the dapr component. |
| component_type | Sets the dapr component type. |
| ignore_errors | Sets whether component errors are ignored. |
| init_timeout | Sets the initialization timeout. |
| add_metadata | Adds a piece of metadata to the dapr component. |
| add_secret_metadata | Adds a piece of metadata that references a secret to the dapr component. |
| add_scope | Adds a scope, can either be a string or a reference to a container app. |
| add_scopes | Adds one or more scopes, which can either be strings or references to container apps. |
| version | Sets the dapr component version. |
| cron_binding | Helper for setting fields required for a cron binding |
| azure_storage_queue_binding | Helper for setting fields required for an Azure Storage Queue binding |
| azure_servicebus_queues_pubsub | Helper for setting fields required for an Azure Service Bus Queue |

#### Example

```fsharp
open Farmer
open Farmer.Builders
open Farmer.Arm
open Farmer.ContainerApp

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
                    // Use 'resources' with a ConsumptionPlanResources value for consumption plans
                    // to ensure a valid CPU/memory combination is selected at compile time.
                    resources ConsumptionPlanResources.Cores0_25
                    add_volume_mounts [ "empty-v", "/tmp" ]
               }
                container {
                    name "myservice2"
                    public_docker_image containerRegistryDomain containerRegistry "myimage2" version
                    resources ConsumptionPlanResources.Cores0_5
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
