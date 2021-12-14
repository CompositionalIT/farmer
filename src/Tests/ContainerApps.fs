module ContainerApps

open Expecto
open Farmer
open Farmer.Builders
open Newtonsoft.Json.Linq
open Farmer.ContainerApp

let fullContainerAppDeployment =
    let xmasLogs = logAnalytics { name "xmaslogs" }
    let containerRegistryDomain = "myregistry.azurecr.io"
    let containerRegistry = "myimage"
    let version = "1.0.0"
    let containerEnv =
        containerEnvironment {
            name "xmascontainers"
            log_analytics_instance xmasLogs
            add_containers [
                containerApp {
                    name "http"
                    active_revision_mode ActiveRevisionsMode.Single
                    private_docker_image containerRegistryDomain containerRegistry "http" version
                    replicas 1 5
                    add_env_variable "ServiceBusQueueName" "wishrequests"
                    add_secret_parameter "servicebusconnectionkey"
                    ingress_visibility External
                    ingress_target_port 80us
                    ingress_transport Auto
                    dapr_app_id "http"
                    add_scale_rule "http-rule" (ScaleRule.Http { ConcurrentRequests = 100 })
                }
                containerApp {
                    name "servicebus"
                    active_revision_mode ActiveRevisionsMode.Single
                    private_docker_image containerRegistryDomain containerRegistry "servicebus" version
                    replicas 0 3
                    add_env_variable "ServiceBusQueueName" "wishrequests"
                    add_secret_parameter "servicebusconnectionkey"
                    add_scale_rule
                        "sb-keda-scale" 
                        (ScaleRule.ServiceBus {
                            QueueName = "wishrequests"
                            MessageCount = 5
                            SecretRef = "servicebusconnectionkey" })
                }
            ]
        }
    arm {
        add_resources [
            containerEnv
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
        // This is really the parameter for the whole container registry, so we might want to name the paramter soemthing like my-registry.azurecr.io-password
        Expect.isNotNull (jobj.SelectToken("parameters.docker-password-for-myimage")) "Missing 'docker-password-for-myimage' parameter"
    }
    test "Full container environment kubeEnvironment" {
        let kubeEnv = jobj.SelectToken("resources[?(@.name=='xmascontainers')]")
        Expect.equal (kubeEnv.["type"] |> string) "Microsoft.Web/kubeEnvironments" "Incorrect type for kuberenetes environment"
        Expect.equal (kubeEnv.["kind"] |> string) "containerenvironment" "Incorrect kind for kuberenetes environment"        
        let kubeEnvAppLogConfig = jobj.SelectToken("resources[?(@.name=='xmascontainers')].properties.appLogsConfiguration")
        Expect.equal (kubeEnvAppLogConfig.["destination"] |> string) "log-analytics" "Incorrect type for app log config"
        let kubeEnvLogAnalyticsCustomerId = jobj.SelectToken("resources[?(@.name=='xmascontainers')].properties.appLogsConfiguration.logAnalyticsConfiguration")
        Expect.equal (kubeEnvLogAnalyticsCustomerId.["customerId"] |> string) "[reference(resourceId('Microsoft.OperationalInsights/workspaces', 'xmaslogs'), '2020-03-01-preview').customerId]" "Incorrect log analytics customerId reference"
    }
    test "Full container environment containerApp" {
        let httpContainerApp = jobj.SelectToken("resources[?(@.name=='http')]")
        Expect.equal (httpContainerApp.["type"] |> string) "Microsoft.Web/containerApps" "Incorrect type for containerApps"
        Expect.equal (httpContainerApp.["kind"] |> string) "containerapp" "Incorrect kind for containerApps"
        let ingress = httpContainerApp.SelectToken("properties.configuration.ingress")
        Expect.isTrue (ingress.SelectToken("external") |> string |> bool.Parse) "Incorrect external ingress"
        Expect.equal (ingress.SelectToken("targetPort") |> string |> int) 80 "Incorrect targetPort"
        Expect.equal (ingress.SelectToken("transport") |> string) "auto" "Incorrect transport"
        let registries = httpContainerApp.SelectToken("properties.configuration.registries")
        Expect.hasLength registries 1 "Expected 1 registry"
        let firstRegistry = registries |> Seq.head
        Expect.equal (firstRegistry.SelectToken("passwordSecretRef") |> string) "container-registry-password-for-myimage" "Incorrect registry password secretRef"
        Expect.equal (firstRegistry.SelectToken("server") |> string) "myregistry.azurecr.io" "Incorrect registry"
        // The value here doesn't seem quite right. Is it really supposed to be the name of the repository in the registry?
        Expect.equal (firstRegistry.SelectToken("username") |> string) "myimage" "Incorrect registry username"
        let secrets = httpContainerApp.SelectToken("properties.configuration.secrets")
        Expect.hasLength secrets 2 "Expecting 2 secrets"
        Expect.equal (secrets.[0].["name"] |> string) "container-registry-password-for-myimage" "Incorrect name for registry password secret"
        Expect.equal (secrets.[0].["value"] |> string) "[parameters('docker-password-for-myimage')]" "Incorrect password parameter for registry password secret"
        Expect.equal (httpContainerApp.SelectToken("properties.kubeEnvironmentId") |> string) "[resourceId('Microsoft.Web/kubeEnvironments', 'xmascontainers')]" "Incorrect kube environment Id"

        let containers = httpContainerApp.SelectToken("properties.template.containers")
        Expect.hasLength containers 1 "Expected 1 http container"
        let httpContainer = containers |> Seq.head
        Expect.equal (httpContainer.["image"] |> string ) "myregistry.azurecr.io/myimage/http:1.0.0" "Incorrect container image"
        Expect.equal (httpContainer.["name"] |> string ) "http" "Incorrect container name"
        Expect.equal (httpContainer.SelectToken("resources.cpu") |> float ) 0.25 "Incorrect container cpu resources"
        Expect.equal (httpContainer.SelectToken("resources.memory") |> string ) "0.50Gi" "Incorrect container memory resources"

        let scale = httpContainerApp.SelectToken("properties.template.scale")
        Expect.isNotNull scale "properties.scale was null"
        Expect.equal (scale.["minReplicas"] |> int) 1 "Incorrect min replicas"
        Expect.equal (scale.["maxReplicas"] |> int) 5 "Incorrect max replicas"
    }
]
