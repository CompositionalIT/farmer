module DiagnosticSettings

open Expecto
open Farmer
open Farmer.Arm.Storage
open Farmer.Arm.LogAnalytics
open Farmer.Arm.EventHub
open Farmer.DiagnosticSettings
open Farmer.Builders
open Microsoft.Azure.Management.OperationalInsights
open Microsoft.Azure.Management.Monitor.Models
open Microsoft.Rest
open System

let dummyClient =
    new OperationalInsightsManagementClient(Uri "http://management.azure.com", TokenCredentials "NotNullOrWhiteSpace")

let logicAppResource =
    ResourceType("Microsoft.Logic/workflows", "").resourceId "LogicApp"

let asAzureResource (ws: DiagnosticSettingsConfig) =
    arm { add_resource ws }
    |> findAzureResources<DiagnosticSettingsResource> dummyClient.SerializationSettings
    |> List.head

let tests =
    testList
        "Diagnostic Settings"
        [
            test "Creates diagnostic settings with raw sinks" {
                let storageAccount =
                    ResourceId.create (storageAccounts, ResourceName "storagename", "storage-rg")

                let workspace = workspaces.resourceId "workspacename"

                let eventHub =
                    Namespaces.authorizationRules.resourceId (
                        ResourceName "eventhubns",
                        ResourceName "RootManageSharedAccessKey"
                    )

                let config =
                    diagnosticSettings {
                        name "myDiagnosticSetting"
                        metrics_source logicAppResource
                        add_destination storageAccount
                        add_destination workspace
                        add_destination eventHub
                        capture_metrics [ MetricSetting.Create("AllMetrics", 2<Days>, TimeSpan.FromMinutes 1.) ]
                        capture_logs [ LogSetting.Create("WorkflowRuntime", 1<Days>) ]
                    }

                let result = asAzureResource config

                Expect.equal
                    result.Name
                    "LogicApp/Microsoft.Insights/myDiagnosticSetting"
                    ("Incorrect Name : " + result.Name)

                Expect.equal result.StorageAccountId (storageAccount.Eval()) "Incorrect StorageAccount ResourceId"
                Expect.equal result.WorkspaceId (workspace.Eval()) "Incorrect Workspace ResourceId"
                Expect.equal result.EventHubAuthorizationRuleId (eventHub.Eval()) "Incorrect Event Hub Auth Rule"
                Expect.equal result.Metrics.[0].Category "AllMetrics" "Incorrect MetricCategory"
                Expect.equal result.Metrics.[0].RetentionPolicy.Days 2 "Incorrect MetricretentionPeriod"

                Expect.equal
                    result.Metrics.[0].TimeGrain
                    (Nullable(TimeSpan(0, 1, 0)))
                    "Incorrect MetricRetentionPeriod"

                Expect.equal result.Logs.[0].Category "WorkflowRuntime" "Incorrect LogCategory"
                Expect.equal result.Logs.[0].RetentionPolicy.Days 1 "Incorrect LogRetentionPeriod"
            }

            test "Event hub name can't be specified without the Event hub authorization rule id" {
                Expect.throws
                    (fun _ ->
                        diagnosticSettings {
                            metrics_source logicAppResource
                            event_hub_destination_name "myeventhubname"
                        }
                        |> ignore)
                    (sprintf "Should have thrown an exception for not specifying Event Hub authorization rule id")
            }
            test "Event hub name is set correctly" {
                let settings =
                    diagnosticSettings {
                        metrics_source logicAppResource

                        add_destination (
                            Namespaces.authorizationRules.resourceId (
                                ResourceName "eventhubns",
                                ResourceName "RootManageSharedAccessKey"
                            )
                        )

                        event_hub_destination_name "myeventhubname"
                        capture_logs [ LogSetting.Create "WorkflowRuntime" ]
                    }

                let result = asAzureResource settings
                Expect.equal result.EventHubName "myeventhubname" "Incorrect event hub name"
            }
            test "Works with Farmer resources" {
                let storageAccount = storageAccount { name "foo" }
                let workspace = logAnalytics { name "logs" }

                let eventHub =
                    eventHub {
                        name "hub"
                        namespace_name "ns"
                    }

                let config =
                    diagnosticSettings {
                        add_destination storageAccount
                        add_destination workspace
                        add_destination eventHub
                        capture_logs [ LogSetting.Create "WorkflowRuntime" ]
                    }

                let result = asAzureResource config

                Expect.equal
                    result.StorageAccountId
                    (storageAccount.ResourceId.Eval())
                    "Incorrect StorageAccount ResourceId"

                Expect.equal
                    result.WorkspaceId
                    ((workspace :> IBuilder).ResourceId.Eval())
                    "Incorrect Workspace ResourceId"

                Expect.equal
                    result.EventHubAuthorizationRuleId
                    (eventHub.DefaultAuthorizationRule.Eval())
                    "Incorrect Event Hub Auth Rule"

                Expect.equal result.EventHubName eventHub.Name.Value "Incorrect Event Hub Name"
            }

            test "Can't create Diagnostic Settings without at least one data sink" {
                Expect.throws
                    (fun _ ->
                        diagnosticSettings {
                            name "myDiagnosticSetting"
                            metrics_source logicAppResource
                            capture_logs [ LogSetting.Create "WorkflowRuntime" ]
                        }
                        |> ignore)
                    "Should have thrown an exception for not specifying at least on data sink"
            }

            test "Can't create test with retention period outside 1 and 365" {
                for days in [ 0<Days>; 366<Days> ] do
                    Expect.throws
                        (fun _ -> LogSetting.Create("", days) |> ignore)
                        (sprintf "Should have thrown for %d" days)

                    Expect.throws
                        (fun _ -> MetricSetting.Create("", days) |> ignore)
                        (sprintf "Should have thrown for %d" days)
            }

            test "Supports segmented names such as SQL databases" {
                let config =
                    let storageAccount = storageAccount { name "foo" }

                    diagnosticSettings {
                        name "myDiagnosticSetting"
                        add_destination storageAccount

                        metrics_source (
                            Arm.Sql.databases.resourceId (ResourceName "sqlserver", ResourceName "sqldatabase")
                        )

                        capture_logs [ Logging.Sql.Servers.Databases.AutomaticTuning ]
                    }

                let result = asAzureResource config
                Expect.equal result.Name "sqlserver/sqldatabase/Microsoft.Insights/myDiagnosticSetting" "Incorrect Name"
            }
        ]
