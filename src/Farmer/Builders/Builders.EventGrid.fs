[<AutoOpen>]
module Farmer.Builders.EventGrid

open Farmer
open EventGrid
open Farmer.Arm.EventGrid
open System

type IEventGridEvent = abstract member ToEvent : EventGridEvent
type SubscriptionEvent = SubscriptionEvent of string interface IEventGridEvent with member this.ToEvent = match this with SubscriptionEvent e -> EventGridEvent e
type ResourceGroupEvent = ResourceGroupEvent of string interface IEventGridEvent with member this.ToEvent = match this with ResourceGroupEvent e -> EventGridEvent e
type StorageEvent = StorageEvent of string interface IEventGridEvent with member this.ToEvent = match this with StorageEvent e -> EventGridEvent e
type AppServiceConfigurationEvent = AppServiceConfigurationEvent of string interface IEventGridEvent with member this.ToEvent = match this with AppServiceConfigurationEvent e -> EventGridEvent e
type EventHubEvent = EventHubEvent of string interface IEventGridEvent with member this.ToEvent = match this with EventHubEvent e -> EventGridEvent e
type IoTHubEvent = IoTHubEvent of string interface IEventGridEvent with member this.ToEvent = match this with IoTHubEvent e -> EventGridEvent e
type ServiceBusEvent = ServiceBusEvent of string interface IEventGridEvent with member this.ToEvent = match this with ServiceBusEvent e -> EventGridEvent e
type ContainerRegistryEvent = ContainerRegistryEvent of string interface IEventGridEvent with member this.ToEvent = match this with ContainerRegistryEvent e -> EventGridEvent e
type MediaServicesEvent = MediaServicesEvent of string interface IEventGridEvent with member this.ToEvent = match this with MediaServicesEvent e -> EventGridEvent e
type MapsEvent = MapsEvent of string interface IEventGridEvent with member this.ToEvent = match this with MapsEvent e -> EventGridEvent e
type EventGridTopicEvent = EventGridTopicEvent of string interface IEventGridEvent with member this.ToEvent = match this with EventGridTopicEvent e -> EventGridEvent e
type EventGridDomainEvent = EventGridDomainEvent of string interface IEventGridEvent with member this.ToEvent = match this with EventGridDomainEvent e -> EventGridEvent e
type KeyVaultEvent = KeyVaultEvent of string interface IEventGridEvent with member this.ToEvent = match this with KeyVaultEvent e -> EventGridEvent e
type AppServiceEvent = AppServiceEvent of string interface IEventGridEvent with member this.ToEvent = match this with AppServiceEvent e -> EventGridEvent e
type AppServicePlanEvent = AppServicePlanEvent of string interface IEventGridEvent with member this.ToEvent = match this with AppServicePlanEvent e -> EventGridEvent e
type SignalRServiceEvent = SignalRServiceEvent of string interface IEventGridEvent with member this.ToEvent = match this with SignalRServiceEvent e -> EventGridEvent e
type MachineLearningEvent = MachineLearningEvent of string interface IEventGridEvent with member this.ToEvent = match this with MachineLearningEvent e -> EventGridEvent e

