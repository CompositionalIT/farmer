[<AutoOpen>]
module Farmer.Arm.CognitiveServices

open Farmer

type Accounts =
    { Name : ResourceName
      Sku : string
      Kind : string }
    interface IArmResource with
        member this.ResourceName = this.Name
        member this.ToArmObject location =
            {| name = this.Name.Value
               ``type`` = "Microsoft.CognitiveServices/accounts"
               apiVersion = "2017-04-18"
               sku = {| name = this.Sku |}
               kind = this.Kind
               location = location.ArmValue
               tags = {||}
               properties = {||} |} :> _
