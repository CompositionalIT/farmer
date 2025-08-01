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

let private createDataCollectionEndpoint =
    ResourceType("Microsoft.Insights/dataCollectionEndpoints", "2022-06-01")

type DataCollectionEndpoint = {
    Name: ResourceName
    OsType: OS
    Location: Location
    Dependencies: ResourceId Set
} with

    interface IArmResource with
        member this.ResourceId = createDataCollectionEndpoint.resourceId this.Name

        member this.JsonModel = {|
            createDataCollectionEndpoint.Create(this.Name, dependsOn = this.Dependencies) with
                kind = this.OsType
                location = this.Location
        |}

let private createDataCollectionRules =
    ResourceType("Microsoft.Insights/dataCollectionRules", "2022-06-01")

type MonitoringAccount = {
    AccountResourceId: ResourceId
    Name: ResourceName
}

module DataSourceConfig =
    type PrometheusForwarder = {
        Name: string
        Streams: string list
        LabelIncludeFilter: obj option
    } with

        member this.ToArmJson = {|
            name = this.Name
            streams = this.Streams
            labelIncludeFilter =
                match this.LabelIncludeFilter with
                | Some filter -> filter
                | None -> Unchecked.defaultof<_>
        |}

    type DataSource = {
        PrometheusForwarder: (PrometheusForwarder list) option
    } with

        static member Default = { PrometheusForwarder = None }

    let toArmJson (config: DataSource) = {|
        prometheusForwarder =
            (match config.PrometheusForwarder with
             | Some forwarders -> forwarders |> List.map (fun f -> f.ToArmJson)
             | None -> Unchecked.defaultof<_>)
    |}


type DataCollectionRule = {
    Name: ResourceName
    OsType: OS
    Location: Location
    Endpoint: ResourceId
    MonitoringAccounts: MonitoringAccount list
    Streams: string list
    MetricLabelsAllowList: string option
    MetricAnnotationsAllowList: string option
    Dependencies: ResourceId Set
    DataSources: DataSourceConfig.DataSource option
} with

    interface IArmResource with
        member this.ResourceId = createDataCollectionRules.resourceId this.Name

        member this.JsonModel =
            let depends = this.Dependencies + Set [ this.Endpoint ]

            {|
                createDataCollectionRules.Create(this.Name, dependsOn = depends) with
                    kind = this.OsType
                    location = this.Location.ArmValue
                    properties = {|
                        dataCollectionEndpointId = this.Endpoint.Eval()
                        dataFlows = [
                            {|
                                destinations = (this.MonitoringAccounts |> List.map (fun d -> d.Name))
                                streams = this.Streams
                            |}
                        ]
                        dataSources =
                            this.DataSources
                            |> Option.map DataSourceConfig.toArmJson
                            |> Option.defaultValue Unchecked.defaultof<_>
                        destinations = {|
                            monitoringAccounts =
                                this.MonitoringAccounts
                                |> List.map (fun d -> {|
                                    name = d.Name
                                    accountResourceId = d.AccountResourceId.Eval()
                                |})
                        |}
                    |}
            |}