[<AutoOpen>]
module Farmer.Builders.ContainerApps

open Farmer

type Secret = {
    Name : string
    Value : string
}

type Ingress = {
    External : bool
    TargetPort : int
    Transport : string
}

type DaprSettings = {
    AppId :string
}

[<RequireQualifiedAccess>]
type EnvironmentVariable =
| SecretRef of string * string
| Value of string * string

type Resources = {
    CPU : float
    Memory : string
}

[<RequireQualifiedAccess>]
type ScaleRuleType =
| EventHubs of {| ConsumerGroup : string; UnprocessedEventThreshold: int; CheckpointBlobContainerName: string; EventHubConnectionSecretRef : string; StorageConnectionSecretRef : string |}
| ServiceBus of {| QueueName : string; MessageCount: int; SecretRef : string |}
| Http of {| ConcurrentRequests : int |}
| Custom of obj

type ScaleRule = {
    Name : string
    Type : ScaleRuleType
}

[<RequireQualifiedAccess>]
type ActiveRevisionsMode =
| Single
| Multiple

// Create a reference to the full ARM registries type and version.
let containerAppResourceType = ResourceType ("Microsoft.Web/containerApps", "2021-03-01")

type ContainerAppConfig =
    { Name : ResourceName
      ContainerEnvironment : ResourceId option
      Secrets : Secret list
      ActiveRevisionsMode : ActiveRevisionsMode
      Resources : Resources option
      Ingress : Ingress option
      ScaleRules : ScaleRule list
      MinReplicas : int
      MaxReplicas : int
      Settings : Map<string, Setting>
      DaprSettings : DaprSettings option
      EnvironmentVariables : EnvironmentVariable list
      DockerImage : {| RegistryDomain : string; RegistryName : string; ContainerName : string; Version:string |} option
      Location : Location }

    interface IParameters with
        member this.SecureParameters =
            this.Settings
            |> Map.toList
            |> List.choose(fun (_,value) ->
                match value with
                | ParameterSetting s -> Some s
                | ExpressionSetting _
                | LiteralSetting _ ->
                    None)

    interface IArmResource with
        member this.ResourceId = containerAppResourceType.resourceId this.Name
        member this.JsonModel =
            let containerSettings = this.DockerImage.Value
            {|  name = this.Name.Value
                ``type`` = containerAppResourceType.Type
                apiVersion = containerAppResourceType.ApiVersion
                kind = "containerapp"
                location = this.Location.ArmValue
                properties =
                    {|
                        kubeEnvironmentId = this.ContainerEnvironment.Value.Eval()
                        configuration =
                            {|
                                secrets = [|
                                    yield
                                        {|
                                            name = $"container-registry-password-for-{containerRegistry}"
                                            value = $"[parameters('docker-password-for-{containerRegistry}')]"
                                        |}
                                    for secret in this.Secrets -> {| name = secret.Name; value = secret.Value |}
                                |]
                                activeRevisionsMode =
                                    match this.ActiveRevisionsMode with
                                    | ActiveRevisionsMode.Single -> "Single"
                                    | ActiveRevisionsMode.Multiple -> "Multiple"
                                registries =
                                    [|
                                        {|
                                            server = containerSettings.RegistryDomain
                                            username = containerSettings.RegistryName
                                            passwordSecretRef = $"container-registry-password-for-{containerSettings.RegistryName}"
                                        |}
                                    |]
                                ingress =
                                    match this.Ingress with
                                    | Some ingress ->
                                        {|
                                            external = ingress.External
                                            targetPort = ingress.TargetPort
                                            transport = ingress.Transport
                                        |}
                                        :> obj
                                    | _ -> null

                                |}
                        template =
                            {|
                                containers = [|
                                    {|
                                        image = $"{containerSettings.RegistryDomain}/{containerSettings.RegistryName}/{containerSettings.ContainerName}:{containerSettings.Version}"
                                        name = this.Name.Value
                                        env =
                                            [|
                                                for env in this.EnvironmentVariables do
                                                    match env with
                                                    | EnvironmentVariable.SecretRef(name,secretRef) ->
                                                        [ "name", name
                                                          "secretref", secretRef
                                                        ]
                                                        |> readOnlyDict
                                                    | EnvironmentVariable.Value(name,v) ->
                                                        [ "name", name
                                                          "value", v
                                                        ]
                                                        |> readOnlyDict
                                            |]
                                        resources =
                                            match this.Resources with
                                            | Some resources ->
                                                {|
                                                    cpu = resources.CPU
                                                    memory = resources.Memory
                                                |}
                                                :> obj
                                            | None ->
                                                {|
                                                    cpu = 0.25
                                                    memory = "0.5Gi"
                                                |}
                                                :> obj
                                    |}
                                |]
                                scale =
                                    {|
                                        minReplicas = this.MinReplicas
                                        maxReplicas = this.MaxReplicas
                                        rules = [|
                                            for rule in this.ScaleRules do
                                                match rule.Type with
                                                | ScaleRuleType.Custom customRule ->
                                                    {|
                                                        name = rule.Name
                                                        custom = customRule
                                                    |}
                                                    :> obj
                                                | ScaleRuleType.EventHubs settings ->
                                                    {|
                                                        name = rule.Name
                                                        custom =
                                                            {|
                                                                // https://keda.sh/docs/scalers/azure-event-hub/
                                                                ``type`` = "azure-eventhub"
                                                                metadata =
                                                                    {|
                                                                        consumerGroup = settings.ConsumerGroup
                                                                        unprocessedEventThreshold = string settings.UnprocessedEventThreshold
                                                                        blobContainer = settings.CheckpointBlobContainerName
                                                                        checkpointStrategy = "blobMetadata"
                                                                    |}
                                                                auth = [|
                                                                    {|
                                                                        secretRef = settings.EventHubConnectionSecretRef
                                                                        triggerParameter = "connection"
                                                                    |}
                                                                    {|
                                                                        secretRef = settings.StorageConnectionSecretRef
                                                                        triggerParameter = "storageConnection"
                                                                    |}
                                                                |]
                                                            |}
                                                    |}
                                                    :> obj
                                                | ScaleRuleType.ServiceBus settings ->
                                                    {|
                                                        name = rule.Name
                                                        custom =
                                                            {|
                                                                // https://keda.sh/docs/scalers/azure-service-bus/
                                                                ``type`` = "azure-servicebus"
                                                                metadata =
                                                                    {|
                                                                        queueName = settings.QueueName
                                                                        messageCount = string settings.MessageCount
                                                                    |}
                                                                auth = [|
                                                                    {|
                                                                        secretRef = settings.SecretRef
                                                                        triggerParameter = "connection"
                                                                    |}
                                                                |]
                                                            |}
                                                    |}
                                                    :> obj
                                                | ScaleRuleType.Http settings ->
                                                    {|
                                                        name = rule.Name
                                                        http =
                                                            {|
                                                                metadata =
                                                                    {|
                                                                        concurrentRequests = string settings.ConcurrentRequests
                                                                    |}
                                                            |}
                                                    |}
                                                    :> obj
                                        |]
                                    |}
                                dapr =
                                    match this.DaprSettings with
                                    | Some settings ->
                                        {|
                                            enabled = true
                                            appId = settings.AppId
                                        |}
                                        :> obj
                                    | None ->
                                        {|
                                            enabled = false
                                        |}
                                        :> obj
                            |}
                |}
            |} :> _ // upcast to obj


