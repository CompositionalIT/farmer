[<AutoOpen>]
module Farmer.Arm.App

open System
open Farmer.ContainerApp
open Farmer.Identity
open Farmer

let containerApps = ResourceType("Microsoft.App/containerApps", "2022-03-01")

let managedEnvironments =
    ResourceType("Microsoft.App/managedEnvironments", "2022-03-01")

let storages =
    ResourceType("Microsoft.App/managedEnvironments/storages", "2022-03-01")

open Farmer.ContainerAppValidation
open Farmer.Identity

type Container =
    {
        Name: string
        DockerImage: Containers.DockerImage
        VolumeMounts: Map<string, string>
        Resources: {| CPU: float<VCores>
                      Memory: float<Gb>
                      EphemeralStorage: float<Gb> option |}
    }

type ManagedEnvironmentStorage =
    {
        Name: ResourceName
        Environment: ResourceId
        AzureFile: {| ShareName: ResourceName
                      AccountName: Storage.StorageAccountName
                      AccountKey: string
                      AccessMode: StorageAccessMode |}
        Dependencies: Set<ResourceId>
    }

    interface IArmResource with
        member this.ResourceId = storages.resourceId this.Name

        member this.JsonModel =
            {| storages.Create(
                   ResourceName $"{this.Environment.Name.Value}/{this.Name.Value}",
                   dependsOn = this.Dependencies
               ) with
                properties =
                    {|
                        azureFile =
                            {|
                                shareName = this.AzureFile.ShareName.Value
                                accountName = this.AzureFile.AccountName.ResourceName.Value
                                accountKey = this.AzureFile.AccountKey
                                accessMode = this.AzureFile.AccessMode.ArmValue
                            |}
                    |}
            |}

    static member from(env: ResourceId) =
        function
        | KeyValue (name, Volume.AzureFileShare (share, accountName, accessMode)) ->
            Some
                {
                    Name = ResourceName name
                    Environment = env
                    Dependencies = Set.ofList [ env; Storage.storageAccounts.resourceId accountName.ResourceName ]
                    AzureFile =
                        {|
                            ShareName = share
                            AccountName = accountName
                            AccountKey =
                                $"[listKeys('Microsoft.Storage/storageAccounts/{accountName.ResourceName.Value}', '2018-07-01').keys[0].value]"
                            AccessMode = accessMode
                        |}
                }
        | _ -> None

