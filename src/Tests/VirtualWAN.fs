module VirtualWAN

open Expecto
open Farmer
open Farmer.Builders
open System
open Microsoft.Azure.Management.Network
open Microsoft.Azure.Management.Network.Models
open Microsoft.Rest

/// Client instance so we can get the JSON serialization settings.
let client = new NetworkManagementClient(Uri "http://management.azure.com", TokenCredentials "NotNullOrWhiteSpace")
/// Helper for tests to get the virtual hub resource that is defined in the deployment.
let getVirtualWanResource = findAzureResources<VirtualWAN> client.SerializationSettings >> List.head

/// Collection of tests for the VirtualHub resource and builders. Needs to be included in AllTests.fs
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
]
