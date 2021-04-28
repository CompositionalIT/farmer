module CommunicationServices

open Expecto
open Farmer
open Farmer.Builders
open Farmer.Arm

let tests = testList "Communication Services" [
    test "Basic test" {
        let tags = [ "a", "1"; "b", "2" ]
        let swa = communicationServices {
            name "test"
            add_tags tags
            data_location Location.NorthEurope
        }
        let baseArm = (swa :> IBuilder).BuildResources(Location.WestEurope).[0]
        let bsArm = baseArm :?> CommunicationServices.Resource
        Expect.equal bsArm.Name (ResourceName "test") "Name"
        Expect.equal bsArm.Location Location.WestEurope "Location"
        Expect.equal bsArm.DataLocation Location.NorthEurope "Data Location"
        Expect.equal bsArm.Tags (Map tags) "Tags"
    }

    test "Default options test" {
        let swa = communicationServices {
            name "test"
        }

        let baseArm = (swa :> IBuilder).BuildResources(Location.WestEurope).[0]
        let bsArm = baseArm :?> CommunicationServices.Resource
        Expect.equal bsArm.Name (ResourceName "test") "Name"
        Expect.equal bsArm.Location Location.WestEurope "Location"
        Expect.equal bsArm.DataLocation bsArm.Location "Data Location"
        Expect.isEmpty bsArm.Tags "Tags"
    }
]