type ContainerAppBuilder() =
    member _.Yield _ =
        { Name = ResourceName.Empty
          ContainerEnvironment = None
          Secrets = []
          ActiveRevisionsMode = ActiveRevisionsMode.Single
          DockerImage = None
          EnvironmentVariables = []
          MinReplicas = 1
          MaxReplicas = 1
          ScaleRules = []
          Settings = Map.empty
          Ingress = None
          DaprSettings = None
          Resources = None
          Location = Location.NorthEurope }

    member _.Run (state:ContainerAppConfig) =
            match state.DockerImage with
            | None -> raiseFarmer $"The container image settings were not set. Please use the docker_image function of the containerApp builder."
            | _ ->
                state

    /// Sets the name of the Azure Container App.
    [<CustomOperation "name">]
    member _.Name(state: ContainerAppConfig, name:string) = { state with Name = ResourceName name }

    /// Adds a scale rule to the Azure Container App.
    [<CustomOperation "add_scale_rule">]
    member _.AddScaleRule(state: ContainerAppConfig, name:string, rule) =
        { state with ScaleRules = { Name = name; Type = rule} :: state.ScaleRules }

    /// Sets the ingress settings of the Azure Container App.
    [<CustomOperation "ingress">]
    member _.SetIngress(state: ContainerAppConfig, ingress:Ingress) =
        { state with Ingress = Some ingress }

    /// Sets the dapr settings of the Azure Container App.
    [<CustomOperation "dapr">]
    member _.SetDapr(state: ContainerAppConfig, dapr:DaprSettings) =
        { state with DaprSettings = Some dapr }

    /// Sets the replicas settings of the Azure Container App.
    [<CustomOperation "replicas">]
    member _.SetReplicas(state: ContainerAppConfig, minReplicas:int, maxReplicas: int) =
        { state with MinReplicas = minReplicas; MaxReplicas = maxReplicas }

    [<CustomOperation "docker_image">]
    /// Set docker credentials
    member _.SetDockerImage(state:ContainerAppConfig, registryDomain, registryName, containerName, version) =
        { state with
            DockerImage =
                Some {| RegistryDomain = registryDomain
                        RegistryName = registryName
                        ContainerName = containerName
                        Version = version |} }

    /// Sets the environment of the Azure Container App.
    [<CustomOperation "activeRevisionsMode">]
    member _.SetActiveRevisionsMode(state: ContainerAppConfig, mode:ActiveRevisionsMode) = { state with ActiveRevisionsMode = mode }

    /// Adds secrets to the Azure Container App.
    [<CustomOperation "add_secrets">]
    member _.AddSecrets(state: ContainerAppConfig, secrets) = { state with Secrets = secrets @ state.Secrets }

    /// Adds a secret to the Azure Container App.
    [<CustomOperation "add_secret">]
    member _.AddSecret(state: ContainerAppConfig, secret) = { state with Secrets = secret :: state.Secrets }

    /// Creates a setting for the Azure Container App whose value will be supplied as a secret parameter.
    [<CustomOperation "secret_setting">]
    member _.AddSecretSetting (state:ContainerAppConfig, key) =
        { state with
            Settings = state.Settings.Add(key, ParameterSetting (SecureParameter key))
            Secrets = { Name = key.ToLower(); Value = $"[parameters('{key}')]" } :: state.Secrets
            EnvironmentVariables = EnvironmentVariable.SecretRef(key,key.ToLower()) :: state.EnvironmentVariables }

    /// Adds a secretRef to the Azure Container App environment variables.
    [<CustomOperation "add_secretref_variable">]
    member _.AddSecretRefEnvironmentVariable(state: ContainerAppConfig, name, secretRef) = { state with EnvironmentVariables = EnvironmentVariable.SecretRef(name,secretRef) :: state.EnvironmentVariables }

    /// Adds a variable to the Azure Container App environment variables.
    [<CustomOperation "setting">]
    member _.AddEnvironmentVariable(state: ContainerAppConfig, name, v) = { state with EnvironmentVariables = EnvironmentVariable.Value(name,v) :: state.EnvironmentVariables }

