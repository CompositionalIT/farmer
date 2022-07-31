[<AutoOpen>]
module Farmer.Arm.App

open Farmer.ContainerApp
open Farmer.Identity
open Farmer

let containerApps = ResourceType ("Microsoft.App/containerApps", "2022-03-01")
let managedEnvironments = ResourceType ("Microsoft.App/managedEnvironments", "2022-03-01")
let daprComponents = ResourceType ("Microsoft.App/managedEnvironments/daprComponents", "2022-01-01-preview")
let storages = ResourceType ("Microsoft.App/managedEnvironments/storages", "2022-03-01")

open Farmer.ContainerAppValidation

type Container =
    { Name : string
      DockerImage : Containers.DockerImage
      VolumeMounts : Map<string,string> 
      Resources : {| CPU : float<VCores>; Memory : float<Gb>; EphemeralStorage : float<Gb> option |} }

type ManagedEnvironmentStorage =
    { Name : ResourceName
      Environment : ResourceId
      AzureFile : {| ShareName : ResourceName; AccountName : Storage.StorageAccountName; AccountKey: string; AccessMode : StorageAccessMode |}
      Dependencies : Set<ResourceId> }
    interface IArmResource with
        member this.ResourceId = storages.resourceId this.Name
        member this.JsonModel =
            {| storages.Create(ResourceName $"{this.Environment.Name.Value}/{this.Name.Value}", dependsOn = this.Dependencies) with
                properties = {|
                    azureFile = {| shareName = this.AzureFile.ShareName.Value
                                   accountName = this.AzureFile.AccountName.ResourceName.Value
                                   accountKey = this.AzureFile.AccountKey
                                   accessMode = this.AzureFile.AccessMode.ArmValue |}
                |}
            |}
    static member from (env: ResourceId) = function 
        | KeyValue (name, Volume.AzureFileShare(share, accountName, accessMode)) -> 
            Some { Name = ResourceName name
                   Environment = env
                   Dependencies = Set.ofList [ env; Storage.storageAccounts.resourceId accountName.ResourceName ]
                   AzureFile = {|
                       ShareName = share
                       AccountName = accountName
                       AccountKey = $"[listKeys('Microsoft.Storage/storageAccounts/{accountName.ResourceName.Value}', '2018-07-01').keys[0].value]"
                       AccessMode = accessMode
                   |}} 
        | _ ->
            None

