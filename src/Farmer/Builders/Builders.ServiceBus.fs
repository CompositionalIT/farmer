[<AutoOpen>]
module Farmer.Builders.ServiceBus

open Farmer
open Farmer.ServiceBus
open Farmer.Arm.ServiceBus
open Namespaces
open Topics
open System

module CommonMembers =
    type IForwardTo<'TState> =
        abstract member SetForwardTo: 'TState -> ResourceName option -> 'TState

open CommonMembers

type ServiceBusQueueConfig =
    { Name: ResourceName
      Namespace: LinkedResource
      LockDuration: TimeSpan option
      DuplicateDetection: TimeSpan option
      DefaultMessageTimeToLive: TimeSpan option
      ForwardTo: ResourceName option
      Session: bool option
      DeadLetteringOnMessageExpiration: bool option
      MaxDeliveryCount: int option
      MaxSizeInMegabytes: int<Mb> option
      EnablePartitioning: bool option
      AuthorizationRules: Map<ResourceName, AuthorizationRuleRight Set> }
    interface IBuilder with
        member this.ResourceId = queues.resourceId (this.Namespace.Name / this.Name)

        member this.BuildResources location =
            [ { Name = this.Name
                Namespace = this.Namespace
                LockDuration =
                  this.LockDuration
                  |> Option.map IsoDateTime.OfTimeSpan
                DuplicateDetectionHistoryTimeWindow =
                  this.DuplicateDetection
                  |> Option.map IsoDateTime.OfTimeSpan
                Session = this.Session
                DeadLetteringOnMessageExpiration = this.DeadLetteringOnMessageExpiration
                DefaultMessageTimeToLive =
                  this.DefaultMessageTimeToLive
                  |> Option.map IsoDateTime.OfTimeSpan
                ForwardTo = this.ForwardTo
                MaxDeliveryCount = this.MaxDeliveryCount
                MaxSizeInMegabytes = this.MaxSizeInMegabytes
                EnablePartitioning = this.EnablePartitioning }
              for rule in this.AuthorizationRules do
                  { QueueAuthorizationRule.Name =
                      rule.Key.Map(fun name -> $"{this.Namespace.Name.Value}/{this.Name.Value}/%s{name}")
                    Location = location
                    Dependencies =
                      [ namespaces.resourceId this.Name
                        queues.resourceId (this.Name, this.Name) ]
                    Rights = rule.Value } ]

