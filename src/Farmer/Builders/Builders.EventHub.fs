[<AutoOpen>]
module Farmer.Builders.EventHub

open Farmer
open Farmer.EventHub
open Farmer.Arm.EventHub
open Farmer.Arm.Storage
open Namespaces
open EventHubs

/// Shortcut for Manage, Send and Listen rights.
let AllAuthorizationRights = [ Manage; Send; Listen ]

type EventHubConfig = {
    EventHubNamespace: ResourceRef<EventHubConfig>
    Name: ResourceName
    Sku: EventHubSku
    Capacity: int
    ZoneRedundant: bool option
    ThroughputSettings: InflateSetting option
    MessageRetentionInDays: int option
    Partitions: int
    ConsumerGroups: ResourceName Set
    CaptureDestination: CaptureDestination option
    AuthorizationRules: Map<ResourceName, AuthorizationRuleRight Set>
    Dependencies: ResourceId Set
    Tags: Map<string, string>
} with

    member private this.CreateKeyExpression(resourceId: ResourceId) =
        ArmExpression
            .create($"listkeys({resourceId.ArmExpression.Value}, '2017-04-01').primaryConnectionString")
            .WithOwner(eventHubs.resourceId this.Name)

    member this.EventHubNamespaceName = this.EventHubNamespace.resourceId(this).Name

    /// Gets an ARM expression for the path to the key of a specific authorization rule for this event hub.
    member this.GetKey(ruleName: string) =
        let ruleResource =
            EventHubs.authorizationRules.resourceId (this.EventHubNamespaceName, this.Name, ResourceName ruleName)

        this.CreateKeyExpression ruleResource

    /// Gets an ARM expression for the path to the key of the default RootManageSharedAccessKey for the entire namespace.
    member this.DefaultKey = this.CreateKeyExpression this.DefaultAuthorizationRule

    member this.DefaultAuthorizationRule =
        Namespaces.authorizationRules.resourceId (this.EventHubNamespaceName, ResourceName "RootManageSharedAccessKey")

    interface IBuilder with
        member this.ResourceId = namespaces.resourceId this.EventHubNamespaceName

        member this.BuildResources location = [
            let eventHubName =
                this.Name.Map(fun hubName -> $"{this.EventHubNamespaceName.Value}/{hubName}")

            // Namespace
            match this.EventHubNamespace with
            | DeployableResource this _ -> {
                Name = this.EventHubNamespaceName
                Location = location
                Sku = {|
                    Name = this.Sku
                    Capacity = this.Capacity
                |}
                ZoneRedundant = this.ZoneRedundant
                AutoInflateSettings = this.ThroughputSettings
                Tags = this.Tags
              }
            | _ -> ()

            // Event hub
            {
                Name = eventHubName
                Location = location
                MessageRetentionDays = this.MessageRetentionInDays
                Partitions = this.Partitions
                CaptureDestination = this.CaptureDestination
                Dependencies =
                    Set [
                        namespaces.resourceId this.EventHubNamespaceName
                        yield!
                            this.CaptureDestination
                            |> Option.mapList (fun (StorageAccount(name, _)) -> storageAccounts.resourceId name)
                        yield! this.Dependencies
                    ]
                Tags = this.Tags
            }

            // Consumer groups
            for consumerGroup in this.ConsumerGroups do
                {
                    ConsumerGroupName = consumerGroup
                    EventHub = eventHubName
                    Location = location
                    Dependencies = [
                        namespaces.resourceId this.EventHubNamespaceName
                        eventHubs.resourceId (this.EventHubNamespaceName, this.Name)
                    ]
                }

            // Auth rules
            for rule in this.AuthorizationRules do
                {
                    Name = rule.Key.Map(fun rule -> $"{this.EventHubNamespaceName.Value}/{this.Name.Value}/%s{rule}")
                    Location = location
                    Dependencies = [
                        namespaces.resourceId this.EventHubNamespaceName
                        eventHubs.resourceId (this.EventHubNamespaceName, this.Name)
                    ]
                    Rights = rule.Value
                }
        ]

