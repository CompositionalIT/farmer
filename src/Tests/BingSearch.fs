module BingSearch

open Expecto
open Farmer
open Farmer.Builders
open Farmer.BingSearch
open Farmer.Arm
open System
open TestHelpers

let tests = testList "Bing Search" [
    test "Basic test" {
        let tags = [ "a", "1"; "b", "2" ]
        let swa = bingSearch {
            name "test"
            sku S0
            add_tags tags
            enable_statistics
        }
        let baseArm = (swa :> IBuilder).BuildResources(Location.WestEurope).[0]
        let bsArm = baseArm :?> BingSearch.Accounts
        let model = baseArm.JsonModel |> convertTo<{| kind: string |}>
        Expect.equal bsArm.Name (ResourceName "test") "Name"
        Expect.equal bsArm.Location Location.WestEurope "Location"
        Expect.isTrue bsArm.Properties.statisticsEnabled "Statistics enabled"
        Expect.equal bsArm.Sku S0 "Sku"
        Expect.equal model.kind "Bing.Search.v7" "kind"
        Expect.equal bsArm.Tags (tags |> Map.ofList) "Tags"
    }

    test "Default options test" {
        let swa = bingSearch {
            name "test"
        }

        let baseArm = (swa :> IBuilder).BuildResources(Location.WestEurope).[0]
        let bsArm = baseArm :?> BingSearch.Accounts
        let model = baseArm.JsonModel |> convertTo<{| kind: string |}>
        Expect.equal bsArm.Name (ResourceName "test") "Name"
        Expect.equal bsArm.Location Location.WestEurope "Location"
        Expect.isFalse bsArm.Properties.statisticsEnabled "Statistics enabled"
        Expect.equal bsArm.Sku F1 "Sku"
        Expect.equal model.kind "Bing.Search.v7" "kind"
        Expect.isEmpty bsArm.Tags "Tags"
    }
]