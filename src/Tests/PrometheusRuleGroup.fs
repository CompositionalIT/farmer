module PrometheusRuleGroup

open Expecto
open Farmer
open Farmer.Builders

let tests =
    testList "Prometheus Rule Group" [
        test "Create prometheus rule group with rules" {
            let myRule1 = prometheusRule {
                record (Some "myRecord")
                expression "up == 1"
            }

            let myRule2 = prometheusRule {
                record (Some "myRecord1")
                expression "up == 1"
                labels (Some(Map [ "workload_type", "deployment" ]))
            }

            let monitoringAccountType =
                ResourceType("Microsoft.Monitor/accounts", "2025-05-03-preview")

            let monitorAccountId =
                ResourceId.create (monitoringAccountType, ResourceName "monitorAccount")

            let myGroup = prometheusRuleGroup {
                name "myGroup"
                add_rules [ myRule1; myRule2 ]
                azure_monitor_workspace_id monitorAccountId
            }

            let template = arm { add_resources [ myGroup ] }
            let jsn = template.Template |> Writer.toJson
            let jobj = jsn |> Newtonsoft.Json.Linq.JObject.Parse

            let actualRules =
                jobj.SelectToken("resources[?(@.name=='myGroup')].properties.rules").ToString()

            let actualRule1 =
                jobj
                    .SelectToken("resources[?(@.name=='myGroup')].properties.rules[0]")
                    .ToString()

            let actualRule2 =
                jobj
                    .SelectToken("resources[?(@.name=='myGroup')].properties.rules[1].labels")
                    .ToString()

            let actualScopes =
                jobj.SelectToken("resources[?(@.name=='myGroup')].properties.scopes").ToString()

            Expect.isNotNull actualRules "Expected rules is not null"
            Expect.isTrue (actualRule1.Contains("myRecord")) "Expected rule with record 'myRecord' exists"
            Expect.isTrue (actualRule2.Contains("workload_type")) "Expected rule with label 'workload_type' exists"
            Expect.isNotNull actualScopes "Expected scopes is not null"
            Expect.isTrue (actualScopes.Contains(monitorAccountId.Eval())) "Expected monitor workspace id in scopes"
        }

        test "Create prometheus rule group with rules and set interval" {
            let myRule1 = prometheusRule {
                record (Some "myRecord")
                expression "up == 1"
            }

            let monitoringAccountType =
                ResourceType("Microsoft.Monitor/accounts", "2025-05-03-preview")

            let monitorAccountId =
                ResourceId.create (monitoringAccountType, ResourceName "monitorAccount")

            let myGroup = prometheusRuleGroup {
                name "myGroup"
                add_rules [ myRule1 ]
                azure_monitor_workspace_id monitorAccountId
                interval (IsoDateTime "PT1M")
            }

            let template = arm { add_resources [ myGroup ] }
            let jsn = template.Template |> Writer.toJson
            let jobj = jsn |> Newtonsoft.Json.Linq.JObject.Parse

            let actualInterval =
                jobj
                    .SelectToken("resources[?(@.name=='myGroup')].properties.interval")
                    .ToString()

            Expect.isTrue (actualInterval.Contains("PT1M")) "Expected interval is set to PT1M"
        }

        test "Prometheus rule without expression throws" {
            Expect.throws
                (fun _ -> prometheusRule { record (Some "myRecord") } |> ignore)
                (sprintf "Should have thrown an exception for not specifying Prometheus rule expression")
        }

        test "Prometheus rule group without monitoring workspace id throws" {
            let myRule = prometheusRule {
                record (Some "myRecord")
                expression "up == 1"
            }

            Expect.throws
                (fun _ ->
                    prometheusRuleGroup {
                        name "myGroup"
                        add_rules [ myRule ]
                    }
                    |> ignore)
                (sprintf "Should have thrown an exception for not specifying monitoring workspace id")
        }

        test "Prometheus rule group without rules throws" {
            let monitoringAccountType =
                ResourceType("Microsoft.Monitor/accounts", "2025-05-03-preview")

            let monitorAccountId =
                ResourceId.create (monitoringAccountType, ResourceName "monitorAccount")

            Expect.throws
                (fun _ ->
                    prometheusRuleGroup {
                        name "myGroup"
                        azure_monitor_workspace_id monitorAccountId
                    }
                    |> ignore)
                (sprintf "Should have thrown an exception for not specifying rules")
        }

        test "Enable prometheus rule group" {
            let myRule1 = prometheusRule {
                record (Some "myRecord")
                expression "up == 1"
            }

            let monitoringAccountType =
                ResourceType("Microsoft.Monitor/accounts", "2025-05-03-preview")

            let monitorAccountId =
                ResourceId.create (monitoringAccountType, ResourceName "monitorAccount")

            let myGroup = prometheusRuleGroup {
                name "myGroup"
                add_rules [ myRule1 ]
                azure_monitor_workspace_id monitorAccountId
                enable_rule_group
            }

            let template = arm { add_resources [ myGroup ] }
            let jsn = template.Template |> Writer.toJson
            let jobj = jsn |> Newtonsoft.Json.Linq.JObject.Parse

            let actualEnabled =
                jobj
                    .SelectToken("resources[?(@.name=='myGroup')].properties.enabled")
                    .ToString()
                    .ToLower()

            Expect.isTrue (actualEnabled = "true") "Expected rule group to be enabled"
        }

        test "Edit cluster name for prometheus rule group" {
            let myRule1 = prometheusRule {
                record (Some "myRecord")
                expression "up == 1"
            }

            let monitoringAccountType =
                ResourceType("Microsoft.Monitor/accounts", "2025-05-03-preview")

            let monitorAccountId =
                ResourceId.create (monitoringAccountType, ResourceName "monitorAccount")

            let myGroup = prometheusRuleGroup {
                name "myGroup"
                add_rules [ myRule1 ]
                azure_monitor_workspace_id monitorAccountId
                enable_rule_group
                cluster_name (Some(ResourceName "myCluster"))
            }

            let template = arm { add_resources [ myGroup ] }
            let jsn = template.Template |> Writer.toJson
            let jobj = jsn |> Newtonsoft.Json.Linq.JObject.Parse

            let actualClusterName =
                jobj
                    .SelectToken("resources[?(@.name=='myGroup')].properties.clusterName")
                    .ToString()

            Expect.isTrue (actualClusterName = "myCluster") "Expected cluster name to be myCluster"
        }
    ]