module AzCli

open Expecto
open Farmer
open System
open Farmer.Builders

let deployTo resourceGroupName parameters deployment =
    printfn "Creating resource group %s..." resourceGroupName
    let deployResponse = deployment |> Deploy.tryExecute resourceGroupName parameters
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
        let result = deployment |> Deploy.tryExecute "sample-rg" []
        Expect.equal result (Error "The following parameters are missing: p1. Please add them.") ""
    }

    test "Deploys and deletes a resource group" {
        let resourceGroupName = sprintf "farmer-integration-test-delete-%O" (Guid.NewGuid())
        arm { location Location.NorthEurope } |> deployTo resourceGroupName []
    }

    test "Deploys and deletes lots of different resources" {
        let number = Random().Next(1000, 10000).ToString()

        let sql = sqlServer { name ("farmersql" + number); admin_username "farmersqladmin"; add_databases [ sqlDb { name "farmertestdb"; use_encryption } ]; enable_azure_firewall }
        let storage = storageAccount { name ("farmerstorage" + number) }
        let web = webApp { name ("farmerwebapp" + number) }
        let fns = functions { name ("farmerfuncs" + number) }
        let svcBus = serviceBus { name ("farmerbus" + number); sku ServiceBus.Sku.Standard; add_queues [ queue { name "queue1" } ]; add_topics [ topic { name "topic1"; add_subscriptions [ subscription { name "sub1" } ] } ] }
        let cdn = cdn { name ("farmercdn" + number); add_endpoints [ endpoint { name ("farmercdnendpoint" + number); origin storage.WebsitePrimaryEndpointHost } ] }

        let deployment = arm {
            location Location.NorthEurope
            add_resources [ sql; storage; web; fns; svcBus; cdn ]
        }
        let resourceGroupName = sprintf "farmer-integration-test-delete-%O" (Guid.NewGuid())
        deployment |> deployTo resourceGroupName [ sql.PasswordParameter, Guid.NewGuid().ToString() ]
    }
]
