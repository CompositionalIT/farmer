module ContainerApps

open Expecto
open Farmer
open Farmer.Builders
open Newtonsoft.Json.Linq
open Farmer.ContainerApp
open Farmer.Identity
open Farmer.Arm

let msi = createUserAssignedIdentity "appUser"
let containerRegistryName = "myregistry"

let fullContainerAppDeployment =
    let containerLogs = logAnalytics { name "containerlogs" }
    let containerRegistryDomain = $"{containerRegistryName}.azurecr.io"
    let acr = containerRegistry { 
        name containerRegistryName
    }
    let version = "1.0.0"
    let containerEnv =
        containerEnvironment {
            name "kubecontainerenv"
            log_analytics_instance containerLogs
            add_containers [
                containerApp {
                    name "http"
                    add_identity msi
                    active_revision_mode Single
                    add_registry_credentials [
                        registry containerRegistryDomain containerRegistryName
                    ]
                    add_containers [
                        container {
                            name "http"
                            private_docker_image containerRegistryDomain "http" version
                            cpu_cores 0.25<VCores>
                            memory 0.5<Gb>
                        }
                    ]
                    replicas 1 5
                    add_env_variable "ServiceBusQueueName" "wishrequests"
                    add_secret_parameter "servicebusconnectionkey"
                    ingress_state Enabled
                    ingress_target_port 80us
                    ingress_transport Auto
                    dapr_app_id "http"
                    add_http_scale_rule "http-rule" { ConcurrentRequests = 100 }
                }
                containerApp {
                    name "multienv"
                    add_simple_container "mcr.microsoft.com/dotnet/samples" "aspnetapp"
                    ingress_target_port 80us
                    ingress_transport Auto
                    add_http_scale_rule "http-scaler" { ConcurrentRequests = 10 }
                    add_cpu_scale_rule "cpu-scaler" { Utilisation = 50 }
                    add_secret_parameters ["servicebusconnectionkey"]
                    add_env_variables ["ServiceBusQueueName","wishrequests"]
                    add_secret_expressions ["containerlogs", containerLogs.PrimarySharedKey]
                }
                containerApp {
                    name "servicebus"
                    active_revision_mode Single
                    reference_registry_credentials [(acr :> IBuilder).ResourceId]
                    add_containers [
                        container {
                            name "servicebus"
                            private_docker_image containerRegistryDomain "servicebus" version
                        }
                    ]
                    replicas 0 3
                    add_env_variable "ServiceBusQueueName" "wishrequests"
                    add_secret_parameter "servicebusconnectionkey"
                    add_servicebus_scale_rule
                        "sb-keda-scale"
                        { QueueName = "wishrequests"
                          MessageCount = 5
                          SecretRef = "servicebusconnectionkey" }
                }
            ]
        }
    arm {
        add_resources [
            containerEnv
            msi
        ]
    }

