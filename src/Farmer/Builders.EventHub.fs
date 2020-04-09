[<AutoOpen>]
module Farmer.Resources.EventHub

open Farmer
open Farmer.Models

/// The SKU of the event hub instance.
type EventHubSku =
    | Basic
    | Standard
    | Premium
type AuthorizationRuleRight = Manage | Send | Listen
/// Shortcut for Manage, Send and Listen rights.
let AllAuthorizationRights = [ Manage; Send; Listen ]
type ThroughputSettings = ManualInflate | AutoInflate of maxThroughput:int
type EventHubConfig =
    { EventHubNamespace : ResourceRef
      Name : ResourceName
      Sku : EventHubSku
      Capacity : int
      ZoneRedundant : bool option
      ThroughputSettings : ThroughputSettings option
      KafkaEnabled : bool option
      MessageRetentionInDays : int option
      Partitions : int
      ConsumerGroups : string Set
      AuthorizationRules : Map<ResourceName, AuthorizationRuleRight Set> }
    member private this.ToKeyExpression = sprintf "listkeys(%s, '2017-04-01').primaryConnectionString" >> ArmExpression

    /// Gets an ARM expression for the path to the key of a specific authorization rule for this event hub.
    member this.GetKey (ruleName:string) =
        sprintf "resourceId('Microsoft.EventHub/namespaces/eventhubs/authorizationRules', '%s', '%s', '%s')"
            this.EventHubNamespace.ResourceName.Value
            this.Name.Value
            ruleName
        |> this.ToKeyExpression

    /// Gets an ARM expression for the path to the key of the default RootManageSharedAccessKey for the entire namespace.
    member this.DefaultKey =
        sprintf "resourceId('Microsoft.EventHub/namespaces/authorizationRules', '%s', 'RootManageSharedAccessKey')"
            this.EventHubNamespace.ResourceName.Value
        |> this.ToKeyExpression


type EventHubBuilder() =
    member __.Yield _ =
        { Name = ResourceName "hub"
          EventHubNamespace = AutomaticPlaceholder
          Sku = Standard
          Capacity = 1
          ZoneRedundant = None
          ThroughputSettings = None
          KafkaEnabled = None
          MessageRetentionInDays = None
          Partitions = 1
          ConsumerGroups = Set [ "$Default" ]
          AuthorizationRules = Map.empty }
    member __.Run state =
        { state with
            EventHubNamespace =
                match state.EventHubNamespace with
                | External name -> External name
                | AutomaticPlaceholder -> AutomaticallyCreated (state.Name.Map(sprintf "%s-ns"))
                | AutomaticallyCreated resourceName -> AutomaticallyCreated resourceName }
    /// Sets the name of the Event Hub instance.
    [<CustomOperation "name">]
    member __.Name(state:EventHubConfig, name) = { state with Name = name }
    member this.Name(state:EventHubConfig, name) = this.Name(state, ResourceName name)
    /// Sets the name of the Event Hub namespace.
    [<CustomOperation "namespace_name">]
    member __.NamespaceName(state:EventHubConfig, name) = { state with EventHubNamespace = AutomaticallyCreated name }
    member this.NamespaceName(state:EventHubConfig, name) = this.NamespaceName(state, ResourceName name)
    /// Sets the name of the Event Hub namespace.
    [<CustomOperation "link_to_namespace">]
    member __.LinkToNamespaceName(state:EventHubConfig, name) = { state with EventHubNamespace = External name }
    member this.LinkToNamespaceName(state:EventHubConfig, name) = this.NamespaceName(state, ResourceName name)
    /// Sets the sku of the Event Hub instance.
    [<CustomOperation "sku">]
    member __.Sku(state:EventHubConfig, sku) = { state with Sku = sku }
    [<CustomOperation "capacity">]
    member __.ReplicaCount(state:EventHubConfig, capacity:int) = { state with Capacity = capacity }
    [<CustomOperation "enable_zone_redundant">]
    member __.ZoneRedundant(state:EventHubConfig) = { state with ZoneRedundant = Some true }
    [<CustomOperation "enable_auto_inflate">]
    member __.AutoInflate(state:EventHubConfig, maxThroughput) = { state with ThroughputSettings = Some (AutoInflate maxThroughput) }
    [<CustomOperation "disable_auto_inflate">]
    member __.MaximumThroughputUnits(state:EventHubConfig) = { state with ThroughputSettings = Some ManualInflate }
    [<CustomOperation "disable_kafka">]
    member __.Kafka(state:EventHubConfig) = { state with KafkaEnabled = Some false }
    [<CustomOperation "message_retention_days">]
    member __.MessageRetentionDays(state:EventHubConfig, days) = { state with MessageRetentionInDays = Some days }
    [<CustomOperation "partitions">]
    member __.Partitions(state:EventHubConfig, partitions) = { state with Partitions = partitions }
    [<CustomOperation "add_consumer_group">]
    member __.AddConsumerGroup(state:EventHubConfig, name) = { state with ConsumerGroups = state.ConsumerGroups.Add name }
    [<CustomOperation "add_authorization_rule">]
    member __.AddAuthorizationRule(state:EventHubConfig, name, rights) = { state with AuthorizationRules = state.AuthorizationRules.Add(ResourceName name, Set rights) }

