[<AutoOpen>]
module Farmer.Builders.LogAnalytics

open Farmer
open Farmer.Arm
open Farmer.LogAnalytics
open Farmer.CoreTypes

let private (|InBounds|OutOfBounds|) days =
    if days < 30<Days> then OutOfBounds
    elif days > 730<Days> then OutOfBounds
    else InBounds days

type WorkspaceConfig =
    { Name: ResourceName
      Sku: Sku
      IngestionSupport: FeatureFlag option
      QuerySupport: FeatureFlag option
      DailyCap : int<Gb> option
      Tags: Map<string,string> }
    interface IBuilder with
        member this.DependencyName = this.Name
        member this.BuildResources location = [
            { Name = this.Name
              Location = location
              Sku = this.Sku
              IngestionSupport = this.IngestionSupport
              QuerySupport = this.QuerySupport
              DailyCap = this.DailyCap
              Tags = this.Tags }
        ]

type WorkspaceBuilder() =
    /// Required - creates default "starting" values
    member _.Yield _ =
        { Name = ResourceName.Empty
          Sku = PerGb 30<Days>
          DailyCap = None
          IngestionSupport = None
          QuerySupport = None
          Tags = Map.empty }

    member _.Run (state:WorkspaceConfig) =
        match state.Sku with
        | Standard
        | Premium
        | Free
        | (Standalone (InBounds _) | PerNode (InBounds _) | PerGb (InBounds _)) ->
            ()
        | (Standalone OutOfBounds | PerNode OutOfBounds | PerGb OutOfBounds) ->
            failwithf "The retention period for PerNode, PerGb and Standalone must be between 30 and 730"

        state

    /// Sets the name of the Log Analytics workspace.
    [<CustomOperation "name">]
    member _.Name(state: WorkspaceConfig, name) = { state with Name = ResourceName name }

    /// Sets the SKU of the Log Analytics workspace.
    [<CustomOperation "sku">]
    member _.Sku(state: WorkspaceConfig,sku) = { state with Sku = sku }

    // /// The workspace data retention in days. -1 means Unlimited retention for the Unlimited Sku. 730 days is the maximum allowed for all other Skus. Standard and Premium pricing tiers which have fixed data retention of 30 and 365 days respectively.
    // [<CustomOperation "retention_period">]
    // member _.RetentionInDays(state: WorkspaceConfig, retentionInDays) =
    //     { state with RetentionPeriod = Some retentionInDays }

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

    [<CustomOperation "add_tags">]
        member _.Tags(state:WorkspaceConfig, pairs) =
            { state with
                Tags = pairs |> List.fold (fun map (key, value) -> Map.add key value map) state.Tags }

    [<CustomOperation "add_tag">]
        member this.Tag(state:WorkspaceConfig, key, value) = this.Tags(state, [ key, value ])

let logAnalytics = WorkspaceBuilder()