type ServiceBusQueueBuilder() =
    member _.Yield _ =
        { Name = ResourceName.Empty
          Namespace = Managed(namespaces.resourceId ResourceName.Empty)
          LockDuration = None
          DuplicateDetection = None
          Session = None
          DeadLetteringOnMessageExpiration = None
          DefaultMessageTimeToLive = None
          ForwardTo = None
          MaxDeliveryCount = None
          MaxSizeInMegabytes = None
          EnablePartitioning = None
          AuthorizationRules = Map.empty }

    /// The name of the queue.
    [<CustomOperation "name">]
    member _.Name(state: ServiceBusQueueConfig, name) = { state with Name = ResourceName name }

    /// The length of time that a lock can be held on a message.
    [<CustomOperation "lock_duration_minutes">]
    member _.LockDurationMinutes(state: ServiceBusQueueConfig, duration) =
        { state with LockDuration = Some(TimeSpan.FromMinutes(float duration)) }

    /// Whether to enable duplicate detection, and if so, how long to check for.ServiceBusQueueConfig
    [<CustomOperation "duplicate_detection">]
    member _.DuplicateDetectionTs(state: ServiceBusQueueConfig, maxTimeWindow) =
        { state with DuplicateDetection = maxTimeWindow }

    member _.DuplicateDetectionTs(state: ServiceBusQueueConfig, maxTimeWindow) =
        { state with DuplicateDetection = Some maxTimeWindow }

    /// Whether to enable duplicate detection, and if so, how long to check for.ServiceBusQueueConfig
    [<CustomOperation "duplicate_detection_minutes">]
    member _.DuplicateDetection(state: ServiceBusQueueConfig, maxTimeWindow) =
        { state with DuplicateDetection = Some(TimeSpan.FromMinutes(float maxTimeWindow)) }

    /// The maximum size for the queue in megabytes.
    [<CustomOperation "max_queue_size">]
    member _.MaxTopicSize(state: ServiceBusQueueConfig, maxTopicSize: int<Mb>) =
        { state with MaxSizeInMegabytes = Some maxTopicSize }

    /// The default time-to-live for messages in a timespan string (e.g. '00:05:00'). If not specified, the maximum TTL will be set for the SKU.
    [<CustomOperation "message_ttl">]
    member _.MessageTtl(state: ServiceBusQueueConfig, ttl) =
        { state with DefaultMessageTimeToLive = Some(TimeSpan.Parse ttl) }

    /// The default time-to-live for messages in days. If not specified, the maximum TTL will be set for the SKU.
    member _.MessageTtl(state: ServiceBusQueueConfig, ttl: int<Days>) =
        { state with
            DefaultMessageTimeToLive =
                ttl / 1<Days>
                |> float
                |> TimeSpan.FromDays
                |> Some }
    //// The default time-to-live for messages defined in a .NET TimeSpan.
    member _.MessageTtl(state: ServiceBusQueueConfig, timespan: TimeSpan) =
        { state with DefaultMessageTimeToLive = Some timespan }

    /// Enables session support.
    [<CustomOperation "max_delivery_count">]
    member _.MaxDeliveryCount(state: ServiceBusQueueConfig, count) =
        { state with MaxDeliveryCount = Some count }

    /// Whether to enable duplicate detection, and if so, how long to check for.ServiceBusQueueConfig
    [<CustomOperation "enable_session">]
    member _.Session(state: ServiceBusQueueConfig) = { state with Session = Some true }

    /// Enables dead lettering of messages that expire.
    [<CustomOperation "enable_dead_letter_on_message_expiration">]
    member _.DeadLetteringOnMessageExpiration(state: ServiceBusQueueConfig) =
        { state with DeadLetteringOnMessageExpiration = Some true }

    /// Enables partition support on the queue.
    [<CustomOperation "enable_partition">]
    member _.EnablePartition(state: ServiceBusQueueConfig) =
        { state with EnablePartitioning = Some true }

    /// Add authorization rule on the queue.
    [<CustomOperation "add_authorization_rule">]
    member _.AddAuthorizationRule(state: ServiceBusQueueConfig, name, rights) =
        { state with AuthorizationRules = state.AuthorizationRules.Add(ResourceName name, Set rights) }

    /// Instead of creating or modifying a namespace, configure this subscription to point to another unmanaged namespace instance.
    [<CustomOperation "link_to_unmanaged_namespace">]
    member this.LinkToUnmanagedNamespace(state: ServiceBusQueueConfig, namespaceName: ResourceName) =
        { state with Namespace = Unmanaged(namespaces.resourceId namespaceName) }

    member this.LinkToUnmanagedNamespace(state: ServiceBusQueueConfig, namespaceName) =
        { state with Namespace = Unmanaged(namespaces.resourceId (ResourceName namespaceName)) }

    interface IForwardTo<ServiceBusQueueConfig> with
        member this.SetForwardTo state resource = { state with ForwardTo = resource }

