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
    { EventHubNamespace : ResourceRef<EventHubConfig>
      Name : ResourceName
      Sku : EventHubSku
      Capacity : int
      ZoneRedundant : bool option
      ThroughputSettings : InflateSetting option
      KafkaEnabled : bool option
      MessageRetentionInDays : int option
      Partitions : int
      ConsumerGroups : ResourceName Set
      CaptureDestination : CaptureDestination option
      AuthorizationRules : Map<ResourceName, AuthorizationRuleRight Set>
      Dependencies : ResourceId list
      Tags: Map<string,string>  }
    member private this.CreateKeyExpression (resourceId:ResourceId) =
        ArmExpression
            .create(sprintf "listkeys(%s, '2017-04-01').primaryConnectionString" resourceId.ArmExpression.Value)
            .WithOwner(ResourceId.create(eventHubs, this.Name))
    member this.EventHubNamespaceName = this.EventHubNamespace.CreateResourceId(this).Name
    /// Gets an ARM expression for the path to the key of a specific authorization rule for this event hub.
    member this.GetKey (ruleName:string) =
        let ruleResource = ResourceId.create(authorizationRules, this.EventHubNamespaceName, this.Name, ResourceName ruleName)
        this.CreateKeyExpression ruleResource

    /// Gets an ARM expression for the path to the key of the default RootManageSharedAccessKey for the entire namespace.
    member this.DefaultKey =
        let ruleResource = ResourceId.create(authorizationRules, this.EventHubNamespaceName, ResourceName "RootManageSharedAccessKey")
        this.CreateKeyExpression ruleResource
    interface IBuilder with
        member this.DependencyName = this.EventHubNamespaceName
        member this.BuildResources location = [
            let eventHubName = this.Name.Map(fun hubName -> sprintf "%s/%s" this.EventHubNamespaceName.Value hubName)

            // Namespace
            match this.EventHubNamespace with
            | DeployableResource this _ ->
                { Name = this.EventHubNamespaceName
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
                  ResourceId.create this.EventHubNamespaceName
                  match this.CaptureDestination with
                  | Some (StorageAccount(name, _)) -> ResourceId.create name
                  | None -> ()
                  yield! this.Dependencies
              ]
              Tags = this.Tags  }

            // Consumer groups
            for consumerGroup in this.ConsumerGroups do
              { ConsumerGroupName = consumerGroup
                EventHub = eventHubName
                Location = location
                Dependencies = [
                    ResourceId.create(namespaces, this.EventHubNamespaceName)
                    ResourceId.create(eventHubs, this.EventHubNamespaceName, this.Name)
                ] }

            // Auth rules
            for rule in this.AuthorizationRules do
                { Name = rule.Key.Map(fun rule -> sprintf "%s/%s/%s" this.EventHubNamespaceName.Value this.Name.Value rule)
                  Location = location
                  Dependencies = [
                      ResourceId.create(namespaces, this.EventHubNamespaceName)
                      ResourceId.create(eventHubs, this.EventHubNamespaceName, this.Name)
                  ]
                  Rights = rule.Value }
        ]

type EventHubBuilder() =
    member __.Yield _ =
        { Name = ResourceName "hub"
          EventHubNamespace = derived (fun config -> config.Name.Map(sprintf "%s-ns"))
          Sku = Standard
          Capacity = 1
          ZoneRedundant = None
          ThroughputSettings = None
          KafkaEnabled = None
          MessageRetentionInDays = None
          Partitions = 1
          CaptureDestination = None
          ConsumerGroups = Set [ ResourceName "$Default" ]
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
    member __.AddConsumerGroup(state:EventHubConfig, name) = { state with ConsumerGroups = state.ConsumerGroups.Add (ResourceName name) }
    [<CustomOperation "add_authorization_rule">]
    member __.AddAuthorizationRule(state:EventHubConfig, name, rights) = { state with AuthorizationRules = state.AuthorizationRules.Add(ResourceName name, Set rights) }
    [<CustomOperation "capture_to_storage">]
    member _.CaptureToStorage(state:EventHubConfig, storageName:ResourceName, container) =
        { state with
            CaptureDestination = Some (StorageAccount(storageName, container)) }
    member this.CaptureToStorage(state:EventHubConfig, storageAccount:StorageAccountConfig, container) =
        this.CaptureToStorage(state, storageAccount.Name.ResourceName, container)

    member private _.AddDependency (state:EventHubConfig, resourceName:ResourceName) = { state with Dependencies = ResourceId.create resourceName :: state.Dependencies }
    member private _.AddDependencies (state:EventHubConfig, resourceNames:ResourceName list) = { state with Dependencies = (resourceNames |> List.map ResourceId.create) @ state.Dependencies }
    /// Sets a dependency for the web app.
    [<CustomOperation "depends_on">]
    member this.DependsOn(state:EventHubConfig, resourceName) = this.AddDependency(state, resourceName)
    member this.DependsOn(state:EventHubConfig, resources) = this.AddDependencies(state, resources)
    member this.DependsOn(state:EventHubConfig, builder:IBuilder) = this.AddDependency(state, builder.DependencyName)
    member this.DependsOn(state:EventHubConfig, builders:IBuilder list) = this.AddDependencies(state, builders |> List.map (fun x -> x.DependencyName))
    member this.DependsOn(state:EventHubConfig, resource:IArmResource) = this.AddDependency(state, resource.ResourceName)
    member this.DependsOn(state:EventHubConfig, resources:IArmResource list) = this.AddDependencies(state, resources |> List.map (fun x -> x.ResourceName))

    [<CustomOperation "add_tags">]
    member _.Tags(state:EventHubConfig, pairs) =
        { state with
            Tags = pairs |> List.fold (fun map (key,value) -> Map.add key value map) state.Tags }
    [<CustomOperation "add_tag">]
    member this.Tag(state:EventHubConfig, key, value) = this.Tags(state, [ (key,value) ])

let eventHub = EventHubBuilder()