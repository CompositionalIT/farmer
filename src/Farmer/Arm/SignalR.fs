[<AutoOpen>]
module Farmer.Arm.SignalRService

open Farmer
open Farmer.CoreTypes
open Farmer.SignalR

let signalR = ResourceType ("Microsoft.SignalRService/signalR", "2018-10-01")

type SignalR =
    { Name : ResourceName
      Location : Location
      Sku : Sku
      Capacity : int option
      AllowedOrigins : string list
      Tags: Map<string,string>  }
    interface IArmResource with
        member this.ResourceName = this.Name
        member this.JsonModel =
            {| signalR.Create(this.Name, this.Location, tags = this.Tags) with
                sku =
                    {| name =
                        match this.Sku with
                        | Free -> "Free_F1"
                        | Standard -> "Standard_S1"
                       capacity =
                        match this.Capacity with
                        | Some c -> c.ToString()
                        | None -> null |}
                properties =
                    {| cors =
                        match this.AllowedOrigins with
                        | [] -> null
                        | aos -> box {| allowedOrigins = aos |} |}
            |} :> _