[<AutoOpen>]
module Farmer.Arm.CognitiveServices

open Farmer
open Farmer.CoreTypes

let accounts = ResourceType ("Microsoft.CognitiveServices/accounts", "2017-04-18")

type Accounts =
    { Name : ResourceName
      Location : Location
      Sku : CognitiveServices.Sku
      Kind : CognitiveServices.Kind
      Tags: Map<string,string>  }
    interface IArmResource with
        member this.ResourceName = this.Name
        member this.JsonModel =
            {| accounts.Create(this.Name, this.Location, tags = this.Tags) with
                sku = {| name = string this.Sku |}
                kind = this.Kind.ToString().Replace("_", ".")
                properties = {||}
            |} :> _
