[<AutoOpen>]
module Farmer.Arm.LogAnalytics

open Farmer
open Farmer.LogAnalytics
open Farmer.CoreTypes

let workspaces = ResourceType("Microsoft.OperationalInsights/workspaces", "2020-03-01-preview")

type Workspace =
    { Name: ResourceName
      Location: Location
      Sku: Sku
      IngestionSupport: FeatureFlag option
      QuerySupport: FeatureFlag option
      Tags: Map<string, string> }
    member this.RetentionPeriod =
        match this.Sku with
        | Standard ->
            30<Days>
        | Premium ->
            365<Days>
        | Free ->
            7<Days>
        | PerNode days
        | PerGb days
        | Standalone days ->
            days

    interface IArmResource with
        member this.ResourceName = this.Name
        member this.JsonModel =
            {| workspaces.Create(this.Name, this.Location, tags = this.Tags) with
                properties =
                    {| sku = {| name = this.Sku.ArmValue |}
                       retentionInDays = this.RetentionPeriod
                       publicNetworkAccessForIngestion =
                        this.IngestionSupport |> Option.map(fun f -> f.ArmValue) |> Option.toObj
                       publicNetworkAccessForQuery =
                        this.QuerySupport |> Option.map(fun f -> f.ArmValue) |> Option.toObj |}
            |} :> _
