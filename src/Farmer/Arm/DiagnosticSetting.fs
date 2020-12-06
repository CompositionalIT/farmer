[<AutoOpen>]
module Farmer.Arm.DiagnosticSetting
open System
open Farmer


type RetentionPolicy = {
    Enabled:bool
    Retention_period:int<Days> }

type MetricSettings =
    { Category:string
      TimeGrain: TimeSpan  option
      Enabled:bool
      RetentionPolicy:RetentionPolicy option }

type LogSettings =
    { Category:string
      Enabled:bool
      RetentionPolicy:RetentionPolicy option }

let private tryGetIso (v:IsoDateTime option) = v |> Option.map(fun v -> v.Value) |> Option.toObj

let diagnosticSettingsType parentResourceType  = 
    ResourceType(parentResourceType + "providers/diagnosticSettings", "2017-05-01-preview")

let metricSettingsBuilder (state: MetricSettings) =
    {| category = state.Category
       enabled = state.Enabled
       timeGrain = tryGetIso (state.TimeGrain |> Option.map IsoDateTime.OfTimeSpan )
       retentionPolicy =
       match state.RetentionPolicy with
       | None -> null
       | Some x ->
           box
             {| enabled = x.Enabled
                days = x.Retention_period
             |}
    
    |}
let logSettingsBuilder (statex:LogSettings) =
        {| category = statex.Category
           enabled = statex.Enabled
           retentionPolicy =
           match statex.RetentionPolicy with
           | None -> null
           | Some x ->
               box
                 {| enabled = x.Enabled
                    days = x.Retention_period
                 |} 
        
        |}

type DiagnosticSettings =
    { Name : ResourceName
      ParentResourceType : string
      Location : Location
      Dependencies : ResourceId List
      StorageAccountId : ResourceId option 
      ServiceBusRuleId: ResourceId option 
      EventHubAuthorizationRuleId: ResourceId option
      EventHubName : string option
      Metrics : MetricSettings array
      Logs : LogSettings array
      WorkspaceId : ResourceId option
      DedicatedLogAnalyticsDestination : string option
      Tags : Map<string, string> }

    interface IArmResource with
        member this.ResourceId = diagnosticSettingsType(this.ParentResourceType).resourceId this.Name
        member this.JsonModel =
            {| diagnosticSettingsType(this.ParentResourceType).Create(this.Name,this.Location, tags = this.Tags,dependsOn = this.Dependencies) with
                properties =
                    {| serviceBusRuleId = this.ServiceBusRuleId
                       LogAnalyticsDestinationType = this.DedicatedLogAnalyticsDestination |> Option.toObj 
                       eventHubName = this.EventHubName |> Option.toObj
                       eventHubAuthorizationRuleId = this.EventHubAuthorizationRuleId |> Option.map(fun x -> x.Eval()) |> Option.toObj 
                       storageAccountId = this.StorageAccountId |> Option.map( fun x -> x.Eval()) |> Option.toObj
                       metrics = this.Metrics |> Array.map  metricSettingsBuilder  
                       logs = this.Logs |> Array.map  logSettingsBuilder
                       workspaceId = this.WorkspaceId |> Option.map( fun x -> x.Eval()) |> Option.toObj  |}
            |} :> _
