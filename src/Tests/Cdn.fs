module Cdn

open Expecto
open Farmer
open Farmer.Builders
open Farmer.Cdn
open Microsoft.Azure.Management.Cdn
open Microsoft.Azure.Management.Cdn.Models
open Microsoft.Rest
open System

/// Client instance needed to get the serializer settings.
let dummyClient = new CdnManagementClient (Uri "http://management.azure.com", TokenCredentials "NotNullOrWhiteSpace")

let asAzureResource (cdn:CdnConfig) =
    arm { add_resource cdn }
    |> findAzureResources<Profile> dummyClient.SerializationSettings
    |> List.head
    |> fun r ->
        r.Validate()
        r

let tests = testList "CDN" [
    test "CDN profile is created correctly" {
        let profile =
            cdn {
                name "test-cdn"
                sku Cdn.Sku.Premium_Verizon
            } |> asAzureResource

        Expect.equal profile.Name "test-cdn" "Incorrect name"
        Expect.equal profile.Sku.Name "Premium_Verizon" "Incorrect SKU"
    }
]