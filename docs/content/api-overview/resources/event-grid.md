---
title: "Event Grid"
date: 2020-07-06T20:00:00+01:00
chapter: false
weight: 9
---

## Overview
The Event Grid is a simple but powerful builder that links events from Azure services such as Storage and App Service to one or many subscribers which can consume the events. The event grid builder supports a degree of type safety - all system events are provided from a strongly-typed list, and events are directly linked to specific builders - so, for example, you cannot accidentally subscribe to Storage Account events if the event publisher is a Web App. It supports the following ARM resources.

* Topics (`Microsoft.EventGrid/systemTopics`)
* Subscriptions (`Microsoft.EventGrid/systemTopics/eventSubscriptions`)

### Builder Keywords
|  Keyword | Purpose |
|-|-|
| topic_name | The name of the topic that will be created. |
| source | The source of the events. See below for the full list of builder configurations that are supported. |
| add_queue_subscriber | Adds a new storage queue subscriber. Requires the storage account config that will receive the events, the queue name and the list of events to subscribe to. |
| add_webhook_subscriber| Adds a new web hook (HTTP) subscriber. Requires the web app config that will receive the event, associated URI local path and the list of events to subscribe to. Also contains an overload that takes in a Web App name and the full Uri of the web hook. |
| add_eventhub_subscriber| Adds a new event hub subscriber. Requiresthe event hub builder config that will receive the events and the list of events to subscribe to. |

### Supported Sources
Farmer supports the following Event Grid sources using Farmer builders:

| Builder | Events namespace |
|-|-|-|
| [StorageAccount](storage-account) | SystemEvents.Storage |
| [WebApp](web-app) | SystemEvents.AppServer |
| [KeyVault](keyvault) | SystemEvents.KeyVault |
| [SignalR](signalr) | SystemEvents.SignalR |
| [Maps](maps) | SystemEvents.Maps |
| [ContainerRegistry](container-registry) | SystemEvents.ContainerRegistry |
| [ServiceBus](service-bus) | SystemEvents.ServiceBus |
| [IotHub](iot-hub) | SystemEvents.IotHub |
| [EventHub](eventhub) | SystemEvents.EventHub |

#### Example
The following sample creates a source storage account that emits events on the event grid topic, whilst two destinations are created: an event hub and a storage queue, each listening for different events.

```fsharp
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
```