#r @"./libs/Newtonsoft.Json.dll"
#r @"../../src/Farmer/bin/Debug/netstandard2.0/Farmer.dll"

open Farmer
open Farmer.Builders

let queueName = "events"

let storageSource = storageAccount { name "isaacstorageacc"; add_private_container "data" }
let destionationHub = eventHub { name "isaachub"; namespace_name "isaacns" }
let destinationStorage = storageAccount { name "destinationstorage"; add_queue queueName; add_private_container "events" }

let eventHubGrid = eventGrid {
    topic_name "isaacHubTopic"
    source storageSource
    add_eventhub_subscriber destionationHub [ SystemEvents.Storage.BlobCreated; SystemEvents.Storage.BlobDeleted ]
    add_queue_subscriber destinationStorage queueName [ SystemEvents.Storage.BlobCreated ]
}

let template =
    arm {
        add_resources [
            storageSource
            eventHubGrid
            destinationStorage
            destionationHub
        ]
    }

template
|> Writer.quickWrite "generated-template"

template
|> Deploy.execute "my-resource-group" []
|> ignore