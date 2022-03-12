module AppInsights

open Expecto
open Farmer
open Farmer.Builders.AppInsights
open Farmer.Builders.LogAnalytics
open Newtonsoft.Json.Linq

let tests = testList "AppInsights" [
    test "Creates keys on an AI instance correctly" {
        let ai = appInsights { name "foo" }
        Expect.equal ai.InstrumentationKey.Owner.Value.ArmExpression.Value "resourceId('Microsoft.Insights/components', 'foo')" "Incorrect owner"
        Expect.equal ai.InstrumentationKey.Value ("reference(resourceId('Microsoft.Insights/components', 'foo'), '2014-04-01').InstrumentationKey") "Incorrect Value"
    }

    test "Creates with classic version by default" {
        let deployment = arm { add_resource (appInsights { name "foo" }) }
        let json = deployment.Template |> Writer.toJson |> JObject.Parse
        let version = json.SelectToken("resources[?(@.name=='foo')].apiVersion").ToString()
        Expect.equal version "2014-04-01" "Incorrect API version"
    }

    test "Create generated keys correctly" {
        let generatedKey = AppInsights.getInstrumentationKey(ResourceId.create(Arm.Insights.components, ResourceName "foo", "group"))
        Expect.equal generatedKey.Value "reference(resourceId('group', 'Microsoft.Insights/components', 'foo'), '2014-04-01').InstrumentationKey" "Incorrect generated key"
    }

    test "Creates LA-enabled workspace" {
        let workspace = logAnalytics { name "la" }
        let ai = appInsights { name "ai"; log_analytics_workspace workspace }
        let deployment = arm {
            add_resources [ workspace; ai ]
        }
        let json = deployment.Template |> Writer.toJson |> JObject.Parse
        let version = json.SelectToken("resources[?(@.name=='ai')].apiVersion").ToString()
        let resourceId = json.SelectToken("resources[?(@.name=='ai')].properties.WorkspaceResourceId").ToString()
        let dependencies = json.SelectToken("resources[?(@.name=='ai')].dependsOn").Children() |> Seq.map string |> Seq.toArray

        Expect.equal resourceId "[resourceId('Microsoft.OperationalInsights/workspaces', 'la')]" "Incorrect workspace id"
        Expect.equal version "2020-02-02-preview" "Incorrect API version"
        Expect.equal ai.InstrumentationKey.Value ("reference(resourceId('Microsoft.Insights/components', 'ai'), '2020-02-02-preview').InstrumentationKey") "Incorrect Instrumentation Key reference"
        Expect.sequenceEqual dependencies [ "[resourceId('Microsoft.OperationalInsights/workspaces', 'la')]" ] "Incorrect dependencies"
   }
]