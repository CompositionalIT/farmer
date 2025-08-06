module PrometheusRuleGroup

open Expecto
open Farmer
open Farmer.Builders

let tests =
    testList "Prometheus Rule Group" [
        test "Create prometheus rule group" {
            let myRule = prometheusRule {
                record (Some "myRecord")
                expression "up == 1"
            }

            let myGroup = prometheusRuleGroup {
                name "myGroup"
                add_rules [ myRule ]
            }

            let template = arm { add_resources [ myGroup ] }
            let jsn = template.Template |> Writer.toJson
            let jobj = jsn |> Newtonsoft.Json.Linq.JObject.Parse

            let actualRules =
                jobj.SelectToken("resources[?(@.name=='myGroup')].properties.rules").ToString()

            Expect.isNotNull actualRules "Expected rules is not null"
        }
    ]