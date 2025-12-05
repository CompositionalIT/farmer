module DefenderForCloud

open Expecto
open Farmer
open Farmer.Builders
open Farmer.Arm.Security
open Newtonsoft.Json.Linq

let tests =
    testList "Defender for Cloud" [
        test "Enables Defender for Virtual Machines" {
            let defender = defenderForCloud { plan DefenderPlan.VirtualMachines }

            let deployment = arm { add_resource defender }
            let jobj = deployment.Template |> Writer.toJson |> JToken.Parse

            let defenderResource = jobj.SelectToken("resources[?(@.type=='Microsoft.Security/pricings')]")

            Expect.isNotNull defenderResource "Defender resource should exist"
            Expect.equal (defenderResource.SelectToken("name").ToString()) "VirtualMachines" "Plan should be VirtualMachines"

            Expect.equal
                (defenderResource.SelectToken("properties.pricingTier").ToString())
                "Standard"
                "Tier should be Standard"
        }

        test "Can enable multiple Defender plans" {
            let vmDefender = defenderForCloud { plan DefenderPlan.VirtualMachines }
            let sqlDefender = defenderForCloud { plan DefenderPlan.SqlServers }
            let storageDefender = defenderForCloud { plan DefenderPlan.StorageAccounts }

            let deployment = arm { add_resources [ vmDefender; sqlDefender; storageDefender ] }

            let jobj = deployment.Template |> Writer.toJson |> JToken.Parse
            let resources = jobj.SelectTokens("resources[?(@.type=='Microsoft.Security/pricings')]")

            Expect.equal (Seq.length resources) 3 "Should have 3 Defender plans"
        }

        test "Can disable a Defender plan" {
            let defender =
                defenderForCloud {
                    plan DefenderPlan.AppServices
                    disable
                }

            let deployment = arm { add_resource defender }
            let jobj = deployment.Template |> Writer.toJson |> JToken.Parse

            let defenderResource = jobj.SelectToken("resources[?(@.type=='Microsoft.Security/pricings')]")

            Expect.equal
                (defenderResource.SelectToken("properties.pricingTier").ToString())
                "Free"
                "Tier should be Free when disabled"
        }

        test "Can explicitly enable a Defender plan" {
            let defender =
                defenderForCloud {
                    plan DefenderPlan.Containers
                    enable
                }

            let deployment = arm { add_resource defender }
            let jobj = deployment.Template |> Writer.toJson |> JToken.Parse

            let defenderResource = jobj.SelectToken("resources[?(@.type=='Microsoft.Security/pricings')]")

            Expect.equal
                (defenderResource.SelectToken("properties.pricingTier").ToString())
                "Standard"
                "Tier should be Standard when enabled"
        }
    ]
