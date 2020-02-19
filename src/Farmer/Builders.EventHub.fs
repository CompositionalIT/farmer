[<AutoOpen>]
module Farmer.Resources.EventHub

open Farmer

/// The SKU of the event hub instance.
type EventHubSku =
    | Basic
    | Standard
    | Premium

type EventHubConfig =
    { Name : ResourceName
      Sku : EventHubSku
      Capacity : int
      ZoneRedundant : bool option
      AutoInflate : bool option
      MaximumThroughputUnits : int option
      KafkaEnabled : bool option
      MessageRetentionInDays : int option
      Partitions : int }

type EventHubBuilder() =
    member __.Yield _ =
        { Name = ResourceName.Empty
          Sku = Standard
          Capacity = 1
          ZoneRedundant = None
          AutoInflate = None
          MaximumThroughputUnits = None
          KafkaEnabled = None
          MessageRetentionInDays = None
          Partitions = 0 }
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
    member __.ZoneRedunant(state:EventHubConfig) = { state with ZoneRedundant = Some true }
    [<CustomOperation "enable_auto_inflate">]
    member __.AutoInflate(state:EventHubConfig) = { state with AutoInflate = Some true }
    [<CustomOperation "enable_kafka">]
    member __.Kafka(state:EventHubConfig) = { state with KafkaEnabled = Some true }
    [<CustomOperation "max_throughput">]
    member __.MaximumThroughputUnits(state:EventHubConfig, maxThroughput) = { state with MaximumThroughputUnits = Some maxThroughput }
    [<CustomOperation "message_retention_days">]
    member __.MessageRetentionDays(state:EventHubConfig, days) = { state with MessageRetentionInDays = Some days }
    [<CustomOperation "partitions">]
    member __.Partitions(state:EventHubConfig, partitions) = { state with Partitions = partitions }

module Converters =
    open Farmer.Models
    let eventHub location (eventHub:EventHubConfig) =
        let eventHubNamespace : EventHubNamespace =
            { Name = eventHub.Name
              Location = location
              Sku =
                {| Name = string eventHub.Sku
                   Tier = string eventHub.Sku
                   Capacity = eventHub.Capacity |}
              ZoneRedundant = eventHub.ZoneRedundant
              IsAutoInflateEnabled = eventHub.AutoInflate
              MaxThroughputUnits = eventHub.MaximumThroughputUnits
              KafkaEnabled = eventHub.KafkaEnabled }
        let eventHub : EventHub =
            { Name = eventHub.Name.Map(sprintf "%s/hub")
              Location = location
              MessageRetentionDays = eventHub.MessageRetentionInDays
              Partitions = eventHub.Partitions
              Dependencies = [ eventHub.Name ] }
        let consumerGroup : EventHubConsumerGroup =
            { Name = eventHub.Name.Map(sprintf "%s/$Default")
              Location = location
              Dependencies = [
                  eventHubNamespace.Name
                  eventHub.Name
              ]
            }

        {| EventHubNamespace = eventHubNamespace
           EventHub = eventHub
           ConsumerGroup = consumerGroup |}      
let eventHub = EventHubBuilder()
