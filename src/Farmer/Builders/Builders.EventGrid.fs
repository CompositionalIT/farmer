[<AutoOpen>]
module Farmer.Builders.EventGrid

open Farmer
open EventGrid
open Farmer.Arm.EventGrid
open System

type SubscriptionEvent = interface end
type ResourceGroupEvent = interface end
type StorageEvent = interface end
type AppServiceConfigurationEvent = interface end
type EventHubEvent = interface end
type IoTHubEvent = interface end
type ServiceBusEvent = interface end
type ContainerRegistryEvent = interface end
type MediaServicesEvent = interface end
type MapsEvent = interface end
type EventGridTopicEvent = interface end
type EventGridDomainEvent = interface end
type KeyVaultEvent = interface end
type AppServiceEvent = interface end
type AppServicePlanEvent = interface end
type SignalRServiceEvent = interface end
type MachineLearningEvent = interface end

module SystemEvents =
    let toEvent<'T> : string -> EventGridEvent<'T> = EventGridEvent
    module EventHub =
        let CaptureFileCreated = toEvent<EventHubEvent> "Microsoft.EventHub.CaptureFileCreated"
    module Storage =
        let BlobCreated = toEvent<StorageEvent> "Microsoft.Storage.BlobCreated"
        let BlobDeleted = toEvent<StorageEvent> "Microsoft.Storage.BlobDeleted"
        let DirectoryCreated = toEvent<StorageEvent> "Microsoft.Storage.DirectoryCreated"
        let DirectoryDeleted = toEvent<StorageEvent> "Microsoft.Storage.DirectoryDeleted"
        let BlobRenamed = toEvent<StorageEvent> "Microsoft.Storage.BlobRenamed"
        let DirectoryRenamed = toEvent<StorageEvent> "Microsoft.Storage.DirectoryRenamed"
    module IotHub =
        let DeviceCreated = toEvent<IoTHubEvent> "Microsoft.Devices.DeviceCreated"
        let DeviceDeleted = toEvent<IoTHubEvent> "Microsoft.Devices.DeviceDeleted"
        let DeviceConnected = toEvent<IoTHubEvent> "Microsoft.Devices.DeviceConnected"
        let DeviceDisconnected = toEvent<IoTHubEvent> "Microsoft.Devices.DeviceDisconnected"
        let DeviceTelemetry = toEvent<IoTHubEvent> "Microsoft.Devices.DeviceTelemetry"
    module ServiceBus =
        let ActiveMessagesAvailableWithNoListeners = toEvent<ServiceBusEvent> "Microsoft.ServiceBus.ActiveMessagesAvailableWithNoListeners"
        let DeadLetterMessagesAvailableWithNoListeners = toEvent<ServiceBusEvent> "Microsoft.ServiceBus.DeadLetterMessagesAvailableWithNoListeners"
    module ContainerRegistry =
        let ImagePushed = toEvent<ContainerRegistryEvent> "Microsoft.ContainerRegistry.ImagePushed"
        let ImageDeleted = toEvent<ContainerRegistryEvent> "Microsoft.ContainerRegistry.ImageDeleted"
        let ChartPushed = toEvent<ContainerRegistryEvent> "Microsoft.ContainerRegistry.ChartPushed"
        let ChartDeleted = toEvent<ContainerRegistryEvent> "Microsoft.ContainerRegistry.ChartDeleted"
    module Maps =
        let GeofenceEntered = toEvent<MapsEvent> "Microsoft.Maps.GeofenceEntered"
        let GeofenceExited = toEvent<MapsEvent> "Microsoft.Maps.GeofenceExited"
        let GeofenceResult = toEvent<MapsEvent> "Microsoft.Maps.GeofenceResult"
    module KeyVault =
        let CertificateNewVersionCreated = toEvent<KeyVaultEvent> "Microsoft.KeyVault.CertificateNewVersionCreated"
        let CertificateNearExpiry = toEvent<KeyVaultEvent> "Microsoft.KeyVault.CertificateNearExpiry"
        let CertificateExpired = toEvent<KeyVaultEvent> "Microsoft.KeyVault.CertificateExpired"
        let SecretNewVersionCreated = toEvent<KeyVaultEvent> "Microsoft.KeyVault.SecretNewVersionCreated"
        let SecretNearExpiry = toEvent<KeyVaultEvent> "Microsoft.KeyVault.SecretNearExpiry"
        let SecretExpired = toEvent<KeyVaultEvent> "Microsoft.KeyVault.SecretExpired"
        let KeyNewVersionCreated = toEvent<KeyVaultEvent> "Microsoft.KeyVault.KeyNewVersionCreated"
        let KeyNearExpiry = toEvent<KeyVaultEvent> "Microsoft.KeyVault.KeyNearExpiry"
        let KeyExpired = toEvent<KeyVaultEvent> "Microsoft.KeyVault.KeyExpired"
    module AppService =
        let AppUpdated = toEvent<AppServiceEvent> "Microsoft.Web.AppUpdated"
        let BackupOperationStarted = toEvent<AppServiceEvent> "Microsoft.Web.BackupOperationStarted"
        let BackupOperationCompleted = toEvent<AppServiceEvent> "Microsoft.Web.BackupOperationCompleted"
        let BackupOperationFailed = toEvent<AppServiceEvent> "Microsoft.Web.BackupOperationFailed"
        let RestoreOperationStarted = toEvent<AppServiceEvent> "Microsoft.Web.RestoreOperationStarted"
        let RestoreOperationCompleted = toEvent<AppServiceEvent> "Microsoft.Web.RestoreOperationCompleted"
        let RestoreOperationFailed = toEvent<AppServiceEvent> "Microsoft.Web.RestoreOperationFailed"
        let SlotSwapStarted = toEvent<AppServiceEvent> "Microsoft.Web.SlotSwapStarted"
        let SlotSwapCompleted = toEvent<AppServiceEvent> "Microsoft.Web.SlotSwapCompleted"
        let SlotSwapFailed = toEvent<AppServiceEvent> "Microsoft.Web.SlotSwapFailed"
        let SlotSwapWithPreviewStarted = toEvent<AppServiceEvent> "Microsoft.Web.SlotSwapWithPreviewStarted"
        let SlotSwapWithPreviewCancelled = toEvent<AppServiceEvent> "Microsoft.Web.SlotSwapWithPreviewCancelled"
    module SignalR =
        let ClientConnectionConnected = toEvent<SignalRServiceEvent> "Microsoft.SignalRService.ClientConnectionConnected"
        let ClientConnectionDisconnected = toEvent<SignalRServiceEvent> "Microsoft.SignalRService.ClientConnectionDisconnected"

