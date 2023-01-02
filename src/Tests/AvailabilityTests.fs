module AppInsightsAvailability

open Expecto
open Farmer
open Farmer.Builders

let tests =
    testList
        "AvailabilityTests"
        [
            test "Create an availability test" {
                let ai = appInsights { name "ai" }

                let availabilityTest =
                    availabilityTest {
                        name "avTest"
                        link_to_app_insights ai
                        timeout 60<Seconds>
                        frequency 800<Seconds>
                        locations [ AvailabilityTest.TestSiteLocation.CentralUS ]
                        web_test ("https://google.com" |> System.Uri |> AvailabilityTest.WebsiteUrl)
                    }

                let template = arm { add_resources [ availabilityTest; ai ] }
                let jsn = template.Template |> Writer.toJson
                let jobj = jsn |> Newtonsoft.Json.Linq.JObject.Parse

                let hasWebTest =
                    jobj.SelectToken("resources[?(@.name=='avTest')].properties.Configuration.WebTest")

                Expect.isNotNull hasWebTest "WebTest context missing"

                let availabilityLocation =
                    jobj.SelectToken("resources[?(@.name=='avTest')].properties.Locations[0].Id")

                Expect.equal (availabilityLocation.ToString()) "us-fl-mia-edge" "WebTest location incorrect"
                let dependsAi = jobj.SelectToken("resources[?(@.name=='avTest')].dependsOn")
                Expect.isNotNull dependsAi "AppInsights dependency missing"
            }
        ]
