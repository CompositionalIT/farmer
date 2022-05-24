[<AutoOpen>]
module Farmer.Arm.Insights

open Farmer

let private createComponents version =
    ResourceType("Microsoft.Insights/components", version)

/// Classic AI instance
let components = createComponents "2014-04-01"
/// Workspace-enabled AI instance
let componentsWorkspace = createComponents "2020-02-02"

/// The type of AI instance to create.
type InstanceKind =
    | Classic
    | Workspace of ResourceId
    member this.ResourceType =
        match this with
        | Classic -> components
        | Workspace _ -> componentsWorkspace

type Components =
    { Name: ResourceName
      Location: Location
      LinkedWebsite: ResourceName option
      DisableIpMasking: bool
      SamplingPercentage: int
      InstanceKind: InstanceKind
      Tags: Map<string, string>
      Dependencies: ResourceId Set }
    interface IArmResource with
        member this.ResourceId = components.resourceId this.Name

        member this.JsonModel =
            let tags =
                match this.LinkedWebsite with
                | Some linkedWebsite ->
                    this.Tags.Add(
                        $"[concat('hidden-link:', resourceGroup().id, '/providers/Microsoft.Web/sites/', '{linkedWebsite.Value}')]",
                        "Resource"
                    )
                | None -> this.Tags

            {| this.InstanceKind.ResourceType.Create(this.Name, this.Location, this.Dependencies, tags) with
                kind = "web"
                properties =
                    {| name = this.Name.Value
                       Application_Type = "web"
                       ApplicationId =
                        match this.LinkedWebsite with
                        | Some linkedWebsite -> linkedWebsite.Value
                        | None -> null
                       DisableIpMasking = this.DisableIpMasking
                       SamplingPercentage = this.SamplingPercentage
                       IngestionMode =
                        match this.InstanceKind with
                        | Workspace _ -> "LogAnalytics"
                        | Classic -> null
                       WorkspaceResourceId =
                        match this.InstanceKind with
                        | Workspace resourceId -> resourceId.Eval()
                        | Classic -> null |} |}