type EventGridConfig<'T> =
    { TopicName : ResourceName
      Source : ResourceName * TopicType
      Subscriptions :
        {| Name : ResourceName
           Destination : ResourceName
           Endpoint : EndpointType
           SystemEvents : EventGridEvent<'T> list |} list
      Tags: Map<string,string>
    }
    interface IBuilder with
        member this.ResourceId = systemTopics.resourceId this.TopicName
        member this.BuildResources location = [
            { Name = this.TopicName
              Location = location
              Source = fst this.Source
              TopicType = snd this.Source
              Tags = this.Tags }

            for sub in this.Subscriptions do
                { Name = sub.Name
                  Topic = this.TopicName
                  Destination = sub.Destination
                  DestinationEndpoint = sub.Endpoint
                  Events = sub.SystemEvents }
        ]

type EventGridBuilder() =
    static member private ChangeTopic<'TNew>(state:EventGridConfig<_>, source, topic) : EventGridConfig<'TNew> =
      { TopicName = state.TopicName
        Source = source, topic
        Subscriptions = []
        Tags = Map.empty }
    static member private AddSub(state:EventGridConfig<'T>, name, destination:ResourceName, endpoint, events) =
        let name = destination.Value + "-" + name
        { state with
            Subscriptions =
                {| Name = ResourceName name
                   Destination = destination
                   Endpoint = endpoint
                   SystemEvents = events |} :: state.Subscriptions }
    member _.Yield _ =
        { TopicName = ResourceName.Empty
          Source = ResourceName.Empty, TopicType(ResourceType("", ""), "")
          Subscriptions = []
          Tags = Map.empty }
    [<CustomOperation "topic_name">]
    member _.Name (state:EventGridConfig<'T>, name) = { state with TopicName = ResourceName name }
    [<CustomOperation "source">]
    member _.Source(state:EventGridConfig<_>, source:StorageAccountConfig) = EventGridBuilder.ChangeTopic<StorageEvent>(state, source.Name.ResourceName, Topics.StorageAccount)
    member _.Source(state:EventGridConfig<_>, source:WebAppConfig) = EventGridBuilder.ChangeTopic<AppServiceEvent>(state, source.Name, Topics.AppService)
    member _.Source(state:EventGridConfig<_>, source:KeyVaultConfig) = EventGridBuilder.ChangeTopic<KeyVaultEvent>(state, source.Name, Topics.KeyVault)
    member _.Source(state:EventGridConfig<_>, source:SignalRConfig) = EventGridBuilder.ChangeTopic<SignalRServiceEvent>(state, source.Name, Topics.SignalR)
    member _.Source(state:EventGridConfig<_>, source:MapsConfig) = EventGridBuilder.ChangeTopic<MapsEvent>(state, source.Name, Topics.MapsAccount)
    member _.Source(state:EventGridConfig<_>, source:ContainerRegistryConfig) = EventGridBuilder.ChangeTopic<ContainerRegistryEvent>(state, source.Name, Topics.ContainerRegistry)
    member _.Source(state:EventGridConfig<_>, source:ServiceBusConfig) = EventGridBuilder.ChangeTopic<ServiceBusEvent>(state, source.Name, Topics.ServiceBusNamespace)
    member _.Source(state:EventGridConfig<_>, source:IotHubConfig) = EventGridBuilder.ChangeTopic<IoTHubEvent>(state, source.Name, Topics.IoTHubAccount)
    member _.Source(state:EventGridConfig<_>, source:EventHubConfig) = EventGridBuilder.ChangeTopic<EventHubEvent>(state, source.EventHubNamespaceName, Topics.EventHubsNamespace)

    [<CustomOperation "add_queue_subscriber">]
    member _.AddQueueSubscription(state:EventGridConfig<'T>, storageAccount:StorageAccountConfig, queueName, events) =
        EventGridBuilder.AddSub(state, queueName + "-queue", storageAccount.Name.ResourceName, StorageQueue (ResourceName queueName), events)
    [<CustomOperation "add_webhook_subscriber">]
    member _.AddWebSubscription(state:EventGridConfig<'T>, webAppName:ResourceName, webHookEndpoint:Uri, events) =
        EventGridBuilder.AddSub(state, webHookEndpoint.LocalPath + "-webhook", webAppName, WebHook webHookEndpoint, events)
    member this.AddWebSubscription(state:EventGridConfig<_>, webApp:WebAppConfig, route, events) =
        this.AddWebSubscription(state, webApp.Name, Uri (sprintf "https://%s/%s" webApp.Endpoint route), events)
    [<CustomOperation "add_eventhub_subscriber">]
    member _.AddEventHubSubscription(state:EventGridConfig<'T>, eventHub:EventHubConfig, events:EventGridEvent<_> list) =
        EventGridBuilder.AddSub(state, eventHub.Name.Value + "-eventhub", eventHub.EventHubNamespaceName, EventHub eventHub.Name, events)

    [<CustomOperation "add_tags">]
    member _.Tags(state:EventGridConfig<'T>, pairs) =
        { state with
            Tags = pairs |> List.fold (fun map (key,value) -> Map.add key value map) state.Tags }
    [<CustomOperation "add_tag">]
    member this.Tag(state:EventGridConfig<'T>, key, value) = this.Tags(state, [ (key,value) ])

let eventGrid = EventGridBuilder()