module AzCli

open Expecto
open Farmer
open System
open Farmer.Builders

let deployTo resourceGroupName parameters (deployment:Deployment) =
    printfn "Creating resource group %s..." resourceGroupName
    let deployResponse = deployment.TryDeploy(resourceGroupName, List.toArray parameters)
    let deleteResponse = Deploy.Az.delete resourceGroupName
    match deployResponse, deleteResponse with
    | Ok _, Ok _ -> ()
    | Error e, _ -> failwithf "Something went wrong during the deployment: %s" e
    | _, Error e -> failwithf "Something went wrong during the delete: %s" e


let tests = testList "Azure CLI" [
    test "Can connect to Az CLI" {
        match Deploy.checkVersion Deploy.Az.MinimumVersion with
        | Ok _ -> ()
        | Error x -> failwithf "Version check failed: %s" x
    }
    test "If parameters are missing, deployment is immediately rejected" {
        let deployment = Template.TestHelpers.createSimpleDeployment [ "p1" ]
        let result = deployment.TryDeploy "sample-rg"
        Expect.equal result (Error "The following parameters are missing: p1. Please add them.") ""
    }

    test "Deploys and deletes a resource group" {
        let resourceGroupName = sprintf "farmer-integration-test-delete-%O" (Guid.NewGuid())
        arm { location Location.NorthEurope } |> deployTo resourceGroupName []
    }
]
