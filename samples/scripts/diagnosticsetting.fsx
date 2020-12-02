#r "./libs/Newtonsoft.Json.dll"
#r "../../src/Farmer/bin/Debug/netstandard2.0/Farmer.dll"
open Farmer
open Farmer.Builders
open System

let storageAccountResourceType=ResourceType("Microsoft.Storage/storageAccounts","")
let storageAccountName=ResourceName ("bccrmintegration")
let storageAccountResourceId=ResourceId.create(storageAccountResourceType,storageAccountName,"BC_CRM_Integration_POC")
let workspaceResourceType=ResourceType("Microsoft.OperationalInsights/workspaces","")
let workspaceName=ResourceName("tryw")
let workspaceResourceId=ResourceId.create(workspaceResourceType,workspaceName)

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