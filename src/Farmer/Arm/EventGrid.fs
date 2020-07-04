[<AutoOpen>]
module Farmer.Arm.EventGrid

open Farmer
open Farmer.CoreTypes

let systemTopics = ResourceType "Microsoft.EventGrid/systemTopics"
let eventSubscriptions = ResourceType "Microsoft.EventGrid/systemTopics/eventSubscriptions"

type EventGridEvent = EventGridEvent of string member this.Value = match this with EventGridEvent s -> s

type IEventGridEvent = abstract member ToEvent : EventGridEvent
type SubscriptionEvent = SubscriptionEvent of EventGridEvent interface IEventGridEvent with member this.ToEvent = match this with SubscriptionEvent e -> e
type ResourceGroupEvent = ResourceGroupEvent of EventGridEvent interface IEventGridEvent with member this.ToEvent = match this with ResourceGroupEvent e -> e
type StorageEvent = StorageEvent of EventGridEvent interface IEventGridEvent with member this.ToEvent = match this with StorageEvent e -> e
type AppServiceConfigurationEvent = AppServiceConfigurationEvent of EventGridEvent interface IEventGridEvent with member this.ToEvent = match this with AppServiceConfigurationEvent e -> e
type EventHubEvent = EventHubEvent of EventGridEvent interface IEventGridEvent with member this.ToEvent = match this with EventHubEvent e -> e
type IoTHubEvent = IoTHubEvent of EventGridEvent interface IEventGridEvent with member this.ToEvent = match this with IoTHubEvent e -> e
type ServiceBusEvent = ServiceBusEvent of EventGridEvent interface IEventGridEvent with member this.ToEvent = match this with ServiceBusEvent e -> e
type ContainerRegistryEvent = ContainerRegistryEvent of EventGridEvent interface IEventGridEvent with member this.ToEvent = match this with ContainerRegistryEvent e -> e
type MediaServicesEvent = MediaServicesEvent of EventGridEvent interface IEventGridEvent with member this.ToEvent = match this with MediaServicesEvent e -> e
type MapsEvent = MapsEvent of EventGridEvent interface IEventGridEvent with member this.ToEvent = match this with MapsEvent e -> e
type EventGridTopicEvent = EventGridTopicEvent of EventGridEvent interface IEventGridEvent with member this.ToEvent = match this with EventGridTopicEvent e -> e
type EventGridDomainEvent = EventGridDomainEvent of EventGridEvent interface IEventGridEvent with member this.ToEvent = match this with EventGridDomainEvent e -> e
type KeyVaultEvent = KeyVaultEvent of EventGridEvent interface IEventGridEvent with member this.ToEvent = match this with KeyVaultEvent e -> e
type AppServiceEvent = AppServiceEvent of EventGridEvent interface IEventGridEvent with member this.ToEvent = match this with AppServiceEvent e -> e
type AppServicePlanEvent = AppServicePlanEvent of EventGridEvent interface IEventGridEvent with member this.ToEvent = match this with AppServicePlanEvent e -> e
type SignalRServiceEvent = SignalRServiceEvent of EventGridEvent interface IEventGridEvent with member this.ToEvent = match this with SignalRServiceEvent e -> e
type MachineLearningEvent = MachineLearningEvent of EventGridEvent interface IEventGridEvent with member this.ToEvent = match this with MachineLearningEvent e -> e

