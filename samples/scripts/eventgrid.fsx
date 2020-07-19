#r @"./libs/Newtonsoft.Json.dll"
#r @"../../src/Farmer/bin/Debug/netstandard2.0/Farmer.dll"

open Farmer
open Farmer.Builders

/// Monitor this storage account
let storageSource = storageAccount { name "isaacstorageacc"; add_private_container "data" }

/// Send events to this event hub
let destinationHub = eventHub { name "isaachub"; namespace_name "isaacns" }

/// Tie them together using event grid.
let eventHubGrid = eventGrid {
    topic_name "isaacHubTopic"
    source storageSource
    add_eventhub_subscriber destinationHub [ SystemEvents.Storage.BlobCreated; SystemEvents.Storage.BlobDeleted ]
}

let deployment = arm {
    add_resources [
        storageSource
        eventHubGrid
        destinationHub
    ]
}

// Generate the ARM template here...
deployment
|> Writer.quickWrite "farmer-deploy"