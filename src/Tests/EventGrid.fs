module EventGrid

open Expecto
open Farmer
open Farmer.Builders
open Farmer.Arm
open Microsoft.Rest
open System

let tests = testList "Event Grid" [
    test "Creates topics correctly" {
        let b = eventGrid { topic_name "my-topic" } :> IBuilder
        let resources = b.BuildResources Location.WestEurope
        let t = resources.[0] :?> Topic
        Expect.equal t.Location Location.WestEurope "Incorrect location"
        Expect.equal t.Name (ResourceName "my-topic") "Incorrect name"
    }
    test "Creates a storage source correctly" {
        let storage = storageAccount { name "test" }
        let g = eventGrid { topic_name "topic-test"; source storage }
        Expect.equal g.Source (ResourceName "test", Topics.StorageAccount) "Invalid Source"
    }
    test "Creates a queues subscriber correctly" {
        let storage = storageAccount { name "test" }
        let g = eventGrid { add_queue_subscriber "queue-sub" storage "test-queue" [ SystemEvents.Storage.BlobCreated ] }
        let sub = g.Subscriptions.[0]
        Expect.equal sub.Name (ResourceName "queue-sub") "Incorrect subscription name"
        Expect.equal sub.Endpoint (EndpointType.StorageQueue "test-queue") "Incorrect endpoint type"
        Expect.equal sub.Destination storage.Name "Incorrect destination"
        Expect.equal sub.SystemEvents [ (SystemEvents.Storage.BlobCreated :> IEventGridEvent).ToEvent ] "Incorrect system events"
    }
]