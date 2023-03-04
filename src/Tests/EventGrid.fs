module EventGrid

open Expecto
open Farmer
open Farmer.Builders
open Farmer.Arm
open Microsoft.Rest
open System

let tests =
    testList "Event Grid" [
        test "Creates topics correctly" {
            let b = eventGrid { topic_name "my-topic" } :> IBuilder
            let resources = b.BuildResources Location.WestEurope
            let t = resources.[0] :?> Topic
            Expect.equal t.Location Location.WestEurope "Incorrect location"
            Expect.equal t.Name (ResourceName "my-topic") "Incorrect name"
        }
        test "Creates a storage source correctly" {
            let storage = storageAccount { name "test" }

            let grid = eventGrid {
                topic_name "topic-test"
                source storage
            }

            Expect.equal grid.Source (ResourceName "test", Topics.StorageAccount) "Invalid Source"
        }
        test "Creates a queue subscriber correctly" {
            let storage = storageAccount { name "test" }

            let grid = eventGrid { add_queue_subscriber storage "thequeue" [ SystemEvents.Storage.BlobCreated ] }

            let sub = grid.Subscriptions.[0]
            Expect.equal sub.Name (ResourceName "test-thequeue-queue") "Incorrect subscription name"
            Expect.equal sub.Endpoint (EndpointType.StorageQueue(ResourceName "thequeue")) "Incorrect endpoint type"
            Expect.equal sub.Destination (ResourceName "test") "Incorrect destination"
            Expect.equal sub.SystemEvents [ SystemEvents.Storage.BlobCreated ] "Incorrect system events"
        }
        test "Creates a webhook subscriber correctly" {
            let app = webApp { name "test" }
            let grid = eventGrid { add_webhook_subscriber app "api/events" [] }
            let sub = grid.Subscriptions.[0]
            Expect.equal sub.Name (ResourceName "test-/api/events-webhook") "Incorrect subscription name"

            Expect.equal
                sub.Endpoint
                (EndpointType.WebHook(Uri "https://test.azurewebsites.net/api/events"))
                "Incorrect endpoint type"

            Expect.equal sub.Destination (ResourceName "test") "Incorrect destination"
        }
        test "Creates an eventhub subscriber correctly" {
            let hub = eventHub {
                name "hub"
                namespace_name "ns"
            }

            let grid = eventGrid { add_eventhub_subscriber hub [] }
            let sub = grid.Subscriptions.[0]
            Expect.equal sub.Name (ResourceName "ns-hub-eventhub") "Incorrect subscription name"
            Expect.equal sub.Endpoint (EndpointType.EventHub hub.Name) "Incorrect endpoint type"
            Expect.equal sub.Destination hub.EventHubNamespaceName "Incorrect destination"
        }
        test "Creates a service bus queue subscriber correctly" {
            let q = queue { name "queuequeue" }

            let bus = serviceBus {
                name "busbus"
                add_queues [ q ]
            }

            let grid = eventGrid { add_servicebus_queue_subscriber bus q [] }
            let sub = grid.Subscriptions.[0]
            Expect.equal sub.Name (ResourceName "queuequeue-busbus-servicebus-queue") "Incorrect subscription name"

            Expect.equal
                sub.Endpoint
                (EndpointType.ServiceBus(ServiceBusEndpointType.Queue { Queue = q.Name; Bus = bus.Name }))
                "Incorrect endpoint type"

            Expect.equal sub.Destination q.Name "Incorrect destination"
        }
        test "Creates a service bus topic subscriber correctly" {
            let t = topic { name "topictopic" }

            let bus = serviceBus {
                name "busbus"
                add_topics [ t ]
            }

            let grid = eventGrid { add_servicebus_topic_subscriber bus t [] }
            let sub = grid.Subscriptions.[0]
            Expect.equal sub.Name (ResourceName "topictopic-busbus-servicebus-topic") "Incorrect subscription name"

            Expect.equal
                sub.Endpoint
                (EndpointType.ServiceBus(ServiceBusEndpointType.Topic { Topic = t.Name; Bus = bus.Name }))
                "Incorrect endpoint type"

            Expect.equal sub.Destination t.Name "Incorrect destination"
        }
    ]
