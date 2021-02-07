[<AutoOpen>]
module Farmer.Arm.EventGrid

open Farmer
open EventGrid

let systemTopics = ResourceType ("Microsoft.EventGrid/systemTopics", "2020-04-01-preview")
let eventSubscriptions = ResourceType ("Microsoft.EventGrid/systemTopics/eventSubscriptions", "2020-04-01-preview")

type TopicType =
    | TopicType of ResourceType * topic:string
    member this.Value = match this with TopicType (_, s) -> s
    member this.ResourceType = match this with TopicType (r, _) -> r

module Topics =
    let EventHubsNamespace = TopicType (EventHub.namespaces, "Microsoft.Eventhub.Namespaces")
    let StorageAccount = TopicType (storageAccounts, "Microsoft.Storage.StorageAccounts")
    let IoTHubAccount = TopicType (iotHubs, "Microsoft.Devices.IoTHubs")
    let ServiceBusNamespace = TopicType (namespaces, "Microsoft.ServiceBus.Namespaces")
    let ContainerRegistry = TopicType (registries, "Microsoft.ContainerRegistry.Registries")
    let MapsAccount = TopicType (accounts, "Microsoft.Maps.Accounts")
    let KeyVault = TopicType (vaults, "Microsoft.KeyVault.vaults")
    let AppService = TopicType (sites, "Microsoft.Web.Sites")
    let AppServicePlan = TopicType (serverFarms, "Microsoft.Web.ServerFarms")
    let SignalR = TopicType (signalR, "Microsoft.SignalRService.SignalR")

type EndpointType =
    | WebHook of System.Uri
    | EventHub of eventHub:ResourceName
    | StorageQueue of queue:string

type Topic =
    { Name : ResourceName
      Location : Location
      Source : ResourceName
      TopicType : TopicType
      Tags: Map<string,string>  }
    interface IArmResource with
        member this.ResourceId = systemTopics.resourceId this.Name
        member this.JsonModel =
            let sourceResourceId = this.TopicType.ResourceType.resourceId this.Source
            {| systemTopics.Create(this.Name, this.Location, [ sourceResourceId ], this.Tags) with
                properties =
                    {| source = sourceResourceId.Eval()
                       topicType = this.TopicType.Value |}
             |} :> _

type Subscription<'T> =
    { Name : ResourceName
      Topic : ResourceName
      Destination : ResourceName
      DestinationEndpoint : EndpointType
      Events : EventGridEvent<'T> list }
    interface IArmResource with
        member this.ResourceId = eventSubscriptions.resourceId (this.Topic/this.Name)
        member this.JsonModel =
            let destinationResourceId =
                match this.DestinationEndpoint with
                | EventHub hubName -> Some (Namespaces.eventHubs.resourceId (this.Destination, hubName))
                | StorageQueue _ -> Some (storageAccounts.resourceId this.Destination)
                | WebHook _ -> None

            {| eventSubscriptions.Create(this.Topic/this.Name, dependsOn = [ systemTopics.resourceId this.Topic; yield! Option.toList destinationResourceId ]) with
                 properties =
                   {| destination =
                          match this.DestinationEndpoint with
                          | WebHook uri ->
                            {| endpointType = "WebHook"
                               properties = {| endpointUrl = uri.ToString() |}
                            |} |> box
                          | EventHub hubName ->
                            {| endpointType = "EventHub"
                               properties = {| resourceId = Namespaces.eventHubs.resourceId(this.Destination, hubName).Eval() |}
                            |} :> _
                          | StorageQueue queueName ->
                            {| endpointType = "StorageQueue"
                               properties =
                                {| resourceId = (storageAccounts.resourceId this.Destination).Eval()
                                   queueName = queueName |}
                            |} :> _
                      filter = {| includedEventTypes = [ for event in this.Events do event.Value ] |}
                   |}
            |} :> _