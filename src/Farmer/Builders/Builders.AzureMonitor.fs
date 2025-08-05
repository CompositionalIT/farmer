[<AutoOpen>]
module Farmer.Builders.AzureMonitor

open Farmer
open Farmer.Arm
open Farmer.Arm.Monitor

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

type DataCollectionRuleConfig = {
    Name: ResourceName
    OsType: OS
    Endpoint: ResourceId
    DataFlows: (DataFlow list) option
    DataSources: DataSourceConfig.DataSource option
    Destinations: DestinationsConfig.Destinations option
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
                DataSources = this.DataSources
                Destinations = this.Destinations
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
        DataSources = None
        Destinations = None
        Tags = Map.empty
        Dependencies = Set.empty
    }

    [<CustomOperation "name">]
    member _.Name(state: DataCollectionRuleConfig, name) = { state with Name = ResourceName name }

    [<CustomOperation "os_type">]
    member _.OsType(state: DataCollectionRuleConfig, osType) = { state with OsType = osType }

    [<CustomOperation "endpoint">]
    member _.Endpoint(state: DataCollectionRuleConfig, endpoint) = { state with Endpoint = endpoint }

    [<CustomOperation "data_flows">]
    member _.DataFlows(state: DataCollectionRuleConfig, dataFlows) = {
        state with
            DataFlows = Some dataFlows
    }

    [<CustomOperation "destinations">]
    member _.Destinations(state: DataCollectionRuleConfig, destinations) = {
        state with
            Destinations = Some destinations
    }

    [<CustomOperation "data_sources">]
    member _.DataSources(state: DataCollectionRuleConfig, dataSources) = {
        state with
            DataSources = Some dataSources
    }

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
    Description: string option
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
        Description = None
        Dependencies = Set.empty
    }

    [<CustomOperation "name">]
    member _.Name(state: DataCollectionRuleAssociationConfig, name) = { state with Name = ResourceName name }

    [<CustomOperation "associated_resource">]
    member _.AssociatedResource(state: DataCollectionRuleAssociationConfig, associationResource) = {
        state with
            AssociatedResource = associationResource
    }

    [<CustomOperation "rule_id">]
    member _.RuleId(state: DataCollectionRuleAssociationConfig, ruleId) = { state with RuleId = ruleId }

    [<CustomOperation "description">]
    member _.Description(state: DataCollectionRuleAssociationConfig, description) = {
        state with
            Description = Some description
    }

    interface IDependable<DataCollectionRuleConfig> with
        member _.Add state newDeps = {
            state with
                Dependencies = state.Dependencies + newDeps
        }

let dataCollectionRuleAssociation = DataCollectionRuleAssociationBuilder()