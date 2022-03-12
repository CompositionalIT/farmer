[<AutoOpen>]
module Farmer.Arm.Insights

open Farmer

let components = ResourceType("Microsoft.Insights/components", "2014-04-01")
let componentsWorkspace = ResourceType("Microsoft.Insights/components", "2020-02-02")

/// The type of AI instance to create.
type ComponentsType =
    | Classic
    | Workspace of ResourceId
    member this.ComponentsType =
        match this with
        | Classic -> components
        | Workspace _ -> componentsWorkspace

type Components =
    { Name : ResourceName
      Location : Location
      LinkedWebsite : ResourceName option
      DisableIpMasking : bool
      SamplingPercentage : int
      Type : ComponentsType
      Tags: Map<string,string>
      Dependencies : ResourceId Set }
    interface IArmResource with
        member this.ResourceId = components.resourceId this.Name
        member this.JsonModel =
            let tags =
                match this.LinkedWebsite with
                | Some linkedWebsite -> this.Tags.Add($"[concat('hidden-link:', resourceGroup().id, '/providers/Microsoft.Web/sites/', '{linkedWebsite.Value}')]", "Resource")
                | None -> this.Tags
            {| this.Type.ComponentsType.Create(this.Name, this.Location, this.Dependencies, tags) with
                kind = "web"
                properties =
                    {|
                        name = this.Name.Value
                        Application_Type = "web"
                        ApplicationId =
                            match this.LinkedWebsite with
                            | Some linkedWebsite -> linkedWebsite.Value
                            | None -> null
                        DisableIpMasking = this.DisableIpMasking
                        SamplingPercentage = this.SamplingPercentage
                        IngestionMode =
                            match this.Type with
                            | Workspace _ -> "LogAnalytics"
                            | Classic -> null
                        WorkspaceResourceId =
                            match this.Type with
                            | Workspace resourceId -> resourceId.Eval()
                            | Classic -> null |}
            |}