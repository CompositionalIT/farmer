[<AutoOpen>]
module Farmer.Builders.ContainerApps

open Farmer
open Farmer.Builders
open Farmer.ContainerApp
open Farmer.ContainerAppValidation
open Farmer.Arm.App
open Farmer.Identity

type ContainerConfig = {
    ContainerName: string
    DockerImage: Containers.DockerImage option
    /// Volume mounts for the container
    VolumeMounts: Map<string, string>
    Resources: {|
        CPU: float<VCores>
        Memory: float<Gb>
        EphemeralStorage: float<Gb> option
    |}
} with

    member internal this.BuildContainer: Container =
        match this.DockerImage with
        | Some dockerImage -> {
            Name = this.ContainerName
            DockerImage = dockerImage
            Resources = this.Resources
            VolumeMounts = this.VolumeMounts
          }
        | None -> raiseFarmer $"Container '{this.ContainerName}' requires a docker image."

type ContainerAppConfig = {
    Name: ResourceName
    ActiveRevisionsMode: ActiveRevisionsMode
    IngressMode: IngressMode option
    ScaleRules: Map<string, ScaleRule>
    Identity: ManagedIdentity
    Replicas: {| Min: int; Max: int |} option
    DaprConfig:
        {|
            AppId: string option
            Port: uint16 option
        |} option
    Secrets: Map<ContainerAppSettingKey, SecretValue>
    EnvironmentVariables: Map<string, EnvVar>
    Volumes: Map<string, Volume>
    /// Credentials for image registries used by containers in this environment.
    ImageRegistryCredentials: ImageRegistryAuthentication list
    Containers: ContainerConfig list
    Dependencies: Set<ResourceId>
} with

    member this.ResourceId = containerApps.resourceId this.Name

    member this.LatestRevisionFqdn =
        ArmExpression
            .reference(containerApps, this.ResourceId)
            .Map(sprintf "%s.latestRevisionFqdn")

type DaprComponent = {
    Name: ResourceName
    ComponentType: string
    IgnoreErrors: bool option
    InitTimeout: string option
    Metadata: Map<string, DaprMetadataValue>
    Scopes: string list
    Secrets: Map<string, SecretValue>
    SecretStoreComponent: ResourceName option
    Version: string
    ResiliencyPolicy: DaprResiliencyPolicy
}

type ContainerEnvironmentConfig = {
    Name: ResourceName
    InternalLoadBalancerState: FeatureFlag
    ContainerApps: ContainerAppConfig list
    AppInsights: AppInsightsConfig option
    LogAnalytics: ResourceRef<ContainerEnvironmentConfig>
    DaprComponents: DaprComponent list
    Dependencies: Set<ResourceId>
    Tags: Map<string, string>
} with

    interface IBuilder with
        member this.ResourceId = managedEnvironments.resourceId this.Name

        member this.BuildResources location = [
            let logAnalyticsResourceId = this.LogAnalytics.resourceId this

            {
                Name = this.Name
                InternalLoadBalancerState = this.InternalLoadBalancerState
                LogAnalytics = logAnalyticsResourceId
                Location = location
                AppInsightsInstrumentationKey = this.AppInsights |> Option.map (fun r -> r.InstrumentationKey)
                Dependencies = this.Dependencies.Add logAnalyticsResourceId
                Tags = this.Tags
            }

            match this.LogAnalytics with
            | DeployableResource this resourceId ->
                let workspaceConfig =
                    {
                        Name = resourceId.Name
                        RetentionPeriod = None
                        IngestionSupport = None
                        QuerySupport = None
                        DailyCap = None
                        Tags = Map.empty
                    }
                    :> IBuilder

                yield! workspaceConfig.BuildResources location
            | _ -> ()

            for containerApp in this.ContainerApps do
                {
                    Name = containerApp.Name
                    Environment = managedEnvironments.resourceId this.Name
                    ActiveRevisionsMode = containerApp.ActiveRevisionsMode
                    Identity = containerApp.Identity
                    IngressMode = containerApp.IngressMode
                    ScaleRules = containerApp.ScaleRules
                    Replicas = containerApp.Replicas
                    DaprConfig =
                        containerApp.DaprConfig
                        |> Option.map (fun x ->
                            match x.AppId with
                            | Some appId -> {| AppId = appId; Port = x.Port |}
                            | None ->
                                raiseFarmer
                                    $"The container app '{containerApp.Name.Value}' requires a Dapr App ID when Dapr is enabled.")
                    Secrets = containerApp.Secrets
                    EnvironmentVariables =
                        let env = containerApp.EnvironmentVariables

                        match this.AppInsights with
                        | Some resource ->
                            env.Add(
                                EnvVar.createSecureExpression
                                    "APPINSIGHTS_INSTRUMENTATIONKEY"
                                    resource.InstrumentationKey
                            )
                        | None -> env
                    ImageRegistryCredentials = containerApp.ImageRegistryCredentials
                    Containers = containerApp.Containers |> List.map (fun c -> c.BuildContainer)
                    Location = location
                    Volumes = containerApp.Volumes
                    Dependencies = containerApp.Dependencies
                }

            for app in this.ContainerApps do
                let uniqueVolumes =
                    app.Volumes
                    |> Seq.choose (ManagedEnvironmentStorage.from (managedEnvironments.resourceId this.Name))
                    |> Seq.distinctBy _.Name

                for volume in uniqueVolumes do
                    volume

            for daprComponent in this.DaprComponents do
                {
                    Name = daprComponent.Name
                    Environment = managedEnvironments.resourceId this.Name
                    ComponentType = daprComponent.ComponentType
                    IgnoreErrors = daprComponent.IgnoreErrors
                    InitTimeout = daprComponent.InitTimeout
                    Metadata = daprComponent.Metadata
                    Scopes = daprComponent.Scopes
                    Secrets = daprComponent.Secrets
                    SecretStoreComponent = daprComponent.SecretStoreComponent
                    Version = daprComponent.Version
                    ResiliencyPolicy = daprComponent.ResiliencyPolicy
                }
        ]

