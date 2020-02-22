[<AutoOpen>]
module Farmer.Resources.EventHub

open Farmer

/// The SKU of the event hub instance.
type EventHubSku =
    | Basic
    | Standard
    | Premium

type ThroughputSettings = AutoInflate | ManualInflate of maxThroughput:int
type EventHubConfig =
    { Name : ResourceName
      Sku : EventHubSku
      Capacity : int
      ZoneRedundant : bool option
      ThroughputSettings : ThroughputSettings option
      KafkaEnabled : bool option
      MessageRetentionInDays : int option
      Partitions : int
      ConsumerGroups : string Set }

type EventHubBuilder() =
    member __.Yield _ =
        { Name = ResourceName.Empty
          Sku = Standard
          Capacity = 1
          ZoneRedundant = None
          ThroughputSettings = None
          KafkaEnabled = None
          MessageRetentionInDays = None
          Partitions = 0
          ConsumerGroups = Set [ "$Default" ] }
    /// Sets the name of the Event Hub instance.
    [<CustomOperation "name">]
    member __.Name(state:EventHubConfig, name) = { state with Name = name }
    member this.Name(state:EventHubConfig, name) = this.Name(state, ResourceName name)
    /// Sets the sku of the Event Hub instance.
    [<CustomOperation "sku">]
    member __.Sku(state:EventHubConfig, sku) = { state with Sku = sku }
    [<CustomOperation "capacity">]
    member __.ReplicaCount(state:EventHubConfig, capacity:int) = { state with Capacity = capacity }
    [<CustomOperation "enable_zone_redundant">]
    member __.ZoneRedundant(state:EventHubConfig) = { state with ZoneRedundant = Some true }
    [<CustomOperation "enable_auto_inflate">]
    member __.AutoInflate(state:EventHubConfig) = { state with ThroughputSettings = Some AutoInflate }
    [<CustomOperation "enable_kafka">]
    member __.Kafka(state:EventHubConfig) = { state with KafkaEnabled = Some true }
    [<CustomOperation "max_throughput">]
    member __.MaximumThroughputUnits(state:EventHubConfig, maxThroughput) = { state with ThroughputSettings = Some (ManualInflate maxThroughput) }
    [<CustomOperation "message_retention_days">]
    member __.MessageRetentionDays(state:EventHubConfig, days) = { state with MessageRetentionInDays = Some days }
    [<CustomOperation "partitions">]
    member __.Partitions(state:EventHubConfig, partitions) = { state with Partitions = partitions }
    [<CustomOperation "add_consumer_group">]
    member __.AddConsumerGroup(state:EventHubConfig, name) = { state with ConsumerGroups = state.ConsumerGroups.Add name }

module Converters =
    open Farmer.Models
    let eventHub location (eventHubConfig:EventHubConfig) =
        let eventHubNamespace : EventHubNamespace =
            { Name = eventHubConfig.Name
              Location = location
              Sku =
                {| Name = string eventHubConfig.Sku
                   Tier = string eventHubConfig.Sku
                   Capacity = eventHubConfig.Capacity |}
              ZoneRedundant = eventHubConfig.ZoneRedundant
              IsAutoInflateEnabled =
                    eventHubConfig.ThroughputSettings
                    |> Option.map (function
                        | AutoInflate -> true
                        | ManualInflate _ -> false)
              MaxThroughputUnits =
                    eventHubConfig.ThroughputSettings
                    |> Option.map (function
                        | AutoInflate -> 0
                        | ManualInflate throughput -> throughput)
              KafkaEnabled = eventHubConfig.KafkaEnabled }
        let eventHub : EventHub =
            { Name = eventHubConfig.Name.Map(sprintf "%s/hub")
              Location = location
              MessageRetentionDays = eventHubConfig.MessageRetentionInDays
              Partitions = eventHubConfig.Partitions
              Dependencies = [ eventHubConfig.Name ] }
        let consumerGroups =
            [ for consumerGroup in eventHubConfig.ConsumerGroups ->
                { Name = eventHub.Name.Map(fun name -> sprintf "%s/%s" name consumerGroup)
                  Location = location
                  Dependencies = [
                      eventHubNamespace.Name
                      eventHub.Name
                  ]
                }
            ]
        {| EventHubNamespace = eventHubNamespace
           EventHub = eventHub
           ConsumerGroups = consumerGroups |}      
let eventHub = EventHubBuilder()
