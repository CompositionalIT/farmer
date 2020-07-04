[<AutoOpen>]
module Farmer.Arm.EventGrid

open Farmer
open Farmer.CoreTypes

let systemTopics = ResourceType "Microsoft.EventGrid/systemTopics"
let eventSubscriptions = ResourceType "Microsoft.EventGrid/systemTopics/eventSubscriptions"

type Event = Event of string member this.Value = match this with Event s -> s

type EventGridEvent =
    | SubscriptionEvent of Event
    | ResourceGroupEvent of Event
    | StorageEvent of Event
    | AppServiceConfigurationEvent of Event
    | EventHubEvent of Event
    | IoTHubEvent of Event
    | ServiceBusEvent of Event
    | ContainerRegistryEvent of Event
    | MediaServicesEvent of Event
    | MapsEvent of Event
    | EventGridTopicEvent of Event
    | EventGridDomainEvent of Event
    | KeyVaultEvent of Event
    | AppServiceEvent of Event
    | AppServicePlanEvent of Event
    | SignalRServiceEvent of Event
    | MachineLearningEvent of Event
    member this.Event =
        match this with
        | SubscriptionEvent event | ResourceGroupEvent event | StorageEvent event
        | AppServiceConfigurationEvent event | EventHubEvent event | IoTHubEvent event
        | ServiceBusEvent event | ContainerRegistryEvent event | MediaServicesEvent event
        | MapsEvent event | EventGridTopicEvent event | EventGridDomainEvent event
        | KeyVaultEvent event | AppServiceEvent event | AppServicePlanEvent event
        | SignalRServiceEvent event | MachineLearningEvent event ->
            event

