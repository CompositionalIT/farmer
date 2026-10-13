module ApiManagement

open Expecto
open Farmer
open Farmer.Builders.ApiManagement
open Farmer.Arm.ApiManagement
open Newtonsoft.Json.Linq

let tests =
    testList "ApiManagement" [
        test "Creates an API Management service" {
            let service = apiManagementService {
                name "apim"
                location Location.NorthEurope
                publisher_name "Farmer"
                publisher_email "farmer@example.com"
                sku ApiManagementSku.Basic
                capacity 1
                add_tags [ "environment", "test" ]
            }

            let deployment = arm { add_resource service }
            let json = deployment.Template |> Writer.toJson |> JObject.Parse
            let resource = json.SelectToken("resources[?(@.name=='apim')]")

            Expect.equal (resource.SelectToken("type").ToString()) "Microsoft.ApiManagement/service" "Incorrect type"
            Expect.equal (resource.SelectToken("apiVersion").ToString()) "2021-08-01" "Incorrect API version"
            Expect.equal (resource.SelectToken("location").ToString()) "northeurope" "Incorrect location"
            Expect.equal (resource.SelectToken("sku.name").ToString()) "Basic" "Incorrect SKU"
            Expect.equal (resource.SelectToken("sku.capacity").ToString()) "1" "Incorrect capacity"

            Expect.equal
                (resource.SelectToken("properties.publisherName").ToString())
                "Farmer"
                "Incorrect publisher name"

            Expect.equal
                (resource.SelectToken("properties.publisherEmail").ToString())
                "farmer@example.com"
                "Incorrect publisher email"

            Expect.equal (resource.SelectToken("tags.environment").ToString()) "test" "Incorrect tag"
        }

        test "Creates typed API, operation, and product resources" {
            let service = apiManagementService {
                name "apim"
                publisher_name "Farmer"
                publisher_email "farmer@example.com"
            }

            let api = apiManagementApi {
                service "apim"
                name "catalog"
                display_name "Catalog"
                path "catalog"
                protocols [ "https" ]
                subscription_required true
            }

            let operation = apiManagementOperation {
                service "apim"
                api "catalog;rev=1"
                name "list"
                display_name "List catalog items"
                method "GET"
                url_template "/items"
            }

            let product = apiManagementProduct {
                service "apim"
                name "public"
                display_name "Public API"
                state "published"
                subscription_required false
            }

            let deployment = arm { add_resources [ service; api; operation; product ] }
            let json = deployment.Template |> Writer.toJson |> JObject.Parse
            let resources = json.SelectToken("resources")

            Expect.equal (resources.Children() |> Seq.length) 4 "Incorrect resource count"
            Expect.equal (resources.[1].SelectToken("name").ToString()) "apim/catalog;rev=1" "Incorrect API name"

            Expect.equal
                (resources.[1].SelectToken("properties.displayName").ToString())
                "Catalog"
                "Incorrect API display name"

            Expect.equal (resources.[2].SelectToken("properties.method").ToString()) "GET" "Incorrect operation method"

            Expect.equal
                (resources.[3].SelectToken("properties.state").ToString())
                "published"
                "Incorrect product state"
        }

        test "Creates typed backend, named value, and policy resources" {
            let backend = apiManagementBackend {
                service "apim"
                name "catalog-backend"
                url "https://example.com"
                protocol "http"
                title "Catalog backend"
            }

            let namedValue = apiManagementNamedValue {
                service "apim"
                name "catalog-key"
                display_name "Catalog-key"
                value "secret-value"
                secret
            }

            let policy = apiManagementPolicy {
                service "apim"
                scope "apis"
                api "catalog;rev=1"
                name "policy"
                xml "<policies><inbound /></policies>"
            }

            let deployment = arm { add_resources [ backend; namedValue; policy ] }
            let json = deployment.Template |> Writer.toJson |> JObject.Parse
            let resources = json.SelectToken("resources")

            Expect.equal (resources.Children() |> Seq.length) 3 "Incorrect resource count"

            Expect.equal
                (resources.[0].SelectToken("properties.url").ToString())
                "https://example.com"
                "Incorrect backend URL"

            Expect.isTrue
                (resources.[1].SelectToken("properties.secret").ToObject<bool>())
                "Named value should be secret"

            Expect.equal (resources.[2].SelectToken("properties.format").ToString()) "rawxml" "Incorrect policy format"
        }
    ]