type ContainerEnvironmentBuilder() =
    member _.Yield _ = {
        Name = ResourceName.Empty
        InternalLoadBalancerState = Disabled
        ContainerApps = []
        AppInsights = None
        LogAnalytics = derived (fun cfg -> Arm.LogAnalytics.workspaces.resourceId (cfg.Name - "workspace"))
        DaprComponents = []
        Dependencies = Set.empty
        Tags = Map.empty
    }

    /// Sets the name of the Azure Container App Environment.
    [<CustomOperation "name">]
    member _.Name(state: ContainerEnvironmentConfig, name: string) = { state with Name = ResourceName name }

    /// Adds the instrumentation key to each container app and configures for Dapr.
    [<CustomOperation "app_insights_instance">]
    member _.SetAppInsights(state: ContainerEnvironmentConfig, appInsights: AppInsightsConfig) = {
        state with
            AppInsights = Some appInsights
    }

    /// Sets the Log Analytics workspace of the Azure Container App.
    [<CustomOperation "log_analytics_instance">]
    member _.SetLogAnalytics(state: ContainerEnvironmentConfig, logAnalytics: WorkspaceConfig) = {
        state with
            LogAnalytics = unmanaged (Arm.LogAnalytics.workspaces.resourceId logAnalytics.Name)
    }

    /// Sets whether an internal load balancer should be used for load balancing traffic to container app replicas.
    [<CustomOperation "internal_load_balancer_state">]
    member _.SetInternalLoadBalancerState(state: ContainerEnvironmentConfig, internalLoadBalancerState: FeatureFlag) = {
        state with
            InternalLoadBalancerState = internalLoadBalancerState
    }

    /// Adds a container to the Azure Container App Environment.
    [<CustomOperation "add_container">]
    member _.AddContainerApp(state: ContainerEnvironmentConfig, containerApp: ContainerAppConfig) = {
        state with
            ContainerApps = containerApp :: state.ContainerApps
    }

    /// Adds multiple containers to the Azure Container App Environment.
    [<CustomOperation "add_containers">]
    member _.AddContainerApps(state: ContainerEnvironmentConfig, containerApps: ContainerAppConfig list) = {
        state with
            ContainerApps = containerApps @ state.ContainerApps
    }

    [<CustomOperation "add_dapr_component">]
    member _.AddDaprComponent(state: ContainerEnvironmentConfig, daprComponent: DaprComponent) = {
        state with
            DaprComponents = daprComponent :: state.DaprComponents
    }

    [<CustomOperation "add_dapr_components">]
    member _.AddDaprComponents(state: ContainerEnvironmentConfig, daprComponents: DaprComponent list) = {
        state with
            DaprComponents = daprComponents @ state.DaprComponents
    }

    interface ITaggable<ContainerEnvironmentConfig> with
        /// Adds a tag to this Container App Environment.
        member _.Add state tags = {
            state with
                Tags = state.Tags |> Map.merge tags
        }

    interface IDependable<ContainerEnvironmentConfig> with
        /// Adds an explicit dependency to this Container App Environment.
        member _.Add state newDeps = {
            state with
                Dependencies = state.Dependencies + newDeps
        }

