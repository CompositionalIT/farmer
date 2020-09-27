---
title: "Service Bus"
date: 2020-02-05T08:53:46+01:00
chapter: false
weight: 20
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
| Queue | message_ttl | Time To Live (TTL) value for messages expressed as a TimeSpan string, such as '01:30:00' 1 hour, 30 minutes. |
| Queue | message_ttl_days | Time To Live (TTL) value for messages in days. |
| Subscription | name | The name of the subscription. |
| Subscription | lock_duration_minutes | The length of time that a lock can be held on a message. |
| Subscription | max_delivery_count | The maximum number of times a message can be delivered before dead lettering. |
| Subscription | duplicate_detection_minutes | Whether to enable duplicate detection, and if so, how long to check for. |
| Subscription | enable_session | Enables session support. |
| Subscription | enable_dead_letter_on_message_expiration | Enables dead lettering of messages that expire. |
| Subscription | enable_partition | Enables partition support on the queue. |
| Subscription | link_to_namespace | Link this queue to an existing namespace instead of creating a new one. |
| Subscription | add_filters | Adds multiple filters to a subscription |
| Subscription | add_sql_filter | Adds a filter to a subscription using SQL syntax. |
| Subscription | add_correlation_filter | Adds a filter to a subscription using header value correlation. |
| Topic | name | The name of the topic. |
| Topic | duplicate_detection_minutes | Whether to enable duplicate detection, and if so, how long to check for. |
| Topic | enable_partition | Enables partition support on the topic. |
| Topic | message_ttl | Time To Live (TTL) value for messages expressed as a TimeSpan string, such as '01:30:00' 1 hour, 30 minutes, or as an integer days e.g. `4<Days>`. |
| Namespace | sku | The ServiceBusNamespaceSku e.g. Standard |
| Namespace | namespace_name | The name of the namespace that holds the queue. |
| Namespace | depends_on | Sets dependencies on the service bus namespace. |

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
