module DiagnosticSetting

open Expecto
open Farmer
open Farmer.Arm.Storage
open Farmer.Arm.LogAnalytics
open Farmer.Builders
open Microsoft.Azure.Management.OperationalInsights
open Microsoft.Azure.Management.Monitor.Models
open Microsoft.Rest
open System

let dummyClient = new OperationalInsightsManagementClient (Uri "http://management.azure.com", TokenCredentials "NotNullOrWhiteSpace")

let asAzureResource (ws:diagnosticSettingsConfig) = 
    arm { add_resource ws }
    |> findAzureResources<DiagnosticSettingsResource> dummyClient.SerializationSettings
    |> List.head

let tests = testList "Diagnostic Settings" [
    test "Creates diagnostic settings" {
        let storageAccountResourceId = ResourceId.create(storageAccounts, ResourceName ("bccrmintegration"), "BC_CRM_Integration_POC")
        let workspaceResourceId = ResourceId.create(workspaces, ResourceName("tryw") )
        
        let myLog = log {
            category "WorkflowRuntime"
            retention_period 1<Days>  }

        let myMetric = metric { 
            category "AllMetrics" 
            retention_period 2<Days>
            time_grain (TimeSpan(0,1,0)) }

        let config =
            diagnosticSettings {
                name  "LogicApp" "myDiagnosticSetting"
                parent_resource_type "Microsoft.Logic" "workflows"
                storage_account_id storageAccountResourceId
                work_space_id workspaceResourceId
                metrics [myMetric]
                logs [myLog]
                
            }
        let result = asAzureResource config

        Expect.equal result.Name  "LogicApp/Microsoft.Insights/myDiagnosticSetting" ("Incorrect Name : " + result.Name )
        Expect.equal result.StorageAccountId (storageAccountResourceId.Eval()) "Incorrect StorageAccountId"
        Expect.equal result.WorkspaceId (workspaceResourceId.Eval()) "Incorrect WorkSpaceResourceId"
        Expect.equal result.Metrics.[0].Category "AllMetrics" "Incorrect MetricCategory"
        Expect.equal result.Metrics.[0].RetentionPolicy.Days 2 "Incorrect MetricretentionPeriod"
        Expect.equal result.Metrics.[0].TimeGrain (Nullable (TimeSpan(0,1,0))) "Incorrect MetricRetentionPeriod"
        Expect.equal result.Logs.[0].Category "WorkflowRuntime" "Incorrect LogCategory"
        Expect.equal result.Logs.[0].RetentionPolicy.Days 1 "Incorrect LogRetentionPeriod"
    }

    test "Can't create Diagnostic Settings without at least one data sink " {
       let myLog = log { category "WorkflowRuntime"}

       Expect.throws (fun _ -> 
           diagnosticSettings {
               name  "LogicApp" "myDiagnosticSetting"
               parent_resource_type "Microsoft.Logic" "workflows"
               logs [myLog]
           } |> ignore) (sprintf "Should have thrown an exception for not specifying at least on data sink") 
    }

    test "Can't create test with retention period outside 1 and 365 " {
        for days in [ 0<Days>; 366<Days> ] do
            Expect.throws (fun _ -> log { retention_period days } |> ignore) (sprintf "Should have thrown for %d" days)
            Expect.throws (fun _ -> metric { retention_period days } |> ignore) (sprintf "Should have thrown for %d" days)
    }
]
