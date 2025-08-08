module DataCollection

open Expecto
open Farmer
open Farmer.Builders
open Farmer.Arm.Monitor
open System

let tests =

    testList "Data Collection" [
        test "Create data collection endpoint" {
            let myEndpoint = dataCollectionEndpoint {
                name "myEndpoint"
                os_type OS.Linux
            }

            let template = arm { add_resources [ myEndpoint ] }
            let jsn = template.Template |> Writer.toJson
            let jobj = jsn |> Newtonsoft.Json.Linq.JObject.Parse

            let name = jobj.SelectToken("resources[?(@.name=='myEndpoint')].name").ToString()
            let kind = jobj.SelectToken("resources[?(@.name=='myEndpoint')].kind").ToString()


            Expect.equal kind "Linux" "Expected Linux OS type"
            Expect.equal name "myEndpoint" "Expected endpoint name"
        }

        test "Create data collection rule with prometheus forwarder" {
            let myEndpoint = dataCollectionEndpoint {
                name "myEndpoint"
                os_type OS.Linux
            }

            let rule = dataCollectionRule {
                name "myRule"
                os_type OS.Linux
                endpoint (myEndpoint :> IBuilder).ResourceId

                data_flows [
                    {
                        Streams = [ (CustomStream "Microsoft-PrometheusMetrics") ]
                        Destinations = [ "Account1" ]
                    }
                ]

                data_sources [
                    PrometheusForwarder [
                        {
                            Name = "PrometheusForwarder"
                            Streams = [ "Microsoft-PrometheusMetrics" ]
                        }
                    ]
                ]

                destinations [
                    MonitoringAccounts [
                        {
                            AccountResourceId = dataCollectionEndpoints.resourceId "myAccount"
                            Name = ResourceName "myAccount"
                        }
                    ]
                ]
            }

            let template = arm { add_resources [ rule ] }
            let jsn = template.Template |> Writer.toJson
            let jobj = jsn |> Newtonsoft.Json.Linq.JObject.Parse

            let isLinux = jobj.SelectToken("resources[?(@.name=='myRule')].kind").ToString()

            let actualEndpoint =
                jobj
                    .SelectToken("resources[?(@.name=='myRule')].properties.dataCollectionEndpointId")
                    .ToString()

            let actualDependsOn =
                jobj.SelectToken("resources[?(@.name=='myRule')].dependsOn[0]").ToString()

            let actualDataFlows =
                jobj
                    .SelectToken("resources[?(@.name=='myRule')].properties.dataFlows")
                    .ToString()

            let actualDataSources =
                jobj
                    .SelectToken("resources[?(@.name=='myRule')].properties.dataSources")
                    .ToString()

            let actualDestinations =
                jobj
                    .SelectToken("resources[?(@.name=='myRule')].properties.destinations")
                    .ToString()

            let actualPrometheusForwarder =
                jobj
                    .SelectToken("resources[?(@.name=='myRule')].properties.dataSources.prometheusForwarder")
                    .ToString()

            Expect.equal isLinux "Linux" "Expected Linux OS type"
            Expect.equal actualEndpoint ((myEndpoint :> IBuilder).ResourceId.Eval()) "Expected matching endpoint Id"

            Expect.equal
                actualDependsOn
                ((dataCollectionEndpoints.resourceId "myEndpoint").Eval())
                "Expected matching endpoint dependency"

            Expect.isNotNull actualDataFlows "Expected data flows to be present"
            Expect.isNotNull actualDataSources "Expected data sources to be present"
            Expect.isNotNull actualDestinations "Expected destinations to be present"
            Expect.isNotNull actualPrometheusForwarder "Expected Prometheus forwarder to be present"
        }

        test "Create data collection rule without endpoint throws" {
            Expect.throws
                (fun _ ->
                    dataCollectionRule {
                        name "myRule"
                        os_type OS.Linux
                    }
                    |> ignore)
                "An `endpoint` must be specified for data collection rule."
        }

        test "Create data collection rule association with aks resource" {
            let myAks = aks {
                name "myAks"
                service_principal_use_msi
                enable_azure_monitor
            }

            let myRule = dataCollectionRule {
                name "myRule"
                os_type OS.Linux
                endpoint (dataCollectionEndpoints.resourceId "myEndpoint")
            }

            let expectedRuleId = (myRule :> IBuilder).ResourceId

            let ruleAssociation = dataCollectionRuleAssociation {
                name "myRuleAssociation"
                associated_resource ((myAks :> IBuilder).ResourceId)
                rule_id expectedRuleId
            }

            let template = arm { add_resources [ myAks; ruleAssociation ] }
            let jsn = template.Template |> Writer.toJson
            let jobj = jsn |> Newtonsoft.Json.Linq.JObject.Parse

            let actualRuleId =
                jobj
                    .SelectToken("resources[?(@.name=='myRuleAssociation')].properties.dataCollectionRuleId")
                    .ToString()

            let actualDependsOn =
                jobj
                    .SelectToken("resources[?(@.name=='myRuleAssociation')].dependsOn")
                    .ToString()

            let actualDataCollectionRuleId =
                jobj
                    .SelectToken("resources[?(@.name=='myRuleAssociation')].properties.dataCollectionRuleId")
                    .ToString()

            Expect.equal actualRuleId (expectedRuleId.Eval()) "Expected matching rule Id"

            Expect.isTrue
                (actualDependsOn.Contains((myAks :> IBuilder).ResourceId.Eval()))
                "Expected associated aks resource to be in dependencies"

            Expect.isTrue (actualDependsOn.Contains(expectedRuleId.Eval())) "Expected rule Id to be in dependencies"
            Expect.equal actualDataCollectionRuleId (expectedRuleId.Eval()) "Expected matching data collection rule Id"
        }

        test "Create data collection rule association should throw if no rule id is specified" {
            let myAks = aks {
                name "myAks"
                service_principal_use_msi
                enable_azure_monitor
            }

            Expect.throws
                (fun _ ->
                    dataCollectionRuleAssociation {
                        name "myRuleAssociation"
                        associated_resource ((myAks :> IBuilder).ResourceId)
                    }
                    |> ignore)
                (sprintf
                    "Should have thrown an exception for not specifying rule id for data collection rule association")
        }

        test "Create data collection rule association should throw if no associated resource id is specified" {
            let myRule = dataCollectionRule {
                name "myRule"
                os_type OS.Linux
                endpoint (dataCollectionEndpoints.resourceId "myEndpoint")
            }

            let expectedRuleId = (myRule :> IBuilder).ResourceId

            Expect.throws
                (fun _ ->
                    dataCollectionRuleAssociation {
                        name "myRuleAssociation"
                        rule_id expectedRuleId
                    }
                    |> ignore)
                (sprintf
                    "Should have thrown an exception for not specifying associated resource id for data collection rule")
        }
    ]