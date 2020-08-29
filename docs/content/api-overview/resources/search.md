---
title: "Search"
date: 2020-02-05T08:53:46+01:00
chapter: false
weight: 19
---

#### Overview
The Search builder creates storage accounts and their associated containers.

* Search (`Microsoft.Search/searchServices`)

#### Builder Keywords
| Keyword | Purpose |
|-|-|
| name | Sets the name of the Azure Search instance. |
| sku | Sets the sku of the Azure Search instance. |
| replicas | Sets the replica count of the Azure Search instance. |
| partitions | Sets the number of partitions of the Azure Search instance. |

#### Configuration Members

| Member | Purpose |
|-|-|
| AdminKey | Gets an ARM expression for the admin key of the search instance. |
| QueryKey | Gets an ARM expression for the query key of the search instance. |

#### Example

```fsharp
open Farmer
open Farmer.Builders

let mySearch = search {
    name "isaacsSearch"
    sku Search.Basic
}
```