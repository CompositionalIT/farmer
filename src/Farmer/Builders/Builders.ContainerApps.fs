[<AutoOpen>]
module Farmer.Builders.ContainerApps

open Farmer
open Farmer.Builders
open Farmer.ContainerApp
open Farmer.ContainerAppValidation
open Farmer.Arm.Web
open Farmer.Arm.Web.ContainerApp

type ContainerConfig =
    { ContainerName : string
      DockerImage : DockerImageKind option
      Resources : {| CPU : float<VCores>; Memory : float<Gb> |} }
    member internal this.BuildContainer : Container =
        match this.DockerImage with
        | Some dockerImage ->
            { Name = this.ContainerName
              DockerImage = dockerImage
              Resources = this.Resources }
        | None -> raiseFarmer $"Container '{this.ContainerName}' requires a docker image."

type ContainerAppConfig =
    { Name : ResourceName
      ActiveRevisionsMode : ActiveRevisionsMode
      IngressMode : IngressMode option
      ScaleRules : Map<string, ScaleRule>
      Replicas : {| Min : int; Max : int |} option
      DaprConfig : {| AppId : string |} option
      Secrets : Map<ContainerAppSettingKey, SecretValue>
      EnvironmentVariables : Map<string, EnvVar>
      /// Credentials for image registries used by containers in this environment.
      ImageRegistryCredentials : ImageRegistryAuthentication list
      Containers : ContainerConfig list
      Dependencies : Set<ResourceId>
      ResourceAllocation : ContainerAppResourceLevel option }

type ContainerEnvironmentConfig =
    { Name : ResourceName
      InternalLoadBalancerState : FeatureFlag
      ContainerApps : ContainerAppConfig list
      LogAnalytics : ResourceRef<ContainerEnvironmentConfig>
      Dependencies: Set<ResourceId>
      Tags: Map<string,string> }
    interface IBuilder with
        member this.ResourceId = containerApps.resourceId this.Name
        member this.BuildResources location = [
            let logAnalyticsResourceId = this.LogAnalytics.resourceId this
            { Name = this.Name
              InternalLoadBalancerState = this.InternalLoadBalancerState
              LogAnalytics = logAnalyticsResourceId
              Location = location
              Dependencies = this.Dependencies.Add logAnalyticsResourceId
              Tags = this.Tags }

            match this.LogAnalytics with
            | DeployableResource this resourceId ->
                let workspaceConfig =
                    { Name = resourceId.Name
                      RetentionPeriod = None
                      IngestionSupport = None
                      QuerySupport = None
                      DailyCap = None
                      Tags = Map.empty }
                    :> IBuilder
                yield! workspaceConfig.BuildResources location
            | _ ->
                ()

            for containerApp in this.ContainerApps do
                { Name = containerApp.Name
                  Environment = kubeEnvironments.resourceId this.Name
                  ActiveRevisionsMode = containerApp.ActiveRevisionsMode
                  IngressMode = containerApp.IngressMode
                  ScaleRules = containerApp.ScaleRules
                  Replicas = containerApp.Replicas
                  DaprConfig = containerApp.DaprConfig
                  Secrets = containerApp.Secrets
                  EnvironmentVariables = containerApp.EnvironmentVariables
                  ImageRegistryCredentials = containerApp.ImageRegistryCredentials
                  Containers = containerApp.Containers |> List.map (fun c -> c.BuildContainer)
                  Location = location
                  Dependencies = containerApp.Dependencies }
        ]

