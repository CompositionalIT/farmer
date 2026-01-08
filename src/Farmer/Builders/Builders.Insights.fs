[<AutoOpen>]
module Farmer.Builders.Insights

open Farmer
open Farmer.Arm.Insights

type DataCollectionEndpointConfig = {
    Name: ResourceName
} with
    interface IBuilder with
        member this.ResourceId = dataCollectionEndpoints.resourceId this.Name
        member this.BuildResources location = [
            {
                Name = this.Name
                Location = location
            }
        ]

type DataCollectionRuleConfig = {
    Name: ResourceName
    DceResourceId : ResourceId
    LogAnalyticsWorkspaceResourceId : ResourceId option
    StreamDeclarations : Map<string, Column list>
    DataSources : Map<string, Map<string, string> list>
    Destinations : Map<string, Map<string, string> list>
    DataFlows : DataFlow list
    Tags: Map<string, string>
} with
    interface IBuilder with
        member this.ResourceId = dataCollectionRules.resourceId this.Name

        member this.BuildResources location = [
            {
                Name = this.Name
                Location = location
                DceResourceId = this.DceResourceId
                LogAnalyticsWorkspaceResourceId = this.LogAnalyticsWorkspaceResourceId
                StreamDeclarations = this.StreamDeclarations
                DataSources = this.DataSources
                Destinations = this.Destinations
                DataFlows = this.DataFlows
                Tags = this.Tags
            }
        ]

type DataCollectionEndpointBuilder() =
    member _.Yield _ = {
        Name = ResourceName.Empty
    }

    member _.Run(state: DataCollectionEndpointConfig) =
        state

    /// Sets the name of the Data Collection Endpoint.
    [<CustomOperation "name">]
    member _.Name(state: DataCollectionEndpointConfig, name) =
        { state with Name = ResourceName name }


type DataCollectionRuleBuilder() =
    member _.Yield _ = {
        Name = ResourceName.Empty
        DceResourceId = ResourceId.Empty
        LogAnalyticsWorkspaceResourceId = None
        StreamDeclarations = Map.empty
        DataSources = Map.empty
        Destinations = Map.empty
        DataFlows = []
        Tags = Map.empty
    }

    member _.Run(state: DataCollectionRuleConfig) =
        state

    /// Sets the name of the Data Collection Rule.
    [<CustomOperation "name">]
    member _.Name(state: DataCollectionRuleConfig, name) =
        { state with Name = ResourceName name }

    /// Sets the Data Collection Endpoint Resource ID.
    [<CustomOperation "data_collection_endpoint">]
    member _.DataCollectionEndpoint(state: DataCollectionRuleConfig, dceResourceId: ResourceId) =
        if dceResourceId.Type.Type <> Arm.Insights.dataCollectionEndpoints.Type then
            raiseFarmer $"given resource was not of type '{Arm.Insights.dataCollectionEndpoints.Type}'."
        { state with DceResourceId = dceResourceId }

    /// Sets the Log Analytics Workspace Resource ID.
    [<CustomOperation "log_analytics">]
    member _.LogAnalytics(state: DataCollectionRuleConfig, logAnalyticsWorkspaceResourceId: ResourceId) =
        if logAnalyticsWorkspaceResourceId.Type.Type <> Arm.LogAnalytics.workspaces.Type then
            raiseFarmer $"given resource was not of type '{Arm.LogAnalytics.workspaces.Type}'."
        { state with LogAnalyticsWorkspaceResourceId = Some logAnalyticsWorkspaceResourceId }

    /// Adds stream declarations.
    [<CustomOperation "stream_declarations">]
    member _.StreamDeclarations(state: DataCollectionRuleConfig, streams: List<string * Column list>) =
        { state with StreamDeclarations = streams |> List.map (fun (k, v) -> $"Custom-{k}_CL", v) |> Map.ofList  }

    /// Adds data sources.
    [<CustomOperation "data_sources">]
    member _.DataSources(state: DataCollectionRuleConfig, dataSources: List<string * List<string * string> list>) =
        { state with DataSources = dataSources |> List.map (fun (k, v) -> k, v |> List.map Map.ofList) |> Map.ofList }

    /// Adds destinations.
    [<CustomOperation "destinations">]
    member _.Destinations(state: DataCollectionRuleConfig, destinations: List<string * List<string * string> list>) =
        { state with Destinations = destinations |> List.map (fun (k, v) -> k, v |> List.map Map.ofList) |> Map.ofList }

    /// Adds data flows.
    [<CustomOperation "data_flows">]
    member _.DataFlows(state: DataCollectionRuleConfig, dataFlows: DataFlow list) =
        { state with DataFlows = dataFlows }

    interface ITaggable<DataCollectionRuleConfig> with
        member _.Add state tags = {
            state with
                Tags = state.Tags |> Map.merge tags
        }

let dataCollectionEndpoint = DataCollectionEndpointBuilder()

let dataCollectionRule = DataCollectionRuleBuilder()