type ContainerApp =
    {
        Name: ResourceName
        Environment: ResourceId
        ActiveRevisionsMode: ActiveRevisionsMode
        IngressMode: IngressMode option
        ScaleRules: Map<string, ScaleRule>
        Identity: ManagedIdentity
        Replicas: {| Min: int; Max: int |} option
        DaprConfig: {| AppId: string |} option
        Secrets: Map<ContainerAppSettingKey, SecretValue>
        EnvironmentVariables: Map<string, EnvVar>
        ImageRegistryCredentials: ImageRegistryAuthentication list
        Containers: Container list
        Location: Location
        Dependencies: Set<ResourceId>
        Volumes: Map<string, Volume>
    }

    member private this.dependencies =
        [
            yield this.Environment
            yield! this.Dependencies
            yield!
                this.Volumes
                |> Seq.choose (function
                    | KeyValue (name, Volume.AzureFileShare (_)) ->
                        storages.resourceId (this.Environment.Name, ResourceName name) |> Some
                    | _ -> None)
            yield! this.Identity.Dependencies
        ]

    member private this.ResourceId = containerApps.resourceId this.Name
    member this.SystemIdentity = SystemIdentity this.ResourceId

    interface IParameters with
        member this.SecureParameters =
            [
                for secret in this.Secrets do
                    match secret.Value with
                    | ParameterSecret sp -> sp
                    | ExpressionSecret _ -> ()
                for credential in this.ImageRegistryCredentials do
                    match credential with
                    | ImageRegistryAuthentication.Credential credential -> credential.Password
                    | ImageRegistryAuthentication.ListCredentials _ -> ()
                    | ImageRegistryAuthentication.ManagedIdentityCredential _ -> ()

            ]

    interface IArmResource with
        member this.ResourceId = containerApps.resourceId this.Name

        member this.JsonModel =
            let usernameSecretName (resourceId: ResourceId) = $"{resourceId.Name.Value}-username"

            {| containerApps.Create(this.Name, this.Location, this.dependencies) with
                kind = "containerapp"
                identity =
                    if this.Identity = ManagedIdentity.Empty then
                        Unchecked.defaultof<_>
                    else
                        this.Identity.ToArmJson
                properties =
                    {|
                        managedEnvironmentId = this.Environment.Eval()
                        configuration =
                            {|
                                secrets =
                                    [|
                                        for cred in this.ImageRegistryCredentials do
                                            match cred with
                                            | ImageRegistryAuthentication.Credential cred ->
                                                {|
                                                    name = cred.Username
                                                    value = cred.Password.ArmExpression.Eval()
                                                |}
                                            | ImageRegistryAuthentication.ListCredentials resourceId ->
                                                {|
                                                    name = usernameSecretName resourceId
                                                    value =
                                                        ArmExpression
                                                            .create(
                                                                $"listCredentials({resourceId.ArmExpression.Value}, '2019-05-01').passwords[0].value"
                                                            )
                                                            .Eval()
                                                |}
                                            | ImageRegistryAuthentication.ManagedIdentityCredential cred ->
                                                {|
                                                    name = cred.Server
                                                    value =
                                                        if cred.Identity.Dependencies.Length > 0 then
                                                            cred.Identity.Dependencies.Head.ArmExpression.Eval()
                                                        else
                                                            String.Empty



                                                |}
                                        for setting in this.Secrets do
                                            {|
                                                name = setting.Key.Value
                                                value = setting.Value.Value
                                            |}
                                    |]
                                activeRevisionsMode =
                                    match this.ActiveRevisionsMode with
                                    | Single -> "Single"
                                    | Multiple -> "Multiple"
                                registries =
                                    [|
                                        for cred in this.ImageRegistryCredentials do
                                            match cred with
                                            | ImageRegistryAuthentication.Credential cred ->
                                                {|
                                                    server = cred.Server
                                                    username = cred.Username
                                                    passwordSecretRef = cred.Username
                                                    identity = null
                                                |}
                                            | ImageRegistryAuthentication.ListCredentials resourceId ->
                                                {|
                                                    server = $"{resourceId.Name.Value}.azurecr.io"
                                                    username =
                                                        ArmExpression
                                                            .create(
                                                                $"listCredentials({resourceId.ArmExpression.Value}, '2019-05-01').username"
                                                            )
                                                            .Eval()
                                                    passwordSecretRef = usernameSecretName resourceId
                                                    identity = null
                                                |}
                                            | ImageRegistryAuthentication.ManagedIdentityCredential cred ->
                                                {|
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
                                    | Some (External (targetPort, transport)) ->
                                        box
                                            {|
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
                            |}

                        template =
                            {|
                                containers =
                                    [|
                                        for container in this.Containers do
                                            {|
                                                image = container.DockerImage.ImageTag
                                                name = container.Name
                                                env =
                                                    [|
                                                        for env in this.EnvironmentVariables do
                                                            match env.Value with
                                                            | EnvValue value ->
                                                                {|
                                                                    name = env.Key
                                                                    value = value
                                                                    secretref = null
                                                                |}
                                                            | SecureEnvExpression armExpr ->
                                                                {|
                                                                    name = env.Key
                                                                    value = null
                                                                    secretref = armExpr.Eval()
                                                                |}
                                                            | SecureEnvValue _ ->
                                                                {|
                                                                    name = env.Key
                                                                    value = null
                                                                    secretref = env.Key
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
                                                    |> Seq.map (fun kvp ->
                                                        {|
                                                            volumeName = kvp.Key
                                                            mountPath = kvp.Value
                                                        |})
                                                    |> List.ofSeq
                                                    |> function
                                                        | [] -> Unchecked.defaultof<_>
                                                        | vms -> vms
                                            |}
                                    |]
                                scale =
                                    {|
                                        minReplicas = this.Replicas |> Option.map (fun c -> c.Min) |> Option.toNullable
                                        maxReplicas = this.Replicas |> Option.map (fun c -> c.Max) |> Option.toNullable
                                        rules =
                                            [|
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
                                                            custom =
                                                                {|
                                                                    ``type`` = "azure-eventhub"
                                                                    metadata =
                                                                        {|
                                                                            consumerGroup = settings.ConsumerGroup
                                                                            unprocessedEventThreshold =
                                                                                string
                                                                                    settings.UnprocessedEventThreshold
                                                                            blobContainer =
                                                                                settings.CheckpointBlobContainerName
                                                                            checkpointStrategy = "blobMetadata"
                                                                        |}
                                                                    auth =
                                                                        [|
                                                                            {|
                                                                                secretRef =
                                                                                    settings.EventHubConnectionSecretRef
                                                                                triggerParameter = "connection"
                                                                            |}
                                                                            {|
                                                                                secretRef =
                                                                                    settings.StorageConnectionSecretRef
                                                                                triggerParameter = "storageConnection"
                                                                            |}
                                                                        |]
                                                                |}
                                                        |}
                                                        :> obj
                                                    | ScaleRule.ServiceBus settings ->
                                                        {|
                                                            name = rule.Key
                                                            custom =
                                                                {|
                                                                    ``type`` = "azure-servicebus"
                                                                    metadata =
                                                                        {|
                                                                            queueName = settings.QueueName
                                                                            messageCount = string settings.MessageCount
                                                                        |}
                                                                    auth =
                                                                        [|
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
                                                            http =
                                                                {|
                                                                    metadata =
                                                                        {|
                                                                            concurrentRequests =
                                                                                string settings.ConcurrentRequests
                                                                        |}
                                                                |}
                                                        |}
                                                        :> obj
                                                    | ScaleRule.CPU settings ->
                                                        {|
                                                            name = rule.Key
                                                            custom =
                                                                {|
                                                                    ``type`` = "cpu"
                                                                    metadata =
                                                                        {|
                                                                            ``type`` =
                                                                                match settings with
                                                                                | Utilisation _ -> "Utilisation"
                                                                                | AverageValue _ -> "AverageValue"
                                                                            value =
                                                                                match settings with
                                                                                | Utilisation v ->
                                                                                    v.Utilisation |> string
                                                                                | AverageValue v ->
                                                                                    v.AverageValue |> string
                                                                        |}
                                                                |}
                                                        |}
                                                        :> obj
                                                    | ScaleRule.Memory settings ->
                                                        {|
                                                            name = rule.Key
                                                            custom =
                                                                {|
                                                                    ``type`` = "memory"
                                                                    metadata =
                                                                        {|
                                                                            ``type`` =
                                                                                match settings with
                                                                                | Utilisation _ -> "Utilisation"
                                                                                | AverageValue _ -> "AverageValue"
                                                                            value =
                                                                                match settings with
                                                                                | Utilisation v ->
                                                                                    v.Utilisation |> string
                                                                                | AverageValue v ->
                                                                                    v.AverageValue |> string
                                                                        |}
                                                                |}
                                                        |}
                                                        :> obj
                                                    | ScaleRule.StorageQueue settings ->
                                                        {|
                                                            name = rule.Key
                                                            custom =
                                                                {|
                                                                    ``type`` = "azure-queue"
                                                                    metadata =
                                                                        {|
                                                                            queueName = settings.QueueName
                                                                            queueLength = string settings.QueueLength
                                                                            connectionFromEnv =
                                                                                settings.StorageConnectionSecretRef
                                                                            accountName = settings.AccountName
                                                                        |}
                                                                |}
                                                        |}
                                            |]
                                    |}
                                dapr =
                                    match this.DaprConfig with
                                    | Some settings ->
                                        {|
                                            enabled = true
                                            appId = settings.AppId
                                        |}
                                        :> obj
                                    | None -> {| enabled = false |} :> obj
                                volumes =
                                    [
                                        for key, value in Map.toSeq this.Volumes do
                                            match key, value with
                                            | volumeName, Volume.AzureFileShare (shareName, accountName, _) ->
                                                {|
                                                    name = volumeName
                                                    storageType = "AzureFile"
                                                    storageName = volumeName
                                                |}
                                            | volumeName, Volume.EmptyDirectory ->
                                                {|
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

type ManagedEnvironment =
    {
        Name: ResourceName
        Location: Location
        InternalLoadBalancerState: FeatureFlag
        LogAnalytics: ResourceId
        Dependencies: Set<ResourceId>
        Tags: Map<string, string>
    }

    interface IArmResource with
        member this.ResourceId = managedEnvironments.resourceId this.Name

        member this.JsonModel =
            {| managedEnvironments.Create(this.Name, this.Location, this.Dependencies, this.Tags) with
                kind = "containerenvironment"
                properties =
                    {|
                        ``type`` = "managed"
                        internalLoadBalancerEnabled = this.InternalLoadBalancerState.AsBoolean
                        appLogsConfiguration =
                            {|
                                destination = "log-analytics"
                                logAnalyticsConfiguration =
                                    {|
                                        customerId = LogAnalytics.getCustomerId(this.LogAnalytics).Eval()
                                        sharedKey = LogAnalytics.getPrimarySharedKey(this.LogAnalytics).Eval()
                                    |}
                            |}
                    |}
            |}
