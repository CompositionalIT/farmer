module Types

open Expecto
open Farmer
open Farmer.Builders
open System
open Newtonsoft.Json.Linq

let tests =
    testList "Type Tests" [
        test "Creates deterministic GUID correctly" {
            let actual = DeterministicGuid.create "hello"
            Expect.equal (Guid.Parse "4fbe461c-3438-55c4-941e-d1c2013210c5") actual "Incorrect GUID"
        }
        test "Location.ResourceGroup emits correct ARM expression" {
            Expect.equal
                Location.ResourceGroup.ArmValue
                "[resourceGroup().location]"
                "Incorrect expression emitted for Location.ResourceGroup"
        }
        ftest "Default location for 'arm' builder uses resourceGroup location" {
            let deployment =
                let dummyResource = storageAccount { name "mystorageaccount74785" }
                arm { add_resource dummyResource }

            let jobj = deployment.Template |> Writer.toJson |> JToken.Parse

            Expect.equal
                (jobj.SelectToken "resources[?(@.name=='mystorageaccount74785')].location")
                (JValue "[resourceGroup().location]")
                "Default location on resource should be resource group."
        }
    ]