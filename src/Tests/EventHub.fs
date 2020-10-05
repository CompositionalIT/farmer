module EventHub

open Expecto
open Farmer.Builders

let tests = testList "EventHub" [
    test "Gets key on a Hub correctly" {
        let hub = eventHub { name "foo" }
        Expect.equal hub.DefaultKey.Owner.Value.ArmExpression.Value "resourceId('Microsoft.EventHub/namespaces/eventhubs', 'foo')" "Incorrect owner"
        Expect.equal hub.DefaultKey.Value "listkeys(resourceId('Microsoft.EventHub/namespaces/eventhubs/AuthorizationRules', 'foo-ns', 'RootManageSharedAccessKey'), '2017-04-01').primaryConnectionString" "Incorrect key"
    }
]