type ContainerEnvironmentBuilder() =
    member _.Yield _ =
        { Name = ResourceName.Empty
          InternalLoadBalancerState = Disabled
          ContainerApps = []
          LogAnalytics = ResourceRef.derived (fun cfg -> Arm.LogAnalytics.workspaces.resourceId(cfg.Name - "workspace"))
          Dependencies = Set.empty
          Tags = Map.empty }

    /// Sets the name of the Azure Container App Environment.
    [<CustomOperation "name">]
    member _.Name  (state:ContainerEnvironmentConfig, name:string) = { state with Name = ResourceName name }

    /// Sets the Log Analytics workspace of the Azure Container App.
    [<CustomOperation "log_analytics_instance">]
    member _.SetLogAnalytics  (state:ContainerEnvironmentConfig, logAnalytics:WorkspaceConfig) =
        { state with LogAnalytics = ResourceRef.unmanaged (Arm.LogAnalytics.workspaces.resourceId logAnalytics.Name) }

    /// Sets whether an internal load balancer should be used for load balancing traffic to container app replicas.
    [<CustomOperation "internal_load_balancer_state">]
    member _.SetInternalLoadBalancerState  (state:ContainerEnvironmentConfig, internalLoadBalancerState:FeatureFlag) =
        { state with InternalLoadBalancerState = internalLoadBalancerState }

    /// Adds a container to the Azure Container App Environment.
    [<CustomOperation "add_container">]
    member _.AddContainerApp  (state:ContainerEnvironmentConfig, containerApp:ContainerAppConfig) =
        { state with ContainerApps = containerApp :: state.ContainerApps }

    /// Adds multiple containers to the Azure Container App Environment.
    [<CustomOperation "add_containers">]
    member _.AddContainerApps  (state:ContainerEnvironmentConfig, containerApps:ContainerAppConfig list) =
        { state with ContainerApps = containerApps @ state.ContainerApps }
    /// Support for adding tags to this Container App Environment.
    interface ITaggable<ContainerEnvironmentConfig> with member _.Add state tags = { state with Tags = state.Tags |> Map.merge tags }
    /// Support for adding dependencies to this Container App Environment.
    interface IDependable<ContainerEnvironmentConfig> with member _.Add state newDeps = { state with Dependencies = state.Dependencies + newDeps }

let private defaultResources = {| CPU = 0.25<VCores>; Memory = 0.5<Gb> |}

[<System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)>]
module ResourceOptimisation =
    open System
    let supportedResourceCombinations = ResourceLevels.AllLevels |> Set.toList
    let MIN_CORE_SIZE = 0.05
    let MIN_RAM_SIZE = 0.01

    let optimise (containers:int) (cores:float<VCores>, memory:float<Gb>) =
        let containers = float containers
        let requiredMinCores = supportedResourceCombinations |> List.map (fun (ContainerAppResourceLevel (cores, _)) -> cores) |> List.tryFind (fun cores -> float cores > containers * MIN_CORE_SIZE)
        let requiredMinRam = supportedResourceCombinations |> List.map (fun (ContainerAppResourceLevel (_, ram)) -> ram) |> List.tryFind (fun ram -> float ram > containers * MIN_RAM_SIZE)

        match requiredMinCores, requiredMinRam with
        | Some minCores, Some minRam ->
            if minCores > cores then Error $"Insufficient cores (minimum is {minCores}VCores)."
            elif minRam > memory then Error $"Insufficient memory (minimum is {minRam}Gb)."
            else
                let cores = float cores
                let memory = float memory

                let vcoresPerContainer = Math.Truncate ((cores / containers) * 20.) / 20.
                let remainingCores = cores - (vcoresPerContainer * containers)

                let gbPerContainer = Math.Truncate ((memory / containers) * 100.) / 100.
                let remainingGb = memory - (gbPerContainer * containers)

                Ok [
                    for container in 1. .. containers do
                        if container = 1. then
                            {| CPU = (vcoresPerContainer + remainingCores) * 1.<VCores>
                               Memory = (gbPerContainer + remainingGb) * 1.<Gb> |}
                        else
                            {| CPU = vcoresPerContainer * 1.<VCores>
                               Memory = gbPerContainer * 1.<Gb> |}
                ]
        | None, _ ->
            Error "Insufficient cores"
        | _, None ->
            Error "Insufficient memory"

