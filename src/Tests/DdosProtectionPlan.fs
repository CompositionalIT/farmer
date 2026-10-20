module DdosProtectionPlan

open Expecto
open Farmer
open Farmer.Builders
open Farmer.Arm.Network
open Newtonsoft.Json.Linq

let tests =
    testList "DDoS Protection Plan" [
        test "Creates a basic DDoS Protection Plan" {
            let ddos = ddosProtectionPlan { name "my-ddos-plan" }
            let deployment = arm { add_resources [ ddos ] }

            let jobj = deployment.Template |> Writer.toJson |> JToken.Parse
            let ddosResource = jobj.SelectToken("resources[?(@.type=='Microsoft.Network/ddosProtectionPlans')]")

            Expect.isNotNull ddosResource "DDoS Protection Plan resource should exist"
            Expect.equal (ddosResource.SelectToken("name").ToString()) "my-ddos-plan" "Name should be correct"
        }

        test "DDoS Protection Plan can have tags" {
            let ddos =
                ddosProtectionPlan {
                    name "my-ddos-plan"
                    add_tags [ "environment", "production"; "cost-center", "security" ]
                }

            let deployment = arm { add_resources [ ddos ] }
            let jobj = deployment.Template |> Writer.toJson |> JToken.Parse
            let ddosResource = jobj.SelectToken("resources[?(@.type=='Microsoft.Network/ddosProtectionPlans')]")

            let tags = ddosResource.SelectToken("tags")
            Expect.isNotNull tags "Tags should exist"
            Expect.equal (tags.SelectToken("environment").ToString()) "production" "Environment tag should be correct"
            Expect.equal
                (tags.SelectToken("cost-center").ToString())
                "security"
                "Cost-center tag should be correct"
        }

        test "DDoS Protection Plan has correct resource ID" {
            let ddos = ddosProtectionPlan { name "test-plan" }
            let resourceId = (ddos :> IBuilder).ResourceId

            Expect.equal resourceId.Type.Type "Microsoft.Network/ddosProtectionPlans" "Type should be correct"
            Expect.equal resourceId.Name.Value "test-plan" "Name should be correct"
        }
    ]
