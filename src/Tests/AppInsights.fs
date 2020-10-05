module AppInsights

open Expecto
open Farmer
open Farmer.Builders.AppInsights
open Farmer.CoreTypes

let tests = testList "AppInsights" [
    test "Creates keys on an AI instance correctly" {
        let ai = appInsights { name "foo" }
        Expect.equal ai.InstrumentationKey.Owner.Value.ArmExpression.Value "resourceId('Microsoft.Insights/components', 'foo')" "Incorrect owner"
        Expect.equal ai.InstrumentationKey.Value ("reference(resourceId('Microsoft.Insights/components', 'foo'), '2014-04-01').InstrumentationKey") "Incorrect Value"
    }

    test "Create generated keys correctly" {
        let generatedKey = AppInsights.getInstrumentationKey(ResourceId.create("foo", "group"))
        Expect.equal generatedKey.Value "reference(resourceId('group', 'Microsoft.Insights/components', 'foo'), '2014-04-01').InstrumentationKey" "Incorrect generated key"
    }
]