module DataCollection

open Expecto
open Farmer
open Farmer.Builders
open Farmer.Arm.Monitor
open System

let tests =

    testList "DataCollection" [
        test "Create data collection endpoint" {
            let myEndpoint = dataCollectionEndpoint {
                name "myEndpoint"
                os_type OS.Linux
            }

            let template = arm { add_resources [ myEndpoint ] }
            let jsn = template.Template |> Writer.toJson
            let jobj = jsn |> Newtonsoft.Json.Linq.JObject.Parse

            let kind = jobj.SelectToken("resources[?(@.name=='myEndpoint')].kind").ToString()

            Expect.equal kind "Linux" "Expected Linux OS type"
        }

        test "Create data collection rule with prometheus forwarder" {
            let rule = dataCollectionRule {
                name "myRule"
                os_type OS.Linux
                endpoint (dataCollectionEndpoints.resourceId "myEndpoint")

                data_flows [
                    {
                        Streams = [ Stream.InsightsMetrics ]
                        Destinations = [ "Account1" ]
                    }
                ]

                data_sources {
                    PrometheusForwarder =
                        Some(
                            [
                                {
                                    Name = "PrometheusForwarder"
                                    Streams = [ "Microsoft-PrometheusMetrics" ]
                                    LabelIncludeFilter = None
                                }
                            ]
                        )
                }

                destinations {
                    MonitoringAccounts =
                        Some(
                            [
                                {
                                    AccountResourceId = dataCollectionEndpoints.resourceId "myAccount"
                                    Name = ResourceName "myAccount"
                                }
                            ]
                        )
                }
            }

            let template = arm { add_resources [ rule ] }
            let jsn = template.Template |> Writer.toJson
            let jobj = jsn |> Newtonsoft.Json.Linq.JObject.Parse

            let isLinux = jobj.SelectToken("resources[?(@.name=='myRule')].kind").ToString()

            Expect.equal isLinux "Linux" "Expected Linux OS type"
        }
    ]