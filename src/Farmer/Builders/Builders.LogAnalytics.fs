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
      RetentionPeriod: int<Days> option
      IngestionSupport: FeatureFlag option
      QuerySupport: FeatureFlag option
      Tags: Map<string,string> }
    interface IBuilder with
        member this.DependencyName = this.Name
        member this.BuildResources location = [
            { Name = this.Name
              Location = location
              Sku = this.Sku
              RetentionPeriod = this.RetentionPeriod
              IngestionSupport = this.IngestionSupport
              QuerySupport = this.QuerySupport
              Tags = this.Tags }
        ]

type WorkspaceBuilder() =
    /// Required - creates default "starting" values
    member _.Yield _ =
        { Name = ResourceName.Empty
          Sku = PerGB2018
          RetentionPeriod = None
          IngestionSupport = None
          QuerySupport = None
          Tags = Map.empty }

    member _.Run (state:WorkspaceConfig) =
        match state.RetentionPeriod with
        | None ->
            ()
        | Some days ->
            match state.Sku, days with
            | Standard, 30<Days> -> ()
            | Premium, 365<Days> -> ()
            | Standard, _ -> failwithf "The retention period for Standard must be 30."
            | Premium, _ -> failwithf "The retention period for Premium must be 365."
            | Free, _ -> failwithf "Remove the retention period if you specify a pricing tier of Free."
            | (Standalone | PerNode | PerGB2018), OutOfBounds -> failwithf "The retention period for PerNode, PerGB2018 and Standalone must be between 30 and 730"
            | _, InBounds value -> ()
        state

    /// Sets the name of the Log Analytics workspace.
    [<CustomOperation "name">]
    member _.Name(state: WorkspaceConfig, name) = { state with Name = ResourceName name }

    /// Sets the SKU of the Log Analytics workspace.
    [<CustomOperation "sku">]
    member _.Sku(state: WorkspaceConfig,sku) = { state with Sku = sku }

    /// The workspace data retention in days. -1 means Unlimited retention for the Unlimited Sku. 730 days is the maximum allowed for all other Skus. Standard and Premium pricing tiers which have fixed data retention of 30 and 365 days respectively.
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

    [<CustomOperation "add_tags">]
        member _.Tags(state:WorkspaceConfig, pairs) =
            { state with
                Tags = pairs |> List.fold (fun map (key, value) -> Map.add key value map) state.Tags }

    [<CustomOperation "add_tag">]
        member this.Tag(state:WorkspaceConfig, key, value) = this.Tags(state, [ key, value ])

let logAnalytics = WorkspaceBuilder()


