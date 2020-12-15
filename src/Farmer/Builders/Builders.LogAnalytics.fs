[<AutoOpen>]
module Farmer.Builders.LogAnalytics

open Farmer
open Farmer.Arm

let private (|InBounds|OutOfBounds|) days =
    if days < 30<Days> then OutOfBounds days
    elif days > 730<Days> then OutOfBounds days
    else InBounds days

type WorkspaceConfig =
    { Name: ResourceName
      RetentionPeriod: int<Days> option
      IngestionSupport: FeatureFlag option
      QuerySupport: FeatureFlag option
      DailyCap : int<Gb> option
      Tags: Map<string,string> }
    interface IBuilder with
        member this.ResourceId = workspaces.resourceId this.Name
        member this.BuildResources location = [
            { Name = this.Name
              Location = location
              RetentionPeriod = this.RetentionPeriod
              IngestionSupport = this.IngestionSupport
              QuerySupport = this.QuerySupport
              DailyCap = this.DailyCap
              Tags = this.Tags }
        ]

type WorkspaceBuilder() =
    interface ITaggable<WorkspaceConfig> with member _.SetTags state mergeTags = { state with Tags = mergeTags state.Tags }
    member _.Yield _ =
        { Name = ResourceName.Empty
          RetentionPeriod = None
          DailyCap = None
          IngestionSupport = None
          QuerySupport = None
          Tags = Map.empty }

    member _.Run (state:WorkspaceConfig) =
        match state.RetentionPeriod with
        | Some (OutOfBounds days) ->
            failwithf "The retention period must be between 30 and 730 days. It is currently %d" days
        | None
        | Some (InBounds _) ->
            ()

        state

    /// Sets the name of the Log Analytics workspace.
    [<CustomOperation "name">]
    member _.Name(state: WorkspaceConfig, name) = { state with Name = ResourceName name }

    /// The workspace data retention in days. Must be between 30 and 730 days.
    [<CustomOperation "retention_period">]
    member _.RetentionInDays(state: WorkspaceConfig, retentionInDays) =
        { state with RetentionPeriod = Some retentionInDays }

    /// Enables Log Analytics ingestion
    [<CustomOperation "enable_ingestion">]
    member _.PublicNetworkAccessForIngestion(state: WorkspaceConfig) =
        { state with IngestionSupport = Some Enabled }

    /// Enables Log Analytics querying.
    [<CustomOperation "enable_query">]
    member _.PublicNetworkAccessForQuery(state: WorkspaceConfig) =
        { state with QuerySupport = Some Enabled }

    /// Specifies the daily cap of ingested data.
    [<CustomOperation "daily_cap">]
    member _.DailyCap(state: WorkspaceConfig, cap) = { state with DailyCap = Some cap }


let logAnalytics = WorkspaceBuilder()