let private supportedResourceCombinations =
    Set [
        0.25<VCores>, 0.5<Gb>
        0.5<VCores>, 1.0<Gb>
        0.75<VCores>, 1.5<Gb>
        1.0<VCores>, 2.0<Gb>
        1.25<VCores>, 2.5<Gb>
        1.5<VCores>, 3.0<Gb>
        1.75<VCores>, 3.5<Gb>
        2.0<VCores>, 4.<Gb>
    ]

let private defaultResources = {|
    CPU = 0.25<VCores>
    Memory = 0.5<Gb>
    EphemeralStorage = None
|}

module Volume =
    let emptyDir volumeName = volumeName, Volume.EmptyDirectory

    let azureFile volumeName (shareName: ResourceName) (storageAccount: Storage.StorageAccountName) accessMode =
        volumeName, Volume.AzureFileShare(shareName, storageAccount, accessMode)

type ContainerAppBuilder() =
    member _.Yield _ = {
        Name = ResourceName.Empty
        ActiveRevisionsMode = ActiveRevisionsMode.Single
        ImageRegistryCredentials = []
        Containers = []
        Replicas = None
        ScaleRules = Map.empty
        Secrets = Map.empty
        IngressMode = None
        Volumes = Map.empty
        Identity = ManagedIdentity.Empty
        EnvironmentVariables = Map.empty
        DaprConfig = None
        Dependencies = Set.empty
    }

    member _.Run(state: ContainerAppConfig) =
        let resourceTotals =
            state.Containers
            |> List.fold
                (fun (cpu, ram) container -> cpu + container.Resources.CPU, ram + container.Resources.Memory)
                (0.<VCores>, 0.<Gb>)

        let describe (cpu, ram) = $"({cpu}VCores, {ram}Gb)"

        if not (supportedResourceCombinations.Contains resourceTotals) then
            let supported =
                Set.toList supportedResourceCombinations
                |> List.map describe
                |> String.concat "; "

            raiseFarmer
                $"The container app '{state.Name.Value}' has an invalid combination of CPU and Memory {describe resourceTotals}. All the containers within a container app must have a combined CPU & RAM combination that matches one of the following: [ {supported} ]."

        state

    interface IIdentity<ContainerAppConfig> with
        member _.Add state updater = {
            state with
                Identity = updater state.Identity
        }

    /// Sets the name of the Azure Container App.
    [<CustomOperation "name">]
    member _.ResourceName(state: ContainerAppConfig, name: string) = { state with Name = ResourceName name }

    /// Adds a scale rule to the Azure Container App.
    [<CustomOperation "add_http_scale_rule">]
    member _.AddHttpScaleRule(state: ContainerAppConfig, name, rule: HttpScaleRule) = {
        state with
            ScaleRules = state.ScaleRules.Add(name, ScaleRule.Http rule)
    }

    [<CustomOperation "add_servicebus_scale_rule">]
    member _.AddServiceBusScaleRule(state: ContainerAppConfig, name, rule: ServiceBusScaleRule) = {
        state with
            ScaleRules = state.ScaleRules.Add(name, ScaleRule.ServiceBus rule)
    }

    [<CustomOperation "add_eventhub_scale_rule">]
    member _.AddEventHubScaleRule(state: ContainerAppConfig, name, rule: EventHubScaleRule) = {
        state with
            ScaleRules = state.ScaleRules.Add(name, ScaleRule.EventHub rule)
    }

    [<CustomOperation "add_cpu_scale_rule">]
    member _.AddCpuScaleRule(state: ContainerAppConfig, name, rule) = {
        state with
            ScaleRules = state.ScaleRules.Add(name, ScaleRule.CPU(Utilization rule))
    }

    member _.AddCpuScaleRule(state: ContainerAppConfig, name, rule) = {
        state with
            ScaleRules = state.ScaleRules.Add(name, ScaleRule.CPU(AverageValue rule))
    }

    [<CustomOperation "add_memory_scale_rule">]
    member _.AddMemScaleRule(state: ContainerAppConfig, name, rule) = {
        state with
            ScaleRules = state.ScaleRules.Add(name, ScaleRule.Memory(Utilization rule))
    }

    member _.AddMemScaleRule(state: ContainerAppConfig, name, rule) = {
        state with
            ScaleRules = state.ScaleRules.Add(name, ScaleRule.Memory(AverageValue rule))
    }

    [<CustomOperation "add_queue_scale_rule">]
    member this.AddQueueScaleRule
        (state: ContainerAppConfig, name, storageAccount: StorageAccountConfig, queueName: string, queueLength: int)
        =
        let state =
            this.AddEnvironmentVariable(state, $"scalerule-{name}-queue-name", queueName)

        let secretRef = $"scalerule-{name}-connection"

        let state: ContainerAppConfig =
            this.AddSecretExpression(state, secretRef, storageAccount.Key)

        let queueRule = {
            QueueName = queueName
            QueueLength = queueLength
            StorageConnectionSecretRef = secretRef
            AccountName = storageAccount.Name.ResourceName.Value
        }

        {
            state with
                ScaleRules = state.ScaleRules.Add(name, ScaleRule.StorageQueue queueRule)
        }

    [<CustomOperation "add_custom_scale_rule">]
    member _.AddCustomScaleRule(state: ContainerAppConfig, name, rule) = {
        state with
            ScaleRules = state.ScaleRules.Add(name, ScaleRule.Custom rule)
    }

    /// Actives or deactivates the ingress of the Azure Container App.
    [<CustomOperation "ingress_state">]
    member _.SetIngressVisibility(state: ContainerAppConfig, enabled) = {
        state with
            IngressMode =
                match enabled with
                | Enabled -> External(80us, None)
                | Disabled -> InternalOnly
                |> Some
    }

    /// Configures the ingress of the Azure Container App.
    [<CustomOperation "ingress_target_port">]
    member _.SetIngressTargetPort(state: ContainerAppConfig, targetPort) = {
        state with
            IngressMode =
                let existingTransport =
                    match state.IngressMode with
                    | Some(External(_, transport)) -> transport
                    | Some InternalOnly
                    | None -> None

                Some(External(targetPort, existingTransport))
    }

    /// Configures the ingress of the Azure Container App.
    [<CustomOperation "ingress_transport">]
    member _.SetIngressTransport(state: ContainerAppConfig, transport) = {
        state with
            IngressMode =
                let existingPort =
                    match state.IngressMode with
                    | Some(External(port, _)) -> port
                    | Some InternalOnly
                    | None -> 80us

                Some(External(existingPort, Some transport))
    }

    /// Configures Dapr App Id in the Azure Container App.
    [<CustomOperation "dapr_app_id">]
    member _.SetDaprAppId(state: ContainerAppConfig, appId) = {
        state with
            DaprConfig =
                state.DaprConfig
                |> Option.map (fun x -> {| x with AppId = Some appId |})
                |> Option.defaultWith (fun () -> {| AppId = Some appId; Port = None |})
                |> Some
    }

    /// Configures Dapr app port in the Azure Container App.
    [<CustomOperation "dapr_app_port">]
    member _.SetDaprAppPort(state: ContainerAppConfig, port) = {
        state with
            DaprConfig =
                state.DaprConfig
                |> Option.map (fun x -> {| x with Port = Some port |})
                |> Option.defaultWith (fun () -> {| AppId = None; Port = Some port |})
                |> Some
    }

    /// Sets the minimum and maximum replicas to scale the container app.
    [<CustomOperation "replicas">]
    member _.SetReplicas(state: ContainerAppConfig, minReplicas: int, maxReplicas: int) = {
        state with
            Replicas =
                Some {|
                    Min = minReplicas
                    Max = maxReplicas
                |}
    }

    /// Adds container image registry credentials for images in this container app.
    [<CustomOperation "add_registry_credentials">]
    member _.AddRegistryCredentials(state: ContainerAppConfig, credentials) = {
        state with
            ImageRegistryCredentials =
                state.ImageRegistryCredentials
                @ (credentials |> List.map ImageRegistryAuthentication.Credential)
    }

    /// Reference container registries to import their admin credential at deployment time.
    [<CustomOperation "reference_registry_credentials">]
    member _.ReferenceRegistryCredentials(state: ContainerAppConfig, resourceIds) = {
        state with
            ImageRegistryCredentials =
                state.ImageRegistryCredentials
                @ (resourceIds |> List.map ImageRegistryAuthentication.ListCredentials)
    }

    /// Adds container app registry managed identity credentials for images in this container app.
    [<CustomOperation "add_managed_identity_registry_credentials">]
    member _.ManagedIdentityRegistryCredentials(state: ContainerAppConfig, credentials) = {
        state with
            ImageRegistryCredentials =
                state.ImageRegistryCredentials
                @ (credentials |> List.map ImageRegistryAuthentication.ManagedIdentityCredential)
    }

    /// Adds one or more containers to the container app.
    [<CustomOperation "add_containers">]
    member _.AddContainers(state: ContainerAppConfig, containers: ContainerConfig list) = {
        state with
            Containers = state.Containers @ containers
    }

    /// Sets the active revision mode of the Azure Container App.
    [<CustomOperation "active_revision_mode">]
    member _.SetActiveRevisionsMode(state: ContainerAppConfig, mode: ActiveRevisionsMode) = {
        state with
            ActiveRevisionsMode = mode
    }

    /// Adds an application secret to the Azure Container App.
    [<CustomOperation "add_secret_parameter">]
    member _.AddSecretParameter(state: ContainerAppConfig, key) =
        let key = (ContainerAppSettingKey.Create key).OkValue

        {
            state with
                Secrets = state.Secrets.Add(key, ParameterSecret(SecureParameter key.Value))
                EnvironmentVariables = state.EnvironmentVariables.Add(EnvVar.createSecure key.Value key.Value)
        }

    /// Adds an application secrets to the Azure Container App.
    [<CustomOperation "add_secret_parameters">]
    member this.AddSecretParameters(state: ContainerAppConfig, keys: #seq<_>) =
        keys |> Seq.fold (fun s k -> this.AddSecretParameter(s, k)) state

    /// Adds an application secret to the Azure Container App.
    [<CustomOperation "add_secret_expression">]
    member _.AddSecretExpression(state: ContainerAppConfig, key, expression) =
        let key = (ContainerAppSettingKey.Create key).OkValue

        {
            state with
                Secrets = state.Secrets.Add(key, ExpressionSecret expression)
                EnvironmentVariables = state.EnvironmentVariables.Add(EnvVar.createSecure key.Value key.Value)
                Dependencies =
                    match expression.Owner with
                    | Some owner -> state.Dependencies.Add owner
                    | None -> state.Dependencies
        }

    /// Adds an application secrets to the Azure Container App.
    [<CustomOperation "add_secret_expressions">]
    member this.AddSecretExpressions(state: ContainerAppConfig, xs: #seq<_>) =
        xs |> Seq.fold (fun s (k, e) -> this.AddSecretExpression(s, k, e)) state


    /// Adds a public environment variable to the Azure Container App environment variables.
    [<CustomOperation "add_env_variable">]
    member _.AddEnvironmentVariable(state: ContainerAppConfig, name, value) = {
        state with
            EnvironmentVariables = state.EnvironmentVariables.Add(EnvVar.create name value)
    }

    /// Adds a public environment variables to the Azure Container App environment variables.
    [<CustomOperation "add_env_variables">]
    member this.AddEnvironmentVariables(state: ContainerAppConfig, vars: #seq<_>) =
        vars |> Seq.fold (fun s (k, v) -> this.AddEnvironmentVariable(s, k, v)) state

    [<CustomOperation "add_simple_container">]
    member this.AddSimpleContainer(state: ContainerAppConfig, dockerImage, dockerVersion) =
        let container = {
            ContainerConfig.ContainerName = state.Name.Value
            DockerImage = Some(Containers.PublicImage(dockerImage, Some dockerVersion))
            Resources = defaultResources
            VolumeMounts = Map.empty
        }

        this.AddContainers(state, [ container ])

    /// Adds volumes to the container app so they can be mounted on containers.
    [<CustomOperation "add_volumes">]
    member _.AddVolumes(state: ContainerAppConfig, volumes) =
        let newVolumes = volumes |> Map.ofSeq

        let updatedVolumes =
            state.Volumes
            |> Map.fold (fun current key vol -> Map.add key vol current) newVolumes

        { state with Volumes = updatedVolumes }

    interface IDependable<ContainerAppConfig> with
        /// Adds an explicit dependency to this Container App.
        member _.Add state newDeps = {
            state with
                Dependencies = state.Dependencies + newDeps
        }

type ContainerBuilder() =
    member _.Yield _ = {
        ContainerName = ""
        DockerImage = None
        Resources = defaultResources
        VolumeMounts = Map.empty
    }

    /// Set docker credentials
    [<CustomOperation "name">]
    member _.ContainerName(state: ContainerConfig, name) = { state with ContainerName = name }

    /// Set docker credentials
    [<CustomOperation "private_docker_image">]
    member _.SetPrivateDockerImage(state: ContainerConfig, registry, containerName, version: string) = {
        state with
            DockerImage = Some(Containers.PrivateImage(registry, containerName, Option.ofObj version))
    }

    [<CustomOperation "public_docker_image">]
    member _.SetPublicDockerImage(state: ContainerConfig, containerName, version: string) = {
        state with
            DockerImage = Some(Containers.PublicImage(containerName, Option.ofObj version))
    }

    [<CustomOperation "cpu_cores">]
    member _.CpuCores(state: ContainerConfig, cpuCount: float<VCores>) =
        let numCores = cpuCount / 1.<VCores>

        if numCores > 2. then
            raiseFarmer $"'{state.ContainerName}' exceeds maximum CPU cores of 2.0 for containers in containerApps."

        let roundedCpuCount = System.Math.Round(numCores, 2) * 1.<VCores>

        {
            state with
                Resources = {|
                    state.Resources with
                        CPU = roundedCpuCount
                |}
        }

    [<CustomOperation "ephemeral_storage">]
    member _.EphemeralStorage(state: ContainerConfig, size: float<Gb>) =
        let size = size / 1.<Gb>

        let roundedSize = System.Math.Round(size, 2) * 1.<Gb>

        {
            state with
                Resources = {|
                    state.Resources with
                        EphemeralStorage = Some roundedSize
                |}
        }

    [<CustomOperation "memory">]
    member _.Memory(state: ContainerConfig, memory: float<Gb>) =
        let memory = memory / 1.<Gb>

        if memory > 4. then
            raiseFarmer $"'{state.ContainerName}' exceeds maximum memory of 4.0 Gb for containers in containerApps."

        let roundedMemory = System.Math.Round(memory, 2) * 1.<Gb>

        {
            state with
                Resources = {|
                    state.Resources with
                        Memory = roundedMemory
                |}
        }

    [<CustomOperation "add_volume_mounts">]
    member _.AddVolumeMounts(state: ContainerConfig, mounts: #seq<_>) = {
        state with
            VolumeMounts =
                mounts
                |> Seq.fold (fun s (volumeName, mountPath) -> s |> Map.add volumeName mountPath) state.VolumeMounts
    }

type DaprComponentBuilder() =
    member _.Yield _ = {
        Name = ResourceName.Empty
        ComponentType = ""
        IgnoreErrors = None
        InitTimeout = None
        Metadata = Map.empty
        Scopes = []
        Secrets = Map.empty
        SecretStoreComponent = None
        Version = ""
        ResiliencyPolicy = {
            InboundTimeoutPolicy = None
            InboundHttpRetryPolicy = None
            InboundCircuitBreakerPolicy = None
            OutboundTimeoutPolicy = None
            OutboundHttpRetryPolicy = None
            OutboundCircuitBreakerPolicy = None
        }
    }

    [<CustomOperation "name">]
    member _.ComponentName(state: DaprComponent, name) = { state with Name = ResourceName name }

    [<CustomOperation "component_type">]
    member _.ComponentType(state: DaprComponent, componentType) = {
        state with
            ComponentType = componentType
    }

    [<CustomOperation "ignore_errors">]
    member _.IgnoreErrors(state: DaprComponent, ignoreErrors) = {
        state with
            IgnoreErrors = Some ignoreErrors
    }

    [<CustomOperation "init_timeout">]
    member _.InitTimeout(state: DaprComponent, initTimeout) = {
        state with
            InitTimeout = Some initTimeout
    }

    [<CustomOperation "add_metadata">]
    member _.AddMetadata(state: DaprComponent, metadataName, value) = {
        state with
            Metadata = state.Metadata |> Map.add metadataName (Value value)
    }

    [<CustomOperation "add_secret_metadata">]
    member _.AddSecretMetadata(state: DaprComponent, metadataName, secretName, secretValue) = {
        state with
            Metadata = state.Metadata |> Map.add metadataName (SecretRef secretName)
            Secrets = state.Secrets |> Map.add secretName (ExpressionSecret secretValue)
    }

    [<CustomOperation "add_secret_metadata">]
    member _.AddSecretMetadata(state: DaprComponent, metadataName, secretName, secretValue) = {
        state with
            Metadata = state.Metadata |> Map.add metadataName (SecretRef secretName)
            Secrets = state.Secrets |> Map.add secretName (ParameterSecret secretValue)
    }

    [<CustomOperation "add_scope">]
    member _.AddScope(state: DaprComponent, scopes) = {
        state with
            Scopes = scopes :: state.Scopes
    }

    [<CustomOperation "add_scopes">]
    member _.AddScopes(state: DaprComponent, scopes) = {
        state with
            Scopes = scopes @ state.Scopes
    }

    static member private GetDaprAppId(containerAppConfig: ContainerAppConfig) =
        containerAppConfig.DaprConfig
        |> Option.bind (fun x -> x.AppId)
        |> Option.defaultWith (fun () ->
            raiseFarmer
                $"Container App '{containerAppConfig.Name.Value}' requires a Dapr App ID when linked to Dapr component.")

    [<CustomOperation "add_scope">]
    member _.AddScope(state: DaprComponent, containerAppConfig: ContainerAppConfig) = {
        state with
            Scopes = DaprComponentBuilder.GetDaprAppId containerAppConfig :: state.Scopes
    }

    [<CustomOperation "add_scopes">]
    member _.AddScopes(state: DaprComponent, containerAppConfigs: ContainerAppConfig list) =
        let scopes = containerAppConfigs |> List.map DaprComponentBuilder.GetDaprAppId

        {
            state with
                Scopes = scopes @ state.Scopes
        }

    [<CustomOperation "secret_store_component">]
    member _.SecretStoreComponent(state: DaprComponent, comp: DaprComponent) = {
        state with
            SecretStoreComponent = Some comp.Name
    }

    [<CustomOperation "version">]
    member _.Version(state: DaprComponent, version: string) = { state with Version = version }

    /// <summary>
    /// Shorthand for
    /// <code>
    /// component_type "bindings.cron"
    /// version "v1"
    /// add_metadata "schedule" cronExpression
    /// </code>
    /// </summary>
    [<CustomOperation "cron_binding">]
    member _.CronBinding(state: DaprComponent, cronExpression: string) = {
        state with
            ComponentType = "bindings.cron"
            Version = "v1"
            Metadata = state.Metadata |> Map.add "schedule" (Value cronExpression)
    }

    /// <summary>
    /// Shorthand for
    /// <code>
    /// component_type "bindings.azure.storagequeues"
    /// version "v1"
    /// add_secret_metadata "accountKey" "accountkey" storageAccountKey
    /// add_metadata "accountName" storageAccountName
    /// add_metadata "queueName" queueName
    /// add_metadata "decodeBase64" true
    /// </code>
    /// </summary>
    [<CustomOperation "azure_storage_queue_binding">]
    member _.AzureStorageQueueBinding(state: DaprComponent, storageAccount: StorageAccountConfig, queueName: string) =
        let accountKey =
            ArmExpression.create (
                $"listKeys({storageAccount.ResourceId.ArmExpression.Value}, '2017-10-01').keys[0].value",
                storageAccount.ResourceId
            )

        let accountKeySecretKey = "accountkey"

        {
            state with
                ComponentType = "bindings.azure.storagequeues"
                Version = "v1"
                Secrets = state.Secrets |> Map.add accountKeySecretKey (ExpressionSecret accountKey)
                Metadata =
                    state.Metadata
                    |> Map.add "accountName" (Value storageAccount.Name.ResourceName.Value)
                    |> Map.add "accountKey" (SecretRef accountKeySecretKey)
                    |> Map.add "queueName" (Value queueName)
                    |> Map.add "decodeBase64" (Value "true")
        }

    /// <summary>
    /// Shorthand for
    /// <code>
    /// component_type "pubsub.azure.servicebus.queues"
    /// version "v1"
    /// add_secret_metadata "connectionString" "connectionstring" serviceBusConnectionString
    /// </code>
    /// </summary>
    [<CustomOperation "azure_servicebus_queues_pubsub">]
    member _.AzureServiceBusQueuesPubsub(state: DaprComponent, serviceBus: ServiceBusConfig) =
        let connectionStringSecretKey = "connectionstring"

        {
            state with
                ComponentType = "pubsub.azure.servicebus.queues"
                Version = "v1"
                Secrets =
                    state.Secrets
                    |> Map.add connectionStringSecretKey (ExpressionSecret serviceBus.NamespaceDefaultConnectionString)
                Metadata =
                    state.Metadata
                    |> Map.add "connectionString" (SecretRef connectionStringSecretKey)
        }

    /// <summary>
    /// Sets the timeout policy for inbound requests.
    /// </summary>
    [<CustomOperation "resiliency_policy_inbound_timeout">]
    member _.ResiliencyPolicyInboundTimeout(state: DaprComponent, responseTimeoutInSeconds: int) = {
        state with
            ResiliencyPolicy.InboundTimeoutPolicy =
                Some {
                    ResponseTimeoutInSeconds = responseTimeoutInSeconds
                }
    }

    /// <summary>
    /// Sets the HTTP retry policy for inbound requests.
    /// </summary>
    [<CustomOperation "resiliency_policy_inbound_http_retry">]
    member _.ResiliencyPolicyInboundHttpRetry
        (state: DaprComponent, maxRetries: int, initialDelayInMilliseconds: int, maxIntervalInMilliseconds: int)
        =
        {
            state with
                ResiliencyPolicy.InboundHttpRetryPolicy =
                    Some {
                        MaxRetries = maxRetries
                        RetryBackOff = {
                            InitialDelayInMilliseconds = initialDelayInMilliseconds
                            MaxIntervalInMilliseconds = maxIntervalInMilliseconds
                        }
                    }
        }

    /// <summary>
    /// Sets the circuit breaker policy for inbound requests.
    /// </summary>
    [<CustomOperation "resiliency_policy_inbound_circuit_breaker">]
    member _.ResiliencyPolicyInboundCircuitBreaker
        (state: DaprComponent, consecutiveErrors: int, timeoutInSeconds: int, ?intervalInSeconds: int)
        =
        {
            state with
                ResiliencyPolicy.InboundCircuitBreakerPolicy =
                    Some {
                        ConsecutiveErrors = consecutiveErrors
                        TimeoutInSeconds = timeoutInSeconds
                        IntervalInSeconds = intervalInSeconds
                    }
        }

    /// <summary>
    /// Sets the timeout policy for outbound requests.
    /// </summary>
    [<CustomOperation "resiliency_policy_outbound_timeout">]
    member _.ResiliencyPolicyOutboundTimeout(state: DaprComponent, responseTimeoutInSeconds: int) = {
        state with
            ResiliencyPolicy.OutboundTimeoutPolicy =
                Some {
                    ResponseTimeoutInSeconds = responseTimeoutInSeconds
                }
    }

    /// <summary>
    /// Sets the HTTP retry policy for outbound requests.
    /// </summary>
    [<CustomOperation "resiliency_policy_outbound_http_retry">]
    member _.ResiliencyPolicyOutboundHttpRetry
        (state: DaprComponent, maxRetries: int, initialDelayInMilliseconds: int, maxIntervalInMilliseconds: int)
        =
        {
            state with
                ResiliencyPolicy.OutboundHttpRetryPolicy =
                    Some {
                        MaxRetries = maxRetries
                        RetryBackOff = {
                            InitialDelayInMilliseconds = initialDelayInMilliseconds
                            MaxIntervalInMilliseconds = maxIntervalInMilliseconds
                        }
                    }
        }

    /// <summary>
    /// Sets the circuit breaker policy for outbound requests.
    /// </summary>
    [<CustomOperation "resiliency_policy_outbound_circuit_breaker">]
    member _.ResiliencyPolicyOutboundCircuitBreaker
        (state: DaprComponent, consecutiveErrors: int, timeoutInSeconds: int, ?intervalInSeconds: int)
        =
        {
            state with
                ResiliencyPolicy.OutboundCircuitBreakerPolicy =
                    Some {
                        ConsecutiveErrors = consecutiveErrors
                        TimeoutInSeconds = timeoutInSeconds
                        IntervalInSeconds = intervalInSeconds
                    }
        }

let containerEnvironment = ContainerEnvironmentBuilder()

let containerApp = ContainerAppBuilder()
let container = ContainerBuilder()
let daprComponent = DaprComponentBuilder()