let containerApp = ContainerAppBuilder()

// Create a reference to the full ARM registries type and version.
let containerEnvironmentResourceType = ResourceType ("Microsoft.Web/kubeenvironments", "2021-02-01")

type ContainerEnvironmentConfig =
    { Name : ResourceName
      Location : Location
      InternalLoadBalancerEnabled : bool
      Containers : ContainerAppConfig list
      LogAnalytics : WorkspaceConfig option }

    interface IBuilder with
        member this.ResourceId = containerAppResourceType.resourceId this.Name
        member this.BuildResources location = [
            this
            for container in this.Containers do
                container
        ]

    interface IArmResource with
        member this.ResourceId = containerEnvironmentResourceType.resourceId this.Name

        member this.JsonModel =
            let logAnalytics = this.LogAnalytics.Value
            {|  name = this.Name.Value
                ``type`` = containerEnvironmentResourceType.Type
                apiVersion = containerEnvironmentResourceType.ApiVersion
                kind = "containerenvironment"
                location = this.Location.ArmValue
                properties =
                    {|
                        ``type`` = "managed"
                        internalLoadBalancerEnabled = this.InternalLoadBalancerEnabled
                        appLogsConfiguration =
                            {|
                                destination = "log-analytics"
                                logAnalyticsConfiguration =
                                    {|
                                        customerId = logAnalytics.CustomerId.Eval()
                                        sharedKey = logAnalytics.PrimarySharedKey.Eval()
                                    |}
                            |}
                    |}
            |} :> _ // upcast to obj


type ContainerEnvironmentBuilder() =
    member _.Yield _ =
        { Name = ResourceName.Empty
          Location = Location.NorthEurope
          InternalLoadBalancerEnabled = false
          Containers = []
          LogAnalytics = None }

    member _.Run (state:ContainerEnvironmentConfig) =
        match state.LogAnalytics with
        | None -> raiseFarmer $"The LogAnalytics connections was not set. Please use the logAnalytics function of the containerEnvironment builder."
        | _ ->
            state

    /// Sets the name of the Azure Container App Environment.
    [<CustomOperation "name">]
    member _.Name(state: ContainerEnvironmentConfig, name:string) = { state with Name = ResourceName name }

    /// Sets the environment of the Azure Container App.
    [<CustomOperation "logAnalytics">]
    member _.SetLogAnalytics(state: ContainerEnvironmentConfig, logAnalytics:WorkspaceConfig) =
        { state with LogAnalytics = Some logAnalytics }

    /// Sets the InternalLoadBalancerEnabled property of the Azure Container App Environment.
    [<CustomOperation "internalLoadBalancerEnabled">]
    member _.SetInternalLoadBalancerEnabled(state: ContainerEnvironmentConfig, internalLoadBalancerEnabled) =
        { state with InternalLoadBalancerEnabled = internalLoadBalancerEnabled }

    /// Adds a container to the Azure Container App Environment.
    [<CustomOperation "add_container">]
    member _.AddContainer(state: ContainerEnvironmentConfig, container:ContainerAppConfig) =
        { state with Containers = { container with ContainerEnvironment = Some (state :> IArmResource).ResourceId } :: state.Containers }

    /// Adds multiple containers to the Azure Container App Environment.
    [<CustomOperation "add_containers">]
    member _.AddContainers(state: ContainerEnvironmentConfig, containers:ContainerAppConfig list) =
        { state with
            Containers =
                containers
                |> List.map (fun container -> { container with ContainerEnvironment = Some (state :> IArmResource).ResourceId })
                |> List.append state.Containers }

let containerEnvironment = ContainerEnvironmentBuilder()
