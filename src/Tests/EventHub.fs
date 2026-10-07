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
                "resourceId('Microsoft.EventHub/namespaces/eventhubs', 'foo-ns', 'foo')"
                "Incorrect owner"

            Expect.equal
                hub.DefaultKey.Value
                "listkeys(resourceId('Microsoft.EventHub/namespaces/AuthorizationRules', 'foo-ns', 'RootManageSharedAccessKey'), '2017-04-01').primaryConnectionString"
                "Incorrect key"
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
        test "Adds the namespace as a dependency of the event hub" {
            let hub = eventHub { name "foo" }
            let template = arm { add_resource hub }
            let json = Newtonsoft.Json.Linq.JObject.Parse(template.Template |> Writer.toJson)

            let eventHubResource = json.SelectToken("resources[?(@.name=='foo-ns/foo')]")

            Expect.equal
                (eventHubResource.SelectToken("dependsOn[0]").ToString())
                "[resourceId('Microsoft.EventHub/namespaces', 'foo-ns')]"
                "Event hub should depend on its namespace"
        }
    ]