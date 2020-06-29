module KeyVault

open Expecto
open Farmer.Builders

let tests = testList "KeyVault" [
    test "Can create secrets without popping" {
        secret { name "test" } |> ignore
    }
    test "Can create quick secret" {
        keyVault {
            name "test"
            add_secret "test1"
            add_secret (secret { name "test2" })
        } |> ignore
    }
    test "Fails on empty inline secret" {
        Expect.throws(fun () ->
            keyVault {
                name "test"
                add_secret ""
            } |> ignore
        ) |> ignore
    }
    test "Fails on empty full secret" {
        secret { name "" } |> ignore
    }
]
