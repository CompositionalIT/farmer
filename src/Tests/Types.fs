module Types

open Expecto
open Farmer
open System

let tests =
    testList "Type Tests" [
        test "Creates deterministic GUID correctly" {
            let actual = DeterministicGuid.create "hello"
            Expect.equal (Guid.Parse "4fbe461c-3438-55c4-941e-d1c2013210c5") actual "Incorrect GUID"
        }
    ]