type ServiceBusSubscriptionConfig =
    { Name: ResourceName
      Topic: LinkedResource
      LockDuration: TimeSpan option
      DuplicateDetection: TimeSpan option
      DefaultMessageTimeToLive: TimeSpan option
      ForwardTo: ResourceName option
      MaxDeliveryCount: int option
      Session: bool option
      DeadLetteringOnMessageExpiration: bool option
      Rules: Rule list
      DependsOn: Set<ResourceId> }
    interface IBuilder with
        member this.ResourceId =
            subscriptions.resourceId (
                this.Topic.Name
                / this.Topic.ResourceId.Segments.[0]
                / this.Name
            )

        member this.BuildResources location =
            [ { Name = this.Name
                Topic = this.Topic
                LockDuration =
                  this.LockDuration
                  |> Option.map IsoDateTime.OfTimeSpan
                DuplicateDetectionHistoryTimeWindow =
                  this.DuplicateDetection
                  |> Option.map IsoDateTime.OfTimeSpan
                DefaultMessageTimeToLive =
                  this.DefaultMessageTimeToLive
                  |> Option.map IsoDateTime.OfTimeSpan
                ForwardTo = this.ForwardTo
                MaxDeliveryCount = this.MaxDeliveryCount
                Session = this.Session
                DeadLetteringOnMessageExpiration = this.DeadLetteringOnMessageExpiration
                Rules = this.Rules
                DependsOn = this.DependsOn } ]

type ServiceBusSubscriptionBuilder() =
    member _.Yield _ =
        { Name = ResourceName.Empty
          Topic = Managed(namespaces.resourceId ResourceName.Empty)
          LockDuration = None
          DuplicateDetection = None
          DefaultMessageTimeToLive = None
          ForwardTo = None
          MaxDeliveryCount = None
          Session = None
          DeadLetteringOnMessageExpiration = None
          Rules = List.empty
          DependsOn = Set.empty }

    /// The name of the queue.
    [<CustomOperation "name">]
    member _.Name(state: ServiceBusSubscriptionConfig, name) = { state with Name = ResourceName name }

    /// The length of time that a lock can be held on a message.
    [<CustomOperation "lock_duration_minutes">]
    member _.LockDurationMinutes(state: ServiceBusSubscriptionConfig, duration) =
        { state with LockDuration = Some(TimeSpan.FromMinutes(float duration)) }

    /// Whether to enable duplicate detection, and if so, how long to check for.ServiceBusQueueConfig
    [<CustomOperation "duplicate_detection_minutes">]
    member _.DuplicateDetection(state: ServiceBusSubscriptionConfig, maxTimeWindow) =
        { state with DuplicateDetection = Some(TimeSpan.FromMinutes(float maxTimeWindow)) }

    /// The default time-to-live for messages in a timespan string (e.g. '00:05:00'). If not specified, the maximum TTL will be set for the SKU.
    [<CustomOperation "message_ttl">]
    member _.MessageTtl(state: ServiceBusSubscriptionConfig, ttl) =
        { state with DefaultMessageTimeToLive = Some(TimeSpan.Parse ttl) }

    /// The default time-to-live for messages in days. If not specified, the maximum TTL will be set for the SKU.
    member _.MessageTtl(state: ServiceBusSubscriptionConfig, ttl: int<Days>) =
        { state with
            DefaultMessageTimeToLive =
                ttl / 1<Days>
                |> float
                |> TimeSpan.FromDays
                |> Some }
    //// The default time-to-live for messages defined in a .NET TimeSpan.
    member _.MessageTtl(state: ServiceBusSubscriptionConfig, timespan: TimeSpan) =
        { state with DefaultMessageTimeToLive = Some timespan }

    /// Enables session support.
    [<CustomOperation "max_delivery_count">]
    member _.MaxDeliveryCount(state: ServiceBusSubscriptionConfig, count) =
        { state with MaxDeliveryCount = Some count }

    /// Whether to enable duplicate detection, and if so, how long to check for.ServiceBusQueueConfig
    [<CustomOperation "enable_session">]
    member _.Session(state: ServiceBusSubscriptionConfig) = { state with Session = Some true }

    /// Enables dead lettering of messages that expire.
    [<CustomOperation "enable_dead_letter_on_message_expiration">]
    member _.DeadLetteringOnMessageExpiration(state: ServiceBusSubscriptionConfig) =
        { state with DeadLetteringOnMessageExpiration = Some true }

    /// Automatically forward to a queue or topic
    /// Adds filtering rules for a subscription
    [<CustomOperation "add_filters">]
    member _.AddFilters(state: ServiceBusSubscriptionConfig, filters) =
        { state with Rules = state.Rules @ filters }

    /// Adds a sql filtering rule for a subscription
    [<CustomOperation "add_sql_filter">]
    member this.AddFilter(state: ServiceBusSubscriptionConfig, name, expression) =
        this.AddFilters(state, [ Rule.CreateSqlFilter(name, expression) ])

    /// Adds a correlation filtering rule for a subscription
    [<CustomOperation "add_correlation_filter">]
    member this.AddCorrelationFilter(state: ServiceBusSubscriptionConfig, name, properties) =
        this.AddFilters(state, [ Rule.CreateCorrelationFilter(name, properties) ])

    /// Instead of creating or modifying a namespace, configure this subscription to point to another unmanaged namespace instance.
    [<CustomOperation "link_to_unmanaged_topic">]
    member this.LinkToUnmanagedTopic(state: ServiceBusSubscriptionConfig, topicName: ResourceName) =
        { state with Topic = Unmanaged(topics.resourceId topicName) }

    member this.LinkToUnmanagedTopic(state: ServiceBusSubscriptionConfig, topicName) =
        { state with Topic = Unmanaged(topics.resourceId (ResourceName topicName)) }

    interface IForwardTo<ServiceBusSubscriptionConfig> with
        member this.SetForwardTo state resource = { state with ForwardTo = resource }

    interface IDependable<ServiceBusSubscriptionConfig> with
        member _.Add state resIds =
            { state with DependsOn = state.DependsOn + resIds }