module Converters =
    open Farmer.Models
    let eventHub location (eventHubConfig:EventHubConfig) =
        let eventHubNamespaceName = eventHubConfig.EventHubNamespace.ResourceName
        let eventHubNamespace =
            match eventHubConfig.EventHubNamespace with
            | External _ ->
                None
            | AutomaticPlaceholder | AutomaticallyCreated _ ->
                { Name = eventHubNamespaceName
                  Location = location
                  Sku =
                    {| Name = string eventHubConfig.Sku
                       Tier = string eventHubConfig.Sku
                       Capacity = eventHubConfig.Capacity |}
                  ZoneRedundant = eventHubConfig.ZoneRedundant
                  IsAutoInflateEnabled =
                        eventHubConfig.ThroughputSettings
                        |> Option.map (function
                            | AutoInflate _ -> true
                            | ManualInflate -> false)
                  MaxThroughputUnits =
                        eventHubConfig.ThroughputSettings
                        |> Option.bind (function
                            | AutoInflate throughput -> Some throughput
                            | ManualInflate -> None)
                  KafkaEnabled = eventHubConfig.KafkaEnabled }
                |> Some
        let eventHub =
            { Name = eventHubConfig.Name.Map(sprintf "%s/%s" eventHubNamespaceName.Value)
              Location = location
              MessageRetentionDays = eventHubConfig.MessageRetentionInDays
              Partitions = eventHubConfig.Partitions
              Dependencies = [ eventHubNamespaceName ] }
        let consumerGroups =
            [ for consumerGroup in eventHubConfig.ConsumerGroups ->
                { Name = eventHub.Name.Map(fun name -> sprintf "%s/%s" eventHub.Name.Value consumerGroup)
                  Location = location
                  Dependencies = [
                      eventHubNamespaceName
                      eventHubConfig.Name.Map(sprintf "[resourceId('Microsoft.EventHub/namespaces/eventhubs', '%s', '%s')]" eventHubNamespaceName.Value)
                  ]
                }
            ]
        let authRules =
            [ for rule in eventHubConfig.AuthorizationRules ->
                { Name = rule.Key.Map(sprintf "%s/%s/%s" eventHubNamespaceName.Value eventHubConfig.Name.Value)
                  Location = location
                  Dependencies = [
                      eventHubNamespaceName.Map(sprintf "[resourceId('Microsoft.EventHub/namespaces', '%s')]")
                      eventHubConfig.Name.Map(sprintf "[resourceId('Microsoft.EventHub/namespaces/eventhubs', '%s', '%s')]" eventHubNamespaceName.Value)
                  ]
                  Rights = rule.Value |> Set.map string |> Set.toList }
            ]
        {| EventHubNamespace = eventHubNamespace
           EventHub = eventHub
           ConsumerGroups = consumerGroups
           Rights = authRules |}

    module Outputters =
        let eventHubNs (ns:EventHubNamespace) = {|
            ``type`` = "Microsoft.EventHub/namespaces"
            apiVersion = "2017-04-01"
            name = ns.Name.Value
            location = ns.Location.Value
            sku =
                {| name = ns.Sku.Name
                   tier = ns.Sku.Tier
                   capacity = ns.Sku.Capacity |}
            properties =
                {| zoneRedundant = ns.ZoneRedundant |> Option.toNullable
                   isAutoInflateEnabled = ns.IsAutoInflateEnabled |> Option.toNullable
                   maximumThroughputUnits = ns.MaxThroughputUnits |> Option.toNullable
                   kafkaEnabled = ns.KafkaEnabled |> Option.toNullable |}
        |}

        let eventHub (hub:EventHub) = {|
            ``type`` = "Microsoft.EventHub/namespaces/eventhubs"
            apiVersion = "2017-04-01"
            name = hub.Name.Value
            location = hub.Location.Value
            dependsOn = hub.Dependencies |> List.map(fun d -> d.Value)
            properties =
                {| messageRetentionInDays = hub.MessageRetentionDays |> Option.toNullable
                   partitionCount = hub.Partitions
                   status = "Active" |}
        |}

        let consumerGroup (group:EventHubConsumerGroup) = {|
            ``type`` = "Microsoft.EventHub/namespaces/eventhubs/consumergroups"
            apiVersion = "2017-04-01"
            name = group.Name.Value
            location = group.Location.Value
            dependsOn = group.Dependencies |> List.map(fun d -> d.Value)
        |}

        let authRule (rule:EventHubAuthorizationRule) = {|
            ``type`` = "Microsoft.EventHub/namespaces/eventhubs/AuthorizationRules"
            apiVersion = "2017-04-01"
            name = rule.Name.Value
            location = rule.Location.Value
            dependsOn = rule.Dependencies |> List.map(fun d -> d.Value)
            properties = {| rights = rule.Rights |}
        |}

open Farmer.Models
type ArmBuilder.ArmBuilder with
    member this.AddResource(state:ArmConfig, config:EventHubConfig) =
        let output = Converters.eventHub state.Location config
        let resources = [
            EventHub output.EventHub
            yield! output.ConsumerGroups |> List.map ConsumerGroup
            match output.EventHubNamespace with
            | Some ns ->
              EventHubNamespace ns
              // We only emit auth resources if we're creating a namespace
              yield! output.Rights |> List.map EventHubAuthRule
            | None ->
              ()
        ]
        { state with Resources = resources @ state.Resources }
    member this.AddResources (state, configs) = addResources<EventHubConfig> this.AddResource state configs

let eventHub = EventHubBuilder()
