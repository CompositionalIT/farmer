[<AutoOpen>]
module Farmer.Arm.Monitor

open Farmer

let dataCollectionEndpoints =
    ResourceType("Microsoft.Insights/dataCollectionEndpoints", "2023-03-11")

type DataCollectionEndpoint = {
    Name: ResourceName
    OsType: OS
    Location: Location
    Tags: Map<string, string>
} with

    interface IArmResource with
        member this.ResourceId = dataCollectionEndpoints.resourceId this.Name

        member this.JsonModel = {|
            dataCollectionEndpoints.Create(this.Name, this.Location, tags = this.Tags) with
                kind = string this.OsType
        |}

let dataCollectionRules =
    ResourceType("Microsoft.Insights/dataCollectionRules", "2023-03-11'")

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

    let ToArmJson (config: DataSource) = {|
        prometheusForwarder =
            (match config.PrometheusForwarder with
             | Some forwarders -> forwarders |> List.map (fun f -> f.ToArmJson)
             | None -> Unchecked.defaultof<_>)
    |}

type Stream =
    | Event
    | InsightsMetrics
    | Perf
    | Syslog
    | WindowsEvent
    | CustomStream of string

    static member Print(stream: Stream) =
        match stream with
        | Event -> "Microsoft-Event"
        | InsightsMetrics -> "Microsoft-InsightsMetrics"
        | Perf -> "Microsoft-Perf"
        | Syslog -> "Microsoft-Syslog"
        | WindowsEvent -> "Microsoft-WindowsEvent"
        | CustomStream name -> name

type DataFlow = {
    Destinations: string list
    Streams: Stream list
} with

    member this.ToArmJson = {|
        destinations = this.Destinations
        streams = this.Streams |> List.map Stream.Print
    |}

module DestinationsConfig =
    type MonitoringAccount = {
        AccountResourceId: ResourceId
        Name: ResourceName
    } with

        static member Default = {
            AccountResourceId = ResourceId.Empty
            Name = ResourceName.Empty
        }

        member this.ToArmJson = {|
            name = this.Name.Value
            accountResourceId = this.AccountResourceId.Eval()
        |}

    type Destinations = {
        MonitoringAccounts: (MonitoringAccount list) option
    } with

        static member Default = { MonitoringAccounts = None }

    let ToArmJson (destinations: Destinations) = {|
        monitoringAccounts =
            destinations.MonitoringAccounts
            |> Option.map (List.map (fun d -> d.ToArmJson))
            |> Option.defaultValue Unchecked.defaultof<_>
    |}

type DataCollectionRule = {
    Name: ResourceName
    OsType: OS
    Location: Location
    Endpoint: ResourceId
    DataFlows: (DataFlow list) option
    DataSources: DataSourceConfig.DataSource option
    Destinations: DestinationsConfig.Destinations option
    Tags: Map<string, string>
    Dependencies: Set<ResourceId>
} with

    interface IArmResource with
        member this.ResourceId = dataCollectionRules.resourceId this.Name

        member this.JsonModel =
            let dependencies = [ this.Endpoint ] @ (List.ofSeq this.Dependencies)

            {|
                dataCollectionRules.Create(this.Name, this.Location, dependencies, this.Tags) with
                    kind = string this.OsType
                    properties = {|
                        dataCollectionEndpointId = this.Endpoint.Eval()
                        dataFlows =
                            this.DataFlows
                            |> Option.map (List.map (fun flow -> flow.ToArmJson))
                            |> Option.defaultValue Unchecked.defaultof<_>
                        dataSources =
                            this.DataSources
                            |> Option.map DataSourceConfig.ToArmJson
                            |> Option.defaultValue Unchecked.defaultof<_>
                        destinations =
                            this.Destinations
                            |> Option.map DestinationsConfig.ToArmJson
                            |> Option.defaultValue Unchecked.defaultof<_>
                    |}
            |}


let dataCollectionRuleAssociations (resourceType: ResourceType) =
    ResourceType($"{resourceType.Type}/providers/dataCollectionRuleAssociations", "2022-06-01")

type DataCollectionRuleAssociation = {
    Name: ResourceName
    LinkedResource: ResourceId
    Location: Location
    RuleId: ResourceId
    Description: string option
} with

    interface IArmResource with
        member this.ResourceId =
            dataCollectionRuleAssociations(this.LinkedResource.Type).resourceId (this.Name)

        member this.JsonModel = {|
            dataCollectionRuleAssociations(this.LinkedResource.Type)
                .Create(this.Name, this.Location) with
                properties = {|
                    description = this.Description
                    dataCollectionRuleId = this.RuleId.Eval()
                |}
                dependsOn = [ this.LinkedResource, this.RuleId ]
        |}