[<AutoOpen>]
module Farmer.Builders.ServiceBus

open Farmer
open Farmer.CoreTypes
open Farmer.ServiceBus
open Farmer.Arm.ServiceBus
open Namespaces
open Topics
open System

type ServiceBusQueueConfig =
    { Name : ResourceName
      LockDuration : TimeSpan option
      DuplicateDetection : TimeSpan option
      DefaultMessageTimeToLive : TimeSpan option
      Session : bool option
      DeadLetteringOnMessageExpiration : bool option
      MaxDeliveryCount : int option
      EnablePartitioning : bool option }

type ServiceBusQueueBuilder() =
    member _.Yield _ =
        { Name = ResourceName.Empty
          LockDuration = None
          DuplicateDetection = None
          Session = None
          DeadLetteringOnMessageExpiration = None
          DefaultMessageTimeToLive = None
          MaxDeliveryCount = None
          EnablePartitioning = None }

    /// The name of the queue.
    [<CustomOperation "name">] member _.Name(state:ServiceBusQueueConfig, name) = { state with Name = ResourceName name }
    /// The length of time that a lock can be held on a message.
    [<CustomOperation "lock_duration_minutes">] member _.LockDurationMinutes(state:ServiceBusQueueConfig, duration) = { state with LockDuration = Some (TimeSpan.FromMinutes (float duration)) }
    /// The maximum number of times a message can be delivered before dead lettering.
    [<CustomOperation "duplicate_detection_minutes">] member _.DuplicateDetection(state:ServiceBusQueueConfig, maxTimeWindow) = { state with DuplicateDetection = Some (TimeSpan.FromMinutes (float maxTimeWindow)) }
    /// The default time-to-live for messages. If not specified, the maximum TTL will be set for the SKU.
    [<CustomOperation "message_ttl_days">] member _.MessageTtl(state:ServiceBusQueueConfig, ttl) = { state with DefaultMessageTimeToLive = Some (TimeSpan.FromDays (float ttl)) }
    /// Enables session support.
    [<CustomOperation "max_delivery_count">] member _.MaxDeliveryCount(state:ServiceBusQueueConfig, count) = { state with MaxDeliveryCount = Some count }
    /// Whether to enable duplicate detection, and if so, how long to check for.ServiceBusQueueConfig
    [<CustomOperation "enable_session">] member _.Session(state:ServiceBusQueueConfig) = { state with Session = Some true }
    /// Enables dead lettering of messages that expire.
    [<CustomOperation "enable_dead_letter_on_message_expiration">] member _.DeadLetteringOnMessageExpiration(state:ServiceBusQueueConfig) = { state with DeadLetteringOnMessageExpiration = Some true }
    /// Enables partition support on the queue.
    [<CustomOperation "enable_partition">] member _.EnablePartition(state:ServiceBusQueueConfig) = { state with EnablePartitioning = Some true }

type ServiceBusSubscriptionConfig =
    { Name : ResourceName

      LockDuration : TimeSpan option
      DuplicateDetection : TimeSpan option
      DefaultMessageTimeToLive : TimeSpan option
      MaxDeliveryCount : int option
      Session : bool option
      DeadLetteringOnMessageExpiration : bool option }

type ServiceBusSubscriptionBuilder() =
    member _.Yield _ =
        { Name = ResourceName.Empty

          LockDuration = None
          DuplicateDetection = None
          DefaultMessageTimeToLive = None
          MaxDeliveryCount = None
          Session = None
          DeadLetteringOnMessageExpiration = None }

    /// The name of the queue.
    [<CustomOperation "name">] member _.Name(state:ServiceBusSubscriptionConfig, name) = { state with Name = ResourceName name }
    /// The length of time that a lock can be held on a message.
    [<CustomOperation "lock_duration_minutes">] member _.LockDurationMinutes(state:ServiceBusSubscriptionConfig, duration) = { state with LockDuration = Some (TimeSpan.FromMinutes (float duration)) }
    /// Whether to enable duplicate detection, and if so, how long to check for.ServiceBusQueueConfig
    [<CustomOperation "duplicate_detection_minutes">] member _.DuplicateDetection(state:ServiceBusSubscriptionConfig, maxTimeWindow) = { state with DuplicateDetection = Some (TimeSpan.FromMinutes (float maxTimeWindow)) }
    /// The default time-to-live for messages. If not specified, the maximum TTL will be set for the SKU.
    [<CustomOperation "message_ttl_days">] member _.MessageTtl(state:ServiceBusSubscriptionConfig, ttl) = { state with DefaultMessageTimeToLive = Some (TimeSpan.FromDays (float ttl)) }
    /// Enables session support.
    [<CustomOperation "max_delivery_count">] member _.MaxDeliveryCount(state:ServiceBusQueueConfig, count) = { state with MaxDeliveryCount = Some count }
    /// Whether to enable duplicate detection, and if so, how long to check for.ServiceBusQueueConfig
    [<CustomOperation "enable_session">] member _.Session(state:ServiceBusQueueConfig) = { state with Session = Some true }
    /// Enables dead lettering of messages that expire.
    [<CustomOperation "enable_dead_letter_on_message_expiration">] member _.DeadLetteringOnMessageExpiration(state:ServiceBusQueueConfig) = { state with DeadLetteringOnMessageExpiration = Some true }

