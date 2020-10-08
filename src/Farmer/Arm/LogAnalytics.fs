[<AutoOpen>]
module Farmer.Arm.LogAnalytics
open Farmer
open Farmer.LogAnalytics
open Farmer.CoreTypes

let workspaces = ResourceType ("Microsoft.OperationalInsights/workspaces", "2020-08-01")

type WorkSpace =
    { Name: ResourceName
      Location: Location
      Sku: Sku
      retentionInDays: int option
      publicNetworkAccessForIngestion: string option 
      publicNetworkAccessForQuery: string option
      Tags: Map<string,string>
      }
    interface IArmResource with
        member this.ResourceName = this.Name
        member this.JsonModel =
            {| workspaces.Create(this.Name,this.Location,tags=this.Tags) with
                   properties =
                       {|  sku = {| name = this.Sku.ArmValue|}
                           retentionInDays = this.retentionInDays  |>  Option.toNullable
                           publicNetworkAccessForIngestiont = this.publicNetworkAccessForIngestion |> Option.toObj
                           publicNetworkAccessForQuery = this.publicNetworkAccessForQuery |> Option.toObj |} |} :> _

