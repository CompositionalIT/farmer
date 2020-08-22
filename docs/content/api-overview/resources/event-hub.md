---
title: "Event Hub"
date: 2020-02-23T20:00:00+01:00
chapter: false
weight: 10
---

#### Overview
The Event Hub builder creates event hub namespaces, event hubs, consumer groups and authorization rules in a single builder.

* Event Hub Namespace (`Microsoft.EventHub/namespaces`)
* Event Hub (`Microsoft.EventHub/namespaces/eventhubs`)
* Consumer Group (`Microsoft.EventHub/namespaces/eventhubs/consumergroups`)
* Authorization Rule (`Microsoft.EventHub/namespaces/eventhubs/AuthorizationRules"`)

> The Event Hub builder works in a similar fashion to the [web app](web-app) builder in that it automatically creates the host (in this case, the event hub *namespace*) when creating the event hub. If you wish to create multiple hubs in the same namespace, configure the namespace-level properties in the first event hub; subsequent event hubs should *link* to the namespace of the hub created by the first hub.

#### Builder Keywords
| Applies To | Keyword | Purpose |
|-|-|-|
| Namespace | namespace_name | Sets the name of the event hub namespace, if you are creating the namespace along with the hub. |
| Namespace | sku | Sets the SKU of the event hub namespace. |
| Namespace | capacity | Sets the capacity of the event hub namespace (see [here](https://docs.microsoft.com/en-gb/azure/event-hubs/event-hubs-faq#dedicated-clusters) for more details) |
| Namespace | enable_zone_redundant | Enables zone redundancy on the event hub namespace. |
| Namespace | enable_auto_inflate | Enables auto inflate throughput; you must supply the maximum throughput level. |
| Namespace | disable_auto_inflate | Disables auto inflate throughput. |
| Namespace | disable_kafka | Disables Kafka support. |
| Event Hub | name | Sets the name of the event hub. |
| Event Hub | message_retention_days | Sets the number of days to retain messages for on the event hub. |
| Event Hub | partitions | Sets the number of partitions on the event hub. |
| Event Hub | add_consumer_group | Creates a consumer group for the event hub. |
| Event Hub | add_authorization_rule | Adds a named authorization rule on the event hub. |
| Event Hub | link_to_namespace | Sets the name of an existing or already-defined event hub namespace that this event hub should link to. |
| Event Hub | capture_to_storage | Activates [Event Hub data capture](https://docs.microsoft.com/en-us/azure/event-hubs/event-hubs-capture-overview) to a Storage Account. Takes in a storage account or resource name, and the container to write events to.

#### Configuration Members
| Member | Purpose |
|-|-|
| DefaultKey | Gets an ARM expression for the root namespace key of the Event Hub namespace. |
| GetKey | Gets an ARM expression for a named key on this event hub. |

#### Example

```fsharp
open Farmer
open Farmer.Builders

let primaryHub = eventHub {
    namespace_name "allmyevents"
    sku EventHub.Standard
    disable_kafka
    enable_zone_redundant
    enable_auto_inflate 3
    add_authorization_rule "FirstRule" [ EventHub.Listen; EventHub.Send ]
    add_authorization_rule "SecondRule" AllAuthorizationRights

    name "first-hub"
    partitions 2
    message_retention_days 3
    add_consumer_group "myGroup"
}

let secondHub = eventHub {
    name "second-hub"
    link_to_namespace "allmyevents"
    partitions 1
    message_retention_days 1
    capture_to_storage myStorageAccount "mycontainer"
}
```