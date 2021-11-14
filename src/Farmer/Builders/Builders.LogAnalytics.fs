[<AutoOpen>]
module Farmer.Builders.LogAnalytics

open Farmer
open Farmer.Arm.LogAnalytics

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
      Location : Location option
      Tags: Map<string,string> }
    interface IBuilder with
        member this.ResourceId = workspaces.resourceId this.Name
        member this.BuildResources location = 
            let resource : Workspace =
                { Name = this.Name
                  Location = this.Location |> Option.defaultValue location
                  RetentionPeriod = this.RetentionPeriod
                  IngestionSupport = this.IngestionSupport
                  QuerySupport = this.QuerySupport
                  DailyCap = this.DailyCap
                  Tags = this.Tags }
            [ resource ]

type WorkspaceBuilder() =
    member _.Yield _ =
        { Name = ResourceName.Empty
          RetentionPeriod = None
          DailyCap = None
          IngestionSupport = None
          QuerySupport = None
          Location = None
          Tags = Map.empty }

    member _.Run (state:WorkspaceConfig) =
        match state.RetentionPeriod with
        | Some (OutOfBounds days) ->
            raiseFarmer $"The retention period must be between 30 and 730 days. It is currently {days}."
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

    /// Sets the location  of the Log Analytics workspace.
    [<CustomOperation "location">]
    member _.Location (state: WorkspaceConfig, location) = { state with Location = Some location }        

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

    interface ITaggable<WorkspaceConfig> with member _.Add state tags = { state with Tags = state.Tags |> Map.merge tags }

let logAnalytics = WorkspaceBuilder()


