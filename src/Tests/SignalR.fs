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

let client =
    new SignalRManagementClient(Uri "http://management.azure.com", TokenCredentials "NotNullOrWhiteSpace")

let tests =
    testList
        "SignalR"
        [
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

                Expect.equal
                    "[listKeys(resourceId('Microsoft.SignalRService/SignalR', 'my-signalr'), providers('Microsoft.SignalRService', 'SignalR').apiVersions[0]).primaryKey]"
                    (mySignalR.Key.Eval())
                    "Key is incorrect"

                Expect.equal
                    "[listKeys(resourceId('Microsoft.SignalRService/SignalR', 'my-signalr'), providers('Microsoft.SignalRService', 'SignalR').apiVersions[0]).primaryConnectionString]"
                    (mySignalR.ConnectionString.Eval())
                    "Connection String is incorrect"
            }

            test "Can create a SignalR account with upstream configuration" {
                let resource =
                    let mySignalR =
                        signalR {
                            name "my-signalr~@"
                            sku Standard
                            capacity 10
                            add_upstream "test-url-template"
                            add_upstream "test-url-template-2" (Arm.SignalRService.List [ "hub1"; "hub2" ]) (Arm.SignalRService.List [ "category1"; "category2" ]) (Arm.SignalRService.List [ "event1"; "event2" ])
                        }

                    arm { add_resource mySignalR }
                    |> findAzureResources<SignalRResource> client.SerializationSettings
                    |> List.head

                resource.Validate()
                Expect.hasLength resource.Upstream.Templates 2 "Should have one upstream config"
                Expect.equal resource.Upstream.Templates.[0].UrlTemplate "test-url-template" "Url Template does not match"
                Expect.equal resource.Upstream.Templates.[0].HubPattern "*" "Hub Pattern does not match"
                Expect.equal resource.Upstream.Templates.[0].CategoryPattern "*" "Category Pattern does not match"
                Expect.equal resource.Upstream.Templates.[0].EventPattern "*" "Event Pattern does not match"
                Expect.equal resource.Upstream.Templates.[1].UrlTemplate "test-url-template2" "Url Template does not match"
                Expect.equal resource.Upstream.Templates.[1].HubPattern "hub1,hub2" "Hub Pattern does not match"
                Expect.equal resource.Upstream.Templates.[1].CategoryPattern "category1,category2" "Category Pattern does not match"
                Expect.equal resource.Upstream.Templates.[1].EventPattern "event1,event2" "Event Pattern does not match"
            }
        ]