type ContainerAppBuilder () =
    member _.Yield _ =
        { Name = ResourceName.Empty
          ActiveRevisionsMode = ActiveRevisionsMode.Single
          ImageRegistryCredentials = []
          Containers = []
          Replicas = None
          ScaleRules = Map.empty
          Secrets = Map.empty
          IngressMode = None
          EnvironmentVariables = Map.empty
          DaprConfig = None
          Dependencies = Set.empty
          ResourceAllocation = None }

    member _.Run (state:ContainerAppConfig) =
        let state =
            match state.ResourceAllocation with
            | Some (ContainerAppResourceLevel (cores, memory)) ->
                if state.Containers |> List.exists (fun r -> r.Resources <> defaultResources) then
                    raiseFarmer "You have set resource allocation at the Container App level, but also set the resource levels of some individual containers. If you are using Container App Resource Allocation, you cannot set resources of individual containers."

                let split = ResourceOptimisation.optimise state.Containers.Length (cores, memory)
                match split with
                | Ok resources ->
                    let containersAndResources = List.zip state.Containers resources

                    { state with
                        Containers = [
                            for (container, resources) in containersAndResources do
                                { container with Resources = resources }
                        ]
                    }
                | Error msg ->
                    raiseFarmer msg
            | None ->
                state

        let resourceTotals =
            state.Containers
            |> List.fold (fun (cpu, ram) container ->
                cpu + container.Resources.CPU, ram + container.Resources.Memory
            ) (0.<VCores>, 0.<Gb>)
            |> ContainerAppResourceLevel

        let describe (ContainerAppResourceLevel (cpu, ram)) = $"({cpu}VCores, {ram}Gb)"
        if not (ResourceLevels.AllLevels.Contains resourceTotals) then
            let supported = Set.toList ResourceLevels.AllLevels |> List.map describe |> String.concat "; "
            raiseFarmer $"The container app '{state.Name.Value}' has an invalid combination of CPU and Memory {describe resourceTotals}. All the containers within a container app must have a combined CPU & RAM combination that matches one of the following: [ {supported} ]."

        state

    /// Sets the name of the Azure Container App.
    [<CustomOperation "name">]
    member _.ResourceName (state:ContainerAppConfig, name:string) =
        { state with Name = ResourceName name }
    /// Adds a scale rule to the Azure Container App.
    [<CustomOperation "add_http_scale_rule">]
    member _.AddHttpScaleRule (state:ContainerAppConfig, name, rule:HttpScaleRule) =
        { state with ScaleRules = state.ScaleRules.Add (name, ScaleRule.Http rule) }
    [<CustomOperation "add_servicebus_scale_rule">]
    member _.AddServiceBusScaleRule (state:ContainerAppConfig, name, rule:ServiceBusScaleRule) =
        { state with ScaleRules = state.ScaleRules.Add (name, ScaleRule.ServiceBus rule) }
    [<CustomOperation "add_eventhub_scale_rule">]
    member _.AddEventHubScaleRule (state:ContainerAppConfig, name, rule:EventHubScaleRule) =
        { state with ScaleRules = state.ScaleRules.Add (name, ScaleRule.EventHub rule) }
    [<CustomOperation "add_cpu_scale_rule">]
    member _.AddCpuScaleRule (state:ContainerAppConfig, name, rule) =
        { state with ScaleRules = state.ScaleRules.Add (name, ScaleRule.CPU (Utilisation rule)) }
    member _.AddCpuScaleRule (state:ContainerAppConfig, name, rule) =
        { state with ScaleRules = state.ScaleRules.Add (name, ScaleRule.CPU (AverageValue rule)) }
    [<CustomOperation "add_memory_scale_rule">]
    member _.AddMemScaleRule (state:ContainerAppConfig, name, rule) =
        { state with ScaleRules = state.ScaleRules.Add (name, ScaleRule.Memory (Utilisation rule)) }
    member _.AddMemScaleRule (state:ContainerAppConfig, name, rule) =
        { state with ScaleRules = state.ScaleRules.Add (name, ScaleRule.Memory (AverageValue rule)) }
    [<CustomOperation "add_queue_scale_rule">]
    member this.AddQueueScaleRule (state:ContainerAppConfig, name, storageAccount:StorageAccountConfig, queueName:string, queueLength : int) =
        let state = this.AddEnvironmentVariable (state, $"scalerule-{name}-queue-name", queueName)
        let secretRef = $"scalerule-{name}-connection"
        let state : ContainerAppConfig = this.AddSecretExpression(state, secretRef, storageAccount.Key)
        let queueRule =
            {
                QueueName = queueName
                QueueLength = queueLength
                StorageConnectionSecretRef = secretRef
                AccountName = storageAccount.Name.ResourceName.Value
            }
        { state with ScaleRules = state.ScaleRules.Add (name, ScaleRule.StorageQueue queueRule) }
    [<CustomOperation "add_custom_scale_rule">]
    member _.AddCustomScaleRule (state:ContainerAppConfig, name, rule) =
        { state with ScaleRules = state.ScaleRules.Add (name, ScaleRule.Custom rule) }

    /// Actives or deactivates the ingress of the Azure Container App.
    [<CustomOperation "ingress_state">]
    member _.SetIngressVisibility (state:ContainerAppConfig, enabled) =
        { state with
            IngressMode =
                match enabled with
                | Enabled -> External (80us, None)
                | Disabled -> InternalOnly
                |> Some
        }

    /// Configures the ingress of the Azure Container App.
    [<CustomOperation "ingress_target_port">]
    member _.SetIngressTargetPort (state:ContainerAppConfig, targetPort) =
        { state with
            IngressMode =
                let existingTransport =
                    match state.IngressMode with
                    | Some (External (_, transport)) -> transport
                    | Some InternalOnly | None -> None
                Some (External (targetPort, existingTransport))
        }
    /// Configures the ingress of the Azure Container App.
    [<CustomOperation "ingress_transport">]
    member _.SetIngressTransport (state:ContainerAppConfig, transport) =
        { state with
            IngressMode =
                let existingPort =
                    match state.IngressMode with
                    | Some (External (port, _)) -> port
                    | Some InternalOnly | None -> 80us
                Some (External (existingPort, Some transport))
        }

    /// Configures Dapr in the Azure Container App.
    [<CustomOperation "dapr_app_id">]
    member _.SetDaprAppId (state:ContainerAppConfig, appId) =
        { state with
            DaprConfig = state.DaprConfig |> Option.map (fun c -> {| c with AppId = appId |})
        }

    /// Sets the minimum and maximum replicas to scale the container app.
    [<CustomOperation "replicas">]
    member _.SetReplicas (state:ContainerAppConfig, minReplicas:int, maxReplicas: int) =
        { state with Replicas = Some {| Min = minReplicas; Max = maxReplicas |} }

    /// Adds container image registry credentials for images in this container app.
    [<CustomOperation "add_registry_credentials">]
    member _.AddRegistryCredentials(state:ContainerAppConfig, credentials) =
        { state with ImageRegistryCredentials = state.ImageRegistryCredentials @ (credentials |> List.map ImageRegistryAuthentication.Credential) }

    /// Reference container registries to import their admin credential at deployment time.
    [<CustomOperation "reference_registry_credentials">]
    member _.ReferenceRegistryCredentials(state:ContainerAppConfig, resourceIds) =
        { state with ImageRegistryCredentials = state.ImageRegistryCredentials @ (resourceIds |> List.map ImageRegistryAuthentication.ListCredentials) }

    /// Adds one or more containers to the container app.
    [<CustomOperation "add_containers">]
    member _.AddContainers(state:ContainerAppConfig, containers:ContainerConfig list) =
        { state with Containers = state.Containers @ containers }

    /// Sets the active revision mode of the Azure Container App.
    [<CustomOperation "active_revision_mode">]
    member _.SetActiveRevisionsMode (state:ContainerAppConfig, mode:ActiveRevisionsMode) = { state with ActiveRevisionsMode = mode }

    /// Adds an application secret to the Azure Container App.
    [<CustomOperation "add_secret_parameter">]
    member _.AddSecretParameter (state:ContainerAppConfig, key) =
        let key = (ContainerAppSettingKey.Create key).OkValue
        { state with
            Secrets = state.Secrets.Add (key, ParameterSecret (SecureParameter key.Value))
            EnvironmentVariables = state.EnvironmentVariables.Add (EnvVar.create key.Value key.Value)
        }

    /// Adds an application secret to the Azure Container App.
    [<CustomOperation "add_secret_expression">]
    member _.AddSecretExpression (state:ContainerAppConfig, key, expression) =
        let key = (ContainerAppSettingKey.Create key).OkValue
        { state with
            Secrets = state.Secrets.Add (key, ExpressionSecret expression)
            EnvironmentVariables = state.EnvironmentVariables.Add (EnvVar.createSecure key.Value key.Value)
            Dependencies =
                match expression.Owner with
                | Some owner -> state.Dependencies.Add owner
                | None -> state.Dependencies
        }

    /// Adds a public environment variable to the Azure Container App environment variables.
    [<CustomOperation "add_env_variable">]
    member _.AddEnvironmentVariable (state:ContainerAppConfig, name, value) =
        { state with
            EnvironmentVariables = state.EnvironmentVariables.Add (EnvVar.create name value)
        }

    [<CustomOperation "add_simple_container">]
    member this.AddSimpleContainer (state:ContainerAppConfig, dockerImage, dockerVersion) =
        let container =
            {
                ContainerConfig.ContainerName = state.Name.Value
                DockerImage = Some (PublicImage (dockerImage, Some dockerVersion))
                Resources = defaultResources
            }
        this.AddContainers(state, [ container ])

    [<CustomOperation "allocate_resources">]
    /// Allocates resources equally to all containers in the container app.
    member _.ShareResources (state:ContainerAppConfig, resourceLevel:ContainerAppResourceLevel) =
        { state with ResourceAllocation = Some resourceLevel }

    /// Support for adding dependencies to this Container App.
    interface IDependable<ContainerAppConfig> with
        member _.Add state newDeps = { state with Dependencies = state.Dependencies + newDeps }

