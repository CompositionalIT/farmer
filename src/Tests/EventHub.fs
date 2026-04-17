module EventHub

open Expecto
open Farmer
open Farmer.Builders
open Farmer.EventHub

let tests =
    testList "EventHub" [
        test "Gets key on a Hub correctly" {
            let hub = eventHub { name "foo" }

            Expect.equal
                hub.DefaultKey.Owner.Value.ArmExpression.Value
                "resourceId('Microsoft.EventHub/namespaces/eventhubs', 'foo')"
                "Incorrect owner"

            Expect.equal
                hub.DefaultKey.Value
                "listkeys(resourceId('Microsoft.EventHub/namespaces/AuthorizationRules', 'foo-ns', 'RootManageSharedAccessKey'), '2017-04-01').primaryConnectionString"
                "Incorrect key"
        }
        test "Gets default connection string on a Hub correctly" {
            let hub = eventHub { name "foo" }

            Expect.equal
                hub.DefaultConnectionString.Value
                "listkeys(resourceId('Microsoft.EventHub/namespaces/AuthorizationRules', 'foo-ns', 'RootManageSharedAccessKey'), '2017-04-01').primaryConnectionString"
                "Incorrect default connection string"
        }
        test "Gets connection string for named authorization rule correctly" {
            let hub = eventHub {
                name "foo"
                add_authorization_rule "MyRule" [ Listen; Send ]
            }

            Expect.equal
                (hub.GetConnectionString "MyRule").Value
                "listkeys(resourceId('Microsoft.EventHub/namespaces/eventhubs/AuthorizationRules', 'foo-ns', 'foo', 'MyRule'), '2017-04-01').primaryConnectionString"
                "Incorrect connection string for named rule"
        }
        test "Does not explicitly create default consumer group" {
            let hub = eventHub {
                name "test-event-hub"
                // When using Basic tier, attempting to explicitly create a "$Default" consumer group
                // will give an error because Basic doesn't support creating consumer groups.
                sku EventHubSku.Basic
            }

            let defaultResourceName = ResourceName "$Default"
            let defaultConsumerGroupExists = hub.ConsumerGroups.Contains defaultResourceName
            Expect.isFalse defaultConsumerGroupExists "Created a default consumer group"
        }
    ]