[<AutoOpen>]
module Farmer.Builders.LogAnalytics

open Farmer
open Farmer.Arm.LogAnalytics

let private (|InBounds|OutOfBounds|) days =
    if days < 30<Days> then OutOfBounds days
    elif days > 730<Days> then OutOfBounds days
    else InBounds days

type TableConfig = {
    Name: ResourceName
    Plan: Plan
    Columns: Column list
    TotalRetentionInDays: int<Days> option
    LogAnalyticsWorkspace: ResourceId
} with
    interface IBuilder with
        member this.ResourceId = tables.resourceId (this.LogAnalyticsWorkspace.Name/this.Name)
        member this.BuildResources _ = [
            let t : Table = {
                Name = this.Name
                Plan = this.Plan
                Columns = this.Columns
                TotalRetentionInDays = this.TotalRetentionInDays
                LogAnalyticsWorkspace = this.LogAnalyticsWorkspace
            }
            t
        ]

type WorkspaceConfig = {
    Name: ResourceName
    RetentionPeriod: int<Days> option
    IngestionSupport: FeatureFlag option
    QuerySupport: FeatureFlag option
    DailyCap: int<Gb> option
    Tags: Map<string, string>
} with

    /// Gets the ARM expression path to the customer ID of this LogAnalytics instance.
    member this.CustomerId = LogAnalytics.getCustomerId this.Name

    /// Gets the ARM expression path to the primary shared key of this LogAnalytics instance.
    member this.PrimarySharedKey = LogAnalytics.getPrimarySharedKey this.Name

    interface IBuilder with
        member this.ResourceId = workspaces.resourceId this.Name

        member this.BuildResources location = [
            {
                Name = this.Name
                Location = location
                RetentionPeriod = this.RetentionPeriod
                IngestionSupport = this.IngestionSupport
                QuerySupport = this.QuerySupport
                DailyCap = this.DailyCap
                Tags = this.Tags
            }
        ]

type TableBuilder() =
    member _.Yield _ = {
        Name = ResourceName.Empty
        Plan = Basic
        Columns = []
        TotalRetentionInDays = None
        LogAnalyticsWorkspace = ResourceId.Empty
    }
    /// Sets the name of the Log Analytics table.
    [<CustomOperation "name">]
    member _.Name(state: TableConfig, name) = { state with Name = ResourceName name }
    /// Sets the plan of the Log Analytics table.
    [<CustomOperation "plan">]
    member _.Plan(state: TableConfig, plan) = { state with Plan = plan }
    /// Sets the columns of the Log Analytics table.
    [<CustomOperation "columns">]
    member _.Columns(state: TableConfig, columns) = { state with Columns = columns }
    /// Sets the total retention period of the Log Analytics table.
    [<CustomOperation "total_retention_in_days">]
    member _.TotalRetentionInDays(state: TableConfig, days) = { state with TotalRetentionInDays = Some days }
    /// Sets the Log Analytics workspace for the table.
    [<CustomOperation "log_analytics_workspace">]
    member _.LogAnalyticsWorkspace(state: TableConfig, workspaceId : ResourceId) =
        if workspaceId.Type.Type <> Arm.LogAnalytics.workspaces.Type then
            raiseFarmer $"given resource was not of type '{Arm.LogAnalytics.workspaces.Type}'."
        { state with LogAnalyticsWorkspace = workspaceId }


type WorkspaceBuilder() =
    member _.Yield _ = {
        Name = ResourceName.Empty
        RetentionPeriod = None
        DailyCap = None
        IngestionSupport = None
        QuerySupport = None
        Tags = Map.empty
    }

    member _.Run(state: WorkspaceConfig) =
        match state.RetentionPeriod with
        | Some(OutOfBounds days) ->
            raiseFarmer $"The retention period must be between 30 and 730 days. It is currently {days}."
        | None
        | Some(InBounds _) -> ()

        state

    /// Sets the name of the Log Analytics workspace.
    [<CustomOperation "name">]
    member _.Name(state: WorkspaceConfig, name) = { state with Name = ResourceName name }

    /// The workspace data retention in days. Must be between 30 and 730 days.
    [<CustomOperation "retention_period">]
    member _.RetentionInDays(state: WorkspaceConfig, retentionInDays) = {
        state with
            RetentionPeriod = Some retentionInDays
    }

    /// Enables Log Analytics ingestion
    [<CustomOperation "enable_ingestion">]
    member _.PublicNetworkAccessForIngestion(state: WorkspaceConfig) = {
        state with
            IngestionSupport = Some Enabled
    }

    /// Enables Log Analytics querying.
    [<CustomOperation "enable_query">]
    member _.PublicNetworkAccessForQuery(state: WorkspaceConfig) = {
        state with
            QuerySupport = Some Enabled
    }

    /// Specifies the daily cap of ingested data.
    [<CustomOperation "daily_cap">]
    member _.DailyCap(state: WorkspaceConfig, cap) = { state with DailyCap = Some cap }

    interface ITaggable<WorkspaceConfig> with
        member _.Add state tags = {
            state with
                Tags = state.Tags |> Map.merge tags
        }

let logAnalytics = WorkspaceBuilder()