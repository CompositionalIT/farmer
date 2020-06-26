---
title: "Service Bus"
date: 2020-02-05T08:53:46+01:00
weight: 19
chapter: false
---

#### Overview
The Service Bus builder creates service bus namespaces and their associated queues.

* Service Bus Namespaces (`Microsoft.ServiceBus/namespaces`)

#### Builder Keywords

| Applies To | Keyword | Purpose |
|-|-|-|
| Queue | name | The name of the queue. |
| Queue | lock_duration_minutes | The length of time that a lock can be held on a message. |
| Queue | max_delivery_count | The maximum number of times a message can be delivered before dead lettering. |
| Queue | duplicate_detection_minutes | Whether to enable duplicate detection, and if so, how long to check for. |
| Queue | enable_session | Enables session support. |
| Queue | enable_dead_letter_on_message_expiration | Enables dead lettering of messages that expire. |
| Queue | enable_partition | Enables partition support on the queue. |
| Queue | link_to_namespace | Link this queue to an existing namespace instead of creating a new one. |
| Namespace | sku | The ServiceBusNamespaceSku e.g. Standard |
| Namespace | namespace_name | The name of the namespace that holds the queue. |
| Namespace | depends_on | Adds a resource that the service bus depends on. |

#### Configuration Members

| Member | Purpose |
|-|-|
| NamespaceDefaultConnectionString  | Returns an ARM expression to retrieve the Primary Connection String of the service bus. |
| DefaultSharedAccessPolicyPrimaryKey | Returns an ARM expression to retrieve the Primary Key of the service bus. |

#### Example

```fsharp
open Farmer
open Farmer.Builders
open Farmer.ServiceBus

let myServiceBus = serviceBus {
    name "my-namespace"
    sku Standard
    add_queues [
        queue { name "queueA" }
        queue { name "queueB" }
    ]
    add_topics [
        topic { name "topicA" }
        topic { name "topicB" }
    ]
}

let deployment = arm {
    location Location.NorthEurope
    add_resource myServiceBus
    output "connectionString" myServiceBus.NamespaceDefaultConnectionString
    output "defaultSharedAccessPolicyPrimaryKey" myServiceBus.DefaultSharedAccessPolicyPrimaryKey
}
```
