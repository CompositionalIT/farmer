[<AutoOpen>]
module Farmer.Arm.Insights

open Farmer

let components = ResourceType("Microsoft.Insights/components", "2014-04-01")

type Components =
    { Name : ResourceName
      Location : Location
      LinkedWebsite : ResourceName option
      DisableIpMasking : bool
      SamplingPercentage : int
      Tags: Map<string,string> }
    interface IArmResource with
        member this.ResourceId = components.resourceId this.Name
        member this.JsonModel =
            let tags =
                match this.LinkedWebsite with
                | Some linkedWebsite -> this.Tags.Add($"[concat('hidden-link:', resourceGroup().id, '/providers/Microsoft.Web/sites/', '{linkedWebsite.Value}')]", "Resource")
                | None -> this.Tags

            {| components.Create(this.Name, this.Location, tags = tags) with
                 kind = "web"
                 properties =
                  {| name = this.Name.Value
                     Application_Type = "web"
                     ApplicationId =
                       match this.LinkedWebsite with
                       | Some linkedWebsite -> linkedWebsite.Value
                       | None -> null
                     DisableIpMasking = this.DisableIpMasking
                     SamplingPercentage = this.SamplingPercentage |}
            |} :> _