[<AutoOpen>]
module Farmer.Arm.DiagnosticSetting

open System
open Farmer

let private (|InBounds|OutOfBounds|) days =
    if days > 365<Days> then OutOfBounds days
    elif days < 1<Days> then OutOfBounds days
    else InBounds days

type RetentionPolicy =
    { Enabled : bool
      RetentionPeriod : int<Days> }
    static member Create (retentionPeriod, ?enabled) =
        match retentionPeriod with
        | OutOfBounds days ->
            failwithf "The retention period must be between 1 and 365 days. It is currently %d." days
        | InBounds _ ->
            { Enabled = defaultArg enabled true
              RetentionPeriod = retentionPeriod }

type MetricSetting =
    { Category : string
      TimeGrain : TimeSpan option
      Enabled : bool
      RetentionPolicy : RetentionPolicy option }
    static member Create (category, ?retentionPeriod, ?timeGrain) =
        { Category = category
          TimeGrain = timeGrain
          Enabled = true
          RetentionPolicy = retentionPeriod |> Option.map (fun days -> RetentionPolicy.Create (days, true)) }

type LogSetting =
    { Category : string
      Enabled : bool
      RetentionPolicy : RetentionPolicy option }
    static member Create (category, ?retentionPeriod) =
        { Category = category
          Enabled = true
          RetentionPolicy = retentionPeriod |> Option.map (fun days -> RetentionPolicy.Create (days, true)) }

let diagnosticSettingsType (parent:ResourceType) =
    ResourceType (parent.Type + "/providers/diagnosticSettings", "2017-05-01-preview")

type DestinationType = AzureDiagnostics | Dedicated

type DiagnosticSettings =
    { Name : ResourceName
      Location : Location
      MetricsSource : ResourceId

      Sinks :
        {| StorageAccount : ResourceId option
           EventHub : {| AuthorizationRuleId : ArmExpression; EventHubName : ResourceName option |} option
           LogAnalyticsWorkspace : (ResourceId * DestinationType) option |}

      Metrics : MetricSetting Set
      Logs : LogSetting Set

      Dependencies : ResourceId Set
      Tags : Map<string, string> }

    interface IArmResource with
        member this.ResourceId = diagnosticSettingsType(this.MetricsSource.Type).resourceId this.Name
        member this.JsonModel =
            {| diagnosticSettingsType(this.MetricsSource.Type)
                .Create(this.MetricsSource.Name/"Microsoft.Insights"/this.Name,this.Location, this.Dependencies, this.Tags) with
                properties =
                    {| LogAnalyticsDestinationType =
                        match this.Sinks.LogAnalyticsWorkspace with
                        | Some (_, Dedicated) -> "Dedicated"
                        | None | Some (_, AzureDiagnostics) -> null
                       eventHubName =
                        this.Sinks.EventHub
                        |> Option.bind(fun hub -> hub.EventHubName |> Option.map(fun r -> r.Value))
                        |> Option.toObj
                       eventHubAuthorizationRuleId =
                        this.Sinks.EventHub
                        |> Option.map(fun hub -> hub.AuthorizationRuleId.Eval())
                        |> Option.toObj
                       storageAccountId = this.Sinks.StorageAccount |> Option.map( fun x -> x.Eval()) |> Option.toObj
                       metrics = [|
                           for metric in this.Metrics do
                            {| category = metric.Category
                               enabled = metric.Enabled
                               timeGrain = metric.TimeGrain |> Option.map (IsoDateTime.OfTimeSpan >> fun v -> v.Value) |> Option.toObj
                               retentionPolicy =
                                metric.RetentionPolicy
                                |> Option.map(fun policy -> box {| enabled = policy.Enabled; days = policy.RetentionPeriod |})
                                |> Option.toObj |}
                       |]
                       logs = [|
                        for log in this.Logs do
                            {| category = log.Category
                               enabled = log.Enabled
                               retentionPolicy =
                                log.RetentionPolicy
                                |> Option.map(fun policy -> box {| enabled = policy.Enabled; days = policy.RetentionPeriod |})
                                |> Option.toObj |}
                       |]
                       workspaceId =
                        this.Sinks.LogAnalyticsWorkspace
                        |> Option.map (fun (resource,_) -> resource.Eval())
                        |> Option.toObj |}
            |} :> _
