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

                let hostGroupProps = hostGroup["properties"]

                let supportAutomaticPlacement: bool =
                    JToken.op_Explicit hostGroupProps["supportAutomaticPlacement"]

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
                                    parent_host_group (ResourceName parentHostGroupName)
                                    sku "Fsv2-Type2"
                                }
                            ]
                    }

                let jobj = deployment.Template |> Writer.toJson |> JObject.Parse

                let host =
                    jobj.SelectToken "resources[?(@.type=='Microsoft.Compute/hostGroups/hosts')]"

                let hostProps = host["properties"]
                let dependsOn = host["dependsOn"] :?> JArray |> Seq.map string
                let licenseType: string = JToken.op_Explicit hostProps["licenseType"]
                let platformFaultDomain: int = JToken.op_Explicit hostProps["platformFaultDomain"]

                Expect.equal
                    licenseType
                    (HostLicenseType.Print HostLicenseType.NoLicense)
                    "Default license type should be no license"

                Expect.equal
                    platformFaultDomain
                    (PlatformFaultDomainCount.ToArmValue (PlatformFaultDomainCount 1))
                    "Default fault domain should be 0"

                Expect.hasLength dependsOn 1 "Should only depend on one resource, the host group"
                Expect.contains
                    dependsOn
                    """[resourceId('Microsoft.Compute/hostGroups', 'myhostGroup')]"""
                    "Parent host group is incorrect"

                ()
            }
            test "Can create a host group with an and a valid domain count" {
                let deployment =
                    arm {
                        location Location.EastUS

                        add_resources
                            [
                                hostGroup {
                                    name "myhostgroup"
                                    support_automatic_placement true
                                    add_availability_zone "1"
                                    platform_fault_domain_count 2
                                }
                                host {
                                    name "myhost"
                                    parent_host_group (ResourceName "myHostGroup")
                                    sku "Fsv2-Type2"
                                }
                            ]
                    }

                let jobj = deployment.Template |> Writer.toJson |> JObject.Parse
                let a: string = deployment.ToString()
                
                let hostGroup =
                    jobj.SelectToken "resources[?(@.type=='Microsoft.Compute/hostGroups')]"

                let hostGroupProps = hostGroup["properties"]
                let zones = hostGroup["zones"] :?> JArray |> Seq.map string

                let platformFaultDomainCount: int =
                    JToken.op_Explicit hostGroupProps["platformFaultDomainCount"]

                let supportAutomaticPlacement: bool =
                    JToken.op_Explicit hostGroupProps["supportAutomaticPlacement"]

                Expect.equal supportAutomaticPlacement true "Automatic placement should be true"
                Expect.hasLength zones 1 "The host group should have one availability zone"

                Expect.contains
                    zones
                    "1"
                    "The zones should contain zone 1"
                ()
            }
        ]
