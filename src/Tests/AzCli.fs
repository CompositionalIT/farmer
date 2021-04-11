module AzCli

open Expecto
open Farmer
open System
open TestHelpers

let deployTo resourceGroupName parameters deployment =
    printfn "Creating resource group %s..." resourceGroupName
    let deployResponse = deployment |> Deploy.tryExecute resourceGroupName parameters
    let deleteResponse = Deploy.Az.delete resourceGroupName
    match deployResponse, deleteResponse with
    | Ok _, Ok _ -> ()
    | Error e, _ -> failwith $"Something went wrong during the deployment: {e}"
    | _, Error e -> failwith $"Something went wrong during the delete: {e}"

let endToEndTests = testList "End to end tests" [
    test "Deploys and deletes a resource group" {
        let resourceGroupName = sprintf "farmer-integration-test-delete-%O" (Guid.NewGuid())
        arm { location Location.NorthEurope } |> deployTo resourceGroupName []
    }

    test "If parameters are missing, deployment is immediately rejected" {
        let deployment = createSimpleDeployment [ "p1" ]
        let result = deployment |> Deploy.tryExecute "sample-rg" []
        Expect.equal result (Error "The following parameters are missing: p1. Please add them.") ""
    }
]

let tests = testList "Azure CLI" [
    test "Can connect to Az CLI" {
        match Deploy.checkVersion Deploy.Az.MinimumVersion with
        | Ok _ -> ()
        | Error msg -> failwith $"Version check failed: {msg}"
    }

    test "Az output is always JSON" {
        // account list always defaults to table, regardless of defaults?
        Deploy.Az.az "account list --all"
        |> Result.map Serialization.ofJson<{| id : Guid; tenantId : Guid; isDefault : bool; |} array>
        |> ignore
    }
]
