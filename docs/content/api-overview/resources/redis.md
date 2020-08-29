---
title: "Redis Cache"
date: 2020-02-23T20:00:00+01:00
chapter: false
weight: 18
---

#### Overview
The Redis builder creates managed Redis Cache accounts.

* Redis (`Microsoft.Cache/redis`)

#### Builder Keywords
| Keyword | Purpose |
|-|-|
| name | Sets the name of the Redis cache instance. |
| sku | Sets the sku of the Redis cache instance. |
| capacity | Sets the capacity level of the Redis cache instance, should be between 1-6 - see [here](https://azure.microsoft.com/en-gb/pricing/details/cache/). |
| enable_non_ssl_port | Enabled access to the cache over the non-SSL port. |
| setting | Allows you to set a Redis-cache specific setting at deployment-time |

#### Configuration Members
| Member | Purpose |
|-|-|
| Key | Gets an ARM expression for the primary key of the Redis cache instance. |

#### Example

```fsharp
open Farmer
open Farmer.Builders.Redis

let myCache = redis {
    name "myredis"
    sku Redis.Standard
    capacity 1
    enable_non_ssl_port
    setting "maxclients" 256
    setting "maxmemory-reserved" 2
    setting "maxfragmentationmemory-reserved" 12
    setting "maxmemory-delta" 2
}
```