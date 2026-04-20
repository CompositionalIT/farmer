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
} with

    member this.BuildResources logAnalyticsWorkspace = [
        {
            Name = ResourceName $"{this.Name.Value}_CL"
            Plan = this.Plan
            Columns = this.Columns
            TotalRetentionInDays = this.TotalRetentionInDays
            LogAnalyticsWorkspace = logAnalyticsWorkspace
        }
        :> IArmResource
    ]

type WorkspaceConfig = {
    Name: ResourceName
    RetentionPeriod: int<Days> option
    IngestionSupport: FeatureFlag option
    QuerySupport: FeatureFlag option
    DailyCap: int<Gb> option
    CustomTables: TableConfig list
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
            for table in this.CustomTables do
                yield! table.BuildResources (this :> IBuilder).ResourceId
        ]

type WorkspaceBuilder() =
    member _.Yield _ = {
        Name = ResourceName.Empty
        RetentionPeriod = None
        DailyCap = None
        IngestionSupport = None
        QuerySupport = None
        CustomTables = []
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

    /// Adds tables to the Log Analytics workspace.
    [<CustomOperation "custom_tables">]
    member _.CustomTables(state: WorkspaceConfig, customTables: TableConfig list) = {
        state with
            CustomTables = customTables
    }

    interface ITaggable<WorkspaceConfig> with
        member _.Add state tags = {
            state with
                Tags = state.Tags |> Map.merge tags
        }

let logAnalytics = WorkspaceBuilder()