let tests = testList "Container Apps" [
    let jsonTemplate = fullContainerAppDeployment.Template |> Writer.toJson
    let jobj = JObject.Parse jsonTemplate

    test "Container automatically creates a log analytics workspace" {
        let env : IBuilder = containerEnvironment { name "testca" }
        let resources = env.BuildResources Location.NorthEurope
        Expect.exists resources (fun r -> r.ResourceId.Name.Value = "testca-workspace") "No Log Analytics workspace was created."
    }

    test "Full container environment parameters" {
        Expect.hasLength jobj.["parameters"] 2 "Expecting 2 parameters"
        Expect.isNotNull (jobj.SelectToken("parameters.servicebusconnectionkey")) "Missing 'servicebusconnectionkey' parameter"
        Expect.isNotNull (jobj.SelectToken("parameters['myregistry.azurecr.io-password']")) "Missing 'myregistry.azurecr.io-password' parameter"
    }
    test "Seq container environment parameters" {
        let containerApp = fullContainerAppDeployment.Template.Resources |> List.find(fun r -> r.ResourceId.Name.Value = "multienv") :?> Farmer.Arm.App.ContainerApp
        containerApp.EnvironmentVariables.["ServiceBusQueueName"] |> ignore
        containerApp.EnvironmentVariables.["servicebusconnectionkey"] |> ignore
        containerApp.EnvironmentVariables.["containerlogs"] |> ignore
    }
    test "Full container managed environments" {
        let kubeEnv = jobj.SelectToken("resources[?(@.name=='kubecontainerenv')]")
        Expect.equal (kubeEnv.["type"] |> string) "Microsoft.App/managedEnvironments" "Incorrect type for kuberenetes environment"
        Expect.equal (kubeEnv.["kind"] |> string) "containerenvironment" "Incorrect kind for kuberenetes environment"
        let kubeEnvAppLogConfig = jobj.SelectToken("resources[?(@.name=='kubecontainerenv')].properties.appLogsConfiguration")
        Expect.equal (kubeEnvAppLogConfig.["destination"] |> string) "log-analytics" "Incorrect type for app log config"
        let kubeEnvLogAnalyticsCustomerId = jobj.SelectToken("resources[?(@.name=='kubecontainerenv')].properties.appLogsConfiguration.logAnalyticsConfiguration")
        Expect.equal (kubeEnvLogAnalyticsCustomerId.["customerId"] |> string) "[reference(resourceId('Microsoft.OperationalInsights/workspaces', 'containerlogs'), '2020-03-01-preview').customerId]" "Incorrect log analytics customerId reference"
    }
    test "Full container environment containerApp" {
        let httpContainerApp = jobj.SelectToken("resources[?(@.name=='http')]")
        Expect.equal (httpContainerApp.["type"] |> string) "Microsoft.App/containerApps" "Incorrect type for containerApps"
        Expect.equal (httpContainerApp.["kind"] |> string) "containerapp" "Incorrect kind for containerApps"
        let ingress = httpContainerApp.SelectToken("properties.configuration.ingress")
        Expect.isTrue (ingress.SelectToken("external") |> string |> bool.Parse) "Incorrect external ingress"
        Expect.equal (ingress.SelectToken("targetPort") |> string |> int) 80 "Incorrect targetPort"
        Expect.equal (ingress.SelectToken("transport") |> string) "auto" "Incorrect transport"
        let registries = httpContainerApp.SelectToken("properties.configuration.registries")
        Expect.hasLength registries 1 "Expected 1 registry"
        let firstRegistry = registries |> Seq.head
        Expect.equal (firstRegistry.SelectToken("passwordSecretRef") |> string) "myregistry" "Incorrect registry password secretRef"
        Expect.equal (firstRegistry.SelectToken("server") |> string) "myregistry.azurecr.io" "Incorrect registry"
        // The value here doesn't seem quite right. Is it really supposed to be the name of the repository in the registry?
        Expect.equal (firstRegistry.SelectToken("username") |> string) "myregistry" "Incorrect registry username"
        let secrets = httpContainerApp.SelectToken("properties.configuration.secrets")
        Expect.hasLength secrets 2 "Expecting 2 secrets"
        Expect.equal (secrets.[0].["name"] |> string) "myregistry" "Incorrect name for registry password secret"
        Expect.equal (secrets.[0].["value"] |> string) "[parameters('myregistry.azurecr.io-password')]" "Incorrect password parameter for registry password secret"
        Expect.equal (httpContainerApp.SelectToken("properties.managedEnvironmentId") |> string) "[resourceId('Microsoft.App/managedEnvironments', 'kubecontainerenv')]" "Incorrect kube environment Id"

        let containers = httpContainerApp.SelectToken("properties.template.containers")
        Expect.hasLength containers 1 "Expected 1 http container"
        let httpContainer = containers |> Seq.head
        Expect.equal (httpContainer.["image"] |> string ) "myregistry.azurecr.io/http:1.0.0" "Incorrect container image"
        Expect.equal (httpContainer.["name"] |> string ) "http" "Incorrect container name"
        Expect.equal (httpContainer.SelectToken("resources.cpu") |> float ) 0.25 "Incorrect container cpu resources"
        Expect.equal (httpContainer.SelectToken("resources.memory") |> string ) "0.50Gi" "Incorrect container memory resources"

        let scale = httpContainerApp.SelectToken("properties.template.scale")
        Expect.isNotNull scale "properties.scale was null"
        Expect.equal (scale.["minReplicas"] |> int) 1 "Incorrect min replicas"
        Expect.equal (scale.["maxReplicas"] |> int) 5 "Incorrect max replicas"
    }
    test "Makes container app with MSI" {
        let containerApp = fullContainerAppDeployment.Template.Resources |> List.find(fun r -> r.ResourceId.Name.Value = "http") :?> App.ContainerApp
        Expect.isNonEmpty containerApp.Identity.UserAssigned "Container app did not have identity"
        Expect.equal containerApp.Identity.UserAssigned.[0] (UserAssignedIdentity(ResourceId.create(userAssignedIdentities, ResourceName "appUser"))) "Expected user identity named 'appUser'."
    }
    test "Turns on Dapr" {
        let containerApp = fullContainerAppDeployment.Template.Resources |> List.find(fun r -> r.ResourceId.Name.Value = "http") :?> App.ContainerApp
        Expect.isSome containerApp.DaprConfig "Dapr config was not set"
    }
    test "Linked ACR references correct secret" {
        let containerApp = fullContainerAppDeployment.Template.Resources |> List.find(fun r -> r.ResourceId.Name.Value = "servicebus") :?> Farmer.Arm.App.ContainerApp
        Expect.isFalse (containerApp.Secrets |> Map.containsKey (ContainerAppValidation.ContainerAppSettingKey.Create $"{containerRegistryName}-username").OkValue) "Container app did not have linked ACR's secret"
    }
]
