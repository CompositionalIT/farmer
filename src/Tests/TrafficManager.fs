module TrafficManager

open Expecto
open Farmer
open Farmer.Builders
open Farmer.Builders.TrafficManager
open Microsoft.Azure.Management.TrafficManager
open System
open Microsoft.Rest


let dummyClient =
    new TrafficManagerManagementClient(Uri "http://management.azure.com", TokenCredentials "NotNullOrWhiteSpace")

let tests =
    testList
        "Traffic Manager"
        [
            test "Correctly serializes to JSON" {
                let t = arm { add_resource (trafficManager { name "test" }) }
                t.Template |> Writer.toJson |> ignore
            }

            test "Can create a basic default Traffic Manager profile with sensible defaults" {
                let tmName = "test-tm"

                let resource =
                    let tm = trafficManager { name tmName }

                    arm { add_resource tm }
                    |> findAzureResources<Models.Profile> dummyClient.SerializationSettings
                    |> List.head

                Expect.equal resource.Name tmName "Profile name does not match"
                Expect.equal resource.DnsConfig.Ttl (Nullable(int64 30)) "Default DNS TTL is incorrect"

                Expect.equal
                    resource.TrafficRoutingMethod
                    (Nullable Models.TrafficRoutingMethod.Performance)
                    "Default TrafficRoutingMethod is incorrect"

                Expect.equal
                    resource.TrafficViewEnrollmentStatus
                    (Nullable Models.TrafficViewEnrollmentStatus.Disabled)
                    "Default TrafficViewEnrollmentStatus is incorrect"

                Expect.equal resource.Location "global" "Location is incorrect"
                Expect.equal resource.MonitorConfig.Path "/" "Default MonitorConfig Path is incorrect"
                Expect.equal resource.MonitorConfig.Port (Nullable(int64 80)) "Default MonitorConfig Port is incorrect"

                Expect.equal
                    resource.MonitorConfig.IntervalInSeconds
                    (Nullable(int64 30))
                    "Default MonitorConfig IntervalInSeconds is incorrect"

                Expect.equal
                    resource.MonitorConfig.Protocol
                    (Nullable Models.MonitorProtocol.HTTP)
                    "Default MonitorConfig MonitorProtocol is incorrect"

                Expect.equal
                    resource.MonitorConfig.TimeoutInSeconds
                    (Nullable(int64 10))
                    "Default MonitorConfig TimeoutInSeconds is incorrect"

                Expect.equal
                    resource.MonitorConfig.ToleratedNumberOfFailures
                    (Nullable(int64 3))
                    "Default MonitorConfig TimeoutInSeconds is incorrect"
            }
            test "Basic Traffic Manager profile with performance routing and equal priority endpoints" {
                let tmName = "test-tm"

                let resource =
                    let tm =
                        trafficManager {
                            name tmName

                            add_endpoints
                                [
                                    endpoint {
                                        name "someapp-eastus"
                                        target_external "eastus.farmer.com" Location.EastUS
                                    }
                                    endpoint {
                                        name "someapp-westeurope"
                                        target_external "westeurope.farmer.com" Location.WestEurope
                                    }
                                ]
                        }

                    arm { add_resource tm }
                    |> findAzureResources<Models.Profile> dummyClient.SerializationSettings
                    |> List.head

                Expect.equal resource.Name tmName "Profile name does not match"

                Expect.equal
                    resource.TrafficRoutingMethod
                    (Nullable Models.TrafficRoutingMethod.Performance)
                    "Default TrafficRoutingMethod is incorrect"

                Expect.equal resource.Endpoints.Count 2 "Incorrect number of endpoints"

                for endpoint in resource.Endpoints do
                    Expect.isFalse endpoint.Priority.HasValue "Should not have a value for priority"
                    Expect.isFalse endpoint.Weight.HasValue "Should not have a value for weight"
            }
        ]
