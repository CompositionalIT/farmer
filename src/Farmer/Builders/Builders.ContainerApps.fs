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
      Resources : {| CPU : float<VCores> option; Memory : float<Gb> option |} }
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
      IngressConfig : IngressConfig option
      ScaleRules : Map<string, ScaleRule>
      Replicas : {| Min : int; Max : int |} option
      DaprConfig : {| AppId : string |} option
      Secrets : Map<ContainerAppSettingKey, SecretValue>
      EnvironmentVariables : Map<string, EnvVar>
      /// Credentials for image registries used by containers in this environment.
      ImageRegistryCredentials : ImageRegistryAuthentication list
      Containers : ContainerConfig list
      Dependencies : Set<ResourceId> } 

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
                  IngressConfig = containerApp.IngressConfig
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

    /// Sets the InternalLoadBalancerEnabled property of the Azure Container App Environment.
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

type ContainerAppBuilder () =
    member _.Yield _ =
        { Name = ResourceName.Empty
          ActiveRevisionsMode = ActiveRevisionsMode.Single
          ImageRegistryCredentials = []
          Containers = []
          Replicas = None
          ScaleRules = Map.empty
          Secrets = Map.empty
          IngressConfig = None
          EnvironmentVariables = Map.empty
          DaprConfig = None
          Dependencies = Set.empty }

    /// Sets the name of the Azure Container App.
    [<CustomOperation "name">]
    member _.ResourceName (state:ContainerAppConfig, name:string) = { state with Name = ResourceName name }

    /// Adds a scale rule to the Azure Container App.
    [<CustomOperation "add_scale_rule">]
    member _.AddScaleRule (state:ContainerAppConfig, name, rule:ScaleRule) =
        { state with ScaleRules = state.ScaleRules.Add (name, rule) }
    member this.AddScaleRule (state, name, rule:HttpScaleRule) =
        this.AddScaleRule(state, name, ScaleRule.Http rule)
    member this.AddScaleRule (state, name, rule:ServiceBusScaleRule) =
        this.AddScaleRule(state, name, ScaleRule.ServiceBus rule)
    member this.AddScaleRule (state, name, rule:EventHubScaleRule) =
        this.AddScaleRule(state, name, ScaleRule.EventHub rule)
    member this.AddScaleRule (state, name, rule:CpuScaleRule) =
        this.AddScaleRule(state, name, ScaleRule.CPU rule)
    member this.AddScaleRule (state, name, rule:MemoryScaleRule) =
        this.AddScaleRule(state, name, ScaleRule.Memory rule)
    member this.AddScaleRule (state, name, rule:StorageQueueScaleRule) =
        this.AddScaleRule(state, name, ScaleRule.StorageQueue rule)

    [<CustomOperation "add_queue_scale_rule">]
    member this.AddQueueScaleRule (state, name, account:StorageAccountConfig, queueName:string, queueLength : int) =
        let state = this.AddEnvironmentVariable (state, $"scalerule-{name}-queue-name", queueName)
        let secretRef = $"scalerule-{name}-connection"
        let state = this.AddSecretExpression(state, secretRef, account.Key)
        this.AddScaleRule(state, name, ScaleRule.StorageQueue { QueueName = queueName; QueueLength = queueLength; StorageConnectionSecretRef = secretRef; AccountName = account.Name.ResourceName.Value })

    /// Configures the ingress of the Azure Container App.
    [<CustomOperation "ingress_visibility">]
    member _.SetIngressVisibility (state:ContainerAppConfig, visibility) =
        { state with
            IngressConfig =
                state.IngressConfig
                |> Option.defaultValue { Visibility = None; TargetPort = 80us; Transport = None }
                |> fun cfg -> { cfg with Visibility = Some visibility }
                |> Some
        }

    /// Configures the ingress of the Azure Container App.
    [<CustomOperation "ingress_target_port">]
    member _.SetIngressTargetPort (state:ContainerAppConfig, targetPort) =
        { state with
            IngressConfig =
                state.IngressConfig
                |> Option.defaultValue { Visibility = None; TargetPort = 80us; Transport = None }
                |> fun cfg -> { cfg with TargetPort = targetPort }
                |> Some
        }
    /// Configures the ingress of the Azure Container App.
    [<CustomOperation "ingress_transport">]
    member _.SetIngressTransport (state:ContainerAppConfig, transport) =
        { state with
            IngressConfig =
                state.IngressConfig
                |> Option.defaultValue { Visibility = None; TargetPort = 80us; Transport = None }
                |> fun cfg -> { cfg with Transport = Some transport }
                |> Some
        }

    /// Configures Dapr in the Azure Container App.
    [<CustomOperation "dapr_app_id">]
    member _.SetDaprAppId (state:ContainerAppConfig, appId) =
        { state with
            DaprConfig = state.DaprConfig |> Option.map (fun c -> {| c with AppId = appId |})
        }

    /// Sets the replicas settings of the Azure Container App.
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

    /// Support for adding dependencies to this Container App.
    interface IDependable<ContainerAppConfig> with member _.Add state newDeps = { state with Dependencies = state.Dependencies + newDeps }

type ContainerBuilder () =
    member _.Yield _ =
        { ContainerName = ""
          DockerImage = None
          Resources = {| CPU = Some 0.25<VCores>; Memory = Some 0.5<Gb> |} }
    /// Set docker credentials
    [<CustomOperation "container_name">]
    member _.ContainerName (state:ContainerConfig, name) =
        { state with ContainerName = name }

        /// Set docker credentials
    [<CustomOperation "private_docker_image">]
    member _.SetPrivateDockerImage (state:ContainerConfig, registryDomain, registryName, containerName, version) =
        { state with
            DockerImage =
                Some (PrivateImage
                    {| RegistryDomain = registryDomain
                       RegistryName = registryName
                       ContainerName = containerName
                       Version = version |})
        }

    [<CustomOperation "public_docker_image">]
    member _.SetPublicDockerImage (state:ContainerConfig, path) =
        { state with DockerImage = Some (PublicImage path) }

let containerEnvironment = ContainerEnvironmentBuilder()
let containerApp = ContainerAppBuilder()
let container = ContainerBuilder()
