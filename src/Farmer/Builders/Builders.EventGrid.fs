[<AutoOpen>]
module Farmer.Builders.EventGrid

open Farmer
open Farmer.CoreTypes
open Farmer.Arm.EventGrid

type EventGridConfig<'T> =
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
    static member private ChangeTopic<'TNew>(state:EventGridConfig<_>, source, topic) : EventGridConfig<'TNew> =
      { TopicName = state.TopicName
        Source = source, topic
        Subscriptions = [] }
    static member private AddSub(state:EventGridConfig<'T>, name, destination, endpoint, events) =
        { state with
            Subscriptions =
                {| Name = ResourceName name
                   Destination = destination
                   Endpoint = endpoint
                   Events = events |} :: state.Subscriptions
        }
    member _.Yield _ =
        { TopicName = ResourceName.Empty
          Source = (ResourceName.Empty, TopicType(Farmer.CoreTypes.ResourceType "", ""))
          Subscriptions = [] }
    [<CustomOperation "topic_name">]
    member _.Name (state:EventGridConfig<'T>, name) = { state with TopicName = ResourceName name }
    [<CustomOperation "source">]
    member _.Source(state:EventGridConfig<_>, source:StorageAccountConfig) = EventGridBuilder.ChangeTopic<StorageEvent>(state, source.Name, Topics.StorageAccount)
    member _.Source(state:EventGridConfig<_>, source:WebAppConfig) = EventGridBuilder.ChangeTopic<AppServiceEvent>(state, source.Name, Topics.AppService)
    [<CustomOperation "add_queue_subscriber">]
    member _.AddSubscription(state:EventGridConfig<'T> when 'T :> IEventGridEvent, name, destination:StorageAccountConfig, queue, events:'T list) =
        EventGridBuilder.AddSub(state, name, destination.Name, StorageQueue queue, events |> List.map (fun x -> x.ToEvent))

let eventGrid = EventGridBuilder()