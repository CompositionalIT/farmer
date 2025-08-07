[<AutoOpen>]
module Farmer.Builders.AzureMonitor

open Farmer
open Farmer.Arm
open Farmer.Arm.Monitor
open Farmer.Arm.Monitor.DataSources
open Farmer.Arm.Monitor.Destinations

type DataCollectionEndpointConfig = {
    Name: ResourceName
    OsType: OS
    Tags: Map<string, string>
} with

    interface IBuilder with
        member this.ResourceId = dataCollectionEndpoints.resourceId this.Name

        member this.BuildResources location = [
            {
                DataCollectionEndpoint.Name = this.Name
                OsType = this.OsType
                Location = location
                Tags = this.Tags
            }
        ]

type DataCollectionEndpointBuilder() =
    member _.Yield _ = {
        Name = ResourceName.Empty
        OsType = Linux
        Tags = Map.empty
    }

    [<CustomOperation "name">]
    member _.Name(state: DataCollectionEndpointConfig, name) = { state with Name = ResourceName name }

    [<CustomOperation "os_type">]
    member _.OsType(state: DataCollectionEndpointConfig, osType) = { state with OsType = osType }

    interface ITaggable<DataCollectionEndpointConfig> with
        member _.Add state tags = {
            state with
                Tags = state.Tags |> Map.merge tags
        }

let dataCollectionEndpoint = DataCollectionEndpointBuilder()

type DataSourceConfig =
    | PrometheusForwarder of PrometheusForwarder list

    static member BuildConfig(dataSources: DataSourceConfig list) : DataSources.DataSource = {
        PrometheusForwarder =
            dataSources
            |> List.tryFind (function
                | PrometheusForwarder _ -> true)
            |> function
                | Some(PrometheusForwarder forwarders) -> Some forwarders
                | None -> None
    }

type DestinationConfig =
    | MonitoringAccounts of MonitoringAccount list

    static member BuildConfig(destinations: DestinationConfig list) : Destinations.Destination = {
        MonitoringAccounts =
            destinations
            |> List.tryFind (function
                | MonitoringAccounts _ -> true)
            |> function
                | Some(MonitoringAccounts accounts) -> Some accounts
                | None -> None
    }

type DataCollectionRuleConfig = {
    Name: ResourceName
    OsType: OS
    Endpoint: ResourceId
    DataFlows: (DataFlow list) option
    DataSources: DataSourceConfig list
    Destinations: DestinationConfig list
    Tags: Map<string, string>
    Dependencies: ResourceId Set
} with

    interface IBuilder with
        member this.ResourceId = dataCollectionRules.resourceId this.Name

        member this.BuildResources location = [
            {
                Name = this.Name
                OsType = this.OsType
                Location = location
                Endpoint = this.Endpoint
                DataFlows = this.DataFlows
                DataSources =
                    match this.DataSources with
                    | [] -> None
                    | dataSources -> dataSources |> DataSourceConfig.BuildConfig |> Some
                Destinations =
                    match this.Destinations with
                    | [] -> None
                    | destinations -> destinations |> DestinationConfig.BuildConfig |> Some
                Tags = this.Tags
                Dependencies = this.Dependencies
            }
        ]

type DataCollectionRuleBuilder() =
    member _.Yield _ = {
        Name = ResourceName.Empty
        OsType = Linux
        Endpoint = ResourceId.Empty
        DataFlows = None
        DataSources = []
        Destinations = []
        Tags = Map.empty
        Dependencies = Set.empty
    }

    [<CustomOperation "name">]
    member _.Name(state: DataCollectionRuleConfig, name) = { state with Name = ResourceName name }

    /// Sets the kind for the data collection rule (Windows or Linux).
    [<CustomOperation "os_type">]
    member _.OsType(state: DataCollectionRuleConfig, osType) = { state with OsType = osType }

    /// Sets the endpoint for the data collection rule.
    [<CustomOperation "endpoint">]
    member _.Endpoint(state: DataCollectionRuleConfig, endpoint) = { state with Endpoint = endpoint }

    /// Sets the data flows for the data collection rule.
    [<CustomOperation "data_flows">]
    member _.DataFlows(state: DataCollectionRuleConfig, dataFlows) = {
        state with
            DataFlows = Some dataFlows
    }

    /// Sets the destinations for the data collection rule.
    [<CustomOperation "destinations">]
    member _.Destinations(state: DataCollectionRuleConfig, destinations) = {
        state with
            Destinations = destinations
    }

    /// Sets the data sources for the data collection rule.
    [<CustomOperation "data_sources">]
    member _.DataSources(state: DataCollectionRuleConfig, dataSources) = { state with DataSources = dataSources }

    interface ITaggable<DataCollectionRuleConfig> with
        member _.Add state tags = {
            state with
                Tags = state.Tags |> Map.merge tags
        }

    interface IDependable<DataCollectionRuleConfig> with
        member _.Add state newDeps = {
            state with
                Dependencies = state.Dependencies + newDeps
        }

let dataCollectionRule = DataCollectionRuleBuilder()

type DataCollectionRuleAssociationConfig = {
    Name: ResourceName
    AssociatedResource: ResourceId
    RuleId: ResourceId
    Description: string
    Dependencies: ResourceId Set
} with

    interface IBuilder with
        member this.ResourceId =
            dataCollectionRuleAssociations(this.AssociatedResource.Type).resourceId this.Name

        member this.BuildResources location = [
            {
                Name = this.Name
                AssociatedResource = this.AssociatedResource
                Location = location
                RuleId = this.RuleId
                Description = this.Description
                Dependencies = this.Dependencies
            }
        ]

type DataCollectionRuleAssociationBuilder() =
    member _.Yield _ = {
        Name = ResourceName.Empty
        AssociatedResource = ResourceId.Empty
        RuleId = ResourceId.Empty
        Description = System.String.Empty
        Dependencies = Set.empty
    }

    member _.Run(config: DataCollectionRuleAssociationConfig) =
        if config.AssociatedResource = ResourceId.Empty then
            raiseFarmer "Associated resource must be specified for data collection rule association."

        if config.RuleId = ResourceId.Empty then
            raiseFarmer "Rule id must be specified for data collection rule association."

        config

    [<CustomOperation "name">]
    member _.Name(state: DataCollectionRuleAssociationConfig, name) = { state with Name = ResourceName name }

    /// Sets resource id of the resource to associate with the data collection rule.
    [<CustomOperation "associated_resource">]
    member _.AssociatedResource(state: DataCollectionRuleAssociationConfig, associationResource) = {
        state with
            AssociatedResource = associationResource
    }

    /// Sets the rule id for the data collection rule association.
    [<CustomOperation "rule_id">]
    member _.RuleId(state: DataCollectionRuleAssociationConfig, ruleId) = { state with RuleId = ruleId }

    [<CustomOperation "description">]
    member _.Description(state: DataCollectionRuleAssociationConfig, description) = {
        state with
            Description = description
    }

    interface IDependable<DataCollectionRuleConfig> with
        member _.Add state newDeps = {
            state with
                Dependencies = state.Dependencies + newDeps
        }

let dataCollectionRuleAssociation = DataCollectionRuleAssociationBuilder()