type ServiceBusTopicConfig =
    { Name: ResourceName
      Namespace: LinkedResource
      DuplicateDetection: TimeSpan option
      DefaultMessageTimeToLive: TimeSpan option
      EnablePartitioning: bool option
      MaxSizeInMegabytes: int<Mb> option
      Subscriptions: Map<ResourceName, ServiceBusSubscriptionConfig> }
    member this.ResourceId = topics.resourceId (this.Namespace.Name, this.Name)

    interface IBuilder with
        member this.ResourceId = this.ResourceId

        member this.BuildResources location =
            [ { Name = this.Name
                Dependencies =
                  [ match this.Namespace with
                    | Managed resId -> resId // Only generate dependency if this is managed by Farmer (same template)
                    | _ -> () ]
                  |> Set.ofList
                Namespace =
                  match this.Namespace with
                  | Managed resId
                  | Unmanaged resId -> resId
                DuplicateDetectionHistoryTimeWindow =
                  this.DuplicateDetection
                  |> Option.map IsoDateTime.OfTimeSpan
                DefaultMessageTimeToLive =
                  this.DefaultMessageTimeToLive
                  |> Option.map IsoDateTime.OfTimeSpan
                EnablePartitioning = this.EnablePartitioning
                MaxSizeInMegabytes = this.MaxSizeInMegabytes }
              for subscription in this.Subscriptions do
                  let subscription =
                      { subscription.Value with Topic = Managed this.ResourceId } :> IBuilder

                  yield! subscription.BuildResources location ]

