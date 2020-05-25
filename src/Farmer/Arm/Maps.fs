[<AutoOpen>]
module Farmer.Arm.Maps

open Farmer
open Farmer.CoreTypes
open Farmer.Maps

type Maps =
    { Name : ResourceName
      Location : Location
      Sku : Sku }
    interface IArmResource with
        member this.ResourceName = this.Name
        member this.JsonModel =
            {| ``type`` = "Microsoft.Maps/accounts"
               apiVersion = "2018-05-01"
               name = this.Name.Value
               location = this.Location.ArmValue
               sku =
                   {|
                     name =
                         match this.Sku with
                         | S0 -> "S0"
                         | S1 -> "S1" |}
            |} :> _
    