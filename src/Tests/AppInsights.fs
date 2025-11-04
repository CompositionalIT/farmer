module AppInsights

open Expecto
open Farmer
open Farmer.Builders.AppInsights
open Farmer.Builders.LogAnalytics
open Newtonsoft.Json.Linq

let tests =
    testList "AppInsights" [
        test "Creates keys on an AI instance correctly" {
            let ai = appInsights { name "foo" }

            Expect.equal
                ai.InstrumentationKey.Owner.Value.ArmExpression.Value
                "resourceId('Microsoft.Insights/components', 'foo')"
                "Incorrect owner"

            Expect.equal
                ai.InstrumentationKey.Value
                ("reference(resourceId('Microsoft.Insights/components', 'foo'), '2014-04-01').InstrumentationKey")
                "Incorrect Value"
        }

        test "Creates with classic version by default" {
            let deployment = arm { add_resource (appInsights { name "foo" }) }
            let json = deployment.Template |> Writer.toJson |> JObject.Parse
            let version = json.SelectToken("resources[?(@.name=='foo')].apiVersion").ToString()
            Expect.equal version "2014-04-01" "Incorrect API version"
        }

        test "Create generated keys correctly" {
            let generatedKey =
                AppInsights.getInstrumentationKey (
                    ResourceId.create (Arm.Insights.components, ResourceName "foo", "group")
                )

            Expect.equal
                generatedKey.Value
                "reference(resourceId('group', 'Microsoft.Insights/components', 'foo'), '2014-04-01').InstrumentationKey"
                "Incorrect generated key"
        }

        test "Creates LA-enabled workspace" {
            let workspace = logAnalytics { name "la" }

            let ai = appInsights {
                name "ai"
                log_analytics_workspace workspace
            }

            let deployment = arm { add_resources [ workspace; ai ] }

            let json = deployment.Template |> Writer.toJson |> JObject.Parse
            let select query = json.SelectToken(query).ToString()

            Expect.equal
                (select "resources[?(@.name=='ai')].properties.WorkspaceResourceId")
                "[resourceId('Microsoft.OperationalInsights/workspaces', 'la')]"
                "Incorrect workspace id"

            Expect.equal (select "resources[?(@.name=='ai')].apiVersion") "2020-02-02" "Incorrect API version"

            Expect.equal
                ai.InstrumentationKey.Value
                ("reference(resourceId('Microsoft.Insights/components', 'ai'), '2020-02-02').InstrumentationKey")
                "Incorrect Instrumentation Key reference"

            Expect.sequenceEqual
                (json.SelectToken("resources[?(@.name=='ai')].dependsOn").Children()
                 |> Seq.map string
                 |> Seq.toArray)
                [ "[resourceId('Microsoft.OperationalInsights/workspaces', 'la')]" ]
                "Incorrect dependencies"
        }

        test "production_sampling sets 20% sampling" {
            let ai = appInsights {
                name "prod-ai"
                production_sampling
            }

            Expect.equal ai.SamplingPercentage 20 "Should set sampling to 20%"
        }

        test "development_sampling sets 100% sampling" {
            let ai = appInsights {
                name "dev-ai"
                development_sampling
            }

            Expect.equal ai.SamplingPercentage 100 "Should set sampling to 100%"
        }

        test "Default sampling is 100%" {
            let ai = appInsights { name "default-ai" }

            Expect.equal ai.SamplingPercentage 100 "Default sampling should be 100%"
        }

        test "Sampling percentage can be set explicitly" {
            let ai = appInsights {
                name "custom-ai"
                sampling_percentage 50
            }

            Expect.equal ai.SamplingPercentage 50 "Should set custom sampling percentage"
        }

        test "production_sampling can be overridden" {
            let ai = appInsights {
                name "override-ai"
                production_sampling
                sampling_percentage 30  // Override the default 20
            }

            Expect.equal ai.SamplingPercentage 30 "Should allow overriding production sampling"
        }

        test "Sampling percentage validation fails for values > 100" {
            Expect.throws
                (fun () ->
                    appInsights {
                        name "test"
                        sampling_percentage 101
                    }
                    |> ignore)
                "Should throw for sampling > 100%"
        }

        test "Sampling percentage validation fails for values <= 0" {
            Expect.throws
                (fun () ->
                    appInsights {
                        name "test"
                        sampling_percentage 0
                    }
                    |> ignore)
                "Should throw for sampling <= 0%"
        }
    ]