type ServiceBusTopicBuilder() =
    member _.Yield _ =
        { Name = ResourceName.Empty
          Namespace = Managed(namespaces.resourceId ResourceName.Empty)
          DuplicateDetection = None
          DefaultMessageTimeToLive = None
          EnablePartitioning = None
          MaxSizeInMegabytes = None
          Subscriptions = Map.empty }

    /// The name of the queue.
    [<CustomOperation "name">]
    member _.Name(state: ServiceBusTopicConfig, name) = { state with Name = ResourceName name }

    /// Whether to enable duplicate detection, and if so, how long to check for.ServiceBusQueueConfig
    [<CustomOperation "duplicate_detection">]
    member _.DuplicateDetectionTs(state: ServiceBusTopicConfig, maxTimeWindow) =
        { state with DuplicateDetection = maxTimeWindow }

    member _.DuplicateDetectionTs(state: ServiceBusTopicConfig, maxTimeWindow) =
        { state with DuplicateDetection = Some maxTimeWindow }

    /// Whether to enable duplicate detection, and if so, how long to check for.ServiceBusQueueConfig
    [<CustomOperation "duplicate_detection_minutes">]
    member _.DuplicateDetection(state: ServiceBusTopicConfig, maxTimeWindow) =
        { state with DuplicateDetection = Some(TimeSpan.FromMinutes(float maxTimeWindow)) }

    /// The maximum size for the topic in megabytes.
    [<CustomOperation "max_topic_size">]
    member _.MaxTopicSize(state: ServiceBusTopicConfig, maxTopicSize: int<Mb>) =
        { state with MaxSizeInMegabytes = Some maxTopicSize }

    /// The default time-to-live for messages in a timespan string (e.g. '00:05:00'). If not specified, the maximum TTL will be set for the SKU.
    [<CustomOperation "message_ttl">]
    member _.MessageTtl(state: ServiceBusTopicConfig, ttl) =
        { state with DefaultMessageTimeToLive = Some(TimeSpan.Parse ttl) }

    /// The default time-to-live for messages in days. If not specified, the maximum TTL will be set for the SKU.
    member _.MessageTtl(state: ServiceBusTopicConfig, ttl: int<Days>) =
        { state with
            DefaultMessageTimeToLive =
                ttl / 1<Days>
                |> float
                |> TimeSpan.FromDays
                |> Some }
    //// The default time-to-live for messages defined in a .NET TimeSpan.
    member _.MessageTtl(state: ServiceBusTopicConfig, timespan: TimeSpan) =
        { state with DefaultMessageTimeToLive = Some timespan }

    /// Enables partition support on the topic.
    [<CustomOperation "enable_partition">]
    member _.EnablePartition(state: ServiceBusTopicConfig) =
        { state with EnablePartitioning = Some true }

    [<CustomOperation "add_subscriptions">]
    member _.AddSubscriptions(state: ServiceBusTopicConfig, subscriptions) =
        { state with
            Subscriptions =
                (state.Subscriptions, subscriptions)
                ||> List.fold (fun state (subscription: ServiceBusSubscriptionConfig) ->
                    state.Add(subscription.Name, subscription)) }

    /// Instead of creating or modifying a namespace, configure this topic to point to another unmanaged namespace instance.
    [<CustomOperation "link_to_unmanaged_namespace">]
    member this.LinkToUnmanagedNamespace(state: ServiceBusTopicConfig, namespaceName: ResourceName) =
        { state with Namespace = Unmanaged(namespaces.resourceId namespaceName) }

    member this.LinkToUnmanagedNamespace(state: ServiceBusTopicConfig, namespaceName) =
        { state with Namespace = Unmanaged(namespaces.resourceId (ResourceName namespaceName)) }

type ServiceBusConfig =
    { Name: ResourceName
      Sku: Sku
      Dependencies: ResourceId Set
      Queues: Map<ResourceName, ServiceBusQueueConfig>
      Topics: Map<ResourceName, ServiceBusTopicConfig>
      AuthorizationRules: Map<ResourceName, AuthorizationRuleRight Set>
      Tags: Map<string, string> }
    member private this.GetKeyPath property =
        let expr =
            $"listkeys(resourceId('Microsoft.ServiceBus/namespaces/authorizationRules', '{this.Name.Value}', 'RootManageSharedAccessKey'), '2017-04-01').{property}"

        ArmExpression.create (expr, namespaces.resourceId this.Name)

    member this.NamespaceDefaultConnectionString =
        this.GetKeyPath "primaryConnectionString"

    member this.DefaultSharedAccessPolicyPrimaryKey = this.GetKeyPath "primaryKey"

    interface IBuilder with
        member this.ResourceId = namespaces.resourceId this.Name

        member this.BuildResources location =
            [ { Name = this.Name
                Location = location
                Sku = this.Sku
                Dependencies = this.Dependencies
                Tags = this.Tags }

              for queue in this.Queues do
                  let queue =
                      { queue.Value with Namespace = Managed(namespaces.resourceId this.Name) } :> IBuilder

                  yield! queue.BuildResources location

              for topic in this.Topics do
                  let topic =
                      { topic.Value with Namespace = Managed(namespaces.resourceId this.Name) } :> IBuilder

                  yield! topic.BuildResources location

              for rule in this.AuthorizationRules do
                  { Name = rule.Key.Map(fun rule -> $"{this.Name.Value}/%s{rule}")
                    Location = location
                    Dependencies = [ namespaces.resourceId this.Name ]
                    Rights = rule.Value } ]