module Events =
    module EventHub =
        let CaptureFileCreated = EventHubEvent (Event "Microsoft.EventHub.CaptureFileCreated")
    module Storage =
        let BlobCreated = StorageEvent (Event "Microsoft.Storage.BlobCreated")
        let BlobDeleted = StorageEvent (Event "Microsoft.Storage.BlobDeleted")
        let DirectoryCreated = StorageEvent (Event "Microsoft.Storage.DirectoryCreated")
        let DirectoryDeleted = StorageEvent (Event "Microsoft.Storage.DirectoryDeleted")
        let BlobRenamed = StorageEvent (Event "Microsoft.Storage.BlobRenamed")
        let DirectoryRenamed = StorageEvent (Event "Microsoft.Storage.DirectoryRenamed")
    module ResourceGroup =
        let ResourceGroupWriteSuccess = ResourceGroupEvent (Event "Microsoft.Resources.ResourceWriteSuccess")
        let ResourceGroupWriteFailure = ResourceGroupEvent (Event "Microsoft.Resources.ResourceWriteFailure")
        let ResourceGroupWriteCancel = ResourceGroupEvent (Event "Microsoft.Resources.ResourceWriteCancel")
        let ResourceGroupDeleteSuccess = ResourceGroupEvent (Event "Microsoft.Resources.ResourceDeleteSuccess")
        let ResourceGroupDeleteFailure = ResourceGroupEvent (Event "Microsoft.Resources.ResourceDeleteFailure")
        let ResourceGroupDeleteCancel = ResourceGroupEvent (Event "Microsoft.Resources.ResourceDeleteCancel")
        let ResourceGroupActionSuccess = ResourceGroupEvent (Event "Microsoft.Resources.ResourceActionSuccess")
        let ResourceGroupActionFailure = ResourceGroupEvent (Event "Microsoft.Resources.ResourceActionFailure")
        let ResourceGroupActionCancel = ResourceGroupEvent (Event "Microsoft.Resources.ResourceActionCancel")
    module Subscription =
        let SubscriptionWriteSuccess = SubscriptionEvent (Event "Microsoft.Resources.ResourceWriteSuccess")
        let SubscriptionWriteFailure = SubscriptionEvent (Event "Microsoft.Resources.ResourceWriteFailure")
        let SubscriptionWriteCancel = SubscriptionEvent (Event "Microsoft.Resources.ResourceWriteCancel")
        let SubscriptionDeleteSuccess = SubscriptionEvent (Event "Microsoft.Resources.ResourceDeleteSuccess")
        let SubscriptionDeleteFailure = SubscriptionEvent (Event "Microsoft.Resources.ResourceDeleteFailure")
        let SubscriptionDeleteCancel = SubscriptionEvent (Event "Microsoft.Resources.ResourceDeleteCancel")
        let SubscriptionActionSuccess = SubscriptionEvent (Event "Microsoft.Resources.ResourceActionSuccess")
        let SubscriptionActionFailure = SubscriptionEvent (Event "Microsoft.Resources.ResourceActionFailure")
        let SubscriptionActionCancel = SubscriptionEvent (Event "Microsoft.Resources.ResourceActionCancel")
    module IoTHub =
        let DeviceCreated = IoTHubEvent (Event "Microsoft.Devices.DeviceCreated")
        let DeviceDeleted = IoTHubEvent (Event "Microsoft.Devices.DeviceDeleted")
        let DeviceConnected = IoTHubEvent (Event "Microsoft.Devices.DeviceConnected")
        let DeviceDisconnected = IoTHubEvent (Event "Microsoft.Devices.DeviceDisconnected")
        let DeviceTelemetry = IoTHubEvent (Event "Microsoft.Devices.DeviceTelemetry")
    module ServiceBus =
        let ActiveMessagesAvailableWithNoListeners = ServiceBusEvent (Event "Microsoft.ServiceBus.ActiveMessagesAvailableWithNoListeners")
        let DeadLetterMessagesAvailableWithNoListeners = ServiceBusEvent (Event "Microsoft.ServiceBus.DeadLetterMessagesAvailableWithNoListeners")
    module ContainerRegistry =
        let ImagePushed = ContainerRegistryEvent (Event "Microsoft.ContainerRegistry.ImagePushed")
        let ImageDeleted = ContainerRegistryEvent (Event "Microsoft.ContainerRegistry.ImageDeleted")
        let ChartPushed = ContainerRegistryEvent (Event "Microsoft.ContainerRegistry.ChartPushed")
        let ChartDeleted = ContainerRegistryEvent (Event "Microsoft.ContainerRegistry.ChartDeleted")
    module MediaServices =
        let Jobstatechange = MediaServicesEvent (Event "Microsoft.Media.JobStateChange")
        let Jobscheduled = MediaServicesEvent (Event "Microsoft.Media.JobScheduled")
        let Jobprocessing = MediaServicesEvent (Event "Microsoft.Media.JobProcessing")
        let Jobcanceling = MediaServicesEvent (Event "Microsoft.Media.JobCanceling")
        let Jobfinished = MediaServicesEvent (Event "Microsoft.Media.JobFinished")
        let Jobcanceled = MediaServicesEvent (Event "Microsoft.Media.JobCanceled")
        let Joberrored = MediaServicesEvent (Event "Microsoft.Media.JobErrored")
        let Joboutputstatechange = MediaServicesEvent (Event "Microsoft.Media.JobOutputStateChange")
        let Joboutputscheduled = MediaServicesEvent (Event "Microsoft.Media.JobOutputScheduled")
        let Joboutputprocessing = MediaServicesEvent (Event "Microsoft.Media.JobOutputProcessing")
        let Joboutputcanceling = MediaServicesEvent (Event "Microsoft.Media.JobOutputCanceling")
        let Joboutputfinished = MediaServicesEvent (Event "Microsoft.Media.JobOutputFinished")
        let Joboutputcanceled = MediaServicesEvent (Event "Microsoft.Media.JobOutputCanceled")
        let Joboutputerrored = MediaServicesEvent (Event "Microsoft.Media.JobOutputErrored")
        let Encoderconnected = MediaServicesEvent (Event "Microsoft.Media.LiveEventEncoderConnected")
        let Streamdatareceived = MediaServicesEvent (Event "Microsoft.Media.LiveEventIncomingStreamReceived")
        let Encoderdisconnected = MediaServicesEvent (Event "Microsoft.Media.LiveEventEncoderDisconnected")
        let Encoderconnectionrejected = MediaServicesEvent (Event "Microsoft.Media.LiveEventConnectionRejected")
        let Trackdiscontinuitydetected = MediaServicesEvent (Event "Microsoft.Media.LiveEventTrackDiscontinuityDetected")
        let Videotracksoutofsync = MediaServicesEvent (Event "Microsoft.Media.LiveEventIncomingVideoStreamsOutOfSync")
        let Incomingdatachunkdropped = MediaServicesEvent (Event "Microsoft.Media.LiveEventIncomingDataChunkDropped")
        let Incomingstreamsoutofsync = MediaServicesEvent (Event "Microsoft.Media.LiveEventIncomingStreamsOutOfSync")
        let Ingestheartbeat = MediaServicesEvent (Event "Microsoft.Media.LiveEventIngestHeartbeat")
        let Joboutputprogress = MediaServicesEvent (Event "Microsoft.Media.JobOutputProgress")
    module Maps =
        let GeofenceEntered = MapsEvent (Event "Microsoft.Maps.GeofenceEntered")
        let GeofenceExited = MapsEvent (Event "Microsoft.Maps.GeofenceExited")
        let GeofenceResult = MapsEvent (Event "Microsoft.Maps.GeofenceResult")
    module AppConfiguration =
        let Keyvaluemodified = AppServiceConfigurationEvent (Event "Microsoft.AppConfiguration.KeyValueModified")
        let Keyvaluedeleted = AppServiceConfigurationEvent (Event "Microsoft.AppConfiguration.KeyValueDeleted")
    module KeyVault =
        let CertificateNewVersionCreated = KeyVaultEvent (Event "Microsoft.KeyVault.CertificateNewVersionCreated")
        let CertificateNearExpiry = KeyVaultEvent (Event "Microsoft.KeyVault.CertificateNearExpiry")
        let CertificateExpired = KeyVaultEvent (Event "Microsoft.KeyVault.CertificateExpired")
        let SecretNewVersionCreated = KeyVaultEvent (Event "Microsoft.KeyVault.SecretNewVersionCreated")
        let SecretNearExpiry = KeyVaultEvent (Event "Microsoft.KeyVault.SecretNearExpiry")
        let SecretExpired = KeyVaultEvent (Event "Microsoft.KeyVault.SecretExpired")
        let KeyNewVersionCreated = KeyVaultEvent (Event "Microsoft.KeyVault.KeyNewVersionCreated")
        let KeyNearExpiry = KeyVaultEvent (Event "Microsoft.KeyVault.KeyNearExpiry")
        let KeyExpired = KeyVaultEvent (Event "Microsoft.KeyVault.KeyExpired")
    module AppService =
        let AppUpdated = AppServiceEvent (Event "Microsoft.Web.AppUpdated")
        let BackupOperationStarted = AppServiceEvent (Event "Microsoft.Web.BackupOperationStarted")
        let BackupOperationCompleted = AppServiceEvent (Event "Microsoft.Web.BackupOperationCompleted")
        let BackupOperationFailed = AppServiceEvent (Event "Microsoft.Web.BackupOperationFailed")
        let RestoreOperationStarted = AppServiceEvent (Event "Microsoft.Web.RestoreOperationStarted")
        let RestoreOperationCompleted = AppServiceEvent (Event "Microsoft.Web.RestoreOperationCompleted")
        let RestoreOperationFailed = AppServiceEvent (Event "Microsoft.Web.RestoreOperationFailed")
        let SlotSwapStarted = AppServiceEvent (Event "Microsoft.Web.SlotSwapStarted")
        let SlotSwapCompleted = AppServiceEvent (Event "Microsoft.Web.SlotSwapCompleted")
        let SlotSwapFailed = AppServiceEvent (Event "Microsoft.Web.SlotSwapFailed")
        let SlotSwapWithPreviewStarted = AppServiceEvent (Event "Microsoft.Web.SlotSwapWithPreviewStarted")
        let SlotSwapWithPreviewCancelled = AppServiceEvent (Event "Microsoft.Web.SlotSwapWithPreviewCancelled")
    module AppServicePlan =
        let AppServicePlanUpdated = AppServicePlanEvent (Event "Microsoft.Web.AppServicePlanUpdated")

    let Clientconnectionconnected = SignalRServiceEvent (Event "Microsoft.SignalRService.ClientConnectionConnected")
    let Clientconnectiondisconnected = SignalRServiceEvent (Event "Microsoft.SignalRService.ClientConnectionDisconnected")

    let Modelregistered = MachineLearningEvent (Event "Microsoft.MachineLearningServices.ModelRegistered")
    let Modeldeployed = MachineLearningEvent (Event "Microsoft.MachineLearningServices.ModelDeployed")
    let Runcompleted = MachineLearningEvent (Event "Microsoft.MachineLearningServices.RunCompleted")
    let Datasetdriftdetected = MachineLearningEvent (Event "Microsoft.MachineLearningServices.DatasetDriftDetected")
    let Runstatuschanged = MachineLearningEvent (Event "Microsoft.MachineLearningServices.RunStatusChanged")

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
                    filter = {| includedEventTypes = [ for event in this.Events do event.Event.Value ] |}
                 |}
            |} :> _