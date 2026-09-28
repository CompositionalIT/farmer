module CommunicationServices

open Expecto
open Farmer
open Farmer.Arm
open Farmer.Builders
open Newtonsoft.Json.Linq

let tests =
    testList "Communication Services" [
        test "Basic test" {
            let tags = [ "a", "1"; "b", "2" ]

            let swa = communicationService {
                name "test"
                add_tags tags
                data_location DataLocation.Australia
            }

            let baseArm = (swa :> IBuilder).BuildResources(Location.WestEurope).[0]
            let bsArm = baseArm :?> CommunicationService
            Expect.equal bsArm.Name (ResourceName "test") "Name"
            Expect.equal bsArm.DataLocation DataLocation.Australia "Data Location"
            Expect.equal bsArm.Tags (Map tags) "Tags"
        }

        test "Default options test" {
            let swa = communicationService { name "test" }

            let baseArm = (swa :> IBuilder).BuildResources(Location.WestEurope).[0]
            let bsArm = baseArm :?> CommunicationService
            Expect.equal bsArm.Name (ResourceName "test") "Name"
            Expect.equal bsArm.DataLocation DataLocation.UnitedStates "Data Location"
            Expect.isEmpty bsArm.Tags "Tags"
        }

        test "Uses stable API version" {
            let deployment = arm { add_resource (communicationService { name "test" }) }
            let json = deployment.Template |> Writer.toJson |> JObject.Parse
            let version = json.SelectToken("resources[?(@.name=='test')].apiVersion").ToString()
            Expect.equal version "2020-08-20" "Incorrect API version"
        }
    ]