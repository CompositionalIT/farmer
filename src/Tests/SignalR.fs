module SignalR

open System.Text.RegularExpressions
open Expecto
open Farmer
open Farmer.Builders
open Farmer.SignalR
open System
open Microsoft.Azure.Management.Compute.Models
open Microsoft.Azure.Management.SignalR
open Microsoft.Azure.Management.SignalR.Models
open Microsoft.Rest

let client = new SignalRManagementClient(Uri "http://management.azure.com", TokenCredentials "NotNullOrWhiteSpace")
let tests = testList "SignalR" [
    test "Can create a basic SignalR account" {
        let resource =
            let mySignalR =
                signalR {
                    name "my-signalr~@"
                    sku Free
                }
            arm { add_resource mySignalR }
            |> findAzureResources<SignalRResource> client.SerializationSettings
            |> List.head

        resource.Validate()
        Expect.equal resource.Name "my-signalr" "Name does not match"
        Expect.equal resource.Sku.Name "Free_F1" "SKU does not match"
    }

    test "Can create a SignalR account with specific allowed origins" {
        let resource =
            let mySignalR =
                signalR {
                    name "my-signalr~@"
                    sku Free
                    allowed_origins [ "https://github.com"; "https://duckduckgo.com" ]
                }
            arm { add_resource mySignalR }
            |> findAzureResources<SignalRResource> client.SerializationSettings
            |> List.head

        resource.Validate()
        Expect.equal resource.Name "my-signalr" "Name does not match"
        Expect.equal resource.Sku.Name "Free_F1" "SKU does not match"
        Expect.containsAll
            resource.Cors.AllowedOrigins
            [ "https://github.com"; "https://duckduckgo.com" ]
            "Missing some or all allowed origins"
    }

    test "Can create a SignalR account with specific capacity" {
        let resource =
            let mySignalR =
                signalR {
                    name "my-signalr~@"
                    sku Standard
                    capacity 10
                }
            arm { add_resource mySignalR }
            |> findAzureResources<SignalRResource> client.SerializationSettings
            |> List.head

        resource.Validate()
        Expect.equal resource.Name "my-signalr" "Name does not match"
        Expect.equal resource.Sku.Name "Standard_S1" "SKU does not match"
        Expect.equal resource.Sku.Capacity (Nullable 10) "Capacity does not match"
    }

    test "Key is correctly emitted" {
        let mySignalR = signalR { name "my-signalr" }
        Expect.equal "[listKeys(resourceId('Microsoft.SignalRService/SignalR', 'my-signalr'), providers('Microsoft.SignalRService', 'SignalR').apiVersions[0]).primaryConnectionString]" (mySignalR.Key.Eval()) "Key is incorrect"
    }
]
