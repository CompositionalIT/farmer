module KeyVault

open Expecto
open Farmer
open Farmer.Builders
open Farmer.ExpressRoute
open Microsoft.Azure.Management.Network
open Microsoft.Azure.Management.Network.Models
open Microsoft.Rest
open System

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