module Events =
    module EventHub =
        let CaptureFileCreated = EventHubEvent (EventGridEvent "Microsoft.EventHub.CaptureFileCreated")
    module Storage =
        let BlobCreated = StorageEvent (EventGridEvent "Microsoft.Storage.BlobCreated")
        let BlobDeleted = StorageEvent (EventGridEvent "Microsoft.Storage.BlobDeleted")
        let DirectoryCreated = StorageEvent (EventGridEvent "Microsoft.Storage.DirectoryCreated")
        let DirectoryDeleted = StorageEvent (EventGridEvent "Microsoft.Storage.DirectoryDeleted")
        let BlobRenamed = StorageEvent (EventGridEvent "Microsoft.Storage.BlobRenamed")
        let DirectoryRenamed = StorageEvent (EventGridEvent "Microsoft.Storage.DirectoryRenamed")
    module ResourceGroup =
        let ResourceGroupWriteSuccess = ResourceGroupEvent (EventGridEvent "Microsoft.Resources.ResourceWriteSuccess")
        let ResourceGroupWriteFailure = ResourceGroupEvent (EventGridEvent "Microsoft.Resources.ResourceWriteFailure")
        let ResourceGroupWriteCancel = ResourceGroupEvent (EventGridEvent "Microsoft.Resources.ResourceWriteCancel")
        let ResourceGroupDeleteSuccess = ResourceGroupEvent (EventGridEvent "Microsoft.Resources.ResourceDeleteSuccess")
        let ResourceGroupDeleteFailure = ResourceGroupEvent (EventGridEvent "Microsoft.Resources.ResourceDeleteFailure")
        let ResourceGroupDeleteCancel = ResourceGroupEvent (EventGridEvent "Microsoft.Resources.ResourceDeleteCancel")
        let ResourceGroupActionSuccess = ResourceGroupEvent (EventGridEvent "Microsoft.Resources.ResourceActionSuccess")
        let ResourceGroupActionFailure = ResourceGroupEvent (EventGridEvent "Microsoft.Resources.ResourceActionFailure")
        let ResourceGroupActionCancel = ResourceGroupEvent (EventGridEvent "Microsoft.Resources.ResourceActionCancel")
    module Subscription =
        let SubscriptionWriteSuccess = SubscriptionEvent (EventGridEvent "Microsoft.Resources.ResourceWriteSuccess")
        let SubscriptionWriteFailure = SubscriptionEvent (EventGridEvent "Microsoft.Resources.ResourceWriteFailure")
        let SubscriptionWriteCancel = SubscriptionEvent (EventGridEvent "Microsoft.Resources.ResourceWriteCancel")
        let SubscriptionDeleteSuccess = SubscriptionEvent (EventGridEvent "Microsoft.Resources.ResourceDeleteSuccess")
        let SubscriptionDeleteFailure = SubscriptionEvent (EventGridEvent "Microsoft.Resources.ResourceDeleteFailure")
        let SubscriptionDeleteCancel = SubscriptionEvent (EventGridEvent "Microsoft.Resources.ResourceDeleteCancel")
        let SubscriptionActionSuccess = SubscriptionEvent (EventGridEvent "Microsoft.Resources.ResourceActionSuccess")
        let SubscriptionActionFailure = SubscriptionEvent (EventGridEvent "Microsoft.Resources.ResourceActionFailure")
        let SubscriptionActionCancel = SubscriptionEvent (EventGridEvent "Microsoft.Resources.ResourceActionCancel")
    module IoTHub =
        let DeviceCreated = IoTHubEvent (EventGridEvent "Microsoft.Devices.DeviceCreated")
        let DeviceDeleted = IoTHubEvent (EventGridEvent "Microsoft.Devices.DeviceDeleted")
        let DeviceConnected = IoTHubEvent (EventGridEvent "Microsoft.Devices.DeviceConnected")
        let DeviceDisconnected = IoTHubEvent (EventGridEvent "Microsoft.Devices.DeviceDisconnected")
        let DeviceTelemetry = IoTHubEvent (EventGridEvent "Microsoft.Devices.DeviceTelemetry")
    module ServiceBus =
        let ActiveMessagesAvailableWithNoListeners = ServiceBusEvent (EventGridEvent "Microsoft.ServiceBus.ActiveMessagesAvailableWithNoListeners")
        let DeadLetterMessagesAvailableWithNoListeners = ServiceBusEvent (EventGridEvent "Microsoft.ServiceBus.DeadLetterMessagesAvailableWithNoListeners")
    module ContainerRegistry =
        let ImagePushed = ContainerRegistryEvent (EventGridEvent "Microsoft.ContainerRegistry.ImagePushed")
        let ImageDeleted = ContainerRegistryEvent (EventGridEvent "Microsoft.ContainerRegistry.ImageDeleted")
        let ChartPushed = ContainerRegistryEvent (EventGridEvent "Microsoft.ContainerRegistry.ChartPushed")
        let ChartDeleted = ContainerRegistryEvent (EventGridEvent "Microsoft.ContainerRegistry.ChartDeleted")
    module MediaServices =
        let Jobstatechange = MediaServicesEvent (EventGridEvent "Microsoft.Media.JobStateChange")
        let Jobscheduled = MediaServicesEvent (EventGridEvent "Microsoft.Media.JobScheduled")
        let Jobprocessing = MediaServicesEvent (EventGridEvent "Microsoft.Media.JobProcessing")
        let Jobcanceling = MediaServicesEvent (EventGridEvent "Microsoft.Media.JobCanceling")
        let Jobfinished = MediaServicesEvent (EventGridEvent "Microsoft.Media.JobFinished")
        let Jobcanceled = MediaServicesEvent (EventGridEvent "Microsoft.Media.JobCanceled")
        let Joberrored = MediaServicesEvent (EventGridEvent "Microsoft.Media.JobErrored")
        let Joboutputstatechange = MediaServicesEvent (EventGridEvent "Microsoft.Media.JobOutputStateChange")
        let Joboutputscheduled = MediaServicesEvent (EventGridEvent "Microsoft.Media.JobOutputScheduled")
        let Joboutputprocessing = MediaServicesEvent (EventGridEvent "Microsoft.Media.JobOutputProcessing")
        let Joboutputcanceling = MediaServicesEvent (EventGridEvent "Microsoft.Media.JobOutputCanceling")
        let Joboutputfinished = MediaServicesEvent (EventGridEvent "Microsoft.Media.JobOutputFinished")
        let Joboutputcanceled = MediaServicesEvent (EventGridEvent "Microsoft.Media.JobOutputCanceled")
        let Joboutputerrored = MediaServicesEvent (EventGridEvent "Microsoft.Media.JobOutputErrored")
        let Encoderconnected = MediaServicesEvent (EventGridEvent "Microsoft.Media.LiveEventEncoderConnected")
        let Streamdatareceived = MediaServicesEvent (EventGridEvent "Microsoft.Media.LiveEventIncomingStreamReceived")
        let Encoderdisconnected = MediaServicesEvent (EventGridEvent "Microsoft.Media.LiveEventEncoderDisconnected")
        let Encoderconnectionrejected = MediaServicesEvent (EventGridEvent "Microsoft.Media.LiveEventConnectionRejected")
        let Trackdiscontinuitydetected = MediaServicesEvent (EventGridEvent "Microsoft.Media.LiveEventTrackDiscontinuityDetected")
        let Videotracksoutofsync = MediaServicesEvent (EventGridEvent "Microsoft.Media.LiveEventIncomingVideoStreamsOutOfSync")
        let Incomingdatachunkdropped = MediaServicesEvent (EventGridEvent "Microsoft.Media.LiveEventIncomingDataChunkDropped")
        let Incomingstreamsoutofsync = MediaServicesEvent (EventGridEvent "Microsoft.Media.LiveEventIncomingStreamsOutOfSync")
        let Ingestheartbeat = MediaServicesEvent (EventGridEvent "Microsoft.Media.LiveEventIngestHeartbeat")
        let Joboutputprogress = MediaServicesEvent (EventGridEvent "Microsoft.Media.JobOutputProgress")
    module Maps =
        let GeofenceEntered = MapsEvent (EventGridEvent "Microsoft.Maps.GeofenceEntered")
        let GeofenceExited = MapsEvent (EventGridEvent "Microsoft.Maps.GeofenceExited")
        let GeofenceResult = MapsEvent (EventGridEvent "Microsoft.Maps.GeofenceResult")
    module AppConfiguration =
        let Keyvaluemodified = AppServiceConfigurationEvent (EventGridEvent "Microsoft.AppConfiguration.KeyValueModified")
        let Keyvaluedeleted = AppServiceConfigurationEvent (EventGridEvent "Microsoft.AppConfiguration.KeyValueDeleted")
    module KeyVault =
        let CertificateNewVersionCreated = KeyVaultEvent (EventGridEvent "Microsoft.KeyVault.CertificateNewVersionCreated")
        let CertificateNearExpiry = KeyVaultEvent (EventGridEvent "Microsoft.KeyVault.CertificateNearExpiry")
        let CertificateExpired = KeyVaultEvent (EventGridEvent "Microsoft.KeyVault.CertificateExpired")
        let SecretNewVersionCreated = KeyVaultEvent (EventGridEvent "Microsoft.KeyVault.SecretNewVersionCreated")
        let SecretNearExpiry = KeyVaultEvent (EventGridEvent "Microsoft.KeyVault.SecretNearExpiry")
        let SecretExpired = KeyVaultEvent (EventGridEvent "Microsoft.KeyVault.SecretExpired")
        let KeyNewVersionCreated = KeyVaultEvent (EventGridEvent "Microsoft.KeyVault.KeyNewVersionCreated")
        let KeyNearExpiry = KeyVaultEvent (EventGridEvent "Microsoft.KeyVault.KeyNearExpiry")
        let KeyExpired = KeyVaultEvent (EventGridEvent "Microsoft.KeyVault.KeyExpired")
    module AppService =
        let AppUpdated = AppServiceEvent (EventGridEvent "Microsoft.Web.AppUpdated")
        let BackupOperationStarted = AppServiceEvent (EventGridEvent "Microsoft.Web.BackupOperationStarted")
        let BackupOperationCompleted = AppServiceEvent (EventGridEvent "Microsoft.Web.BackupOperationCompleted")
        let BackupOperationFailed = AppServiceEvent (EventGridEvent "Microsoft.Web.BackupOperationFailed")
        let RestoreOperationStarted = AppServiceEvent (EventGridEvent "Microsoft.Web.RestoreOperationStarted")
        let RestoreOperationCompleted = AppServiceEvent (EventGridEvent "Microsoft.Web.RestoreOperationCompleted")
        let RestoreOperationFailed = AppServiceEvent (EventGridEvent "Microsoft.Web.RestoreOperationFailed")
        let SlotSwapStarted = AppServiceEvent (EventGridEvent "Microsoft.Web.SlotSwapStarted")
        let SlotSwapCompleted = AppServiceEvent (EventGridEvent "Microsoft.Web.SlotSwapCompleted")
        let SlotSwapFailed = AppServiceEvent (EventGridEvent "Microsoft.Web.SlotSwapFailed")
        let SlotSwapWithPreviewStarted = AppServiceEvent (EventGridEvent "Microsoft.Web.SlotSwapWithPreviewStarted")
        let SlotSwapWithPreviewCancelled = AppServiceEvent (EventGridEvent "Microsoft.Web.SlotSwapWithPreviewCancelled")
    module AppServicePlan =
        let AppServicePlanUpdated = AppServicePlanEvent (EventGridEvent "Microsoft.Web.AppServicePlanUpdated")
    module SignalR =
        let Clientconnectionconnected = SignalRServiceEvent (EventGridEvent "Microsoft.SignalRService.ClientConnectionConnected")
        let Clientconnectiondisconnected = SignalRServiceEvent (EventGridEvent "Microsoft.SignalRService.ClientConnectionDisconnected")
    module MachineLearning =
        let Modelregistered = MachineLearningEvent (EventGridEvent "Microsoft.MachineLearningServices.ModelRegistered")
        let Modeldeployed = MachineLearningEvent (EventGridEvent "Microsoft.MachineLearningServices.ModelDeployed")
        let Runcompleted = MachineLearningEvent (EventGridEvent "Microsoft.MachineLearningServices.RunCompleted")
        let Datasetdriftdetected = MachineLearningEvent (EventGridEvent "Microsoft.MachineLearningServices.DatasetDriftDetected")
        let Runstatuschanged = MachineLearningEvent (EventGridEvent "Microsoft.MachineLearningServices.RunStatusChanged")

