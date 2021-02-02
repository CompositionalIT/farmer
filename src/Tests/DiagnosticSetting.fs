module DiagnosticSetting

open Expecto
open Farmer
open Farmer.Arm.Storage
open Farmer.Arm.LogAnalytics
open Farmer.Arm.DiagnosticSetting
open Farmer.Builders
open Microsoft.Azure.Management.OperationalInsights
open Microsoft.Azure.Management.Monitor.Models
open Microsoft.Rest
open System

let dummyClient = new OperationalInsightsManagementClient (Uri "http://management.azure.com", TokenCredentials "NotNullOrWhiteSpace")
let logicAppResource = ResourceType("Microsoft.Logic/workflows", "").resourceId "LogicApp"

let asAzureResource (ws:DiagnosticSettingsConfig) =
    arm { add_resource ws }
    |> findAzureResources<DiagnosticSettingsResource> dummyClient.SerializationSettings
    |> List.head

let tests = testList "Diagnostic Settings" [
    test "Creates diagnostic settings" {
        let storageAccountResourceId = ResourceId.create(storageAccounts, ResourceName "bccrmintegration", "BC_CRM_Integration_POC")
        let workspaceResourceId = workspaces.resourceId "tryw"

        let config =
            diagnosticSettings {
                name "myDiagnosticSetting"
                metrics_source logicAppResource
                add_destination storageAccountResourceId
                add_destination workspaceResourceId
                capture_metrics [ MetricSetting.Create("AllMetrics", 2<Days>, TimeSpan.FromMinutes 1.) ]
                capture_logs [ LogSetting.Create("WorkflowRuntime", 1<Days>) ]
            }
        let result = asAzureResource config

        Expect.equal result.Name "LogicApp/Microsoft.Insights/myDiagnosticSetting" ("Incorrect Name : " + result.Name )
        Expect.equal result.StorageAccountId (storageAccountResourceId.Eval()) "Incorrect StorageAccountId"
        Expect.equal result.WorkspaceId (workspaceResourceId.Eval()) "Incorrect WorkSpaceResourceId"
        Expect.equal result.Metrics.[0].Category "AllMetrics" "Incorrect MetricCategory"
        Expect.equal result.Metrics.[0].RetentionPolicy.Days 2 "Incorrect MetricretentionPeriod"
        Expect.equal result.Metrics.[0].TimeGrain (Nullable (TimeSpan(0,1,0))) "Incorrect MetricRetentionPeriod"
        Expect.equal result.Logs.[0].Category "WorkflowRuntime" "Incorrect LogCategory"
        Expect.equal result.Logs.[0].RetentionPolicy.Days 1 "Incorrect LogRetentionPeriod"
    }
    test "Event hub name can't be specified without the Event hub authorization rule id  " {
       Expect.throws (fun _ ->
           diagnosticSettings {
               name "myDiagnosticSetting"
               metrics_source logicAppResource
               event_hub_destination_name "myeventhubname"
           } |> ignore) (sprintf "Should have thrown an exception for not specifying Event Hub authorization rule id")
    }
    test "Can't create Diagnostic Settings without at least one data sink " {
       Expect.throws (fun _ ->
           diagnosticSettings {
               name "myDiagnosticSetting"
               metrics_source logicAppResource
               capture_logs [ LogSetting.Create "WorkflowRuntime" ]
           } |> ignore) (sprintf "Should have thrown an exception for not specifying at least on data sink")
    }

    test "Can't create test with retention period outside 1 and 365 " {
        for days in [ 0<Days>; 366<Days> ] do
            Expect.throws (fun _ -> LogSetting.Create("", days) |> ignore) (sprintf "Should have thrown for %d" days)
            Expect.throws (fun _ -> MetricSetting.Create("", days) |> ignore) (sprintf "Should have thrown for %d" days)
    }
]