type ContainerApp =
    { Name : ResourceName
      Environment : ResourceId
      ActiveRevisionsMode : ActiveRevisionsMode
      IngressMode : IngressMode option
      ScaleRules : Map<string, ScaleRule>
      Identity: ManagedIdentity
      Replicas : {| Min : int; Max : int |} option
      DaprConfig : {| AppId : string |} option
      Secrets : Map<ContainerAppSettingKey, SecretValue>
      EnvironmentVariables : Map<string, EnvVar>
      ImageRegistryCredentials : ImageRegistryAuthentication list
      Containers : Container list
      Location : Location
      Dependencies : Set<ResourceId>
      Volumes: Map<string,Volume> }
    member private this.dependencies = [
        yield this.Environment
        yield! this.Dependencies
        yield! this.Volumes
               |> Seq.choose (function KeyValue(name,Volume.AzureFileShare(_)) -> storages.resourceId(this.Environment.Name, ResourceName name) |> Some | _ -> None)
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
            for credential in this.ImageRegistryCredentials do
                match credential with
                | ImageRegistryAuthentication.Credential credential ->
                    credential.Password
                | ImageRegistryAuthentication.ListCredentials _ -> ()
        ]

    interface IArmResource with
        member this.ResourceId = containerApps.resourceId this.Name
        member this.JsonModel =
            let usernameSecretName (resourceId:ResourceId) = $"{resourceId.Name.Value}-username"
            {| containerApps.Create(this.Name, this.Location, this.dependencies) with
                kind = "containerapp"
                identity =
                    if this.Identity = ManagedIdentity.Empty then Unchecked.defaultof<_>
                    else this.Identity.ToArmJson
                properties =
                    {|
                        managedEnvironmentId = this.Environment.Eval()
                        configuration =
                            {|
                                secrets = [|
                                    for cred in this.ImageRegistryCredentials do
                                        match cred with
                                        | ImageRegistryAuthentication.Credential cred ->
                                            {| name = cred.Username
                                               value = cred.Password.ArmExpression.Eval() |}
                                        | ImageRegistryAuthentication.ListCredentials resourceId ->
                                            {| name = usernameSecretName resourceId
                                               value = ArmExpression.create($"listCredentials({resourceId.ArmExpression.Value}, '2019-05-01').passwords[0].value").Eval() |}
                                    for setting in this.Secrets do
                                        {| name = setting.Key.Value
                                           value = setting.Value.Value |}
                                |]
                                activeRevisionsMode =
                                    match this.ActiveRevisionsMode with
                                    | Single -> "Single"
                                    | Multiple -> "Multiple"
                                registries = [|
                                    for cred in this.ImageRegistryCredentials do
                                        match cred with
                                        | ImageRegistryAuthentication.Credential cred ->
                                            {| server = cred.Server
                                               username = cred.Username
                                               passwordSecretRef = cred.Username |}
                                        | ImageRegistryAuthentication.ListCredentials resourceId ->
                                            {| server = $"{resourceId.Name.Value}.azurecr.io"
                                               username = ArmExpression.create($"listCredentials({resourceId.ArmExpression.Value}, '2019-05-01').username").Eval()
                                               passwordSecretRef = usernameSecretName resourceId |}
                                |]
                                ingress =
                                    match this.IngressMode with
                                    | Some InternalOnly ->
                                        box {| external = false |}
                                    | Some (External (targetPort, transport)) ->
                                        box
                                            {| external = true
                                               targetPort = targetPort
                                               transport =
                                                match transport with
                                                | Some HTTP1 -> "http"
                                                | Some HTTP2 -> "http2"
                                                | Some Auto -> "auto"
                                                | None -> null
                                            |}
                                    | None ->
                                        null
                                dapr =
                                    match this.DaprConfig with
                                    | Some settings ->
                                        {| enabled = true
                                           appId = settings.AppId |}
                                        :> obj
                                    | None ->
                                        {| enabled = false |}
                                |}
                        template =
                            {| containers = [|
                                for container in this.Containers do
                                    {| image = container.DockerImage.ImageTag
                                       name = container.Name
                                       env = [|
                                        for env in this.EnvironmentVariables do
                                            match env.Value with
                                            | EnvValue value -> {| name = env.Key; value = value; secretref = null |}
                                            | SecureEnvExpression armExpr -> {| name = env.Key; value = armExpr.Eval(); secretref = null |}
                                            | SecureEnvValue _ -> {| name = env.Key; value = null; secretref = env.Key |}
                                       |]
                                       resources =
                                        {| cpu = container.Resources.CPU
                                           ephemeralStorage = container.Resources.EphemeralStorage |> Option.map (sprintf "%.2fGi") |> Option.toObj
                                           memory = container.Resources.Memory |> sprintf "%.2fGi" |}
                                           :> obj
                                       volumeMounts =
                                           container.VolumeMounts
                                           |> Seq.map (fun kvp -> {| volumeName=kvp.Key; mountPath=kvp.Value |})
                                           |> List.ofSeq |> function [] -> Unchecked.defaultof<_> | vms -> vms
                                    |}
                               |]
                               scale =
                                {| minReplicas = this.Replicas |> Option.map (fun c -> c.Min) |> Option.toNullable
                                   maxReplicas = this.Replicas |> Option.map (fun c -> c.Max) |> Option.toNullable
                                   rules = [|
                                    for rule in this.ScaleRules do
                                        match rule.Value with
                                        | ScaleRule.Custom customRule ->
                                            {| name = rule.Key
                                               custom = customRule |}
                                            :> obj
                                        | ScaleRule.EventHub settings ->
                                            {| name = rule.Key
                                               custom =
                                                   // https://keda.sh/docs/scalers/azure-event-hub/
                                                   {| ``type`` = "azure-eventhub"
                                                      metadata =
                                                        {| consumerGroup = settings.ConsumerGroup
                                                           unprocessedEventThreshold = string settings.UnprocessedEventThreshold
                                                           blobContainer = settings.CheckpointBlobContainerName
                                                           checkpointStrategy = "blobMetadata" |}
                                                      auth = [|
                                                        {| secretRef = settings.EventHubConnectionSecretRef
                                                           triggerParameter = "connection" |}
                                                        {| secretRef = settings.StorageConnectionSecretRef
                                                           triggerParameter = "storageConnection" |}
                                                      |]
                                                   |}
                                            |}
                                        | ScaleRule.ServiceBus settings ->
                                            {| name = rule.Key
                                               custom =
                                                // https://keda.sh/docs/scalers/azure-service-bus/
                                                {| ``type`` = "azure-servicebus"
                                                   metadata =
                                                    {| queueName = settings.QueueName
                                                       messageCount = string settings.MessageCount |}
                                                   auth = [|
                                                    {| secretRef = settings.SecretRef
                                                       triggerParameter = "connection" |}
                                                   |]
                                                |}
                                            |}
                                        | ScaleRule.Http settings ->
                                            {| name = rule.Key
                                               http =
                                                {| metadata =
                                                    {| concurrentRequests = string settings.ConcurrentRequests |}
                                                |}
                                            |}
                                        | ScaleRule.CPU settings ->
                                            {| name = rule.Key
                                               custom =
                                                {| ``type`` = "cpu"
                                                   metadata =
                                                    {| ``type`` = match settings with Utilisation _ -> "Utilisation" | AverageValue _ -> "AverageValue"
                                                       value = match settings with Utilisation v -> v.Utilisation |> string | AverageValue v -> v.AverageValue |> string
                                                    |}
                                                |}
                                            |}
                                        | ScaleRule.Memory settings ->
                                            {| name = rule.Key
                                               custom =
                                                {| ``type`` = "memory"
                                                   metadata =
                                                    {| ``type`` = match settings with Utilisation _ -> "Utilisation" | AverageValue _ -> "AverageValue"
                                                       value = match settings with Utilisation v -> v.Utilisation |> string | AverageValue v -> v.AverageValue |> string
                                                    |}
                                                |}
                                            |}
                                        | ScaleRule.StorageQueue settings ->
                                            {| name = rule.Key
                                               custom =
                                                {| ``type`` = "azure-queue"
                                                   metadata =
                                                    {| queueName = settings.QueueName
                                                       queueLength = string settings.QueueLength
                                                       connectionFromEnv = settings.StorageConnectionSecretRef
                                                       accountName = settings.AccountName
                                                    |}
                                                |}
                                            |}
                                   |]
                                |}
                               volumes = [
                                   for key, value in Map.toSeq this.Volumes do
                                       match key, value with
                                       |  volumeName, Volume.AzureFileShare (shareName, accountName, _) ->
                                           {| name = volumeName
                                              storageType = "AzureFile"
                                              storageName = volumeName |}
                                       |  volumeName, Volume.EmptyDirectory ->
                                           {| name = volumeName
                                              storageType = "EmptyDir"
                                              storageName = null |}
                               ] |> function [] -> Unchecked.defaultof<_> | vs -> vs
                            |}
                |}
            |}
