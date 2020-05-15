[<AutoOpen>]
module Farmer.Builders.ServiceBus

open Farmer
open Farmer.CoreTypes
open Farmer.ServiceBus
open Farmer.Arm.ServiceBus
open System

type ServiceBusQueueConfig =
    { NamespaceName : ResourceRef
      NamespaceSku : Sku
      Name : ResourceName
      LockDuration : TimeSpan option
      DuplicateDetection : TimeSpan option
      DefaultMessageTimeToLive : TimeSpan option
      Session : bool option
      DeadLetteringOnMessageExpiration : bool option
      MaxDeliveryCount : int option
      EnablePartitioning : bool option
      DependsOn : ResourceName list }
    member private _.GetKeyPath sbNsName property =
        sprintf
            "listkeys(resourceId('Microsoft.ServiceBus/namespaces/authorizationRules', '%s', 'RootManageSharedAccessKey'), '2017-04-01').%s"
            sbNsName
            property
        |> ArmExpression
    member this.NamespaceDefaultConnectionString = this.GetKeyPath this.NamespaceName.ResourceName.Value "primaryConnectionString"
    member this.DefaultSharedAccessPolicyPrimaryKey = this.GetKeyPath this.NamespaceName.ResourceName.Value "primaryKey"
    interface IBuilder with
        member this.BuildResources location existingResources = [
            let queue =
                  { Name = this.Name
                    LockDuration = this.LockDuration |> Option.map IsoDateTime.OfTimeSpan
                    DuplicateDetectionHistoryTimeWindow = this.DuplicateDetection |> Option.map IsoDateTime.OfTimeSpan
                    Session = this.Session
                    DeadLetteringOnMessageExpiration = this.DeadLetteringOnMessageExpiration
                    DefaultMessageTimeToLive =
                        match this.DefaultMessageTimeToLive, this.NamespaceSku with
                        | None, Sku.Basic -> TimeSpan.FromDays 14.
                        | None, (Sku.Standard | Sku.Premium _) -> TimeSpan.MaxValue
                        | Some ttl, _ -> ttl
                        |> IsoDateTime.OfTimeSpan
                    MaxDeliveryCount = this.MaxDeliveryCount
                    EnablePartitioning = this.EnablePartitioning
                    DependsOn = [ this.NamespaceName.ResourceName ] }

            match this.NamespaceName with
            | AutomaticallyCreated namespaceName ->
                { Name = namespaceName
                  Location = location
                  Sku = this.NamespaceSku
                  DependsOn = this.DependsOn
                  Queues = [ queue ] }
            | External namespaceName ->
                existingResources
                |> Helpers.mergeResource namespaceName (fun ns -> { ns with Queues = queue :: ns.Queues })
            | AutomaticPlaceholder ->
                failwith "Service Bus Namespace Name has not been set."
        ]

type ServiceBusQueueBuilder() =
    member _.Yield _ =
        { NamespaceName = AutomaticPlaceholder
          NamespaceSku = Basic
          Name = ResourceName.Empty
          LockDuration = None
          DuplicateDetection = None
          Session = None
          DeadLetteringOnMessageExpiration = None
          DefaultMessageTimeToLive = None
          MaxDeliveryCount = None
          EnablePartitioning = None
          DependsOn = List.empty }
    member _.Run (state:ServiceBusQueueConfig) =
        match state.DuplicateDetection, state.NamespaceSku with
        | Some _, Basic -> failwith "Duplicate Detection cannot be set when creating a queue using the Basic tier."
        | _ -> ()

        { state with
            DependsOn = List.rev state.DependsOn
            NamespaceName =
                match state.NamespaceName with
                | AutomaticPlaceholder -> state.Name.Map(sprintf "%s-ns") |> AutomaticallyCreated
                | _ -> state.NamespaceName }

    /// The name of the namespace that holds the queue.
    [<CustomOperation "namespace_name">] member _.NamespaceName(state:ServiceBusQueueConfig, name) = { state with NamespaceName = AutomaticallyCreated (ResourceName name) }
    /// Link this queue to an existing namespace instead of creating a new one.
    [<CustomOperation "link_to_namespace">]
    member _.LinkToNamespace(state:ServiceBusQueueConfig, name) = { state with NamespaceName = External name }
    member _.LinkToNamespace(state:ServiceBusQueueConfig, config) = { state with NamespaceName = External config.NamespaceName.ResourceName }
    /// The SKU of the namespace.
    [<CustomOperation "sku">] member _.Sku(state:ServiceBusQueueConfig, sku) = { state with NamespaceSku = sku }
    /// The name of the queue.
    [<CustomOperation "name">] member _.Name(state:ServiceBusQueueConfig, name) = { state with Name = ResourceName name }
    /// The length of time that a lock can be held on a message.
    [<CustomOperation "lock_duration_minutes">] member _.LockDurationMinutes(state:ServiceBusQueueConfig, duration) = { state with LockDuration = Some (TimeSpan.FromMinutes (float duration)) }
    /// The maximum number of times a message can be delivered before dead lettering.
    [<CustomOperation "max_delivery_count">] member _.MaxDeliveryCount(state:ServiceBusQueueConfig, count) = { state with MaxDeliveryCount = Some count }
    /// Whether to enable duplicate detection, and if so, how long to check for.
    [<CustomOperation "duplicate_detection_minutes">] member _.DuplicateDetection(state:ServiceBusQueueConfig, maxTimeWindow) = { state with DuplicateDetection = Some (TimeSpan.FromMinutes (float maxTimeWindow)) }
    /// The default time-to-live for messages. If not specified, the maximum TTL will be set for the SKU.
    [<CustomOperation "message_ttl_days">] member _.MessageTtl(state:ServiceBusQueueConfig, ttl) = { state with DefaultMessageTimeToLive = Some (TimeSpan.FromDays (float ttl)) }
    /// Enables session support.
    [<CustomOperation "enable_session">] member _.Session(state:ServiceBusQueueConfig) = { state with Session = Some true }
    /// Enables dead lettering of messages that expire.
    [<CustomOperation "enable_dead_letter_on_message_expiration">] member _.DeadLetteringOnMessageExpiration(state:ServiceBusQueueConfig) = { state with DeadLetteringOnMessageExpiration = Some true }
    /// Enables partition support on the queue.
    [<CustomOperation "enable_partition">] member _.EnablePartition(state:ServiceBusQueueConfig) = { state with EnablePartitioning = Some true }
    /// Adds a resource that the service bus depends on.
    [<CustomOperation "depends_on">] member _.DependsOn(state:ServiceBusQueueConfig, resourceName) = { state with DependsOn = resourceName :: state.DependsOn }

let serviceBus = ServiceBusQueueBuilder()