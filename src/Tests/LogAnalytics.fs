module LogAnalytics

open Expecto
open Farmer
open Farmer.Arm
open Farmer.Builders
open Microsoft.Azure.Management.OperationalInsights
open Microsoft.Azure.Management.OperationalInsights.Models
open Microsoft.Rest
open System
open Newtonsoft.Json.Linq

let dummyClient =
    new OperationalInsightsManagementClient(Uri "http://management.azure.com", TokenCredentials "NotNullOrWhiteSpace")

let asAzureResource (ws: WorkspaceConfig) =
    arm { add_resource ws }
    |> findAzureResources<Workspace> dummyClient.SerializationSettings
    |> List.head
    |> fun r ->
        r.Validate()
        r

let tests =
    testList "Log analytics" [
        test "Creates a log analytics workspace" {
            let config = logAnalytics {
                name "myFarmer"
                retention_period 30<Days>
                enable_query
                enable_ingestion
            }

            let workspace = asAzureResource config

            Expect.equal workspace.Location "[resourceGroup().location]" "Incorrect Location"
            Expect.equal workspace.Name "myFarmer" "Incorrect Name"
            Expect.equal workspace.PublicNetworkAccessForIngestion "Enabled" "Incorrect IngestionSupport"
            Expect.equal workspace.PublicNetworkAccessForQuery "Enabled" "QuerySupport"
            Expect.equal workspace.Sku.Name "PerGB2018" "Incorrect Sku"
            Expect.equal workspace.RetentionInDays (Nullable 30) "Incorrect Retention In Days"
        }

        test "Table created under workspace resource" {
            let logging = logAnalytics {
                name "log-analytics"

                custom_tables [
                    {
                        Name = ResourceName "MyTable"
                        Plan = Analytics(Some 1<Days>)
                        Columns = [
                            {
                                Name = "TimeGenerated"
                                Type = ColumnType.DateTime
                            }
                            {
                                Name = "Event"
                                Type = ColumnType.Dynamic
                            }
                        ]
                        TotalRetentionInDays = Some 2<Days>
                    }
                ]
            }

            let deployment = arm { add_resource logging }

            let table =
                deployment.Template.Resources
                |> List.tryFind (fun r ->
                    r.ResourceId.Name.Value = "log-analytics"
                    && not (r.ResourceId.Segments |> List.isEmpty)
                    && (r.ResourceId.Segments |> List.exactlyOne).Value = "MyTable_CL")
                |> Option.map (fun t -> t :?> Farmer.Arm.LogAnalytics.Table)

            Expect.equal (table.Value.Columns.Length) 2 "Incorrect number of columns in table"
            Expect.equal (table.Value.Columns[0].Name) "TimeGenerated" "Incorrect first column name"
            Expect.equal (table.Value.Columns[0].Type) ColumnType.DateTime "Incorrect first column type"
            Expect.equal (table.Value.Columns[1].Name) "Event" "Incorrect second column name"
            Expect.equal (table.Value.Columns[1].Type) ColumnType.Dynamic "Incorrect second column type"
            Expect.equal (table.Value.TotalRetentionInDays) (Some 2<Days>) "Incorrect total retention in days"
            Expect.equal (table.Value.Plan.ArmValue) "Analytics" "Incorrect plan type"
            Expect.equal (table.Value.Plan.RetentionInDays) (Some 1<Days>) "Incorrect plan retention in days"

            Expect.equal
                (table.Value.LogAnalyticsWorkspace.Name.Value)
                "log-analytics"
                "Incorrect workspace name in table resource"
        }

        test "Table JSON emitted correctly" {
            let logging = logAnalytics {
                name "log-analytics"

                custom_tables [
                    {
                        Name = ResourceName "MyTable"
                        Plan = Analytics(Some 1<Days>)
                        Columns = [
                            {
                                Name = "TimeGenerated"
                                Type = ColumnType.DateTime
                            }
                            {
                                Name = "Event"
                                Type = ColumnType.Dynamic
                            }
                        ]
                        TotalRetentionInDays = Some 2<Days>
                    }
                ]
            }

            let deployment = arm { add_resource logging }
            let jsonTemplate = deployment.Template |> Writer.toJson
            let jobj = JObject.Parse jsonTemplate
            let tableJson = jobj["resources"][1]
            Expect.equal (tableJson["type"] |> string) LogAnalytics.tables.Type "Incorrect resource type"
            Expect.equal (tableJson["apiVersion"] |> string) LogAnalytics.tables.ApiVersion "Incorrect api version"

            Expect.equal
                (tableJson["dependsOn"][0] |> string)
                "[resourceId('Microsoft.OperationalInsights/workspaces', 'log-analytics')]"
                "Incorrect dependsOn"

            Expect.equal (tableJson["name"] |> string) "log-analytics/MyTable_CL" "Incorrect resource name"
            Expect.equal (tableJson["properties"]["plan"] |> string) "Analytics" "Incorrect plan type"
            Expect.equal (tableJson["properties"]["retentionInDays"] |> int) 1 "Incorrect plan retention in days"
            Expect.equal (tableJson["properties"]["totalRetentionInDays"] |> int) 2 "Incorrect total retention in days"
            Expect.equal (tableJson["properties"].["schema"].["name"] |> string) "MyTable_CL" "Incorrect table name"
            let columns = tableJson["properties"].["schema"].["columns"]
            Expect.equal (columns.[0]["name"] |> string) "TimeGenerated" "Incorrect first column name"
            Expect.equal (columns.[0]["type"] |> string) "datetime" "Incorrect first column type"
            Expect.equal (columns.[1]["name"] |> string) "Event" "Incorrect second column name"
            Expect.equal (columns.[1]["type"] |> string) "dynamic" "Incorrect second column type"
        }

        test "Ingestion and Query are disabled by default" {
            let workspace = logAnalytics { name "" } |> asAzureResource

            Expect.equal workspace.RetentionInDays (Nullable()) "Retention Period should be off by default"
            Expect.equal workspace.PublicNetworkAccessForQuery null "Query should be off by default"
            Expect.equal workspace.PublicNetworkAccessForIngestion null "Ingestion should be off by default"
        }

        test "Can't create log analytics with retention period outside 30 and 730 " {
            for days in [ 29<Days>; 731<Days> ] do
                Expect.throws
                    (fun _ -> logAnalytics { retention_period days } |> ignore)
                    (sprintf "Should have thrown for %d" days)
        }
    ]