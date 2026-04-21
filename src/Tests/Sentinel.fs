module Sentinel

open Expecto
open Farmer
open Farmer.Builders
open Newtonsoft.Json.Linq

let tests =
    testList "Azure Sentinel" [
        test "Enables Sentinel on a Log Analytics Workspace" {
            let workspace = logAnalytics { name "security-workspace" }

            let sentinel = sentinel { link_to_workspace workspace }

            let deployment = arm { add_resources [ workspace; sentinel ] }
            let jobj = deployment.Template |> Writer.toJson |> JToken.Parse

            let sentinelResource =
                jobj.SelectToken("resources[?(@.type=='Microsoft.SecurityInsights/onboardingStates')]")

            Expect.isNotNull sentinelResource "Sentinel resource should exist"

            Expect.equal
                (sentinelResource.SelectToken("name").ToString())
                "security-workspace/default"
                "Name should be workspace/default"
        }

        test "Sentinel depends on workspace" {
            let workspace = logAnalytics { name "test-workspace" }

            let sentinel = sentinel { link_to_workspace workspace }

            let deployment = arm { add_resources [ workspace; sentinel ] }
            let jobj = deployment.Template |> Writer.toJson |> JToken.Parse

            let sentinelResource =
                jobj.SelectToken("resources[?(@.type=='Microsoft.SecurityInsights/onboardingStates')]")

            let dependsOn = sentinelResource.SelectToken("dependsOn")
            Expect.isNotNull dependsOn "DependsOn should exist"
            Expect.isTrue (dependsOn.ToString().Contains("test-workspace")) "Should depend on workspace"
        }

        test "Sentinel can reference existing workspace by name" {
            let sentinel = sentinel { workspace_name "existing-workspace" }

            let deployment = arm { add_resources [ sentinel ] }
            let jobj = deployment.Template |> Writer.toJson |> JToken.Parse

            let sentinelResource =
                jobj.SelectToken("resources[?(@.type=='Microsoft.SecurityInsights/onboardingStates')]")

            Expect.equal
                (sentinelResource.SelectToken("name").ToString())
                "existing-workspace/default"
                "Name should include existing workspace"
        }
    ]
