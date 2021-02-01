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
    static member Create(category, ?retentionPeriod, ?timeGrain) =
        { Category = category
          TimeGrain = timeGrain
          Enabled = true
          RetentionPolicy = retentionPeriod |> Option.map(fun days -> RetentionPolicy.Create(days, true)) }

type LogSetting =
    { Category : string
      Enabled : bool
      RetentionPolicy : RetentionPolicy option }
    static member Create(category, ?retentionPeriod) =
        { Category = category
          Enabled = true
          RetentionPolicy = retentionPeriod |> Option.map(fun days -> RetentionPolicy.Create(days, true)) }

let diagnosticSettingsType (parent:ResourceType) =
    ResourceType(parent.Type + "/providers/diagnosticSettings", "2017-05-01-preview")

type DiagnosticSettings =
    { Name : ResourceName
      ParentResource : ResourceId
      Location : Location
      StorageAccountId : ResourceId option
      ServiceBusRuleId: ResourceId option
      EventHubAuthorizationRuleId: ResourceId option
      EventHubName : string option
      Metrics : MetricSetting list
      Logs : LogSetting list
      WorkspaceId : ResourceId option
      DedicatedLogAnalyticsDestination : string option
      Dependencies : ResourceId Set
      Tags : Map<string, string> }

    interface IArmResource with
        member this.ResourceId = diagnosticSettingsType(this.ParentResource.Type).resourceId this.Name
        member this.JsonModel =
            {| diagnosticSettingsType(this.ParentResource.Type).Create(this.ParentResource.Name/"Microsoft.Insights"/this.Name,this.Location, tags = this.Tags,dependsOn = this.Dependencies) with
                properties =
                    {| serviceBusRuleId = this.ServiceBusRuleId
                       LogAnalyticsDestinationType = this.DedicatedLogAnalyticsDestination |> Option.toObj
                       eventHubName = this.EventHubName |> Option.toObj
                       eventHubAuthorizationRuleId = this.EventHubAuthorizationRuleId |> Option.map(fun x -> x.Eval()) |> Option.toObj
                       storageAccountId = this.StorageAccountId |> Option.map( fun x -> x.Eval()) |> Option.toObj
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
                       workspaceId = this.WorkspaceId |> Option.map( fun x -> x.Eval()) |> Option.toObj |}
            |} :> _