type ServiceBusTopicConfig =
    { Name : ResourceName
      DuplicateDetection : TimeSpan option
      DefaultMessageTimeToLive : TimeSpan option
      EnablePartitioning : bool option
      Subscriptions : Map<ResourceName, ServiceBusSubscriptionConfig> }

type ServiceBusTopicBuilder() =
    member _.Yield _ =
        { Name = ResourceName.Empty
          DuplicateDetection = None
          DefaultMessageTimeToLive = None
          EnablePartitioning = None
          Subscriptions = Map.empty }

    /// The name of the queue.
    [<CustomOperation "name">] member _.Name(state:ServiceBusTopicConfig, name) = { state with Name = ResourceName name }
    /// Whether to enable duplicate detection, and if so, how long to check for.ServiceBusQueueConfig
    [<CustomOperation "duplicate_detection_minutes">] member _.DuplicateDetection(state:ServiceBusTopicConfig, maxTimeWindow) = { state with DuplicateDetection = Some (TimeSpan.FromMinutes (float maxTimeWindow)) }
    /// The default time-to-live for messages. If not specified, the maximum TTL will be set for the SKU.
    [<CustomOperation "message_ttl_days">] member _.MessageTtl(state:ServiceBusTopicConfig, ttl) = { state with DefaultMessageTimeToLive = Some (TimeSpan.FromDays (float ttl)) }
    /// Enables partition support on the queue.
    [<CustomOperation "enable_partition">] member _.EnablePartition(state:ServiceBusTopicConfig) = { state with EnablePartitioning = Some true }
    [<CustomOperation "add_subscriptions">]
    member _.AddSubscriptions(state:ServiceBusTopicConfig, subscriptions) =
        { state with
            Subscriptions =
                (state.Subscriptions, subscriptions)
                ||> List.fold(fun state (subscription:ServiceBusSubscriptionConfig) -> state.Add(subscription.Name, subscription))
        }

