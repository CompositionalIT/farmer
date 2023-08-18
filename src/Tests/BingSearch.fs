module BingSearch

open Expecto
open Farmer
open Farmer.Builders
open Farmer.BingSearch
open Farmer.Arm
open System
open TestHelpers

let private asJson (arm: IArmResource) =
    arm.JsonModel
    |> convertTo<{|
        kind: string
        properties: {| statisticsEnabled: bool |}
    |} >

let tests =
    testList "Bing Search" [
        test "Basic test" {
            let tags = [ "a", "1"; "b", "2" ]

            let swa = bingSearch {
                name "test"
                sku S0
                add_tags tags
                statistics Enabled
            }

            let baseArm = (swa :> IBuilder).BuildResources(Location.WestEurope).[0]
            let bsArm = baseArm :?> BingSearch.Accounts
            let jsonModel = asJson baseArm
            Expect.equal bsArm.Name (ResourceName "test") "Name"
            Expect.equal bsArm.Location Location.WestEurope "Location"
            Expect.isTrue jsonModel.properties.statisticsEnabled "Statistics enabled in json"
            Expect.equal bsArm.Statistics FeatureFlag.Enabled "Statistics enabled"
            Expect.equal bsArm.Sku S0 "Sku"
            Expect.equal jsonModel.kind "Bing.Search.v7" "kind"
            Expect.equal bsArm.Tags (Map tags) "Tags"
        }

        test "Default options test" {
            let swa = bingSearch { name "test" }

            let baseArm = (swa :> IBuilder).BuildResources(Location.WestEurope).[0]
            let bsArm = baseArm :?> BingSearch.Accounts
            let jsonModel = asJson baseArm
            Expect.equal bsArm.Name (ResourceName "test") "Name"
            Expect.equal bsArm.Location Location.WestEurope "Location"
            Expect.isFalse jsonModel.properties.statisticsEnabled "Statistics not enabled in json"
            Expect.equal bsArm.Statistics Disabled "Statistics not enabled"
            Expect.equal bsArm.Sku F1 "Sku"
            Expect.equal jsonModel.kind "Bing.Search.v7" "kind"
            Expect.isEmpty bsArm.Tags "Tags"
        }
    ]
