[<AutoOpen>]
module Farmer.Arm.DiagnosticSetting

open Farmer
open Farmer.DiagnosticSettings

let diagnosticSettingsType (parent: ResourceType) =
    ResourceType(parent.Type + "/providers/diagnosticSettings", "2017-05-01-preview")

type SinkInformation =
    {
        StorageAccount: ResourceId option
        EventHub: {| AuthorizationRuleId: ResourceId
                     EventHubName: ResourceName option |} option
        LogAnalyticsWorkspace: (ResourceId * LogAnalyticsDestination) option
    }

type DiagnosticSettings =
    {
        Name: ResourceName
        Location: Location
        MetricsSource: ResourceId
        Sinks: SinkInformation
        Metrics: MetricSetting Set
        Logs: LogSetting Set
        Dependencies: ResourceId Set
        Tags: Map<string, string>
    }

    interface IArmResource with
        member this.ResourceId =
            diagnosticSettingsType(this.MetricsSource.Type).resourceId this.Name

        member this.JsonModel =
            let resourceName =
                [
                    this.MetricsSource.Name
                    yield! this.MetricsSource.Segments
                    ResourceName "Microsoft.Insights"
                    this.Name
                ]
                |> List.reduce (/)

            {| diagnosticSettingsType(this.MetricsSource.Type)
                   .Create(resourceName, this.Location, this.Dependencies, this.Tags) with
                properties =
                    {|
                        LogAnalyticsDestinationType =
                            match this.Sinks.LogAnalyticsWorkspace with
                            | Some (_, Dedicated) -> "Dedicated"
                            | None
                            | Some (_, AzureDiagnostics) -> null
                        eventHubName =
                            this.Sinks.EventHub
                            |> Option.bind (fun hub -> hub.EventHubName |> Option.map (fun r -> r.Value))
                            |> Option.toObj
                        eventHubAuthorizationRuleId =
                            this.Sinks.EventHub
                            |> Option.map (fun hub -> hub.AuthorizationRuleId.Eval())
                            |> Option.toObj
                        storageAccountId = this.Sinks.StorageAccount |> Option.map (fun x -> x.Eval()) |> Option.toObj
                        metrics =
                            [|
                                for metric in this.Metrics do
                                    {|
                                        category = metric.Category
                                        enabled = metric.Enabled
                                        timeGrain =
                                            metric.TimeGrain
                                            |> Option.map (IsoDateTime.OfTimeSpan >> fun v -> v.Value)
                                            |> Option.toObj
                                        retentionPolicy =
                                            metric.RetentionPolicy
                                            |> Option.map (fun policy ->
                                                box
                                                    {|
                                                        enabled = policy.Enabled
                                                        days = policy.RetentionPeriod
                                                    |})
                                            |> Option.toObj
                                    |}
                            |]
                        logs =
                            [|
                                for log in this.Logs do
                                    {|
                                        category = log.Category.Value
                                        enabled = log.Enabled
                                        retentionPolicy =
                                            log.RetentionPolicy
                                            |> Option.map (fun policy ->
                                                box
                                                    {|
                                                        enabled = policy.Enabled
                                                        days = policy.RetentionPeriod
                                                    |})
                                            |> Option.toObj
                                    |}
                            |]
                        workspaceId =
                            this.Sinks.LogAnalyticsWorkspace
                            |> Option.map (fun (resource, _) -> resource.Eval())
                            |> Option.toObj
                    |}
            |}
