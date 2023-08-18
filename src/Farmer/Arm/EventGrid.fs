[<AutoOpen>]
module Farmer.Arm.EventGrid

open Farmer
open EventGrid

let systemTopics = ResourceType("Microsoft.EventGrid/systemTopics", "2022-06-15")

let eventSubscriptions =
    ResourceType("Microsoft.EventGrid/systemTopics/eventSubscriptions", "2022-06-15")

type TopicType =
    | TopicType of ResourceType * topic: string

    member this.Value =
        match this with
        | TopicType(_, s) -> s

    member this.ResourceType =
        match this with
        | TopicType(r, _) -> r

module Topics =
    let EventHubsNamespace =
        TopicType(EventHub.namespaces, "Microsoft.Eventhub.Namespaces")

    let StorageAccount = TopicType(storageAccounts, "Microsoft.Storage.StorageAccounts")
    let IoTHubAccount = TopicType(iotHubs, "Microsoft.Devices.IoTHubs")
    let ServiceBusNamespace = TopicType(namespaces, "Microsoft.ServiceBus.Namespaces")

    let ContainerRegistry =
        TopicType(registries, "Microsoft.ContainerRegistry.Registries")

    let MapsAccount = TopicType(accounts, "Microsoft.Maps.Accounts")
    let KeyVault = TopicType(vaults, "Microsoft.KeyVault.vaults")
    let AppService = TopicType(sites, "Microsoft.Web.Sites")
    let AppServicePlan = TopicType(serverFarms, "Microsoft.Web.ServerFarms")
    let SignalR = TopicType(signalR, "Microsoft.SignalRService.SignalR")
    let ResourceGroup = TopicType(resourceGroups, "Microsoft.Resources.ResourceGroups")

type ServiceBusQueueEndpointType = {
    Bus: ResourceName
    Queue: ResourceName
}

type ServiceBusTopicEndpointType = {
    Bus: ResourceName
    Topic: ResourceName
}

type AzureFunctionEndpointType = {
    ResourceId: LinkedResource
    MaxEventsPerBatch: uint
    PreferredBatchSizeInKilobytes: uint
}

type ServiceBusEndpointType =
    | Queue of Queue: ServiceBusQueueEndpointType
    | Topic of Topic: ServiceBusTopicEndpointType

type EndpointType =
    | WebHook of System.Uri
    | EventHub of eventHub: ResourceName
    | StorageQueue of queue: ResourceName
    | ServiceBus of bus: ServiceBusEndpointType
    | AzureFunction of AzureFunctionEndpointType

type Topic = {
    Name: ResourceName
    Location: Location
    Source: ResourceName
    TopicType: TopicType
    Tags: Map<string, string>
} with

    interface IArmResource with
        member this.ResourceId = systemTopics.resourceId this.Name

        member this.JsonModel =
            let dependencies, source =
                match this.TopicType with
                | TopicType(rt, _) when rt = ResourceGroup.resourceGroups -> [], "[resourceGroup().id]"
                | TopicType(rt, _) -> let s = rt.resourceId this.Source in [ s ], s.Eval()

            {|
                systemTopics.Create(this.Name, this.Location, dependencies, this.Tags) with
                    properties = {|
                        source = source
                        topicType = this.TopicType.Value
                    |}
            |}

type Subscription<'T> = {
    Name: ResourceName
    Topic: ResourceName
    Destination: ResourceName
    DestinationEndpoint: EndpointType
    Events: EventGridEvent<'T> list
} with

    interface IArmResource with
        member this.ResourceId = eventSubscriptions.resourceId (this.Topic / this.Name)

        member this.JsonModel =
            let managedDestinationResourceId =
                match this.DestinationEndpoint with
                | AzureFunction {
                                    ResourceId = LinkedResource.Managed rid
                                } -> Some rid
                | AzureFunction _ -> None
                | EventHub hubName -> Some(Namespaces.eventHubs.resourceId (this.Destination, hubName))
                | StorageQueue queue ->
                    Some(Storage.queues.resourceId (this.Destination, ResourceName "default", queue))
                | WebHook _ -> None
                | ServiceBus(Queue { Queue = queue; Bus = bus }) -> Some(ServiceBus.queues.resourceId (bus, queue))
                | ServiceBus(Topic { Topic = topic; Bus = bus }) -> Some(ServiceBus.topics.resourceId (bus, topic))

            {|
                eventSubscriptions.Create(
                    this.Topic / this.Name,
                    dependsOn = [
                        systemTopics.resourceId this.Topic
                        yield! Option.toList managedDestinationResourceId
                    ]
                ) with
                    properties = {|
                        destination =
                            match this.DestinationEndpoint with
                            | AzureFunction fn ->
                                {|
                                    endpointType = "AzureFunction"
                                    properties = {|
                                        resourceId = fn.ResourceId.ResourceId.Eval()
                                        maxEventsPerBatch = fn.MaxEventsPerBatch
                                        preferredBatchSizeInKilobytes = fn.PreferredBatchSizeInKilobytes
                                    |}
                                |}
                                |> box
                            | WebHook uri ->
                                {|
                                    endpointType = "WebHook"
                                    properties = {| endpointUrl = uri.ToString() |}
                                |}
                                |> box
                            | EventHub hubName -> {|
                                endpointType = "EventHub"
                                properties = {|
                                    resourceId = Namespaces.eventHubs.resourceId(this.Destination, hubName).Eval()
                                |}
                              |}
                            | StorageQueue queueName -> {|
                                endpointType = "StorageQueue"
                                properties = {|
                                    resourceId = (storageAccounts.resourceId this.Destination).Eval()
                                    queueName = queueName.Value
                                |}
                              |}
                            | ServiceBus(Queue { Queue = queue; Bus = bus }) -> {|
                                endpointType = "ServiceBusQueue"
                                properties = {|
                                    resourceId = (ServiceBus.queues.resourceId (bus, queue)).Eval()
                                    queueName = queue.Value
                                |}
                              |}
                            | ServiceBus(Topic { Topic = topic; Bus = bus }) -> {|
                                endpointType = "ServiceBusTopic"
                                properties = {|
                                    resourceId = (ServiceBus.topics.resourceId (bus, topic)).Eval()
                                    queueName = topic.Value
                                |}
                              |}
                        filter = {|
                            includedEventTypes = [
                                for event in this.Events do
                                    event.Value
                            ]
                        |}
                    |}
            |}
