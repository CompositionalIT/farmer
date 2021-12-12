[<AutoOpen>]
module Farmer.Builders.ContainerApps

open Farmer
open Farmer.ContainerApp
open Farmer.ContainerAppValidation
open Farmer.Arm.Web
open Farmer.Arm.Web.ContainerApp

type ContainerAppConfig =
    { Name : ResourceName
      ActiveRevisionsMode : ActiveRevisionsMode
      Resources : {| CPU : float<VCores> option; Memory : float<Gb> option |}
      IngressConfig : IngressConfig option
      ScaleRules : Map<string, ScaleRule>
      Replicas : {| Min : int; Max : int |} option
      DaprConfig : {| AppId : string |} option
      Secrets : Map<ContainerAppSettingKey, SecretValue>
      EnvironmentVariables : Map<string, EnvVar>
      DockerImage : DockerImageKind option
      Dependencies : Set<ResourceId> } 

type ContainerEnvironmentConfig =
    { Name : ResourceName
      InternalLoadBalancerState : FeatureFlag
      Containers : ContainerAppConfig list
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

            for container in this.Containers do
                { Name = container.Name
                  Environment = kubeEnvironments.resourceId this.Name
                  ActiveRevisionsMode = container.ActiveRevisionsMode
                  Resources = container.Resources
                  IngressConfig = container.IngressConfig
                  ScaleRules = container.ScaleRules
                  Replicas = container.Replicas
                  DaprConfig = container.DaprConfig
                  Secrets = container.Secrets
                  EnvironmentVariables = container.EnvironmentVariables
                  DockerImage =
                    match container.DockerImage with
                    | Some image -> image
                    | None -> raiseFarmer "The container image settings were not set. Please use the docker_image keyword of the containerApp builder."
                  Location = location
                  Dependencies = container.Dependencies }
        ]

type ContainerEnvironmentBuilder() =
    member _.Yield _ =
        { Name = ResourceName.Empty
          InternalLoadBalancerState = Disabled
          Containers = []
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
    member _.AddContainer  (state:ContainerEnvironmentConfig, container:ContainerAppConfig) =
        { state with Containers = container :: state.Containers }

    /// Adds multiple containers to the Azure Container App Environment.
    [<CustomOperation "add_containers">]
    member _.AddContainers  (state:ContainerEnvironmentConfig, containers:ContainerAppConfig list) =
        { state with Containers = containers @ state.Containers }
    /// Support for adding tags to this Container App Environment.
    interface ITaggable<ContainerEnvironmentConfig> with member _.Add state tags = { state with Tags = state.Tags |> Map.merge tags }
    /// Support for adding dependencies to this Container App Environment.
    interface IDependable<ContainerEnvironmentConfig> with member _.Add state newDeps = { state with Dependencies = state.Dependencies + newDeps }

type ContainerAppBuilder () =
    member _.Yield _ =
        { Name = ResourceName.Empty
          ActiveRevisionsMode = ActiveRevisionsMode.Single
          DockerImage = None
          Replicas = None
          ScaleRules = Map.empty
          Secrets = Map.empty
          IngressConfig = None
          EnvironmentVariables = Map.empty
          DaprConfig = None
          Resources = {| CPU = Some 0.25<VCores>; Memory = Some 0.5<Gb> |}
          Dependencies = Set.empty }

    /// Sets the name of the Azure Container App.
    [<CustomOperation "name">]
    member _.ResourceName (state:ContainerAppConfig, name:string) = { state with Name = ResourceName name }

    /// Adds a scale rule to the Azure Container App.
    [<CustomOperation "add_scale_rule">]
    member _.AddScaleRule (state:ContainerAppConfig, name, rule:ScaleRule) =
        { state with ScaleRules = state.ScaleRules.Add (name, rule) }

    [<CustomOperation "add_scale_rule">]
    member this.AddScaleRule (state, name, rule:HttpScaleRule) =
        this.AddScaleRule(state, name, ScaleRule.Http rule)

    [<CustomOperation "add_scale_rule">]
    member this.AddScaleRule (state, name, rule:ServiceBusScaleRule) =
        this.AddScaleRule(state, name, ScaleRule.ServiceBus rule)

    [<CustomOperation "add_scale_rule">]
    member this.AddScaleRule (state, name, rule:EventHubScaleRule) =
        this.AddScaleRule(state, name, ScaleRule.EventHub rule)

    [<CustomOperation "add_scale_rule">]
    member this.AddScaleRule (state, name, rule:CpuScaleRule) =
        this.AddScaleRule(state, name, ScaleRule.CPU rule)

    [<CustomOperation "add_scale_rule">]
    member this.AddScaleRule (state, name, rule:MemoryScaleRule) =
        this.AddScaleRule(state, name, ScaleRule.Memory rule)

    [<CustomOperation "add_scale_rule">]
    member this.AddScaleRule (state, name, rule:StorageQueueScaleRule) =
        this.AddScaleRule(state, name, ScaleRule.StorageQueue rule)

    [<CustomOperation "add_queue_scale_rule">]
    member this.AddQueueScaleRule (state, name, account:StorageAccountConfig, queueName:string, queueLength : int) =
        let state = this.AddEnvironmentVariable (state, $"scalerule-{name}-queue-name", queueName)
        let secretRef = $"scalerule-{name}-connection"
        let state = this.AddSecret(state, secretRef, account.Key)
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

    /// Set docker credentials
    [<CustomOperation "private_docker_image">]
    member _.SetDockerImage (state:ContainerAppConfig, registryDomain, registryName, containerName, version) =
        { state with
            DockerImage =
                Some (
                    PrivateImage
                        {| RegistryDomain = registryDomain
                           RegistryName = registryName
                           ContainerName = containerName
                           Version = version |}
                )
        }

    [<CustomOperation "public_docker_image">]
    member _.SetDockerImage (state:ContainerAppConfig, path) =
        { state with DockerImage = Some (PublicImage path) }

    /// Sets the active revision mode of the Azure Container App.
    [<CustomOperation "active_revision_mode">]
    member _.SetActiveRevisionsMode (state:ContainerAppConfig, mode:ActiveRevisionsMode) = { state with ActiveRevisionsMode = mode }

    /// Adds an application secret to the Azure Container App.
    [<CustomOperation "add_secret_parameter">]
    member _.AddSecret (state:ContainerAppConfig, key) =
        let key = (ContainerAppSettingKey.Create key).OkValue
        { state with
            Secrets = state.Secrets.Add (key, ParameterSecret (SecureParameter key.Value))
            EnvironmentVariables = state.EnvironmentVariables.Add (EnvVar.create key.Value key.Value)
        }

    /// Adds an application secret to the Azure Container App.
    [<CustomOperation "add_secret_expression">]
    member _.AddSecret (state:ContainerAppConfig, key, expression) =
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

let containerEnvironment = ContainerEnvironmentBuilder()
let containerApp = ContainerAppBuilder()