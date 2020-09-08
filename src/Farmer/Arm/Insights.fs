[<AutoOpen>]
module Farmer.Arm.Insights

open Farmer
open Farmer.CoreTypes
open Farmer.WebApp

let components = ResourceType "Microsoft.Insights/components"

type Components =
    { Name : ResourceName<AppInsightsName>
      Location : Location
      LinkedWebsite : ResourceName<WebAppName> option
      Tags: Map<string,string> }
    interface IArmResource with
        member this.ResourceName = this.Name.Untyped
        member this.JsonModel =
            {| ``type`` = components.ArmValue
               kind = "web"
               name = this.Name.Value
               location = this.Location.ArmValue
               apiVersion = "2014-04-01"
               tags =
                   [ match this.LinkedWebsite with
                     | Some linkedWebsite -> sprintf "[concat('hidden-link:', resourceGroup().id, '/providers/Microsoft.Web/sites/', '%s')]" linkedWebsite.Value, "Resource"
                     | None -> ()
                     "displayName", "AppInsightsComponent" ]
                   |> List.fold (fun map (key,value) -> Map.add key value map ) this.Tags
               properties =
                {| name = this.Name.Value
                   Application_Type = "web"
                   ApplicationId =
                     match this.LinkedWebsite with
                     | Some linkedWebsite -> linkedWebsite.Value
                     | None -> null |}
            |} :> _