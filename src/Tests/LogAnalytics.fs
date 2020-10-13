module LogAnalytics

open Expecto
open Farmer
open Farmer.Arm
open Farmer.Builders
open Farmer.CoreTypes
open Farmer.LogAnalytics

let tests = testList "Log analytics" [
    let makeLogAnalytics theSku retention =
        logAnalytics {
            name "myFarmer"
            sku theSku
            retention_period retention
        } :> IBuilder

    test "Creates a log analytics workspace" {
        let builder =
            logAnalytics {
                name "myFarmer"
                sku PerGB2018
                retention_period 30<Days>
                enable_query
                enable_ingestion
            } :> IBuilder

        let resources = builder.BuildResources Location.WestEurope
        let workspace = resources.[0] :?> WorkSpace

        Expect.equal workspace.Location Location.WestEurope "Incorrect Location"
        Expect.equal workspace.Name (ResourceName "myFarmer") "Incorrect name"
        Expect.equal workspace.IngestionSupport (Some Enabled) "Incorrect publicNetworkAccessForIngestiont"
        Expect.equal workspace.QuerySupport (Some Enabled) "Incorrect publicNetworkAccessForQuery"
        Expect.equal workspace.RetentionPeriod (Some 30<Days>) "Incorrect retention_period"
    }

    test "Ingestion and Query are disabled by default" {
        let builder = makeLogAnalytics PerGB2018 30<Days>
        let resources = builder.BuildResources Location.WestEurope
        let workspace = resources.[0] :?> WorkSpace

        Expect.equal workspace.QuerySupport None "Query should be off by default"
        Expect.equal workspace.IngestionSupport None "Ingestion should be off by default"
    }

    test "Can't create log analytics with Sku eqaul to Standalone, PerNode or PerGB2018 and retention_period is not bettwen 30 and 730 " {
        Expect.throws (fun _ -> makeLogAnalytics PerGB2018 29<Days> |> ignore) "Should have thrown"
        Expect.throws (fun _ -> makeLogAnalytics PerNode 29<Days> |> ignore) "Should have thrown"
        Expect.throws (fun _ -> makeLogAnalytics Standalone 29<Days> |> ignore) "Should have thrown"
    }
    test "Can't create log analytics with Sku eqaul to Standard and retention_period doesn't eqaul to 30 " {
        Expect.throws (fun _ -> makeLogAnalytics Standalone 29<Days> |> ignore) "Should have thrown"
    }

    test "Can't create log analytics with Sku eqaul to Premium and retention_period doesn't eqaul to 365 " {
        Expect.throws (fun _ -> makeLogAnalytics Premium 300<Days> |> ignore) "Should have thrown"
    }
]
