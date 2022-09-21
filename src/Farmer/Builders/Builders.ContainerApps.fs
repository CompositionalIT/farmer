[<AutoOpen>]
module Farmer.Builders.ContainerApps

open Farmer
open Farmer.Builders
open Farmer.ContainerApp
open Farmer.ContainerAppValidation
open Farmer.Arm.App
open Farmer.Identity

type ContainerConfig =
    {
        ContainerName: string
        DockerImage: Containers.DockerImage option
        /// Volume mounts for the container
        VolumeMounts: Map<string, string>
        Resources: {| CPU: float<VCores>
                      Memory: float<Gb>
                      EphemeralStorage: float<Gb> option |}
    }

    member internal this.BuildContainer: Container =
        match this.DockerImage with
        | Some dockerImage ->
            {
                Name = this.ContainerName
                DockerImage = dockerImage
                Resources = this.Resources
                VolumeMounts = this.VolumeMounts
            }
        | None -> raiseFarmer $"Container '{this.ContainerName}' requires a docker image."

type ContainerAppConfig =
    {
        Name: ResourceName
        ActiveRevisionsMode: ActiveRevisionsMode
        IngressMode: IngressMode option
        ScaleRules: Map<string, ScaleRule>
        Identity: ManagedIdentity
        Replicas: {| Min: int; Max: int |} option
        DaprConfig: {| AppId: string |} option
        Secrets: Map<ContainerAppSettingKey, SecretValue>
        EnvironmentVariables: Map<string, EnvVar>
        Volumes: Map<string, Volume>
        /// Credentials for image registries used by containers in this environment.
        ImageRegistryCredentials: ImageRegistryAuthentication list
        Containers: ContainerConfig list
        Dependencies: Set<ResourceId>
    }

    member this.ResourceId = containerApps.resourceId this.Name

    member this.LatestRevisionFqdn =
        ArmExpression
            .reference(containerApps, this.ResourceId)
            .Map(sprintf "%s.latestRevisionFqdn")

