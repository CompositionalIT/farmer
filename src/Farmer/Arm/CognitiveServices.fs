[<AutoOpen>]
module Farmer.Arm.CognitiveServices

open Farmer
open Farmer.CoreTypes

let accounts = ResourceType "Microsoft.CognitiveServices/accounts"

type Accounts =
    { Name : ResourceName
      Location : Location
      Sku : CognitiveServices.Sku
      Kind : CognitiveServices.Kind
      Tags: Map<string,string>  }
    interface IArmResource with
        member this.ResourceName = this.Name
        member this.JsonModel =
            {| name = this.Name.Value
               ``type`` = accounts.ArmValue
               apiVersion = "2017-04-18"
               sku = {| name = string this.Sku |}
               kind = this.Kind.ToString().Replace("_", ".")
               location = this.Location.ArmValue
               tags = {||}
               properties = {||}
               tags = this.Tags |} :> _
