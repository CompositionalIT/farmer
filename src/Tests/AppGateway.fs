module AppGateway

open Expecto
open Microsoft.Azure.Management.Network
open Microsoft.Rest
open System
open Farmer
open Farmer.ApplicationGateway
open Farmer.Builders

let client = new NetworkManagementClient(Uri "http://management.azure.com", TokenCredentials "NotNullOrWhiteSpace")

let tests = testList "Application Gateway Tests" [
    ftest "Empty basic app gateway" {
        let ag =
            appGateway {
                name "ag"
            }
        ()
        let resource =
            arm { add_resource ag }
                |> findAzureResources<Microsoft.Azure.Management.Network.Models.LoadBalancer> client.SerializationSettings
                |> List.head
        Expect.equal resource.Name "ag" "Name did not match"
    }
]