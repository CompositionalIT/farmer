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
    ResourceType("Microsoft.Insights/dataCollectionRules", "2023-03-11")

module DataSources =
    type PrometheusForwarder = {
        Name: string
        Streams: string list
    } with

        member this.ToArmJson = {|
            name = this.Name
            streams = this.Streams
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

/// Represents some of the preset streams that ccan be used for data flow in a data collection rule.
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

module Destinations =
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

    type Destination = {
        MonitoringAccounts: (MonitoringAccount list) option
    } with

        static member Default = { MonitoringAccounts = None }

    let ToArmJson (destinations: Destination) = {|
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
    DataSources: DataSources.DataSource option
    Destinations: Destinations.Destination option
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
                            |> Option.map DataSources.ToArmJson
                            |> Option.defaultValue Unchecked.defaultof<_>
                        destinations =
                            this.Destinations
                            |> Option.map Destinations.ToArmJson
                            |> Option.defaultValue Unchecked.defaultof<_>
                    |}
            |}


let dataCollectionRuleAssociations (resourceType: ResourceType) =
    ResourceType($"{resourceType.Type}/providers/dataCollectionRuleAssociations", "2022-06-01")

type DataCollectionRuleAssociation = {
    Name: ResourceName
    AssociatedResource: ResourceId
    RuleId: ResourceId
    Description: string
    Dependencies: Set<ResourceId>
} with

    interface IArmResource with
        member this.ResourceId =
            dataCollectionRuleAssociations(this.AssociatedResource.Type)
                .resourceId (this.Name)

        member this.JsonModel =
            let dependencies =
                [ this.AssociatedResource; this.RuleId ] @ (List.ofSeq this.Dependencies)

            {|
                dataCollectionRuleAssociations(this.AssociatedResource.Type)
                    .Create(this.Name, dependsOn = dependencies) with
                    properties = {|
                        description = this.Description
                        dataCollectionRuleId = this.RuleId.Eval()
                    |}
            |}