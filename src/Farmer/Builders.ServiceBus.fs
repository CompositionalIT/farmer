[<AutoOpen>]
module Farmer.Resources.ServiceBus

open Farmer
open Farmer.Models

type MessagingUnits = OneUnit | TwoUnits | FourUnits
type ServiceBusNamespaceSku =
    | Basic
    | Standard
    | Premium of MessagingUnits

type ServiceBusQueueConfig =
    { NamespaceName : ResourceRef
      NamespaceSku : ServiceBusNamespaceSku
      Name : ResourceName
      LockDurationMinutes : int option
      DuplicateDetection : int option
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
type ServiceBusQueueBuilder() =
    member _.Yield _ =
        { NamespaceName = ResourceRef.AutomaticPlaceholder
          NamespaceSku = Basic
          Name = ResourceName.Empty
          LockDurationMinutes = None
          DuplicateDetection = None
          Session = None
          DeadLetteringOnMessageExpiration = None
          MaxDeliveryCount = None
          EnablePartitioning = None
          DependsOn = List.empty }
    member _.Run (state:ServiceBusQueueConfig) =
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
    [<CustomOperation "lock_duration_minutes">] member _.LockDurationMinutes(state:ServiceBusQueueConfig, duration) = { state with LockDurationMinutes = Some duration }
    /// The maximum number of times a message can be delivered before dead lettering.
    [<CustomOperation "max_delivery_count">] member _.MaxDeliveryCount(state:ServiceBusQueueConfig, count) = { state with MaxDeliveryCount = Some count }
    /// Whether to enable duplicate detection, and if so, how long to check for.
    [<CustomOperation "duplicate_detection_minutes">] member _.DuplicateDetection(state:ServiceBusQueueConfig, maxTimeWindow) = { state with DuplicateDetection = Some maxTimeWindow }
    /// Enables session support.
    [<CustomOperation "enable_session">] member _.Session(state:ServiceBusQueueConfig) = { state with Session = Some true }
    /// Enables dead lettering of messages that expire.
    [<CustomOperation "enable_dead_letter_on_message_expiration">] member _.DeadLetteringOnMessageExpiration(state:ServiceBusQueueConfig) = { state with DeadLetteringOnMessageExpiration = Some true }
    /// Enables partition support on the queue.
    [<CustomOperation "enable_partition">] member _.EnablePartition(state:ServiceBusQueueConfig) = { state with EnablePartitioning = Some true }
    /// Adds a resource that the service bus depends on.
    [<CustomOperation "depends_on">] member _.DependsOn(state:ServiceBusQueueConfig, resourceName) = { state with DependsOn = resourceName :: state.DependsOn }

module Converters =
    let serviceBusNamespace location (existingNamespaces:ServiceBusNamespace list) config =
        let queue =
              {| Name = config.Name
                 LockDuration = config.LockDurationMinutes |> Option.map (sprintf "PT%dM")
                 DuplicateDetection = config.DuplicateDetection |> Option.map(fun _ -> true)
                 DuplicateDetectionHistoryTimeWindow = config.DuplicateDetection |> Option.map (sprintf "PT%dM")
                 Session = config.Session
                 DeadLetteringOnMessageExpiration = config.DeadLetteringOnMessageExpiration
                 MaxDeliveryCount = config.MaxDeliveryCount
                 EnablePartitioning = config.EnablePartitioning
                 DependsOn = [ config.NamespaceName.ResourceName ] |}

        match config.NamespaceName with
        | AutomaticallyCreated namespaceName ->
            { Name = namespaceName
              Location = location
              Sku =
                match config.NamespaceSku with
                | Basic -> "Basic"
                | Standard -> "Standard"
                | Premium _ -> "Premium"
              Capacity =
                match config.NamespaceSku with
                | Basic -> None
                | Standard -> None
                | Premium OneUnit -> Some 1
                | Premium TwoUnits -> Some 2
                | Premium FourUnits -> Some 4
              DependsOn =
                config.DependsOn
              Queues = [ queue ]
            } |> NewResource
        | External resourceName ->
            existingNamespaces
            |> List.tryFind(fun g -> g.Name = resourceName)
            |> Option.map(fun ns -> MergedResource(ns, { ns with Queues = queue :: ns.Queues }))
            |> Option.defaultValue (CouldNotLocate resourceName)
        | AutomaticPlaceholder ->
            NotSet

    module Outputters =
        let serviceBusNamespace (ns:ServiceBusNamespace) =
            {| ``type`` = "Microsoft.ServiceBus/namespaces"
               apiVersion = "2017-04-01"
               name = ns.Name.Value
               location = ns.Location.ArmValue
               sku =
                 {| name = ns.Sku
                    tier = ns.Sku
                    capacity = ns.Capacity |> Option.toNullable |}
               dependsOn = ns.DependsOn |> List.map (fun r -> r.Value)
               resources =
                 [ for queue in ns.Queues do
                     {| apiVersion = "2017-04-01"
                        name = queue.Name.Value
                        ``type`` = "Queues"
                        dependsOn = queue.DependsOn |> List.map (fun r -> r.Value)
                        properties =
                         {| lockDuration = queue.LockDuration |> Option.toObj
                            requiresDuplicateDetection = queue.DuplicateDetection |> Option.toNullable
                            duplicateDetectionHistoryTimeWindow = queue.DuplicateDetectionHistoryTimeWindow |> Option.toObj
                            requiresSession = queue.Session |> Option.toNullable
                            deadLetteringOnMessageExpiration = queue.DeadLetteringOnMessageExpiration |> Option.toNullable
                            maxDeliveryCount = queue.MaxDeliveryCount |> Option.toNullable
                            enablePartitioning = queue.EnablePartitioning |> Option.toNullable |}
                     |}
                 ]
            |}

let serviceBus = ServiceBusQueueBuilder()

type Farmer.ArmBuilder.ArmBuilder with
    member _.AddResource(state:ArmConfig, config) =
        state.AddOrMergeResource (Converters.serviceBusNamespace state.Location) config (function ServiceBusNamespace config -> Some config | _ -> None) ServiceBusNamespace
    member this.AddResources (state, configs) = addResources<ServiceBusQueueConfig> this.AddResource state configs