type DaprComponent =
    { Name : string
      Location : Location
      ManagedEnvironment : ResourceName
      ComponentType : string
      Version : string
      IgnoreErrors : bool option
      InitTimeout : string
      Metadata : Map<string, EnvVar> }
    member this.ResourceName = this.ManagedEnvironment / this.Name
    interface IArmResource with
        member this.ResourceId = daprComponents.resourceId this.ResourceName
        member this.JsonModel =
            {| daprComponents.Create(this.ResourceName, this.Location, [ ResourceId.create (managedEnvironments, this.ManagedEnvironment) ]) with
                properties =
                    {|
                        componentType = this.ComponentType
                        version = this.Version
                        ignoreErrors = this.IgnoreErrors |> Option.toNullable
                        initTimeout = this.InitTimeout
                        secrets = [
                            for metadata in this.Metadata do
                                match metadata.Value with
                                | SecureEnvValue parameter -> {| name = metadata.Key.ToLower(); value = parameter.ArmExpression.Eval() |}
                                | SecureEnvExpression expr -> {| name = metadata.Key.ToLower(); value = expr.Eval() |}
                                | EnvValue _ -> ()
                        ]
                        metadata = [
                            for metadata in this.Metadata do
                                match metadata.Value with
                                | SecureEnvValue _
                                | SecureEnvExpression _ ->
                                    {| name = metadata.Key; secretRef = metadata.Key.ToLower() |} :> obj
                                | EnvValue value ->
                                    {| name = metadata.Key; value = value |}
                        ]
                    |}
            |}


type ManagedEnvironment =
    { Name : ResourceName
      Location : Location
      InternalLoadBalancerState : FeatureFlag
      LogAnalytics : ResourceId
      Dependencies: Set<ResourceId>
      AppInsightsInstrumentationKey : ArmExpression option
      Tags: Map<string,string> }
    interface IArmResource with
        member this.ResourceId = managedEnvironments.resourceId this.Name
        member this.JsonModel =
            {| managedEnvironments.Create(this.Name, this.Location, this.Dependencies, this.Tags) with
                kind = "containerenvironment"
                properties =
                    {| ``type`` = "managed"
                       internalLoadBalancerEnabled = this.InternalLoadBalancerState.AsBoolean
                       daprAIInstrumentationKey =
                        this.AppInsightsInstrumentationKey
                        |> Option.map (fun x -> x.Eval())
                        |> Option.toObj
                       appLogsConfiguration =
                            {| destination = "log-analytics"
                               logAnalyticsConfiguration =
                               {| customerId = LogAnalytics.getCustomerId(this.LogAnalytics).Eval()
                                  sharedKey = LogAnalytics.getPrimarySharedKey(this.LogAnalytics).Eval() |}
                            |}
                    |}
            |}