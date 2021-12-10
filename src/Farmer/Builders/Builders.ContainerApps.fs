[<AutoOpen>]
module Farmer.Builders.ContainerApps

open Farmer
open Farmer.ContainerApp
open Farmer.Arm.Web
open Farmer.Arm.Web.ContainerApp

type ContainerAppConfig =
    { Name : ResourceName
      ActiveRevisionsMode : ActiveRevisionsMode
      Resources : {| CPU : float<VCores> option; Memory : float<Gb> option |}
      IngressConfig : IngressConfig
      ScaleRules : Map<string, ScaleRule>
      Replicas : {| Min : int; Max : int |}
      DaprConfig : {| AppId : string |} option
      Secrets : List<SecureParameter>
      EnvironmentVariables : Map<string, EnvVar>
      DockerImage : {| RegistryDomain : string; RegistryName : string; ContainerName : string; Version:string |} option }

type ContainerEnvironmentConfig =
    { Name : ResourceName
      InternalLoadBalancerState : FeatureFlag
      Containers : ContainerAppConfig list
      LogAnalytics : ResourceRef<ContainerEnvironmentConfig> }
    interface IBuilder with
        member this.ResourceId = containerApps.resourceId this.Name
        member this.BuildResources location = [
            { Name = this.Name
              InternalLoadBalancerState = this.InternalLoadBalancerState
              LogAnalytics = this.LogAnalytics.resourceId(this).Name
              Location = location }

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
                  Location = location }
        ]

type ContainerEnvironmentBuilder() =
    member _.Yield _ =
        { Name = ResourceName.Empty
          InternalLoadBalancerState = Disabled
          Containers = []
          LogAnalytics = ResourceRef.derived (fun cfg -> Arm.LogAnalytics.workspaces.resourceId(cfg.Name - "workspace")) }

    member _.Run (state:ContainerEnvironmentConfig) =
        state

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

type ContainerAppBuilder () =
    member _.Yield _ =
        { Name = ResourceName.Empty
          ActiveRevisionsMode = ActiveRevisionsMode.Single
          DockerImage = None
          Replicas = {| Min = 1; Max = 1 |}
          ScaleRules = Map.empty
          Secrets = []
          IngressConfig = { Visibility = None; TargetPort = None; Transport = None }
          EnvironmentVariables = Map.empty
          DaprConfig = None
          Resources = {| CPU = Some 0.25<VCores>; Memory = Some 0.5<Gb> |} }

    /// Sets the name of the Azure Container App.
    [<CustomOperation "name">]
    member _.ResourceName (state:ContainerAppConfig, name:string) = { state with Name = ResourceName name }

    /// Adds a scale rule to the Azure Container App.
    [<CustomOperation "add_scale_rule">]
    member _.AddScaleRule (state:ContainerAppConfig, name:string, rule) =
        { state with ScaleRules = state.ScaleRules.Add (name, rule) }

    /// Configures the ingress of the Azure Container App.
    [<CustomOperation "ingress_visibility">]
    member _.SetIngressVisibility (state:ContainerAppConfig, visibility) =
        { state with IngressConfig = { state.IngressConfig with Visibility = Some visibility } }

    /// Configures the ingress of the Azure Container App.
    [<CustomOperation "ingress_target_port">]
    member _.SetIngressTargetPort (state:ContainerAppConfig, targetPort) =
        { state with IngressConfig = { state.IngressConfig with TargetPort = Some targetPort } }

    /// Configures the ingress of the Azure Container App.
    [<CustomOperation "ingress_transport">]
    member _.SetIngressTransport (state:ContainerAppConfig, transport) =
        { state with IngressConfig = { state.IngressConfig with Transport = Some transport } }

    /// Configures Dapr in the Azure Container App.
    [<CustomOperation "dapr_app_id">]
    member _.SetDaprAppId (state:ContainerAppConfig, appId) =
        { state with
            DaprConfig = state.DaprConfig |> Option.map (fun c -> {| c with AppId = appId |})
        }

    /// Sets the replicas settings of the Azure Container App.
    [<CustomOperation "replicas">]
    member _.SetReplicas (state:ContainerAppConfig, minReplicas:int, maxReplicas: int) =
        { state with Replicas = {| Min = minReplicas; Max = maxReplicas |} }

    /// Set docker credentials
    [<CustomOperation "docker_image">]
    member _.SetDockerImage (state:ContainerAppConfig, registryDomain, registryName, containerName, version) =
        { state with
            DockerImage =
                Some {| RegistryDomain = registryDomain
                        RegistryName = registryName
                        ContainerName = containerName
                        Version = version |} }

    /// Sets the active revision mode of the Azure Container App.
    [<CustomOperation "active_revision_mode">]
    member _.SetActiveRevisionsMode (state:ContainerAppConfig, mode:ActiveRevisionsMode) = { state with ActiveRevisionsMode = mode }

    /// Adds application secrets to the Azure Container App.
    [<CustomOperation "add_app_secrets">]
    member _.AddSecrets (state:ContainerAppConfig, secrets) =
        { state with Secrets = state.Secrets @ (secrets |> List.map SecureParameter) }

    /// Adds an application secret to the Azure Container App.
    [<CustomOperation "add_app_secret">]
    member _.AddSecret (state:ContainerAppConfig, key) =
        { state with Secrets = SecureParameter key :: state.Secrets }

    /// Adds a secure environment variable to the Azure Container App environment variables.
    [<CustomOperation "add_secure_env_variable">]
    member _.AddSecretRefEnvironmentVariable (state:ContainerAppConfig, name) =
        { state with
            EnvironmentVariables = state.EnvironmentVariables.Add (EnvVar.createSecure name $"secure-env-{name}")
        }

    /// Adds a public environment variable to the Azure Container App environment variables.
    [<CustomOperation "add_env_variable">]
    member _.AddEnvironmentVariable (state:ContainerAppConfig, name, value) =
        { state with
            EnvironmentVariables = state.EnvironmentVariables.Add (EnvVar.create name value)
        }

let containerEnvironment = ContainerEnvironmentBuilder()
let containerApp = ContainerAppBuilder()