type TopicType =
    | TopicType of ResourceType * topic:string
    member this.Value = match this with TopicType (_, s) -> s
    member this.ResourceType = match this with TopicType (r, _) -> r
module Topics =
    let EventHubsNamespace = TopicType (signalR, "Microsoft.Eventhub.Namespaces")
    let StorageAccount = TopicType (storageAccounts, "Microsoft.Storage.StorageAccounts")
    let IoTHubAccount = TopicType (iotHubs, "Microsoft.Devices.IoTHubs")
    let ServiceBusNamespace = TopicType (namespaces, "Microsoft.ServiceBus.Namespaces")
    let ContainerRegistry = TopicType (registries, "Microsoft.ContainerRegistry.Registries")
    let MapsAccount = TopicType (accounts, "Microsoft.Maps.Accounts")
    let KeyVault = TopicType (vaults, "Microsoft.KeyVault.vaults")
    let AppService = TopicType (sites, "Microsoft.Web.Sites")
    let AppServicePlan = TopicType (serverFarms, "Microsoft.Web.ServerFarms")
    let SignalR = TopicType (signalR, "Microsoft.SignalRService.SignalR")
    // let AppConfiguration = TopicType (, "Microsoft.AppConfiguration.ConfigurationStores")
    // let Subscription = TopicType "Microsoft.Resources.Subscriptions"
    // let ResourceGroup = TopicType "Microsoft.Resources.ResourceGroups"
    // let MicrosoftAzureMediaService = TopicType "Microsoft.Media.MediaServices"
    // let MachineLearningWorkspace = TopicType "Microsoft.MachineLearningServices.Workspaces"

