[<AutoOpen>]
module Farmer.Arm.Insights

open Farmer

let private createComponents version =
    ResourceType("Microsoft.Insights/components", version)

let dataCollectionEndpoints = ResourceType("Microsoft.Insights/dataCollectionEndpoints", "2023-03-11")

let dataCollectionRules = ResourceType("Microsoft.Insights/dataCollectionRules", "2023-03-11")

/// Classic AI instance
let components = createComponents "2014-04-01"
/// Workspace-enabled AI instance
let componentsWorkspace = createComponents "2020-02-02"

/// The type of AI instance to create.
type InstanceKind =
    | Classic
    | Workspace of workspace: ResourceId

    member this.ResourceType =
        match this with
        | Classic -> components
        | Workspace _ -> componentsWorkspace

type Components = {
    Name: ResourceName
    Location: Location
    LinkedWebsite: ResourceName option
    DisableIpMasking: bool
    SamplingPercentage: int
    InstanceKind: InstanceKind
    Tags: Map<string, string>
    Dependencies: ResourceId Set
} with

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

            {|
                this.InstanceKind.ResourceType.Create(this.Name, this.Location, this.Dependencies, tags) with
                    kind = "web"
                    properties = {|
                        name = this.Name.Value
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
                            | Classic -> null
                    |}
            |}

type DataCollectionEndpoint = {
    Name: ResourceName
    Location: Location
} with
    interface IArmResource with
        member this.ResourceId = dataCollectionEndpoints.resourceId this.Name
        member this.JsonModel = {|
            dataCollectionEndpoints.Create(this.Name, this.Location) with properties = {||}
        |}

type DataFlow = {
    Streams : string list
    Destinations : string list
    TransformKQL : string option
    OutputStream : string option
}

type DataCollectionRule = {
    Name: ResourceName
    Location: Location
    DceResourceId : ResourceId
    LogAnalyticsWorkspaceResourceId : ResourceId option
    StreamDeclarations : Map<string, Column list>
    DataSources : Map<string, Map<string, string> list>
    Destinations : Map<string, Map<string, string> list>
    DataFlows : DataFlow list
    Tags: Map<string, string>
} with
    interface IArmResource with
        member this.ResourceId = dataCollectionRules.resourceId this.Name
        member this.JsonModel =
            let deps =
                [
                    this.DceResourceId
                    match this.LogAnalyticsWorkspaceResourceId with
                    | Some logging -> logging
                    | None -> ()
                ]
            {|
                dataCollectionRules.Create(this.Name, this.Location, dependsOn = deps, tags = this.Tags) with
                    properties = {|
                        dataCollectionEndpointId = this.DceResourceId.Eval()
                        streamDeclarations =
                            this.StreamDeclarations
                            |> Map.map (fun _ columns ->
                                {|
                                    columns =
                                        columns
                                        |> List.map (fun col ->
                                            {|
                                                name = col.Name
                                                ``type`` = col.Type
                                            |}
                                        )
                                |}
                            )
                        dataSources = this.DataSources
                        destinations = this.Destinations
                        dataFlows =
                            this.DataFlows
                            |> List.map (fun flow ->
                                {|
                                    streams = flow.Streams
                                    destinations = flow.Destinations
                                    transformKql = flow.TransformKQL |> Option.defaultValue "source"
                                    outputStream = flow.OutputStream |> Option.toObj
                                |}
                            )
                    |}
            |}