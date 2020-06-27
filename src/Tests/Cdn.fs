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
let getResourceAtIndex = getResourceAtIndex dummyClient.SerializationSettings

let asAzureResource (cdn:CdnConfig) =
    arm { add_resource cdn }
    |> findAzureResources<Profile> dummyClient.SerializationSettings
    |> List.head
    |> fun r ->
        r.Validate()
        r

let tests = testList "CDN tests" [
    test "CDN profile is created correctly" {
        let profile =
            cdn {
                name "test-cdn"
                sku Cdn.Sku.Premium_Verizon
            } |> asAzureResource

        Expect.equal profile.Name "test-cdn" "Incorrect name"
        Expect.equal profile.Sku.Name "Premium_Verizon" "Incorrect SKU"
    }
    test "Endpoint is created with correct defaults" {
        let endpoint : Endpoint =
            cdn {
                name "test-cdn"
                add_endpoints [ endpoint {
                    name "test-endpoint"
                    origin "origin"
                } ]
            }
            |> getResourceAtIndex 1

        Expect.equal endpoint.Name "test-cdn/test-endpoint" "Incorrect name"
        Expect.equal endpoint.Origins.[0].HostName "origin" "Incorrect origin"
        Expect.isFalse endpoint.IsCompressionEnabled.Value "Compression should be disabled by default"
        Expect.equal endpoint.OptimizationType "GeneralWebDelivery" "Optimisation type should be web by default"
        Expect.equal endpoint.QueryStringCachingBehavior.Value QueryStringCachingBehavior.UseQueryString "QSCB should be UseQueryString by default"
    }

    test "Endpoint settings are correctly cascaded" {
        let endpoint : Endpoint =
            cdn {
                name "test-cdn"
                add_endpoints [ endpoint {
                    origin "origin"
                    add_compressed_content [ "a"; "b"; "c"; ]
                    optimise_for OptimizationType.GeneralMediaStreaming
                    query_string_caching_behaviour QueryStringCachingBehaviour.BypassCaching
                    disable_http
                    disable_https
                } ]
            }
            |> getResourceAtIndex 1

        Expect.equal endpoint.Name "test-cdn/origin-endpoint" "Incorrect endpoint name"
        Expect.equal (endpoint.ContentTypesToCompress |> Set) (Set [ "a"; "b"; "c" ]) "Incorrect content compression types"
        Expect.isTrue endpoint.IsCompressionEnabled.Value "Compression should be enabled when content is supplied"
        Expect.equal endpoint.OptimizationType "GeneralMediaStreaming" "Optimisation type"
        Expect.equal endpoint.QueryStringCachingBehavior.Value QueryStringCachingBehavior.BypassCaching "Query String Caching Behaviour"
        Expect.isFalse endpoint.IsHttpAllowed.Value "HTTP should be disabled"
        Expect.isFalse endpoint.IsHttpsAllowed.Value "HTTPS should be disabled"
    }
]