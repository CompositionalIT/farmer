module LogAnalytics

open Expecto
open Farmer
open Farmer.Arm
open Farmer.Builders
open Farmer.CoreTypes
open Farmer.Helpers
open Farmer.LogAnalytics
open Microsoft.Azure.Management.OperationalInsights
open Microsoft.Azure.Management.OperationalInsights.Models
open Microsoft.Rest
open System

let dummyClient = new OperationalInsightsManagementClient (Uri "http://management.azure.com", TokenCredentials "NotNullOrWhiteSpace")

let asAzureResource (ws:WorkspaceConfig) =
    arm { add_resource ws }
    |> findAzureResources<Workspace> dummyClient.SerializationSettings
    |> List.head
    |> fun r ->
        r.Validate()
        r

let tests = testList "Log analytics" [
    let makeLogAnalytics theSku =
        logAnalytics {
            name "myFarmer"
            sku theSku
        }

    test "Creates a log analytics workspace" {
        let config =
            logAnalytics {
                name "myFarmer"
                sku (PerGb 30<Days>)
                enable_query
                enable_ingestion
            }
        let ws = asAzureResource config

        Expect.equal ws.Location "westeurope" "Incorrect Location"
        Expect.equal ws.Name "myFarmer" "Incorrect Name"
        Expect.equal ws.PublicNetworkAccessForIngestion "Enabled" "Incorrect IngestionSupport"
        Expect.equal ws.PublicNetworkAccessForQuery "Enabled" "QuerySupport"
        Expect.equal ws.Sku.Name "PerGb2018" "Incorrect Sku"
        Expect.equal ws.RetentionInDays (Nullable 30) "Incorrect Retention In Days"
    }

    test "Ingestion and Query are disabled by default" {
        let ws = makeLogAnalytics (PerGb 30<Days>) |> asAzureResource

        Expect.equal ws.PublicNetworkAccessForQuery null "Query should be off by default"
        Expect.equal ws.PublicNetworkAccessForIngestion null "Ingestion should be off by default"
    }

    test "Can't create log analytics with Sku equal to Standalone, PerNode or PerGB2018 with retention period outside 30 and 730 " {
        let permutations = List.allPairs [ PerGb; PerNode; Standalone ] [ 29<Days>; 731<Days> ]
        for (sku, days) in permutations do
            Expect.throws (fun _ -> makeLogAnalytics (sku days) |> ignore) (sprintf "Should have thrown for %A" (sku days))
    }
    test "Can't create log analytics with Sku equal to Standard and retention_period doesn't equal to 30 " {
        Expect.throws (fun _ -> makeLogAnalytics (Standalone 29<Days>) |> ignore) "Should have thrown"
    }
]
