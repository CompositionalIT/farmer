[<AutoOpen>]
module Farmer.Builders.EventGrid

open Farmer
open Farmer.CoreTypes
open Farmer.Arm.EventGrid

type EventGridConfig =
    { TopicName : ResourceName
      Source : ResourceName * TopicType
      Subscriptions :
        {| Name : ResourceName
           Destination : ResourceName
           Endpoint : EndpointType
           Events : EventGridEvent list |} list
    }
    interface IBuilder with
        member this.DependencyName = this.TopicName
        member this.BuildResources location = [
            { Name = this.TopicName
              Location = location
              Source = fst this.Source
              TopicType = snd this.Source }

            for sub in this.Subscriptions do
                { Name = sub.Name
                  Topic = this.TopicName
                  Destination = sub.Destination
                  DestinationEndpoint = sub.Endpoint
                  Events = sub.Events }
        ]

type EventGridBuilder() =
    member _.Yield _ =
        { TopicName = ResourceName.Empty
          Source = (ResourceName.Empty, TopicType(Farmer.CoreTypes.ResourceType "", ""))
          Subscriptions = [] }
    [<CustomOperation "topic_name">]
    member _.Name (state:EventGridConfig, name) = { state with TopicName = ResourceName name }
    [<CustomOperation "source">]
    member _.Source(state:EventGridConfig, source:StorageAccountConfig) = { state with Source = (source.Name, Topics.StorageAccount) }
    [<CustomOperation "add_queue_subscriber">]
    member _.AddSubscription(state:EventGridConfig, name, destination:StorageAccountConfig, queue, events) =
        { state with
            Subscriptions =
                {| Name = ResourceName name
                   Destination = destination.Name
                   Endpoint = StorageQueue queue
                   Events = events |} :: state.Subscriptions
        }

let eventGrid = EventGridBuilder()