type EventHubBuilder() =
    member _.Yield _ = {
        Name = ResourceName "hub"
        EventHubNamespace = derived (fun config -> namespaces.resourceId (config.Name - "ns"))
        Sku = Standard
        Capacity = 1
        ZoneRedundant = None
        ThroughputSettings = None
        MessageRetentionInDays = None
        Partitions = 1
        CaptureDestination = None
        ConsumerGroups = Set.empty
        AuthorizationRules = Map.empty
        Dependencies = Set.empty
        Tags = Map.empty
    }

    /// Sets the name of the Event Hub instance.
    [<CustomOperation "name">]
    member _.Name(state: EventHubConfig, name) = { state with Name = name }

    member this.Name(state: EventHubConfig, name) = this.Name(state, ResourceName name)

    /// Sets the name of the Event Hub namespace.
    [<CustomOperation "namespace_name">]
    member _.NamespaceName(state: EventHubConfig, name) = {
        state with
            EventHubNamespace = named namespaces name
    }

    member this.NamespaceName(state: EventHubConfig, name) =
        this.NamespaceName(state, ResourceName name)

    /// Sets the name of the Event Hub namespace.
    [<CustomOperation "link_to_namespace">]
    member _.LinkToNamespaceName(state: EventHubConfig, name) = {
        state with
            EventHubNamespace = managed namespaces name
    }

    member this.LinkToNamespaceName(state: EventHubConfig, name) =
        this.LinkToNamespaceName(state, ResourceName name)

    /// Sets the sku of the Event Hub instance.
    [<CustomOperation "sku">]
    member _.Sku(state: EventHubConfig, sku) = { state with Sku = sku }

    [<CustomOperation "capacity">]
    member _.ReplicaCount(state: EventHubConfig, capacity: int) = { state with Capacity = capacity }

    [<CustomOperation "enable_zone_redundant">]
    member _.ZoneRedundant(state: EventHubConfig) = { state with ZoneRedundant = Some true }

    [<CustomOperation "enable_auto_inflate">]
    member _.AutoInflate(state: EventHubConfig, maxThroughput) = {
        state with
            ThroughputSettings = Some(AutoInflate maxThroughput)
    }

    [<CustomOperation "disable_auto_inflate">]
    member _.MaximumThroughputUnits(state: EventHubConfig) = {
        state with
            ThroughputSettings = Some ManualInflate
    }

    [<CustomOperation "message_retention_days">]
    member _.MessageRetentionDays(state: EventHubConfig, days) = {
        state with
            MessageRetentionInDays = Some days
    }

    [<CustomOperation "partitions">]
    member _.Partitions(state: EventHubConfig, partitions) = { state with Partitions = partitions }

    [<CustomOperation "add_consumer_group">]
    member _.AddConsumerGroup(state: EventHubConfig, name) = {
        state with
            ConsumerGroups = state.ConsumerGroups.Add(ResourceName name)
    }

    [<CustomOperation "add_authorization_rule">]
    member _.AddAuthorizationRule(state: EventHubConfig, name, rights) = {
        state with
            AuthorizationRules = state.AuthorizationRules.Add(ResourceName name, Set rights)
    }

    [<CustomOperation "capture_to_storage">]
    member _.CaptureToStorage(state: EventHubConfig, storageName: ResourceName, container) = {
        state with
            CaptureDestination = Some(StorageAccount(storageName, container))
    }

    member this.CaptureToStorage(state: EventHubConfig, storageAccount: StorageAccountConfig, container) =
        this.CaptureToStorage(state, storageAccount.Name.ResourceName, container)

    interface IDependable<EventHubConfig> with
        member _.Add state newDeps = {
            state with
                Dependencies = state.Dependencies + newDeps
        }

    interface ITaggable<EventHubConfig> with
        member _.Add state tags = {
            state with
                Tags = state.Tags |> Map.merge tags
        }

let eventHub = EventHubBuilder()