---
title: "Dedicated Hosts"
date: 2022-10-10T22:26:00-04:00
chapter: false
weight: 5
---

#### Overview
The `hostGroup` and `host` builders create dedicated hosts in azure and their parent resource, a host group, to efficiently manage a physical host resource in Azure. Dedicated hosts are the same physical servers used in our data centers, provided as a resource. To learn more about dedicated hosts, reference the [Azure Docs](https://learn.microsoft.com/en-us/azure/virtual-machines/dedicated-hosts)

* RouteTable (`Microsoft.Compute/hostGroups`)
* Route (`Microsoft.Compute/hostGroups/hosts`)

#### Builder Keywords

| Applies To | Keyword | Purpose |
|-|-|-|
| hostGroup | name | Name of the host group resource |
| hostGroup | add_availability_zones | Add availability zones to the host group. Valid options are 1,2, or 3. |
| hostGroup| supportAutomaticPlacement | Feature flag for automatic placement of the VMs |
| hostGroup| platformFaultDomainCount | How many fault domains to support. Valid options are 1, 2, and 3. |
| host | name | Name of the host resource |
| host | licenseType | The licenses to bring the hosts, i.e. WindowsHybrid, WindowsPerpetual |
| host | autoReplaceOnFailure| Feature flag whether to auto replace the host on failure |
| host | sku | name of the sku for the dedicated hosts. Valid sku's vary by subscription, consult the Dedicated Host documentation |
| host | platformFaultDomain | Fault domain to assign the host |
| host | parentHostGroup | Name of the host group to assign the hosts to  |

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
                supportAutomaticPlacement true
                add_availability_zones [ AvailabilityZone.One; AvailabilityZone.Two ]
                platformFaultDomainCount 2
            }
            host {
                name "myhost"
                parentHostGroup (ResourceName "myhostgroup")
                sku "VSv1-Type3"
            }
        ]
}
```