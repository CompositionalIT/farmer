module AzCli

open Expecto
open Farmer
open System
open Newtonsoft.Json

// #r "nuget: Newtonsoft.Json"
open Newtonsoft.Json
let json = "[]/"
let a = JsonConvert.DeserializeObject(json)
printfn "%A" a

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
    test "Az output is JSON unless overridden" {
        let result = Deploy.Az.az "account list"
        match result with
        | Ok ok -> Newtonsoft.Json.JsonConvert.DeserializeObject(ok) |> ignore
        | Error error -> failwithf "Error checking az json: %s" error

        let result = Deploy.Az.az "account list -o yaml"
        match result with
        | Ok ok ->
            try
                Newtonsoft.Json.JsonConvert.DeserializeObject(ok) |> ignore
            with _ -> ()
        | Error error -> failwithf "Error overriding az output: %s" error

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
