#r "./libs/Newtonsoft.Json.dll"
#r "../../src/Farmer/bin/Debug/netstandard2.0/Farmer.dll"
open Farmer
open Farmer.Arm.Storage
open Farmer.Arm.LogAnalytics
open Farmer.Arm.EventHub
open Farmer.Builders
open System
let storageAccountName = ResourceName ("bccrmintegration")
let storageAccountResourceId = ResourceId.create(storageAccounts,storageAccountName,"BC_CRM_Integration_POC")
let workspaceName = ResourceName("tryw")
let workspaceResourceId = ResourceId.create(workspaces,workspaceName)

let myLog = log { 
    category "WorkflowRuntime"
    retention_period 1<Days>
}

let myMetric = metric { 
    category "AllMetrics" 
    retention_period 2<Days>
    time_grain (TimeSpan(0,1,0))
}

let mydiagnosticSetting = diagnosticSettings {
    name  "test" "myDiagnosticSetting"
    parent_resource_type "Microsoft.Logic" "workflows"
    storage_account_id storageAccountResourceId
    work_space_id workspaceResourceId
    metrics [myMetric]
    logs [myLog]
    enable_dedicated_loganalytics
}

let deployment = arm {
    add_resource mydiagnosticSetting
    location Location.NorthEurope
}

deployment
|> Deploy.execute "test-resource-group" Deploy.NoParameters
|> printfn "%A"