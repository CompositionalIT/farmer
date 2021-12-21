module ContainerApps

open Expecto
open FsCheck
open Farmer
open Farmer.Builders
open Newtonsoft.Json.Linq
open Farmer.ContainerApp

let fullContainerAppDeployment =
    let containerLogs = logAnalytics { name "containerlogs" }
    let containerRegistryDomain = "myregistry.azurecr.io"
    let containerRegistryUsername = "myregistry"
    let version = "1.0.0"
    let containerEnv =
        containerEnvironment {
            name "kubecontainerenv"
            log_analytics_instance containerLogs
            add_containers [
                containerApp {
                    name "http"
                    active_revision_mode Single
                    add_registry_credentials [
                        registry containerRegistryDomain containerRegistryUsername
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
                    name "servicebus"
                    active_revision_mode Single
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
        ]
    }

let standardTests = testList "Standard Tests" [
    let jsonTemplate = fullContainerAppDeployment.Template |> Writer.toJson
    let jobj = JObject.Parse jsonTemplate

    test "Container automatically creates a log analytics workspace" {
        let env : IBuilder = containerEnvironment { name "testca" } :> _
        let resources = env.BuildResources Location.NorthEurope
        Expect.exists resources (fun r -> r.ResourceId.Name.Value = "testca-workspace") "No Log Analytics workspace was created."
    }

    test "Full container environment parameters" {
        Expect.hasLength jobj.["parameters"] 2 "Expecting 2 parameters"
        Expect.isNotNull (jobj.SelectToken("parameters.servicebusconnectionkey")) "Missing 'servicebusconnectionkey' parameter"
        Expect.isNotNull (jobj.SelectToken("parameters['myregistry.azurecr.io-password']")) "Missing 'myregistry.azurecr.io-password' parameter"
    }
    test "Full container environment kubeEnvironment" {
        let kubeEnv = jobj.SelectToken("resources[?(@.name=='kubecontainerenv')]")
        Expect.equal (kubeEnv.["type"] |> string) "Microsoft.Web/kubeEnvironments" "Incorrect type for kuberenetes environment"
        Expect.equal (kubeEnv.["kind"] |> string) "containerenvironment" "Incorrect kind for kuberenetes environment"
        let kubeEnvAppLogConfig = jobj.SelectToken("resources[?(@.name=='kubecontainerenv')].properties.appLogsConfiguration")
        Expect.equal (kubeEnvAppLogConfig.["destination"] |> string) "log-analytics" "Incorrect type for app log config"
        let kubeEnvLogAnalyticsCustomerId = jobj.SelectToken("resources[?(@.name=='kubecontainerenv')].properties.appLogsConfiguration.logAnalyticsConfiguration")
        Expect.equal (kubeEnvLogAnalyticsCustomerId.["customerId"] |> string) "[reference(resourceId('Microsoft.OperationalInsights/workspaces', 'containerlogs'), '2020-03-01-preview').customerId]" "Incorrect log analytics customerId reference"
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
        Expect.equal (firstRegistry.SelectToken("passwordSecretRef") |> string) "myregistry" "Incorrect registry password secretRef"
        Expect.equal (firstRegistry.SelectToken("server") |> string) "myregistry.azurecr.io" "Incorrect registry"
        // The value here doesn't seem quite right. Is it really supposed to be the name of the repository in the registry?
        Expect.equal (firstRegistry.SelectToken("username") |> string) "myregistry" "Incorrect registry username"
        let secrets = httpContainerApp.SelectToken("properties.configuration.secrets")
        Expect.hasLength secrets 2 "Expecting 2 secrets"
        Expect.equal (secrets.[0].["name"] |> string) "myregistry" "Incorrect name for registry password secret"
        Expect.equal (secrets.[0].["value"] |> string) "[parameters('myregistry.azurecr.io-password')]" "Incorrect password parameter for registry password secret"
        Expect.equal (httpContainerApp.SelectToken("properties.kubeEnvironmentId") |> string) "[resourceId('Microsoft.Web/kubeEnvironments', 'kubecontainerenv')]" "Incorrect kube environment Id"

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
]

type Inputs = PositiveInt * float<VCores> * float<Gb>
type ValidInput = ValidInput of Inputs
type InvalidInput = InvalidInput of Inputs

let basicGen = gen {
    let! (ContainerAppResourceLevel (cores,gb)) = Gen.elements ResourceLevels.AllLevels
    let! containers = Arb.Default.PositiveInt () |> Arb.filter(fun (PositiveInt s) -> s < 20) |> Arb.toGen
    return containers, cores, gb
}

let shrinker checker (con:PositiveInt, cor, mem) =
    [
        if con.Get > 1 then PositiveInt (con.Get - 1), cor, mem
        if cor > 0.25<VCores> then con, cor - 0.25<VCores>, mem - 0.5<Gb>
    ]
    |> List.filter checker

type ResourceArb =
    static member IsValid (PositiveInt con, cor, mem) =
        let cores = ResourceLevels.AllLevels |> Seq.map(fun (ContainerAppResourceLevel (cores, _)) -> cores) |> Seq.find (fun cores -> float cores > float con * 0.05) <= cor
        let memory = ResourceLevels.AllLevels |> Seq.map(fun (ContainerAppResourceLevel (_, mem)) -> mem) |> Seq.find (fun mem -> float mem > float con * 0.01) <= mem
        cores && memory

    static member ValidInputs () =
        { new Arbitrary<ValidInput> () with
            override _.Generator = basicGen |> Gen.filter ResourceArb.IsValid |> Gen.map ValidInput
            override _.Shrinker (ValidInput inputs) = inputs |> shrinker ResourceArb.IsValid |> Seq.map ValidInput
        }
    static member InvalidInputs () =
        { new Arbitrary<InvalidInput> () with
            override _.Generator = basicGen |> Gen.filter (ResourceArb.IsValid >> not) |> Gen.map InvalidInput
            override _.Shrinker (InvalidInput inputs) = inputs |> shrinker (ResourceArb.IsValid >> not) |> Seq.map InvalidInput
        }

let config = { FsCheckConfig.defaultConfig with arbitrary = [ typeof<ResourceArb> ] }

let pbTests = testList "Property Based Tests" [
    testPropertyWithConfig config "totals always equal input" <| fun (ValidInput(PositiveInt containers, cores, memory)) ->
        let split = ResourceOptimisation.optimise containers (cores, memory)
        match split with
        | Ok split ->
            let correctCores = split |> List.sumBy (fun s -> s.CPU) |> decimal = decimal cores
            let correctRam = split |> List.sumBy (fun s -> s.Memory) |> decimal = decimal memory
            correctCores && correctRam
        | Error msg ->
            failwith msg

    testPropertyWithConfig config "gives back correct number of resource allocations" <| fun (ValidInput(PositiveInt containers, cores, memory)) ->
        let split = ResourceOptimisation.optimise containers (cores, memory)
        match split with
        | Ok split -> split.Length = containers
        | Error msg -> failwith msg

    testPropertyWithConfig config "never generates an invalid resource allocation" <| fun (ValidInput(PositiveInt containers, cores, memory)) ->
        let split = ResourceOptimisation.optimise containers (cores, memory)
        match split with
        | Ok split -> split |> List.forall(fun s -> s.CPU >= 0.05<VCores> && s.Memory >= 0.01<Gb>)
        | Error msg -> failwith msg

    testPropertyWithConfig config "fails if the inputs are invalid" <| fun (InvalidInput(PositiveInt containers, cores, memory)) ->
        let split = ResourceOptimisation.optimise containers (cores, memory)
        match split with
        | Ok _ -> failwith "Should have failed."
        | Error _ -> true
]

let tests = testList "Container Apps" [
    standardTests
    pbTests
]