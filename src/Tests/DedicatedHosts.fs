module DedicatedHosts

open Expecto
open Farmer
open Farmer.Builders
open Farmer.DedicatedHosts
open Microsoft.Azure.Management.Compute
open Microsoft.Rest
open System
open Newtonsoft.Json.Linq

/// Client instance needed to get the serializer settings.
let client =
    new ComputeManagementClient(Uri "http://management.azure.com", TokenCredentials "NotNullOrWhiteSpace")

let tests =
    testList
        "Dedicated Hosts"
        [
            test "Can create a basic dedicated host group" {
                let deployment =
                    arm {
                        location Location.EastUS

                        add_resources [ hostGroup { name "myhostgroup" } ]
                    }

                let jobj = deployment.Template |> Writer.toJson |> JObject.Parse

                let hostGroup =
                    jobj.SelectToken "resources[?(@.type=='Microsoft.Compute/hostGroups')]"

                let hostGroupProps = hostGroup.["properties"]

                let supportAutomaticPlacement: bool =
                    JToken.op_Explicit hostGroupProps.["supportAutomaticPlacement"]

                Expect.equal supportAutomaticPlacement false "Incorrect default value for supportAutomaticPlacement"
            }
            test "Can create a basic dedicated host group with a host" {
                let parentHostGroupName = "myhostGroup"

                let deployment =
                    arm {
                        location Location.EastUS

                        add_resources
                            [
                                hostGroup { name parentHostGroupName }
                                host {
                                    name "myhost"
                                    parentHostGroup (ResourceName parentHostGroupName)
                                    sku "VSv1-Type3"
                                }
                            ]
                    }

                let jobj = deployment.Template |> Writer.toJson |> JObject.Parse

                let host =
                    jobj.SelectToken "resources[?(@.type=='Microsoft.Compute/hostGroups/hosts')]"

                let hostProps = host.["properties"]
                let dependsOn = host.["dependsOn"] :?> JArray |> Seq.map string
                let licenseType: string = JToken.op_Explicit hostProps.["licenseType"]
                let platformFaultDomain: int = JToken.op_Explicit hostProps.["platformFaultDomain"]

                Expect.equal
                    licenseType
                    (HostLicenseType.Print HostLicenseType.NoLicense)
                    "Default license type should be no license"

                Expect.equal
                    platformFaultDomain
                    (PlatformFaultDomain.ToArmValue PlatformFaultDomain.Zero)
                    "Default fault domain should be 0"

                let parentResourceId =
                    Arm.Compute.hostGroups.resourceId (ResourceName parentHostGroupName)

                Expect.hasLength dependsOn 1 "Should only depend on one resource, the host group"

                Expect.contains
                    dependsOn
                    (string parentResourceId)
                    $"Parent host group is incorrect, should be {parentHostGroupName}"

                ()
            }
            test "Can create a host group with a few availability zones and a valid domain count" {
                let deployment =
                    arm {
                        location Location.EastUS

                        add_resources
                            [
                                hostGroup {
                                    name "myhostgroup"
                                    supportAutomaticPlacement true
                                    add_availability_zones [ AvailabilityZone.One; AvailabilityZone.Two ]
                                    platformFaultDomainCount 2
                                }
                                host {
                                    name "myhost"
                                    parentHostGroup (ResourceName "myHostGroup")
                                    sku "VSv1-Type3"
                                }
                            ]
                    }

                let jobj = deployment.Template |> Writer.toJson |> JObject.Parse
                let a = jobj.ToString()

                let hostGroup =
                    jobj.SelectToken "resources[?(@.type=='Microsoft.Compute/hostGroups')]"

                let hostGroupProps = hostGroup.["properties"]
                let zones = hostGroup.["zones"] :?> JArray |> Seq.map string

                let platformFaultDomainCount: int =
                    JToken.op_Explicit hostGroupProps.["platformFaultDomainCount"]

                let supportAutomaticPlacement: bool =
                    JToken.op_Explicit hostGroupProps.["supportAutomaticPlacement"]

                Expect.equal platformFaultDomainCount 2 "Platform fault domain count should be two"
                Expect.equal supportAutomaticPlacement true "Automatic placement should be true"
                Expect.hasLength zones 2 "The host group should have 2 availability zones"

                Expect.contains
                    zones
                    (AvailabilityZone.ToArmValue AvailabilityZone.One)
                    "The zones should contain zone 1"

                Expect.contains
                    zones
                    (AvailabilityZone.ToArmValue AvailabilityZone.Two)
                    "The zones should contain zone 2"

                ()
            }
        ]
