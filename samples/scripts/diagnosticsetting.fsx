#r "./libs/Newtonsoft.Json.dll"
#r "../../src/Farmer/bin/Debug/netstandard2.0/Farmer.dll"
open Farmer
open Farmer.Arm.Storage
open Farmer.Arm.LogAnalytics
open Farmer.Arm.DiagnosticSetting
open Farmer.Builders
open System

let storageAccountResource = { storageAccounts.resourceId("bccrmintegration") with ResourceGroup  = Some "BC_CRM_Integration_POC" }
let logAnalyticsResource = workspaces.resourceId "tryw"
let logicAppResource = ResourceId.create(ResourceType ("Microsoft.Logic/workflows",""),ResourceName ("Logicapp"))

let mydiagnosticSetting = diagnosticSettings {
    name "myDiagnosticSetting"
    metrics_source logicAppResource

    storage_account storageAccountResource
    log_analytics_workspace logAnalyticsResource
    enable_dedicated_loganalytics

    metrics [
        MetricSetting.Create("AllMetrics", retentionPeriod = 2<Days>, timeGrain = TimeSpan.FromMinutes 1.)
    ]

    logs [
        LogSetting.Create("WorkflowRuntime", retentionPeriod = 1<Days>)
    ]
}

let deployment = arm {
    add_resource mydiagnosticSetting
    location Location.NorthEurope
}

deployment
|> Writer.quickWrite "diagnostics"
|> printfn "%A"