type ContainerEnvironmentConfig =
    {
        Name: ResourceName
        InternalLoadBalancerState: FeatureFlag
        ContainerApps: ContainerAppConfig list
        LogAnalytics: ResourceRef<ContainerEnvironmentConfig>
        Dependencies: Set<ResourceId>
        Tags: Map<string, string>
    }

    interface IBuilder with
        member this.ResourceId = managedEnvironments.resourceId this.Name

        member this.BuildResources location =
            [
                let logAnalyticsResourceId = this.LogAnalytics.resourceId this

                {
                    Name = this.Name
                    InternalLoadBalancerState = this.InternalLoadBalancerState
                    LogAnalytics = logAnalyticsResourceId
                    Location = location
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
                        DaprConfig = containerApp.DaprConfig
                        Secrets = containerApp.Secrets
                        EnvironmentVariables = containerApp.EnvironmentVariables
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
                        |> Seq.distinctBy (fun v -> v.Name)

                    for volume in uniqueVolumes do
                        volume
            ]

type ContainerEnvironmentBuilder() =
    member _.Yield _ =
        {
            Name = ResourceName.Empty
            InternalLoadBalancerState = Disabled
            ContainerApps = []
            LogAnalytics =
                ResourceRef.derived (fun cfg -> Arm.LogAnalytics.workspaces.resourceId (cfg.Name - "workspace"))
            Dependencies = Set.empty
            Tags = Map.empty
        }

    /// Sets the name of the Azure Container App Environment.
    [<CustomOperation "name">]
    member _.Name(state: ContainerEnvironmentConfig, name: string) = { state with Name = ResourceName name }

    /// Sets the Log Analytics workspace of the Azure Container App.
    [<CustomOperation "log_analytics_instance">]
    member _.SetLogAnalytics(state: ContainerEnvironmentConfig, logAnalytics: WorkspaceConfig) =
        { state with
            LogAnalytics = ResourceRef.unmanaged (Arm.LogAnalytics.workspaces.resourceId logAnalytics.Name)
        }

    /// Sets whether an internal load balancer should be used for load balancing traffic to container app replicas.
    [<CustomOperation "internal_load_balancer_state">]
    member _.SetInternalLoadBalancerState(state: ContainerEnvironmentConfig, internalLoadBalancerState: FeatureFlag) =
        { state with
            InternalLoadBalancerState = internalLoadBalancerState
        }

    /// Adds a container to the Azure Container App Environment.
    [<CustomOperation "add_container">]
    member _.AddContainerApp(state: ContainerEnvironmentConfig, containerApp: ContainerAppConfig) =
        { state with
            ContainerApps = containerApp :: state.ContainerApps
        }

    /// Adds multiple containers to the Azure Container App Environment.
    [<CustomOperation "add_containers">]
    member _.AddContainerApps(state: ContainerEnvironmentConfig, containerApps: ContainerAppConfig list) =
        { state with
            ContainerApps = containerApps @ state.ContainerApps
        }

    /// Support for adding tags to this Container App Environment.
    interface ITaggable<ContainerEnvironmentConfig> with
        member _.Add state tags =
            { state with
                Tags = state.Tags |> Map.merge tags
            }
    /// Support for adding dependencies to this Container App Environment.
    interface IDependable<ContainerEnvironmentConfig> with
        member _.Add state newDeps =
            { state with
                Dependencies = state.Dependencies + newDeps
            }

let private supportedResourceCombinations =
    Set
        [
            0.25<VCores>, 0.5<Gb>
            0.5<VCores>, 1.0<Gb>
            0.75<VCores>, 1.5<Gb>
            1.0<VCores>, 2.0<Gb>
            1.25<VCores>, 2.5<Gb>
            1.5<VCores>, 3.0<Gb>
            1.75<VCores>, 3.5<Gb>
            2.0<VCores>, 4.<Gb>
        ]

let private defaultResources =
    {|
        CPU = 0.25<VCores>
        Memory = 0.5<Gb>
        EphemeralStorage = None
    |}

module Volume =
    let emptyDir volumeName = volumeName, Volume.EmptyDirectory

    let azureFile volumeName (shareName: ResourceName) (storageAccount: Storage.StorageAccountName) accessMode =
        volumeName, Volume.AzureFileShare(shareName, storageAccount, accessMode)

type ContainerAppBuilder() =
    member _.Yield _ =
        {
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
        member _.Add state updater =
            { state with
                Identity = updater state.Identity
            }

    /// Sets the name of the Azure Container App.
    [<CustomOperation "name">]
    member _.ResourceName(state: ContainerAppConfig, name: string) = { state with Name = ResourceName name }

    /// Adds a scale rule to the Azure Container App.
    [<CustomOperation "add_http_scale_rule">]
    member _.AddHttpScaleRule(state: ContainerAppConfig, name, rule: HttpScaleRule) =
        { state with
            ScaleRules = state.ScaleRules.Add(name, ScaleRule.Http rule)
        }

    [<CustomOperation "add_servicebus_scale_rule">]
    member _.AddServiceBusScaleRule(state: ContainerAppConfig, name, rule: ServiceBusScaleRule) =
        { state with
            ScaleRules = state.ScaleRules.Add(name, ScaleRule.ServiceBus rule)
        }

    [<CustomOperation "add_eventhub_scale_rule">]
    member _.AddEventHubScaleRule(state: ContainerAppConfig, name, rule: EventHubScaleRule) =
        { state with
            ScaleRules = state.ScaleRules.Add(name, ScaleRule.EventHub rule)
        }

    [<CustomOperation "add_cpu_scale_rule">]
    member _.AddCpuScaleRule(state: ContainerAppConfig, name, rule) =
        { state with
            ScaleRules = state.ScaleRules.Add(name, ScaleRule.CPU(Utilisation rule))
        }

    member _.AddCpuScaleRule(state: ContainerAppConfig, name, rule) =
        { state with
            ScaleRules = state.ScaleRules.Add(name, ScaleRule.CPU(AverageValue rule))
        }

    [<CustomOperation "add_memory_scale_rule">]
    member _.AddMemScaleRule(state: ContainerAppConfig, name, rule) =
        { state with
            ScaleRules = state.ScaleRules.Add(name, ScaleRule.Memory(Utilisation rule))
        }

    member _.AddMemScaleRule(state: ContainerAppConfig, name, rule) =
        { state with
            ScaleRules = state.ScaleRules.Add(name, ScaleRule.Memory(AverageValue rule))
        }

    [<CustomOperation "add_queue_scale_rule">]
    member this.AddQueueScaleRule
        (
            state: ContainerAppConfig,
            name,
            storageAccount: StorageAccountConfig,
            queueName: string,
            queueLength: int
        ) =
        let state =
            this.AddEnvironmentVariable(state, $"scalerule-{name}-queue-name", queueName)

        let secretRef = $"scalerule-{name}-connection"

        let state: ContainerAppConfig =
            this.AddSecretExpression(state, secretRef, storageAccount.Key)

        let queueRule =
            {
                QueueName = queueName
                QueueLength = queueLength
                StorageConnectionSecretRef = secretRef
                AccountName = storageAccount.Name.ResourceName.Value
            }

        { state with
            ScaleRules = state.ScaleRules.Add(name, ScaleRule.StorageQueue queueRule)
        }

    [<CustomOperation "add_custom_scale_rule">]
    member _.AddCustomScaleRule(state: ContainerAppConfig, name, rule) =
        { state with
            ScaleRules = state.ScaleRules.Add(name, ScaleRule.Custom rule)
        }

    /// Actives or deactivates the ingress of the Azure Container App.
    [<CustomOperation "ingress_state">]
    member _.SetIngressVisibility(state: ContainerAppConfig, enabled) =
        { state with
            IngressMode =
                match enabled with
                | Enabled -> External(80us, None)
                | Disabled -> InternalOnly
                |> Some
        }

    /// Configures the ingress of the Azure Container App.
    [<CustomOperation "ingress_target_port">]
    member _.SetIngressTargetPort(state: ContainerAppConfig, targetPort) =
        { state with
            IngressMode =
                let existingTransport =
                    match state.IngressMode with
                    | Some (External (_, transport)) -> transport
                    | Some InternalOnly
                    | None -> None

                Some(External(targetPort, existingTransport))
        }

    /// Configures the ingress of the Azure Container App.
    [<CustomOperation "ingress_transport">]
    member _.SetIngressTransport(state: ContainerAppConfig, transport) =
        { state with
            IngressMode =
                let existingPort =
                    match state.IngressMode with
                    | Some (External (port, _)) -> port
                    | Some InternalOnly
                    | None -> 80us

                Some(External(existingPort, Some transport))
        }

    /// Configures Dapr in the Azure Container App.
    [<CustomOperation "dapr_app_id">]
    member _.SetDaprAppId(state: ContainerAppConfig, appId) =
        { state with
            DaprConfig = state.DaprConfig |> Option.map (fun c -> {| c with AppId = appId |})
        }

    /// Sets the minimum and maximum replicas to scale the container app.
    [<CustomOperation "replicas">]
    member _.SetReplicas(state: ContainerAppConfig, minReplicas: int, maxReplicas: int) =
        { state with
            Replicas =
                Some
                    {|
                        Min = minReplicas
                        Max = maxReplicas
                    |}
        }

    /// Adds container image registry credentials for images in this container app.
    [<CustomOperation "add_registry_credentials">]
    member _.AddRegistryCredentials(state: ContainerAppConfig, credentials) =
        { state with
            ImageRegistryCredentials =
                state.ImageRegistryCredentials
                @ (credentials |> List.map ImageRegistryAuthentication.Credential)
        }

    /// Reference container registries to import their admin credential at deployment time.
    [<CustomOperation "reference_registry_credentials">]
    member _.ReferenceRegistryCredentials(state: ContainerAppConfig, resourceIds) =
        { state with
            ImageRegistryCredentials =
                state.ImageRegistryCredentials
                @ (resourceIds |> List.map ImageRegistryAuthentication.ListCredentials)
        }

    /// Adds container app registry managed identity credentials for images in this container app.
    [<CustomOperation "add_managed_identity_registry_credentials">]
    member _.ManagedIdentityRegistryCredentials(state: ContainerAppConfig, credentials) =
        { state with
            ImageRegistryCredentials =
                state.ImageRegistryCredentials
                @ (credentials |> List.map ImageRegistryAuthentication.ManagedIdentityCredential)
        }

    /// Adds one or more containers to the container app.
    [<CustomOperation "add_containers">]
    member _.AddContainers(state: ContainerAppConfig, containers: ContainerConfig list) =
        { state with
            Containers = state.Containers @ containers
        }

    /// Sets the active revision mode of the Azure Container App.
    [<CustomOperation "active_revision_mode">]
    member _.SetActiveRevisionsMode(state: ContainerAppConfig, mode: ActiveRevisionsMode) =
        { state with
            ActiveRevisionsMode = mode
        }

    /// Adds an application secret to the Azure Container App.
    [<CustomOperation "add_secret_parameter">]
    member _.AddSecretParameter(state: ContainerAppConfig, key) =
        let key = (ContainerAppSettingKey.Create key).OkValue

        { state with
            Secrets = state.Secrets.Add(key, ParameterSecret(SecureParameter key.Value))
            EnvironmentVariables = state.EnvironmentVariables.Add(EnvVar.createSecure key.Value key.Value)
        }

    /// Adds an application secrets to the Azure Container App.
    [<CustomOperation "add_secret_parameters">]
    member __.AddSecretParameters(state: ContainerAppConfig, keys: #seq<_>) =
        keys |> Seq.fold (fun s k -> __.AddSecretParameter(s, k)) state

    /// Adds an application secret to the Azure Container App.
    [<CustomOperation "add_secret_expression">]
    member _.AddSecretExpression(state: ContainerAppConfig, key, expression) =
        let key = (ContainerAppSettingKey.Create key).OkValue

        { state with
            Secrets = state.Secrets.Add(key, ExpressionSecret expression)
            EnvironmentVariables = state.EnvironmentVariables.Add(EnvVar.createSecure key.Value key.Value)
            Dependencies =
                match expression.Owner with
                | Some owner -> state.Dependencies.Add owner
                | None -> state.Dependencies
        }

    /// Adds an application secrets to the Azure Container App.
    [<CustomOperation "add_secret_expressions">]
    member __.AddSecretExpressions(state: ContainerAppConfig, xs: #seq<_>) =
        xs |> Seq.fold (fun s (k, e) -> __.AddSecretExpression(s, k, e)) state


    /// Adds a public environment variable to the Azure Container App environment variables.
    [<CustomOperation "add_env_variable">]
    member _.AddEnvironmentVariable(state: ContainerAppConfig, name, value) =
        { state with
            EnvironmentVariables = state.EnvironmentVariables.Add(EnvVar.create name value)
        }

    /// Adds a public environment variables to the Azure Container App environment variables.
    [<CustomOperation "add_env_variables">]
    member __.AddEnvironmentVariables(state: ContainerAppConfig, vars: #seq<_>) =
        vars |> Seq.fold (fun s (k, v) -> __.AddEnvironmentVariable(s, k, v)) state

    [<CustomOperation "add_simple_container">]
    member this.AddSimpleContainer(state: ContainerAppConfig, dockerImage, dockerVersion) =
        let container =
            {
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

    /// Support for adding dependencies to this Container App.
    interface IDependable<ContainerAppConfig> with
        member _.Add state newDeps =
            { state with
                Dependencies = state.Dependencies + newDeps
            }

type ContainerBuilder() =
    member _.Yield _ =
        {
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
    member _.SetPrivateDockerImage(state: ContainerConfig, registry, containerName, version: string) =
        { state with
            DockerImage = Some(Containers.PrivateImage(registry, containerName, Option.ofObj version))
        }

    [<CustomOperation "public_docker_image">]
    member _.SetPublicDockerImage(state: ContainerConfig, containerName, version: string) =
        { state with
            DockerImage = Some(Containers.PublicImage(containerName, Option.ofObj version))
        }

    [<CustomOperation "cpu_cores">]
    member _.CpuCores(state: ContainerConfig, cpuCount: float<VCores>) =
        let numCores = cpuCount / 1.<VCores>

        if numCores > 2. then
            raiseFarmer $"'{state.ContainerName}' exceeds maximum CPU cores of 2.0 for containers in containerApps."

        let roundedCpuCount = System.Math.Round(numCores, 2) * 1.<VCores>

        { state with
            Resources =
                {| state.Resources with
                    CPU = roundedCpuCount
                |}
        }

    [<CustomOperation "ephemeral_storage">]
    member _.EphemeralStorage(state: ContainerConfig, size: float<Gb>) =
        let size = size / 1.<Gb>
        let roundedSize = System.Math.Round(size, 2) * 1.<Gb>

        { state with
            Resources =
                {| state.Resources with
                    EphemeralStorage = Some roundedSize
                |}
        }

    [<CustomOperation "memory">]
    member _.Memory(state: ContainerConfig, memory: float<Gb>) =
        let memory = memory / 1.<Gb>

        if memory > 4. then
            raiseFarmer $"'{state.ContainerName}' exceeds maximum memory of 4.0 Gb for containers in containerApps."

        let roundedMemory = System.Math.Round(memory, 2) * 1.<Gb>

        { state with
            Resources =
                {| state.Resources with
                    Memory = roundedMemory
                |}
        }

    [<CustomOperation "add_volume_mounts">]
    member _.AddVolumeMounts(state: ContainerConfig, mounts: #seq<_>) =
        { state with
            VolumeMounts =
                mounts
                |> Seq.fold (fun s (volumeName, mountPath) -> s |> Map.add volumeName mountPath) state.VolumeMounts
        }

let containerEnvironment = ContainerEnvironmentBuilder()
let containerApp = ContainerAppBuilder()
let container = ContainerBuilder()