module SystemEvents =
    module EventHub =
        let CaptureFileCreated = EventHubEvent "Microsoft.EventHub.CaptureFileCreated"
    module Storage =
        let BlobCreated = StorageEvent "Microsoft.Storage.BlobCreated"
        let BlobDeleted = StorageEvent "Microsoft.Storage.BlobDeleted"
        let DirectoryCreated = StorageEvent "Microsoft.Storage.DirectoryCreated"
        let DirectoryDeleted = StorageEvent "Microsoft.Storage.DirectoryDeleted"
        let BlobRenamed = StorageEvent "Microsoft.Storage.BlobRenamed"
        let DirectoryRenamed = StorageEvent "Microsoft.Storage.DirectoryRenamed"
    module IotHub =
        let DeviceCreated = IoTHubEvent "Microsoft.Devices.DeviceCreated"
        let DeviceDeleted = IoTHubEvent "Microsoft.Devices.DeviceDeleted"
        let DeviceConnected = IoTHubEvent "Microsoft.Devices.DeviceConnected"
        let DeviceDisconnected = IoTHubEvent "Microsoft.Devices.DeviceDisconnected"
        let DeviceTelemetry = IoTHubEvent "Microsoft.Devices.DeviceTelemetry"
    module ServiceBus =
        let ActiveMessagesAvailableWithNoListeners = ServiceBusEvent "Microsoft.ServiceBus.ActiveMessagesAvailableWithNoListeners"
        let DeadLetterMessagesAvailableWithNoListeners = ServiceBusEvent "Microsoft.ServiceBus.DeadLetterMessagesAvailableWithNoListeners"
    module ContainerRegistry =
        let ImagePushed = ContainerRegistryEvent "Microsoft.ContainerRegistry.ImagePushed"
        let ImageDeleted = ContainerRegistryEvent "Microsoft.ContainerRegistry.ImageDeleted"
        let ChartPushed = ContainerRegistryEvent "Microsoft.ContainerRegistry.ChartPushed"
        let ChartDeleted = ContainerRegistryEvent "Microsoft.ContainerRegistry.ChartDeleted"
    module Maps =
        let GeofenceEntered = MapsEvent "Microsoft.Maps.GeofenceEntered"
        let GeofenceExited = MapsEvent "Microsoft.Maps.GeofenceExited"
        let GeofenceResult = MapsEvent "Microsoft.Maps.GeofenceResult"
    module KeyVault =
        let CertificateNewVersionCreated = KeyVaultEvent "Microsoft.KeyVault.CertificateNewVersionCreated"
        let CertificateNearExpiry = KeyVaultEvent "Microsoft.KeyVault.CertificateNearExpiry"
        let CertificateExpired = KeyVaultEvent "Microsoft.KeyVault.CertificateExpired"
        let SecretNewVersionCreated = KeyVaultEvent "Microsoft.KeyVault.SecretNewVersionCreated"
        let SecretNearExpiry = KeyVaultEvent "Microsoft.KeyVault.SecretNearExpiry"
        let SecretExpired = KeyVaultEvent "Microsoft.KeyVault.SecretExpired"
        let KeyNewVersionCreated = KeyVaultEvent "Microsoft.KeyVault.KeyNewVersionCreated"
        let KeyNearExpiry = KeyVaultEvent "Microsoft.KeyVault.KeyNearExpiry"
        let KeyExpired = KeyVaultEvent "Microsoft.KeyVault.KeyExpired"
    module AppService =
        let AppUpdated = AppServiceEvent "Microsoft.Web.AppUpdated"
        let BackupOperationStarted = AppServiceEvent "Microsoft.Web.BackupOperationStarted"
        let BackupOperationCompleted = AppServiceEvent "Microsoft.Web.BackupOperationCompleted"
        let BackupOperationFailed = AppServiceEvent "Microsoft.Web.BackupOperationFailed"
        let RestoreOperationStarted = AppServiceEvent "Microsoft.Web.RestoreOperationStarted"
        let RestoreOperationCompleted = AppServiceEvent "Microsoft.Web.RestoreOperationCompleted"
        let RestoreOperationFailed = AppServiceEvent "Microsoft.Web.RestoreOperationFailed"
        let SlotSwapStarted = AppServiceEvent "Microsoft.Web.SlotSwapStarted"
        let SlotSwapCompleted = AppServiceEvent "Microsoft.Web.SlotSwapCompleted"
        let SlotSwapFailed = AppServiceEvent "Microsoft.Web.SlotSwapFailed"
        let SlotSwapWithPreviewStarted = AppServiceEvent "Microsoft.Web.SlotSwapWithPreviewStarted"
        let SlotSwapWithPreviewCancelled = AppServiceEvent "Microsoft.Web.SlotSwapWithPreviewCancelled"
    module SignalR =
        let ClientConnectionConnected = SignalRServiceEvent "Microsoft.SignalRService.ClientConnectionConnected"
        let ClientConnectionDisconnected = SignalRServiceEvent "Microsoft.SignalRService.ClientConnectionDisconnected"

type EventGridConfig<'T> =
    { TopicName : ResourceName
      Source : ResourceName * TopicType
      Subscriptions :
        {| Name : ResourceName
           Destination : ResourceName
           Endpoint : EndpointType
           SystemEvents : EventGridEvent list |} list
      Tags: Map<string,string>
    }
    interface IBuilder with
        member this.DependencyName = this.TopicName
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
                   SystemEvents = events |} :: state.Subscriptions
        }
    member _.Yield _ =
        { TopicName = ResourceName.Empty
          Source = ResourceName.Empty, TopicType(CoreTypes.ResourceType("", ""), "")
          Subscriptions = []
          Tags = Map.empty  }
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
    member _.AddQueueSubscription(state:EventGridConfig<'T> when 'T :> IEventGridEvent, storageAccount:StorageAccountConfig, queueName, events:'T list) =
        EventGridBuilder.AddSub(state, queueName + "-queue", storageAccount.Name.ResourceName, StorageQueue queueName, events |> List.map (fun x -> x.ToEvent))
    [<CustomOperation "add_webhook_subscriber">]
    member _.AddWebSubscription(state:EventGridConfig<'T> when 'T :> IEventGridEvent, webAppName:ResourceName, webHookEndpoint:Uri, events:'T list) =
        EventGridBuilder.AddSub(state, webHookEndpoint.LocalPath + "-webhook", webAppName, WebHook webHookEndpoint, events |> List.map (fun x -> x.ToEvent))
    member this.AddWebSubscription(state:EventGridConfig<_>, webApp:WebAppConfig, route, events) =
        this.AddWebSubscription(state, webApp.Name, Uri (sprintf "https://%s/%s" webApp.Endpoint route), events)
    [<CustomOperation "add_eventhub_subscriber">]
    member _.AddEventHubSubscription(state:EventGridConfig<'T> when 'T :> IEventGridEvent, eventHub:EventHubConfig, events:'T list) =
        EventGridBuilder.AddSub(state, eventHub.Name.Value + "-eventhub", eventHub.EventHubNamespaceName, EventHub eventHub.Name, events |> List.map (fun x -> x.ToEvent))
    [<CustomOperation "add_tags">]
    member _.Tags(state:EventGridConfig<'T>, pairs) =
        { state with
            Tags = pairs |> List.fold (fun map (key,value) -> Map.add key value map) state.Tags }
    [<CustomOperation "add_tag">]
    member this.Tag(state:EventGridConfig<'T>, key, value) = this.Tags(state, [ (key,value) ])

let eventGrid = EventGridBuilder()