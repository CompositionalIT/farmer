[<AutoOpen>]
module Farmer.Arm.EventGrid

open Farmer
open Farmer.EventGrid
open Farmer.CoreTypes

let systemTopics = ResourceType "Microsoft.EventGrid/systemTopics"
let eventSubscriptions = ResourceType "Microsoft.EventGrid/systemTopics/eventSubscriptions"

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
        member this.ResourceName = this.Name
        member this.JsonModel =
            {| name = this.Name.Value
               ``type`` = systemTopics.ArmValue
               apiVersion = "2020-04-01-preview"
               location = this.Location.ArmValue
               dependsOn = [ this.Source.Value ]
               properties =
                  {| source = ArmExpression.resourceId(this.TopicType.ResourceType, this.Source).Eval()
                     topicType = this.TopicType.Value |}
               tags = this.Tags
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
                        | EventHub hubName ->
                          {| endpointType = "EventHub"
                             properties = {| resourceId = ArmExpression.resourceId(eventHubs, this.Destination, hubName).Eval() |}
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