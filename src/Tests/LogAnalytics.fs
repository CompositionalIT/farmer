module LogAnalytics

open Expecto
open Farmer
open Farmer.Arm
open Farmer.Builders
open Farmer.Helpers
open Microsoft.Azure.Management.OperationalInsights
open Microsoft.Azure.Management.OperationalInsights.Models
open Microsoft.Rest
open System

let dummyClient = new OperationalInsightsManagementClient (Uri "http://management.azure.com", TokenCredentials "NotNullOrWhiteSpace")

let asAzureResource (ws:WorkspaceConfig) =
    arm { add_resource ws }
    |> findAzureResourcesByType<Workspace> Arm.LogAnalytics.workspaces dummyClient.SerializationSettings
    |> List.head
    |> fun r ->
        r.Validate()
        r

let tests = testList "Log analytics" [
    test "Creates a log analytics workspace" {
        let config =
            logAnalytics {
                name "myFarmer"
                retention_period 30<Days>
                enable_query
                enable_ingestion
            }
        let workspace = asAzureResource config

        Expect.equal workspace.Location "westeurope" "Incorrect Location"
        Expect.equal workspace.Name "myFarmer" "Incorrect Name"
        Expect.equal workspace.PublicNetworkAccessForIngestion "Enabled" "Incorrect IngestionSupport"
        Expect.equal workspace.PublicNetworkAccessForQuery "Enabled" "QuerySupport"
        Expect.equal workspace.Sku.Name "PerGB2018" "Incorrect Sku"
        Expect.equal workspace.RetentionInDays (Nullable 30) "Incorrect Retention In Days"
    }

    test "Ingestion and Query are disabled by default" {
        let workspace = logAnalytics { name "" } |> asAzureResource

        Expect.equal workspace.RetentionInDays (Nullable()) "Retention Period should be off by default"
        Expect.equal workspace.PublicNetworkAccessForQuery null "Query should be off by default"
        Expect.equal workspace.PublicNetworkAccessForIngestion null "Ingestion should be off by default"
    }

    test "Can't create log analytics with retention period outside 30 and 730 " {
        for days in [ 29<Days>; 731<Days> ] do
            Expect.throws (fun _ -> logAnalytics { retention_period days } |> ignore) (sprintf "Should have thrown for %d" days)
    }
]
