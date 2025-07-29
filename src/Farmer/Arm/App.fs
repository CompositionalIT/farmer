[<AutoOpen>]
module Farmer.Arm.App

open System
open Farmer.ContainerApp
open Farmer

let containerApps = ResourceType("Microsoft.App/containerApps", "2023-05-01")

let managedEnvironments =
    ResourceType("Microsoft.App/managedEnvironments", "2022-03-01")

let storages =
    ResourceType("Microsoft.App/managedEnvironments/storages", "2022-03-01")

let daprComponents =
    ResourceType("Microsoft.App/managedEnvironments/daprComponents", "2022-10-01")

open Farmer.ContainerAppValidation
open Farmer.Identity

type HealthProbe =
    | Liveness
    | Readiness
    | Startup

type ProbeMap =
    Map<
        HealthProbe,
        {|
            Protocol: ProbeProtocol
            Route: Uri
            Port: int
        |}
     >

type Container = {
    Name: string
    DockerImage: Containers.DockerImage
    VolumeMounts: Map<string, string>
    Resources: {|
        CPU: float<VCores>
        Memory: float<Gb>
        EphemeralStorage: float<Gb> option
    |}
    Probes: ProbeMap
}

type ManagedEnvironmentStorage = {
    Name: ResourceName
    Environment: ResourceId
    AzureFile: {|
        ShareName: ResourceName
        AccountName: Storage.StorageAccountName
        AccountKey: string
        AccessMode: StorageAccessMode
    |}
    Dependencies: Set<ResourceId>
} with

    interface IArmResource with
        member this.ResourceId = storages.resourceId this.Name

        member this.JsonModel = {|
            storages.Create(
                ResourceName $"{this.Environment.Name.Value}/{this.Name.Value}",
                dependsOn = this.Dependencies
            ) with
                properties = {|
                    azureFile = {|
                        shareName = this.AzureFile.ShareName.Value
                        accountName = this.AzureFile.AccountName.ResourceName.Value
                        accountKey = this.AzureFile.AccountKey
                        accessMode = this.AzureFile.AccessMode.ArmValue
                    |}
                |}
        |}

    static member from(env: ResourceId) =
        function
        | KeyValue(name, Volume.AzureFileShare(share, accountName, accessMode)) ->
            Some {
                Name = ResourceName name
                Environment = env
                Dependencies = Set.ofList [ env; Storage.storageAccounts.resourceId accountName.ResourceName ]
                AzureFile = {|
                    ShareName = share
                    AccountName = accountName
                    AccountKey =
                        $"[listKeys('Microsoft.Storage/storageAccounts/{accountName.ResourceName.Value}', '2018-07-01').keys[0].value]"
                    AccessMode = accessMode
                |}
            }
        | _ -> None

type DaprMetadataValue =
    | SecretRef of string
    | Value of string

type DaprComponent = {
    Name: ResourceName
    Environment: ResourceId
    ComponentType: string
    IgnoreErrors: bool option
    InitTimeout: string option
    Metadata: Map<string, DaprMetadataValue>
    Scopes: string list
    Secrets: Map<string, SecretValue>
    SecretStoreComponent: ResourceName option
    Version: string
} with

    interface IArmResource with
        member this.ResourceId = daprComponents.resourceId this.Name

        member this.JsonModel = {|
            daprComponents.Create(
                ResourceName $"{this.Environment.Name.Value}/{this.Name.Value}",
                dependsOn = [ this.Environment ]
            ) with
                properties = {|
                    componentType = this.ComponentType
                    ignoreErrors = this.IgnoreErrors |> Option.toNullable
                    initTimeout = this.InitTimeout |> Option.toObj
                    metadata = [|
                        for metadata in this.Metadata do
                            match metadata.Value with
                            | SecretRef v -> {|
                                name = metadata.Key
                                secretRef = v
                                value = null
                              |}
                            | Value v -> {|
                                name = metadata.Key
                                secretRef = null
                                value = v
                              |}
                    |]
                    scopes = this.Scopes
                    secrets = [|
                        for secret in this.Secrets ->
                            let defaultArm = {|
                                name = secret.Key
                                value = None
                                keyVaultUrl = None
                                identity = None
                            |}

                            match secret.Value with
                            | ParameterSecret secureParameter -> {|
                                defaultArm with
                                    value = Some(secureParameter.ArmExpression.Eval())
                              |}
                            | ExpressionSecret armExpression -> {|
                                defaultArm with
                                    value = Some(armExpression.Eval())
                              |}
                            | KeyVaultSecretReference(url, identity) -> {|
                                defaultArm with
                                    keyVaultUrl = Some(url.Eval())
                                    identity = Some(identity.Eval())
                              |}
                    |]
                    secretStoreComponent = this.SecretStoreComponent |> Option.map _.Value |> Option.toObj
                    version = this.Version
                |}
        |}