type ServiceBusBuilder() =
    interface IDependable<ServiceBusConfig> with
        member _.Add state newDeps =
            { state with Dependencies = state.Dependencies + newDeps }

    member _.Yield _ =
        { Name = ResourceName.Empty
          Sku = Basic
          Queues = Map.empty
          Topics = Map.empty
          Dependencies = Set.empty
          AuthorizationRules = Map.empty
          Tags = Map.empty }

    member _.Run(state: ServiceBusConfig) =
        let isBetween min max v = v >= min && v <= max

        for queue in state.Queues do
            let queue = queue.Value

            match queue.DuplicateDetection, state.Sku with
            | Some _, Basic ->
                raiseFarmer
                    $"Duplicate Detection cannot be set when creating a queue using the Basic tier (queue '{queue.Name.Value}' fails this check)."
            | _ -> ()

            queue.LockDuration
            |> Option.iter (fun lockDuration ->
                if lockDuration > TimeSpan(0, 5, 0) then
                    raiseFarmer "Lock duration name must not be more than 5 minutes.")

        state

    /// The name of the namespace that holds the queue.
    [<CustomOperation "name">]
    member _.NamespaceName(state: ServiceBusConfig, name) =
        { state with
            Name =
                ServiceBusValidation
                    .ServiceBusName
                    .Create(
                        name
                    )
                    .OkValue
                    .ResourceName }

    /// The SKU of the namespace.
    [<CustomOperation "sku">]
    member _.Sku(state: ServiceBusConfig, sku) = { state with Sku = sku }

    [<CustomOperation "add_queues">]
    member _.AddQueues(state: ServiceBusConfig, queues) =
        { state with
            Queues =
                (state.Queues, queues)
                ||> List.fold (fun queueState (queue: ServiceBusQueueConfig) -> queueState.Add(queue.Name, queue)) }

    [<CustomOperation "add_topics">]
    member _.AddTopics(state: ServiceBusConfig, topics) =
        { state with
            Topics =
                (state.Topics, topics)
                ||> List.fold (fun topics (topic: ServiceBusTopicConfig) ->
                    topics.Add(topic.Name, { topic with Namespace = Managed(namespaces.resourceId state.Name) })) }

    /// Add authorization rule on the namespace.
    [<CustomOperation "add_authorization_rule">]
    member _.AddAuthorizationRule(state: ServiceBusConfig, name, rights) =
        { state with AuthorizationRules = state.AuthorizationRules.Add(ResourceName name, Set rights) }

    interface ITaggable<ServiceBusConfig> with
        member _.Add state tags =
            { state with Tags = state.Tags |> Map.merge tags }

let serviceBus = ServiceBusBuilder()
let topic = ServiceBusTopicBuilder()
let queue = ServiceBusQueueBuilder()
let subscription = ServiceBusSubscriptionBuilder()

type IForwardTo<'TState> with
    [<CustomOperation "forward_to">]
    member this.ForwardTo(state: 'TState, target) = this.SetForwardTo state target

    member this.ForwardTo(state: 'TState, target) = this.SetForwardTo state (Some target)

    member this.ForwardTo(state: 'TState, target) =
        this.SetForwardTo state (Some(ResourceName target))

    member this.ForwardTo(state: 'TState, target: ServiceBusQueueConfig) =
        this.SetForwardTo state (Some target.Name)

    member this.ForwardTo(state: 'TState, target: ServiceBusTopicConfig) =
        this.SetForwardTo state (Some target.Name)