type ServiceBusConfig =
    { Name : ResourceName
      Sku : Sku
      DependsOn : ResourceName list
      Queues : Map<ResourceName, ServiceBusQueueConfig>
      Topics : Map<ResourceName, ServiceBusTopicConfig> }
    member private _.GetKeyPath sbNsName property =
        sprintf
            "listkeys(resourceId('Microsoft.ServiceBus/namespaces/authorizationRules', '%s', 'RootManageSharedAccessKey'), '2017-04-01').%s"
            sbNsName
            property
        |> ArmExpression
    member this.NamespaceDefaultConnectionString = this.GetKeyPath this.Name.Value "primaryConnectionString"
    member this.DefaultSharedAccessPolicyPrimaryKey = this.GetKeyPath this.Name.Value "primaryKey"
    interface IBuilder with
        member this.DependencyName = this.Name
        member this.BuildResources location _ = [
            { Name = this.Name
              Location = location
              Sku = this.Sku
              DependsOn = this.DependsOn
              Queues = [
                  for queue in this.Queues do
                    let queue = queue.Value
                    { Name = queue.Name
                      LockDuration = queue.LockDuration |> Option.map IsoDateTime.OfTimeSpan
                      DuplicateDetectionHistoryTimeWindow = queue.DuplicateDetection |> Option.map IsoDateTime.OfTimeSpan
                      Session = queue.Session
                      DeadLetteringOnMessageExpiration = queue.DeadLetteringOnMessageExpiration
                      DefaultMessageTimeToLive =
                          match queue.DefaultMessageTimeToLive, this.Sku with
                          | None, Sku.Basic -> TimeSpan.FromDays 14.
                          | None, (Sku.Standard | Sku.Premium _) -> TimeSpan.MaxValue
                          | Some ttl, _ -> ttl
                          |> IsoDateTime.OfTimeSpan
                      MaxDeliveryCount = queue.MaxDeliveryCount
                      EnablePartitioning = queue.EnablePartitioning }
              ]
              Topics = [
                  for topic in this.Topics do
                    let topic = topic.Value
                    { Namespaces.Topic.Name = topic.Name
                      DuplicateDetectionHistoryTimeWindow = topic.DuplicateDetection |> Option.map IsoDateTime.OfTimeSpan
                      DefaultMessageTimeToLive = topic.DefaultMessageTimeToLive |> Option.map IsoDateTime.OfTimeSpan
                      EnablePartitioning = topic.EnablePartitioning
                      Subscriptions = [
                          for subscription in topic.Subscriptions do
                            let subscription = subscription.Value
                            { Name = subscription.Name
                              LockDuration = subscription.LockDuration |> Option.map IsoDateTime.OfTimeSpan
                              DuplicateDetectionHistoryTimeWindow = subscription.DuplicateDetection |> Option.map IsoDateTime.OfTimeSpan
                              DefaultMessageTimeToLive = subscription.DefaultMessageTimeToLive |> Option.map IsoDateTime.OfTimeSpan
                              MaxDeliveryCount = subscription.MaxDeliveryCount
                              Session = subscription.Session
                              DeadLetteringOnMessageExpiration = subscription.DeadLetteringOnMessageExpiration }
                      ]
                    }
              ]
            }
        ]

type ServiceBusBuilder() =
    member _.Yield _ =
        { Name = ResourceName.Empty
          Sku = Basic
          Queues = Map.empty
          Topics = Map.empty
          DependsOn = List.empty }
    member _.Run (state:ServiceBusConfig) =
        let isBetween min max v = v >= min && v <= max
        for queue in state.Queues do
            let queue = queue.Value

            match queue.DuplicateDetection, state.Sku with
            | Some _, Basic -> failwithf "Duplicate Detection cannot be set when creating a queue using the Basic tier (queue '%s' fails this check)." queue.Name.Value
            | _ -> ()
            queue.LockDuration |> Option.iter(fun lockDuration -> if lockDuration > TimeSpan(0,5,0) then failwith "Lock duration name must not be more than 5 minutes.")

        if state.Name.Value.Length |> isBetween 6 50 |> not then failwith "Namespace name must be between 6 and 50 characters long"
        state
    /// The name of the namespace that holds the queue.
    [<CustomOperation "name">]
    member _.NamespaceName(state:ServiceBusConfig, name) = { state with Name = ResourceName name }
    /// The SKU of the namespace.
    [<CustomOperation "sku">]
    member _.Sku(state:ServiceBusConfig, sku) = { state with Sku = sku }
    /// Adds a resource that the service bus depends on.
    [<CustomOperation "depends_on">]
    member _.DependsOn(state:ServiceBusConfig, resourceName) = { state with DependsOn = resourceName :: state.DependsOn }
    member _.DependsOn(state:ServiceBusConfig, builder:IBuilder) = { state with DependsOn = builder.DependencyName :: state.DependsOn }
    member _.DependsOn(state:ServiceBusConfig, resource:IArmResource) = { state with DependsOn = resource.ResourceName :: state.DependsOn }
    [<CustomOperation "add_queues">]
    member _.AddQueues(state:ServiceBusConfig, queues) =
        { state with
            Queues =
                (state.Queues, queues)
                ||> List.fold(fun state (queue:ServiceBusQueueConfig) -> state.Add(queue.Name, queue))
        }
    [<CustomOperation "add_topics">]
    member _.AddTopics(state:ServiceBusConfig, topics) =
        { state with
            Topics =
                (state.Topics, topics)
                ||> List.fold(fun state (topic:ServiceBusTopicConfig) -> state.Add(topic.Name, topic))
        }

let serviceBus = ServiceBusBuilder()
let topic = ServiceBusTopicBuilder()
let queue = ServiceBusQueueBuilder()
let subscription = ServiceBusSubscriptionBuilder()