type EndpointType =
    | WebHook of System.Uri
    | EventHub
    | StorageQueue of queue:string

type Topic =
    { Name : ResourceName
      Location : Location
      Source : ResourceName
      TopicType : TopicType }
    interface IArmResource with
        member this.ResourceName = this.Name
        member this.JsonModel =
            {| name = this.Name.Value
               ``type`` = systemTopics.ArmValue
               apiVersion = "2020-04-01-preview"
               location = this.Location.ArmValue
               dependsOn = [ this.Source.Value]
               properties =
               {| source = ArmExpression.resourceId(this.TopicType.ResourceType, this.Source).Eval()
                  topicType = this.TopicType.Value |}
             |} :> _

type Subscription =
    { Name : ResourceName
      Topic : ResourceName
      Destination : ResourceName
      DestinationEndpoint : EndpointType
      Events : EventGridEvent list }
    interface IArmResource with
        member this.ResourceName = this.Name
        member this.JsonModel =
            {| ``type`` = eventSubscriptions.ArmValue
               apiVersion = "2020-04-01-preview"
               name = this.Topic.Value + "/" + this.Name.Value
               dependsOn = [ this.Topic.Value; this.Destination.Value ]
               properties =
                 {| destination =
                        match this.DestinationEndpoint with
                        | WebHook uri ->
                          {| endpointType = "WebHook"
                             properties = {| endpointUrl = uri.ToString() |}
                          |} |> box
                        | EventHub ->
                          {| endpointType = "EventHub"
                             properties = {| resourceId = ArmExpression.resourceId(EventHub.namespaces, this.Destination).Eval() |}
                          |} :> _
                        | StorageQueue queueName ->
                          {| endpointType = "StorageQueue"
                             properties =
                              {| resourceId = ArmExpression.resourceId(Storage.storageAccounts, this.Destination).Eval()
                                 queueName = queueName |}
                          |} :> _
                    filter = {| includedEventTypes = [ for event in this.Events do event.Value ] |}
                 |}
            |} :> _