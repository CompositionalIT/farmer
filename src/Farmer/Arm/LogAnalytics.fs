[<AutoOpen>]
module Farmer.Arm.LogAnalytics

open Farmer
open Farmer.LogAnalytics
open Farmer.CoreTypes

let workspaces = ResourceType("Microsoft.OperationalInsights/workspaces", "2020-08-01")

type WorkSpace =
    { Name: ResourceName
      Location: Location
      Sku: Sku
      RetentionPeriod: int<Days> option
      IngestionSupport: FeatureFlag option
      QuerySupport: FeatureFlag option
      Tags: Map<string, string> }
    interface IArmResource with
        member this.ResourceName = this.Name
        member this.JsonModel =
            {| workspaces.Create(this.Name, this.Location, tags = this.Tags) with
                properties =
                    {| sku = {| name = this.Sku.ArmValue |}
                       retentionInDays = this.RetentionPeriod |> Option.toNullable
                       publicNetworkAccessForIngestion =
                        this.IngestionSupport |> Option.map(fun f -> f.ArmValue) |> Option.toObj
                       publicNetworkAccessForQuery =
                        this.QuerySupport |> Option.map(fun f -> f.ArmValue) |> Option.toObj |}
            |} :> _
