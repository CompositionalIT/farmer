[<AutoOpen>]
module Farmer.Builders.EventHub

open Farmer
open Farmer.CoreTypes
open Farmer.EventHub
open Farmer.Arm.EventHub
open Namespaces
open EventHubs

/// Shortcut for Manage, Send and Listen rights.
let AllAuthorizationRights = [ Manage; Send; Listen ]

type EventHubConfig =
    { EventHubNamespace : ResourceRef<EventHubConfig, EventHubNsName>
      Name : ResourceName
      Sku : EventHubSku
      Capacity : int
      ZoneRedundant : bool option
      ThroughputSettings : InflateSetting option
      KafkaEnabled : bool option
      MessageRetentionInDays : int option
      Partitions : int
      ConsumerGroups : string Set
      CaptureDestination : CaptureDestination option
      AuthorizationRules : Map<ResourceName, AuthorizationRuleRight Set>
      Dependencies : ResourceName list
      Tags: Map<string,string>  }
    member private _.ToKeyExpression = sprintf "listkeys(%s, '2017-04-01').primaryConnectionString"
    member this.EventHubNamespaceName = this.EventHubNamespace.CreateResourceName this
    /// Gets an ARM expression for the path to the key of a specific authorization rule for this event hub.
    member this.GetKey (ruleName:string) =
        ArmExpression
            .resourceId(authorizationRules, this.EventHubNamespaceName.Untyped, this.Name, ResourceName ruleName)
            .Map this.ToKeyExpression

    /// Gets an ARM expression for the path to the key of the default RootManageSharedAccessKey for the entire namespace.
    member this.DefaultKey =
        ArmExpression
            .resourceId(authorizationRules, this.EventHubNamespaceName, ResourceName "RootManageSharedAccessKey")
            .Map this.ToKeyExpression
    interface IBuilder with
        member this.DependencyName = this.EventHubNamespaceName.Untyped
        member this.BuildResources location = [
            let eventHubNamespaceName = this.EventHubNamespaceName
            let eventHubName = this.Name.Map(fun hubName -> sprintf "%s/%s" eventHubNamespaceName.Value hubName)

            // Namespace
            match this.EventHubNamespace with
            | DeployableResource this _ ->
                { Name = eventHubNamespaceName
                  Location = location
                  Sku =
                    {| Name = this.Sku
                       Capacity = this.Capacity |}
                  ZoneRedundant = this.ZoneRedundant
                  AutoInflateSettings = this.ThroughputSettings
                  KafkaEnabled = this.KafkaEnabled
                  Tags = this.Tags  }
            | _ ->
                ()

            // Event hub
            { Name = eventHubName
              Location = location
              MessageRetentionDays = this.MessageRetentionInDays
              Partitions = this.Partitions
              CaptureDestination = this.CaptureDestination
              Dependencies = [
                  eventHubNamespaceName.Untyped
                  match this.CaptureDestination with
                  | Some (StorageAccount(name, _)) -> name.Untyped
                  | None -> ()
                  yield! this.Dependencies
              ]
              Tags = this.Tags  }

            // Consumer groups
            for consumerGroup in this.ConsumerGroups do
              { Name = eventHubName.Map(fun hubName -> sprintf "%s/%s" hubName consumerGroup)
                Location = location
                Dependencies = [
                    eventHubNamespaceName.Untyped
                    ArmExpression.resourceId(eventHubs, eventHubNamespaceName.Untyped, this.Name).Eval() |> ResourceName
                ] }

            // Auth rules
            for rule in this.AuthorizationRules do
                { Name = rule.Key.Map(fun rule -> sprintf "%s/%s/%s" eventHubNamespaceName.Value this.Name.Value rule)
                  Location = location
                  Dependencies = [
                      ArmExpression.resourceId(namespaces, eventHubNamespaceName).Eval() |> ResourceName
                      ArmExpression.resourceId(eventHubs, eventHubNamespaceName.Untyped, this.Name).Eval() |> ResourceName
                  ]
                  Rights = rule.Value }
        ]

type EventHubBuilder() =
    member __.Yield _ =
        { Name = ResourceName "hub"
          EventHubNamespace = derived (fun config -> config.Name.Map(sprintf "%s-ns").TryConvert ResourceName.unsafeWrap |> Result.get)
          Sku = Standard
          Capacity = 1
          ZoneRedundant = None
          ThroughputSettings = None
          KafkaEnabled = None
          MessageRetentionInDays = None
          Partitions = 1
          CaptureDestination = None
          ConsumerGroups = Set [ "$Default" ]
          AuthorizationRules = Map.empty
          Dependencies = []
          Tags = Map.empty }
    /// Sets the name of the Event Hub instance.
    [<CustomOperation "name">]
    member __.Name(state:EventHubConfig, name) = { state with Name = name }
    member this.Name(state:EventHubConfig, name) = this.Name(state, ResourceName name)
    /// Sets the name of the Event Hub namespace.
    [<CustomOperation "namespace_name">]
    member __.NamespaceName(state:EventHubConfig, name) = { state with EventHubNamespace = AutoCreate(Named name) }
    member this.NamespaceName(state:EventHubConfig, name) = this.NamespaceName(state, ResourceName name)
    /// Sets the name of the Event Hub namespace.
    [<CustomOperation "link_to_namespace">]
    member __.LinkToNamespaceName(state:EventHubConfig, name) = { state with EventHubNamespace = External (Managed name) }
    member this.LinkToNamespaceName(state:EventHubConfig, name) = this.LinkToNamespaceName(state, ResourceName name)
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
    [<CustomOperation "capture_to_storage">]
    member _.CaptureToStorage(state:EventHubConfig, storageName:ResourceName<Storage.StorageAccountName>, container) =
        { state with
            CaptureDestination = Some (StorageAccount(storageName, container)) }
    member this.CaptureToStorage(state:EventHubConfig, storageAccount:StorageAccountConfig, container) =
        this.CaptureToStorage(state, storageAccount.Name, container)
    /// Sets a dependency for the event hub.
    [<CustomOperation "depends_on">]
    member __.DependsOn(state:EventHubConfig, resourceName) = { state with Dependencies = resourceName :: state.Dependencies }
    member __.DependsOn(state:EventHubConfig, resources) = { state with Dependencies = List.concat [ resources; state.Dependencies ] }
    member __.DependsOn(state:EventHubConfig, builder:IBuilder) = { state with Dependencies = builder.DependencyName :: state.Dependencies }
    member __.DependsOn(state:EventHubConfig, builders:IBuilder list) = { state with Dependencies = List.concat [ builders |> List.map (fun x -> x.DependencyName); state.Dependencies ] }
    member __.DependsOn(state:EventHubConfig, resource:IArmResource) = { state with Dependencies = resource.ResourceName :: state.Dependencies }
    member __.DependsOn(state:EventHubConfig, resources:IArmResource list) = { state with Dependencies = List.concat [ resources |> List.map (fun x -> x.ResourceName); state.Dependencies ] }

    [<CustomOperation "add_tags">]
    member _.Tags(state:EventHubConfig, pairs) =
        { state with
            Tags = pairs |> List.fold (fun map (key,value) -> Map.add key value map) state.Tags }
    [<CustomOperation "add_tag">]
    member this.Tag(state:EventHubConfig, key, value) = this.Tags(state, [ (key,value) ])

let eventHub = EventHubBuilder()