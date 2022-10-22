---
title: "Dedicated Hosts"
date: 2022-10-10T22:26:00-04:00
chapter: false
weight: 5
---

#### Overview
The `hostGroup` and `host` builders create dedicated hosts in azure and their parent resource, a host group, to efficiently manage a physical host resource in Azure. Dedicated hosts are the same physical servers used in our data centers, provided as a resource. To learn more about dedicated hosts, reference the [Azure Docs](https://learn.microsoft.com/en-us/azure/virtual-machines/dedicated-hosts)

* Host Group (`Microsoft.Compute/hostGroups`)
* Host (`Microsoft.Compute/hostGroups/hosts`)

#### Builder Keywords

| Applies To | Keyword | Purpose |
|-|-|-|
| hostGroup | name | Name of the host group resource |
| hostGroup | add_availability_zone | Assign a zone to the host group. |
| hostGroup| support_automatic_placement | Feature flag for automatic placement of the VMs |
| hostGroup| platform_fault_domain_count | How many fault domains to support, depends on the region. |
| host | name | Name of the host resource |
| host | license_type | The licenses to bring the hosts, i.e. WindowsHybrid, WindowsPerpetual |
| host | auto_replace_on_failure| Feature flag whether to auto replace the host on failure |
| host | sku | name of the sku for the dedicated hosts. Valid sku's vary by subscription, consult the [Dedicated Host documentation](https://learn.microsoft.com/en-us/azure/virtual-machines/dedicated-host-compute-optimized-skus) |
| host | platform_fault_domain | Fault domain to assign the host |
| host | parent_host_group | Name of the host group to assign the hosts to  |

#### Example

```fsharp
#r "nuget:Farmer"

open Farmer
open Farmer.Builders

arm {
    location Location.EastUS

    add_resources
        [
            hostGroup {
                name "myhostgroup"
                support_automatic_placement true
                add_availability_zone "1"
                platform_fault_domain_count 2
            }
            host {
                name "myhost"
                parent_host_group (ResourceName "myHostGroup")
                platform_fault_domain 2
                sku "Fsv2-Type2"
            }
        ]
}
```