[<AutoOpen>]
module Farmer.Builders.LogAnalytics

open Farmer
open Farmer.Arm
open Farmer.LogAnalytics
open Farmer.CoreTypes

type WorkSpaceconfig =
    { Name: ResourceName
      Sku: Sku
      RetentionInDays: int<Days> option
      IngestionSupport: FeatureFlag option
      QuerySupport: FeatureFlag option
      Tags: Map<string,string> }
    interface IBuilder with
        member this.DependencyName = this.Name
        member this.BuildResources location = [
            { Name = this.Name
              Location = location
              Sku = this.Sku
              RetentionPeriod =
                match this.Sku, this.RetentionInDays with
                | Standard, Some 30<Days> ->
                    Some 30<Days>
                | Standard, Some _ ->
                    failwithf "The retention Days for Standard must be 30."
                | Premium, Some 365<Days> ->
                    Some 365<Days>
                | Premium, Some _ ->
                    failwithf "The retention Days for Premium must be 365."
                | Free, None ->
                    None
                | Free, Some _ ->
                    failwithf "Remove the retention period if you specify a pricing tier of Free."
                | (Standalone | PerNode | PerGB2018), Some value when value < 30<Days> || value > 730<Days> ->
                    failwithf "The retention period for PerNode, PerGB2018 and Standalone must be between 30 and 730"
                | _, Some value ->
                    Some value
                | _, None ->
                    None
              IngestionSupport = this.IngestionSupport
              QuerySupport = this.QuerySupport
              Tags = this.Tags }
        ]

type WorkSpaceBuilder() =
    /// Required - creates default "starting" values
    member _.Yield _ =
        { Name = ResourceName.Empty
          Sku = PerGB2018
          RetentionInDays = None
          IngestionSupport = None
          QuerySupport = None
          Tags = Map.empty }

    /// Sets the name of the Log Analytics workspace.
    [<CustomOperation "name">]
    member _.Name(state: WorkSpaceconfig, name) = { state with Name = ResourceName name }

    /// Sets the SKU of the Log Analytics workspace.
    [<CustomOperation "sku">]
    member _.Sku(state: WorkSpaceconfig,sku) = { state with Sku = sku }

    /// The workspace data retention in days. -1 means Unlimited retention for the Unlimited Sku. 730 days is the maximum allowed for all other Skus. Standard and Premium pricing tiers which have fixed data retention of 30 and 365 days respectively.
    [<CustomOperation "retention_period">]
    member _.RetentionInDays(state: WorkSpaceconfig, retentionInDays) =
        { state with RetentionInDays = Some retentionInDays }

    /// Enables Log Analytics ingestion
    [<CustomOperation "enable_ingestion">]
    member _.PublicNetworkAccessForIngestion(state: WorkSpaceconfig) =
        { state with IngestionSupport = Some Enabled }

    /// Enables Log Analytics querying.
    [<CustomOperation "enable_query">]
    member _.PublicNetworkAccessForQuery(state: WorkSpaceconfig) =
        { state with QuerySupport = Some Enabled }

    [<CustomOperation "add_tags">]
        member _.Tags(state:WorkSpaceconfig, pairs) =
            { state with
                Tags = pairs |> List.fold (fun map (key, value) -> Map.add key value map) state.Tags }

    [<CustomOperation "add_tag">]
        member this.Tag(state:WorkSpaceconfig, key, value) = this.Tags(state, [ key, value ])

let logAnalytics = WorkSpaceBuilder()


