[<AutoOpen>]
module Farmer.Arm.Insights

open Farmer

type Components =
    { Name : ResourceName
      Location : Location
      LinkedWebsite : ResourceName option }
    interface IArmResource with
        member this.ResourceName = this.Name
        member this.JsonValue =
            {| ``type`` = "Microsoft.Insights/components"
               kind = "web"
               name = this.Name.Value
               location = this.Location.ArmValue
               apiVersion = "2014-04-01"
               tags =
                   [ match this.LinkedWebsite with
                     | Some linkedWebsite -> sprintf "[concat('hidden-link:', resourceGroup().id, '/providers/Microsoft.Web/sites/', '%s')]" linkedWebsite.Value, "Resource"
                     | None -> ()
                     "displayName", "AppInsightsComponent" ]
                   |> Map.ofList
               properties =
                match this.LinkedWebsite with
                | Some linkedWebsite ->
                   box {| name = this.Name.Value
                          Application_Type = "web"
                          ApplicationId = linkedWebsite.Value |}
                | None ->
                   box {| name = this.Name.Value
                          Application_Type = "web" |}
            |} :> _
