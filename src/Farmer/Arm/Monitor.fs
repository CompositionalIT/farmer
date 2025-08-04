[<AutoOpen>]
module Farmer.Arm.Monitor

open Farmer

let private dataCollectionEndpoints =
    ResourceType("Microsoft.Insights/dataCollectionEndpoints", "2022-06-01")

type DataCollectionEndpoint = {
    Name: ResourceName
    OsType: OS
    Location: Location
} with

    interface IArmResource with
        member this.ResourceId = dataCollectionEndpoints.resourceId this.Name

        member this.JsonModel = {|
            dataCollectionEndpoints.Create(this.Name) with
                kind = this.OsType
                location = this.Location
        |}

let private dataCollectionRules =
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
    DataSources: DataSourceConfig.DataSource option
} with

    interface IArmResource with
        member this.ResourceId = dataCollectionRules.resourceId this.Name

        member this.JsonModel = {|
            dataCollectionRules.Create(this.Name) with
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


let private dataCollectionRuleAssociations (resourceType: ResourceType) =
    ResourceType($"{resourceType.Type}/providers/dataCollectionRuleAssociations", "2022-06-01")

type DataCollectionRuleAssociation = {
    Name: ResourceName
    AssociationResourceId: ResourceId
    Location: Location
    RuleId: ResourceId
    Description: string option
} with

    interface IArmResource with
        member this.ResourceId =
            dataCollectionRuleAssociations(this.AssociationResourceId.Type)
                .resourceId (ResourceName this.Name.Value)

        member this.JsonModel = {|
            dataCollectionRuleAssociations(this.AssociationResourceId.Type)
                .Create(ResourceName this.Name.Value) with
                location = this.Location.ArmValue
                properties = {|
                    description = this.Description
                    dataCollectionRuleId = this.RuleId.Eval()
                |}
                dependsOn = [ this.AssociationResourceId, this.RuleId ]
        |}