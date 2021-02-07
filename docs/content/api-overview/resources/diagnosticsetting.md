---
title: " Diagnostic Settings "
date: 2020-12-02T12:10:03 +00:00
chapter: false
weight: 14
---

#### Overview

The Diagnostic Settings builder is used to create diagnostic settings instance to send platform logs and metrics to different destinations

- Diagnostic Settings (`providers/diagnosticSettings`)

#### Builder Keywords

| Keyword          | Purpose                                                                                   |
| ---------------- | ------------------------------------------------------------------------------------------|
| name             |  Sets the name of the Diagnostic Settings(resourceName,parentResourceName).               |
|parentResourceType|Sets the namespace type of the parent resource                                             |
| storageAccountId | Sets the storage Account Id.                                                              |
| serviceBusRuleId | Sets The service bus rule Id of the diagnostic setting.                                   | 
| eventHubAuthorizationRuleId     |   Sets The authorization rule Id for the event hub .                       |
| eventHubName | Sets The name of the event hub. If none is specified, the default event hub will be selected. |
| metrics | Add metric settings to the resource.                                                               |
| logs | Add Log settings to the resource.                                                                     |
| workspaceId | Sets the log analytics workspace id.                                                           |
| logAnalyticsDestinationType |Enable dedicated log analytics.                                                 |

#### MetricSettings object

| Keyword          | Purpose                                                                                                     |
| ---------------- | ------------------------------------------------------------------------------------------------------------|
| category| Sets the Diagnostic Metric category for a resource type.                                                             |
| timeGrain|  Sets the timeGrain of the metric in ISO8601 format.                                                                |
| retentionPolicy| Sets the retention in days for mettric settings. Must be between 1 and 365 days. 0 is selected by default     |

#### LogSettings object

| Keyword          | Purpose                                                                                             |
| ---------------- | ----------------------------------------------------------------------------------------------------|
| category| Sets the Diagnostic Metric category for a resource type.                                                     |
| retentionPolicy| Sets The retention in days for log settings. Must be between 1 and 365 days. 0 is selected by default.|

#### Example

```fsharp
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
    location Location.WestEurope
    add_resource mydiagnosticSetting 
}
```