type ContainerBuilder () =
    member _.Yield _ =
        { ContainerName = ""
          DockerImage = None
          Resources = defaultResources }

    /// Set docker credentials
    [<CustomOperation "name">]
    member _.ContainerName (state:ContainerConfig, name) =
        { state with ContainerName = name }

        /// Set docker credentials
    [<CustomOperation "private_docker_image">]
    member _.SetPrivateDockerImage (state:ContainerConfig, registry, containerName, version:string) =
        { state with
            DockerImage = Some (PrivateImage (registry, containerName, Option.ofObj version))
        }

    [<CustomOperation "public_docker_image">]
    member _.SetPublicDockerImage (state:ContainerConfig, containerName, version:string) =
        { state with DockerImage = Some (PublicImage (containerName, Option.ofObj version)) }

    [<CustomOperation "cpu_cores">]
    member _.CpuCores (state:ContainerConfig, cpuCount:float<VCores>) =
        let numCores = cpuCount / 1.<VCores>
        if numCores > 2. then raiseFarmer $"'{state.ContainerName}' exceeds maximum CPU cores of 2.0 for containers in containerApps."
        let roundedCpuCount = System.Math.Round(numCores, 2) * 1.<VCores>
        { state with Resources = {| state.Resources with CPU = roundedCpuCount |} }

    [<CustomOperation "memory">]
    member _.Memory (state:ContainerConfig, memory:float<Gb>) =
        let memory = memory / 1.<Gb>
        if memory > 4. then raiseFarmer $"'{state.ContainerName}' exceeds maximum memory of 4.0 Gb for containers in containerApps."
        let roundedMemory = System.Math.Round(memory, 2) * 1.<Gb>
        { state with Resources = {| state.Resources with Memory = roundedMemory |} }

let containerEnvironment = ContainerEnvironmentBuilder ()
let containerApp = ContainerAppBuilder ()
let container = ContainerBuilder ()