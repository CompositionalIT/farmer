module AzCli

open Expecto
open Farmer
open System

let tests = testList "Azure CLI" [
    test "Can connect to Az CLI" {
        match Deploy.checkVersion Deploy.Az.MinimumVersion with
        | Ok _ -> ()
        | Error x -> failwithf "Version check failed: %s" x
    }
    test "If parameters are missing, deployment is immediately rejected" {
        let deployment = Template.TestHelpers.createSimpleDeployment [ "p1" ]
        let result = deployment |> Deploy.tryExecute "sample-rg" []
        Expect.equal result (Error "The following parameters are missing: p1. Please add them.") ""
    }

    test "Deploys and deletes a resource group" {
        let deployment = arm { location Location.NorthEurope }
        let resourceGroupName = sprintf "farmer-integration-test-delete-%O" (Guid.NewGuid())
        printfn "Creating resource group %s..." resourceGroupName
        let deployResponse = deployment |> Deploy.tryExecute resourceGroupName []
        let deleteResponse = Deploy.Az.delete resourceGroupName

        match deployResponse, deleteResponse with
        | Ok _, Ok _ -> ()
        | Error e, _ -> failwithf "Something went wrong during the deployment: %s" e
        | _, Error e -> failwithf "Something went wrong during the delete: %s" e
    }
]
