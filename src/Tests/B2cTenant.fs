module B2cTenant

open Expecto
open Farmer
open Farmer.B2cTenant
open Farmer.Builders
open Newtonsoft.Json.Linq

let tests =
    testList "B2c tenant tests" [
        test "B2c tenant should generate the expected arm template" {
            let deployment = arm {
                location Location.FranceCentral

                add_resources [
                    b2cTenant {
                        initial_domain_name "myb2c"
                        display_name "My B2C tenant"
                        sku Sku.PremiumP1
                        country_code "FR"
                        data_residency B2cDataResidency.Europe
                    }
                ]
            }

            let jobj = deployment.Template |> Writer.toJson |> JObject.Parse

            let generatedTemplate = jobj.SelectToken("resources[0]")

            Expect.equal
                (generatedTemplate.SelectToken("apiVersion").ToString())
                "2021-04-01"
                "Invalid ARM template api version"

            Expect.equal
                (generatedTemplate.SelectToken("type").ToString())
                "Microsoft.AzureActiveDirectory/b2cDirectories"
                "Invalid ARM template type"

            Expect.equal
                (generatedTemplate.SelectToken("name").ToString())
                "myb2c.onmicrosoft.com"
                "`name` should match <initial_domain_name>.onmicrosoft.com"

            Expect.equal
                (generatedTemplate.SelectToken("properties.createTenantProperties.displayName").ToString())
                "My B2C tenant"
                "Invalid display name"

            Expect.equal
                (generatedTemplate.SelectToken("location").ToString())
                "europe"
                "`location` should match with the provided `data_residency`"

            Expect.equal
                (generatedTemplate.SelectToken("properties.createTenantProperties.countryCode").ToString())
                "FR"
                "Invalid country code"

            Expect.equal (generatedTemplate.SelectToken("sku.name").ToString()) "PremiumP1" "Invalid sku"
        }
    ]