[<AutoOpen>]
module Farmer.Arm.LogAnalytics

open Farmer
open Farmer.CoreTypes

let workspaces = ResourceType("Microsoft.OperationalInsights/workspaces", "2020-03-01-preview")

type Workspace =
    { Name: ResourceName
      Location: Location
      RetentionPeriod : int<Days> option
      IngestionSupport: FeatureFlag option
      QuerySupport: FeatureFlag option
      DailyCap : int<Gb> option
      Tags: Map<string, string> }

    interface IArmResource with
        member this.ResourceName = this.Name
        member this.JsonModel =
            {| workspaces.Create(this.Name, this.Location, tags = this.Tags) with
                properties =
                    {| sku = {| name = "PerGB2018" |}
                       retentionInDays = this.RetentionPeriod |> Option.toNullable
                       workspaceCapping =
                        match this.DailyCap with
                        | None -> null
                        | Some cap -> {| dailyQuotaGb = cap |} |> box
                       publicNetworkAccessForIngestion =
                        this.IngestionSupport |> Option.map(fun f -> f.ArmValue) |> Option.toObj
                       publicNetworkAccessForQuery =
                        this.QuerySupport |> Option.map(fun f -> f.ArmValue) |> Option.toObj |}
            |} :> _
