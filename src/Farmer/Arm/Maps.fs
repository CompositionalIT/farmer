[<AutoOpen>]
module Farmer.Arm.Maps

open Farmer
open Farmer.CoreTypes
open Farmer.Maps

let accounts = ResourceType ("Microsoft.Maps/accounts", "2018-05-01")

type Maps =
    { Name : ResourceName
      Location : Location
      Sku : Sku
      Tags: Map<string,string> }
    interface IArmResource with
        member this.ResourceName = this.Name
        member this.JsonModel =
            {| accounts.Create(this.Name, this.Location, tags = this.Tags) with
                sku =
                    {| name =
                        match this.Sku with
                        | S0 -> "S0"
                        | S1 -> "S1" |}
            |} :> _
