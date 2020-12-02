[<AutoOpen>]
module Farmer.Arm.DiagnosticSetting
open System
open Farmer


type RetentionPolicy={
    Enabled:bool
    Retention_period:int<Days> }

type MetricSettings=
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
    {| category=state.Category
       enabled=state.Enabled
       timeGrain = tryGetIso (state.TimeGrain |> Option.map IsoDateTime.OfTimeSpan )
       retentionPolicy=
       match state.RetentionPolicy with
       | None -> null
       | Some x -> 
        {| enabled=x.Enabled
           days=x.Retention_period
        |} |> box
    
    |}
let logSettingsBuilder (statex:LogSettings) =
        {| category = statex.Category
           enabled = statex.Enabled
           retentionPolicy=
           match statex.RetentionPolicy with
           | None -> null
           | Some x -> 
            {| enabled = x.Enabled
               days = x.Retention_period
            |} |> box
        
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
            {| diagnosticSettingsType(this.ParentResourceType).Create(this.Name,this.Location, tags = this.Tags,dependsOn=this.Dependencies) with
                properties =
                    {| serviceBusRuleId = this.ServiceBusRuleId
                       LogAnalyticsDestinationType = 
                        match this.DedicatedLogAnalyticsDestination with
                        | Some x -> x
                        | None -> null
                       eventHubName = this.EventHubName |> Option.toObj
                       eventHubAuthorizationRuleId = 
                        match this.EventHubAuthorizationRuleId with
                        | None -> null
                        | Some x -> x.Eval()
                       storageAccountId = 
                        match this.StorageAccountId with
                        | None  -> null
                        | Some x -> x.Eval()
                       metrics=this.Metrics 
                       |> Array.map(fun x -> metricSettingsBuilder x) 
                       logs=this.Logs
                       |> Array.map(fun x -> logSettingsBuilder x) 
                       workspaceId = 
                        match this.WorkspaceId with
                        | None  -> null
                        | Some x -> x.Eval() |}
            |} :> _
