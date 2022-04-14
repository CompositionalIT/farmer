[<AutoOpen>]
module Farmer.Arm.App

open Farmer.ContainerApp
open Farmer.Identity
open Farmer

let containerApps = ResourceType ("Microsoft.App/containerApps", "2022-01-01-preview")
let managedEnvironments = ResourceType ("Microsoft.App/managedEnvironments", "2022-01-01-preview")

open Farmer.ContainerAppValidation

type Container =
    { Name : string
      DockerImage : Containers.DockerImage
      Resources : {| CPU : float<VCores>; Memory : float<Gb> |} }
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
      Dependencies : Set<ResourceId> }

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
            let dependencies = this.Dependencies.Add this.Environment
            {| containerApps.Create(this.Name, this.Location, dependencies) with
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
                                               {| name = ArmExpression.create($"listCredentials({resourceId.ArmExpression.Value}, '2019-05-01').username").Eval()
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
                                               {| server = ArmExpression.create($"reference({resourceId.ArmExpression.Value}, '2019-05-01').loginServer").Eval()
                                                  username = ArmExpression.create($"listCredentials({resourceId.ArmExpression.Value}, '2019-05-01').username").Eval()
                                                  passwordSecretRef = ArmExpression.create($"listCredentials({resourceId.ArmExpression.Value}, '2019-05-01').username").Eval() |}
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
                                   |}

                           template =
                               {| containers = [|
                                     for container in this.Containers do
                                         {|
                                            image = container.DockerImage.ImageTag
                                            name = container.Name
                                            env = [|
                                              for env in this.EnvironmentVariables do
                                                  match env.Value with
                                                  | EnvValue value -> {| name = env.Key; value = value; secretref = null |}
                                                  | SecureEnvExpression armExpr -> {| name = env.Key; value = null; secretref = armExpr.Eval() |}
                                                  | SecureEnvValue _ -> {| name = env.Key; value = null; secretref = env.Key |}
                                             |]
                                            resources =
                                               {| cpu = container.Resources.CPU
                                                  memory = container.Resources.Memory |> sprintf "%.2fGi" |}
                                               :> obj
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
                                                           {| // https://keda.sh/docs/scalers/azure-event-hub/
                                                              ``type`` = "azure-eventhub"
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
                                                    :> obj
                                                 | ScaleRule.ServiceBus settings ->
                                                    {| name = rule.Key
                                                       custom =
                                                           {| // https://keda.sh/docs/scalers/azure-service-bus/
                                                              ``type`` = "azure-servicebus"
                                                              metadata =
                                                                  {| queueName = settings.QueueName
                                                                     messageCount = string settings.MessageCount |}
                                                              auth = [|
                                                                  {| secretRef = settings.SecretRef
                                                                     triggerParameter = "connection" |}
                                                              |]
                                                           |}
                                                    |}
                                                    :> obj
                                                 | ScaleRule.Http settings ->
                                                    {| name = rule.Key
                                                       http =
                                                           {| metadata =
                                                               {| concurrentRequests = string settings.ConcurrentRequests |}
                                                           |}
                                                    |}
                                                    :> obj
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
                                                   :> obj
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
                                                   :> obj
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
                                  dapr =
                                      match this.DaprConfig with
                                      | Some settings ->
                                          {| enabled = true
                                             appId = settings.AppId |}
                                          :> obj
                                      | None ->
                                          {| enabled = false |}
                                          :> obj
                               |}
                   |}
            |}

type ManagedEnvironment =
    { Name : ResourceName
      Location : Location
      InternalLoadBalancerState : FeatureFlag
      LogAnalytics : ResourceId
      Dependencies: Set<ResourceId>
      Tags: Map<string,string> }
    interface IArmResource with
        member this.ResourceId = managedEnvironments.resourceId this.Name
        member this.JsonModel =
            {| managedEnvironments.Create(this.Name, this.Location, this.Dependencies, this.Tags) with
                kind = "containerenvironment"
                properties =
                    {| ``type`` = "managed"
                       internalLoadBalancerEnabled = this.InternalLoadBalancerState.AsBoolean
                       appLogsConfiguration =
                        {| destination = "log-analytics"
                           logAnalyticsConfiguration =
                           {| customerId = LogAnalytics.getCustomerId(this.LogAnalytics).Eval()
                              sharedKey = LogAnalytics.getPrimarySharedKey(this.LogAnalytics).Eval() |}
                        |}
                    |}
            |}