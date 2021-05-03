module VirtualWAN

open Expecto
open Farmer
open Farmer.Arm
open Farmer.Builders
open System
open Microsoft.Azure.Management.Network
open Microsoft.Azure.Management.Network.Models
open Microsoft.Rest

/// Client instance so we can get the JSON serialization settings.
let client = new NetworkManagementClient(Uri "http://management.azure.com", TokenCredentials "NotNullOrWhiteSpace")
/// Helper for tests to get the virtual hub resource that is defined in the deployment.
let getVirtualWanResource = findAzureResources<VirtualWAN> client.SerializationSettings >> List.head

/// Collection of tests for the VirtualWAN resource and builders. Needs to be included in AllTests.fs
let tests = testList "VirtualWAN" [
    test "Can create a basic VirtualWAN" {
        let vwan =
            arm {
                location Location.WestUS
                add_resources [
                    vwan {
                        name "my-vwan"
                    }
                ]
            } |> getVirtualWanResource
        Expect.equal vwan.Name "my-vwan" "Incorrect Resource Name"
        Expect.equal vwan.Location "westus" "Incorrect Location"
        Expect.equal vwan.VirtualWANType "Basic" "Default should be 'Basic'"
        Expect.isFalse vwan.AllowBranchToBranchTraffic.Value "AllowBranchToBranchTraffic should be false"
        Expect.isFalse vwan.DisableVpnEncryption.Value "DisableVpnEncryption should not have a value"
        Expect.equal vwan.Office365LocalBreakoutCategory "None" "Office365LocalBreakoutCategory should be 'None'"
    }
    test "Can create a standard VirtualWAN" {
        let vwan =
            arm {
                location Location.WestUS
                add_resources [
                    vwan {
                        name "my-vwan"
                        standard_vwan
                    }
                ]
            } |> getVirtualWanResource
        Expect.equal vwan.VirtualWANType "Standard" "" 
    }
    test "Can create a VirtualWAN with DisableVpnEncryption" {
        let vwan =
            arm {
                location Location.WestUS
                add_resources [
                    vwan {
                        name "my-vwan"
                        disable_vpn_encryption
                    }
                ]
            } |> getVirtualWanResource
        Expect.equal vwan.DisableVpnEncryption (Nullable true) "" 
    }
    test "Can create a VirtualWAN with AllowBranchToBranchTraffic" {
        let vwan =
            arm {
                location Location.WestUS
                add_resources [
                    vwan {
                        name "my-vwan"
                        allow_branch_to_branch_traffic
                    }
                ]
            } |> getVirtualWanResource
        Expect.equal vwan.AllowBranchToBranchTraffic (Nullable true) "" 
    }
    test "Can create a VirtualWAN with Office365LocalBreakoutCategory.All" {
        let vwan =
            arm {
                location Location.WestUS
                add_resources [
                    vwan {
                        name "my-vwan"
                        office_365_local_breakout_category Office365LocalBreakoutCategory.All
                    }
                ]
            } |> getVirtualWanResource
        Expect.equal vwan.Office365LocalBreakoutCategory "All" "" 
    }
    test "Can create a VirtualWAN with Office365LocalBreakoutCategory.Optimize" {
        let vwan =
            arm {
                location Location.WestUS
                add_resources [
                    vwan {
                        name "my-vwan"
                        office_365_local_breakout_category Office365LocalBreakoutCategory.Optimize
                    }
                ]
            } |> getVirtualWanResource
        Expect.equal vwan.Office365LocalBreakoutCategory "Optimize" "" 
    }
    test "Can create a VirtualWAN with Office365LocalBreakoutCategory.OptimizeAndAllow" {
        let vwan =
            arm {
                location Location.WestUS
                add_resources [
                    vwan {
                        name "my-vwan"
                        office_365_local_breakout_category Office365LocalBreakoutCategory.OptimizeAndAllow
                    }
                ]
            } |> getVirtualWanResource
        Expect.equal vwan.Office365LocalBreakoutCategory "OptimizeAndAllow" "" 
    }
]