type ContainerApp = {
    Name: ResourceName
    Environment: ResourceId
    ActiveRevisionsMode: ActiveRevisionsMode
    IngressMode: IngressMode option
    ScaleRules: Map<string, ScaleRule>
    Identity: ManagedIdentity
    Replicas: {| Min: int; Max: int |} option
    DaprConfig: {| AppId: string; Port: uint16 option |} option
    Secrets: Map<ContainerAppSettingKey, SecretValue>
    EnvironmentVariables: Map<string, EnvVar>
    ImageRegistryCredentials: ImageRegistryAuthentication list
    Containers: Container list
    Location: Location
    Dependencies: Set<ResourceId>
    Volumes: Map<string, Volume>
} with

    member private this.dependencies = [
        this.Environment
        yield! this.Dependencies
        yield!
            this.Volumes
            |> Seq.choose (function
                | KeyValue(name, Volume.AzureFileShare(_)) ->
                    storages.resourceId (this.Environment.Name, ResourceName name) |> Some
                | _ -> None)
        yield! this.Identity.Dependencies
    ]

    member private this.ResourceId = containerApps.resourceId this.Name

    member this.SystemIdentity = SystemIdentity this.ResourceId

    interface IParameters with
        member this.SecureParameters = [
            for secret in this.Secrets do
                match secret.Value with
                | ParameterSecret sp -> sp
                | ExpressionSecret _ -> ()
                | KeyVaultSecretReference _ -> ()
            for credential in this.ImageRegistryCredentials do
                match credential with
                | ImageRegistryAuthentication.Credential credential -> credential.Password
                | ImageRegistryAuthentication.ListCredentials _ -> ()
                | ImageRegistryAuthentication.ManagedIdentityCredential _ -> ()

        ]

    interface IArmResource with
        member this.ResourceId = containerApps.resourceId this.Name

        member this.JsonModel = {|
            containerApps.Create(this.Name, this.Location, this.dependencies) with
                kind = "containerapp"
                identity =
                    if this.Identity = ManagedIdentity.Empty then
                        Unchecked.defaultof<_>
                    else
                        this.Identity.ToArmJson
                properties = {|
                    managedEnvironmentId = this.Environment.Eval()
                    configuration =
                        let buildPasswordRef (resourceId: ResourceId) =
                            $"password-for-%s{resourceId.Name.Value}-registry"

                        {|
                            secrets = [|
                                for cred in this.ImageRegistryCredentials do
                                    match cred with
                                    | ImageRegistryAuthentication.Credential cred -> {|
                                        name = cred.Username
                                        value = Some(cred.Password.ArmExpression.Eval())
                                        keyVaultUrl = None
                                        identity = None
                                      |}
                                    | ImageRegistryAuthentication.ListCredentials resourceId -> {|
                                        name = buildPasswordRef resourceId
                                        value =
                                            Some(
                                                ArmExpression
                                                    .create(
                                                        $"listCredentials({resourceId.ArmExpression.Value}, '2019-05-01').passwords[0].value"
                                                    )
                                                    .Eval()
                                            )
                                        keyVaultUrl = None
                                        identity = None
                                      |}
                                    | ImageRegistryAuthentication.ManagedIdentityCredential cred -> ()
                                for setting in this.Secrets do
                                    let defaultArm = {|
                                        name = setting.Key.Value
                                        value = None
                                        keyVaultUrl = None
                                        identity = None
                                    |}

                                    match setting.Value with
                                    | ParameterSecret secureParameter -> {|
                                        defaultArm with
                                            value = Some(secureParameter.ArmExpression.Eval())
                                      |}
                                    | ExpressionSecret armExpression -> {|
                                        defaultArm with
                                            value = Some(armExpression.Eval())
                                      |}
                                    | KeyVaultSecretReference(url, identity) -> {|
                                        defaultArm with
                                            keyVaultUrl = Some(url.Eval())
                                            identity = Some(identity.Eval())
                                      |}
                            |]
                            activeRevisionsMode =
                                match this.ActiveRevisionsMode with
                                | Single -> "Single"
                                | Multiple -> "Multiple"
                            registries = [|
                                for cred in this.ImageRegistryCredentials do
                                    match cred with
                                    | ImageRegistryAuthentication.Credential cred -> {|
                                        server = cred.Server
                                        username = cred.Username
                                        passwordSecretRef = cred.Username
                                        identity = null
                                      |}
                                    | ImageRegistryAuthentication.ListCredentials resourceId -> {|
                                        server =
                                            ArmExpression
                                                .create(
                                                    $"reference({resourceId.ArmExpression.Value}, '2019-05-01').loginServer"
                                                )
                                                .Eval()
                                        username =
                                            ArmExpression
                                                .create(
                                                    $"listCredentials({resourceId.ArmExpression.Value}, '2019-05-01').username"
                                                )
                                                .Eval()
                                        passwordSecretRef = buildPasswordRef resourceId
                                        identity = null
                                      |}
                                    | ImageRegistryAuthentication.ManagedIdentityCredential cred -> {|
                                        server = cred.Server
                                        username = String.Empty
                                        passwordSecretRef = null
                                        identity =
                                            if cred.Identity.Dependencies.Length > 0 then
                                                cred.Identity.Dependencies.Head.ArmExpression.Eval()
                                            else
                                                String.Empty
                                      |}
                            |]
                            ingress =
                                match this.IngressMode with
                                | Some InternalOnly -> box {| external = false |}
                                | Some(External(targetPort, transport)) ->
                                    box {|
                                        external = true
                                        targetPort = targetPort
                                        transport =
                                            match transport with
                                            | Some HTTP1 -> "http"
                                            | Some HTTP2 -> "http2"
                                            | Some Auto -> "auto"
                                            | None -> null
                                    |}
                                | None -> null
                            dapr =
                                match this.DaprConfig with
                                | Some settings ->
                                    {|
                                        enabled = true
                                        appId = settings.AppId
                                        appPort = settings.Port |> Option.toNullable
                                    |}
                                    :> obj
                                | None -> {| enabled = false |}
                        |}

                    template = {|
                        containers = [|
                            for container in this.Containers do
                                {|
                                    image = container.DockerImage.ImageTag
                                    name = container.Name
                                    env = [|
                                        for env in this.EnvironmentVariables do
                                            match env.Value with
                                            | EnvValue value -> {|
                                                name = env.Key
                                                value = value
                                                secretref = null
                                              |}
                                            | SecureEnvExpression armExpr -> {|
                                                name = env.Key
                                                value = armExpr.Eval()
                                                secretref = null
                                              |}
                                            | SecureEnvValue _ -> {|
                                                name = env.Key
                                                value = null
                                                secretref = env.Key
                                              |}
                                            | EnvValueSecretReference secretRef -> {|
                                                name = env.Key
                                                value = null
                                                secretref = secretRef
                                              |}
                                    |]
                                    resources =
                                        {|
                                            cpu = container.Resources.CPU
                                            ephemeralStorage =
                                                container.Resources.EphemeralStorage
                                                |> Option.map (sprintf "%.2fGi")
                                                |> Option.toObj
                                            memory = container.Resources.Memory |> sprintf "%.2fGi"
                                        |}
                                        :> obj
                                    volumeMounts =
                                        container.VolumeMounts
                                        |> Seq.map (fun kvp -> {|
                                            volumeName = kvp.Key
                                            mountPath = kvp.Value
                                        |})
                                        |> List.ofSeq
                                        |> function
                                            | [] -> Unchecked.defaultof<_>
                                            | vms -> vms
                                    probes = [
                                        for probe in container.Probes do
                                            let endpoint =
                                                box {|
                                                    path = probe.Value.Route
                                                    port = probe.Value.Port
                                                |}

                                            {|
                                                ``type`` = box (probe.Key.ToString().ToLower())
                                                tcpSocket =
                                                    match probe.Value.Protocol with
                                                    | ProbeProtocol.TCP -> endpoint
                                                    | _ -> null
                                                httpGet =
                                                    match probe.Value.Protocol with
                                                    | ProbeProtocol.HTTPS -> endpoint
                                                    | _ -> null
                                            |}
                                    ]
                                |}
                        |]
                        scale = {|
                            minReplicas = this.Replicas |> Option.map (fun c -> c.Min) |> Option.toNullable
                            maxReplicas = this.Replicas |> Option.map (fun c -> c.Max) |> Option.toNullable
                            rules = [|
                                for rule in this.ScaleRules do
                                    match rule.Value with
                                    | ScaleRule.Custom customRule ->
                                        {|
                                            name = rule.Key
                                            custom = customRule
                                        |}
                                        :> obj
                                    | ScaleRule.EventHub settings ->
                                        {|
                                            name = rule.Key
                                            custom = {|
                                                ``type`` = "azure-eventhub"
                                                metadata = {|
                                                    consumerGroup = settings.ConsumerGroup
                                                    unprocessedEventThreshold =
                                                        string settings.UnprocessedEventThreshold
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
                                    | ScaleRule.ServiceBus settings ->
                                        {|
                                            name = rule.Key
                                            custom = {|
                                                ``type`` = "azure-servicebus"
                                                metadata = {|
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
                                    | ScaleRule.Http settings ->
                                        {|
                                            name = rule.Key
                                            http = {|
                                                metadata = {|
                                                    concurrentRequests = string settings.ConcurrentRequests
                                                |}
                                            |}
                                        |}
                                        :> obj
                                    | ScaleRule.CPU settings ->
                                        {|
                                            name = rule.Key
                                            custom = {|
                                                ``type`` = "cpu"
                                                metadata = {|
                                                    ``type`` =
                                                        match settings with
                                                        | Utilization _ -> "Utilization"
                                                        | AverageValue _ -> "AverageValue"
                                                    value =
                                                        match settings with
                                                        | Utilization v -> v.Utilization |> string
                                                        | AverageValue v -> v.AverageValue |> string
                                                |}
                                            |}
                                        |}
                                        :> obj
                                    | ScaleRule.Memory settings ->
                                        {|
                                            name = rule.Key
                                            custom = {|
                                                ``type`` = "memory"
                                                metadata = {|
                                                    ``type`` =
                                                        match settings with
                                                        | Utilization _ -> "Utilization"
                                                        | AverageValue _ -> "AverageValue"
                                                    value =
                                                        match settings with
                                                        | Utilization v -> v.Utilization |> string
                                                        | AverageValue v -> v.AverageValue |> string
                                                |}
                                            |}
                                        |}
                                        :> obj
                                    | ScaleRule.StorageQueue settings -> {|
                                        name = rule.Key
                                        azureQueue = {|
                                            queueName = settings.QueueName
                                            queueLength = string settings.QueueLength
                                            auth = [|
                                                {|
                                                    secretRef = settings.StorageConnectionSecretRef
                                                    triggerParameter = "connection"
                                                |}
                                            |]
                                        |}
                                      |}
                            |]
                        |}
                        volumes =
                            [
                                for key, value in Map.toSeq this.Volumes do
                                    match key, value with
                                    | volumeName, Volume.AzureFileShare(shareName, accountName, _) -> {|
                                        name = volumeName
                                        storageType = "AzureFile"
                                        storageName = volumeName
                                      |}
                                    | volumeName, Volume.EmptyDirectory -> {|
                                        name = volumeName
                                        storageType = "EmptyDir"
                                        storageName = null
                                      |}
                            ]
                            |> function
                                | [] -> Unchecked.defaultof<_>
                                | vs -> vs
                    |}
                |}
        |}

type ManagedEnvironment = {
    Name: ResourceName
    Location: Location
    InternalLoadBalancerState: FeatureFlag
    LogAnalytics: ResourceId
    AppInsightsInstrumentationKey: ArmExpression option
    Dependencies: Set<ResourceId>
    Tags: Map<string, string>
} with

    interface IArmResource with
        member this.ResourceId = managedEnvironments.resourceId this.Name

        member this.JsonModel = {|
            managedEnvironments.Create(this.Name, this.Location, this.Dependencies, this.Tags) with
                kind = "containerenvironment"
                properties = {|
                    ``type`` = "managed"
                    internalLoadBalancerEnabled = this.InternalLoadBalancerState.AsBoolean
                    daprAIInstrumentationKey =
                        this.AppInsightsInstrumentationKey
                        |> Option.map (fun key -> key.Eval())
                        |> Option.toObj
                    appLogsConfiguration = {|
                        destination = "log-analytics"
                        logAnalyticsConfiguration = {|
                            customerId = LogAnalytics.getCustomerId(this.LogAnalytics).Eval()
                            sharedKey = LogAnalytics.getPrimarySharedKey(this.LogAnalytics).Eval()
                        |}
                    |}
                |}
        |}