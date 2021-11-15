[<AutoOpen>]
module Farmer.Builders.LogAnalytics

open Farmer
open Farmer.Arm.LogAnalytics

let private (|InBounds|OutOfBounds|) days =
    if days < 30<Days> then OutOfBounds days
    elif days > 730<Days> then OutOfBounds days
    else InBounds days

type LogAnalytics =
    static member getCustomerId (resourceId:ResourceId) =
        ArmExpression
            .reference(workspaces, resourceId)
            .Map(fun r -> r + ".customerId")
            .WithOwner(resourceId)
    static member getCustomerId (name:ResourceName, ?resourceGroup) =
        LogAnalytics.getCustomerId(ResourceId.create (workspaces, name, ?group = resourceGroup))

    static member getPrimarySharedKey (resourceId:ResourceId) =
        ArmExpression
            .listKeys(workspaces, resourceId)
            .Map(fun r -> r + ".primarySharedKey")
            .WithOwner(resourceId)
    static member getPrimarySharedKey (name:ResourceName, ?resourceGroup) =
        LogAnalytics.getPrimarySharedKey(ResourceId.create (workspaces, name, ?group = resourceGroup))

type WorkspaceConfig =
    { Name: ResourceName
      RetentionPeriod: int<Days> option
      IngestionSupport: FeatureFlag option
      QuerySupport: FeatureFlag option
      DailyCap : int<Gb> option
      Tags: Map<string,string> }

    /// Gets the ARM expression path to the customer ID of this LogAnalytics instance.
    member this.CustomerId = LogAnalytics.getCustomerId this.Name

    /// Gets the ARM expression path to the primary shared key of this LogAnalytics instance.
    member this.PrimarySharedKey = LogAnalytics.getPrimarySharedKey this.Name

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


