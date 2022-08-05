---
title: "Private Endpoint"
date: 2022-08-05T16:13:00-04:00
chapter: false
weight: 12
---

#### Overview
The Private Endpoint builder (`privateEndpoint`) creates a private endpoint for accessing Azure resources or a private link service without traversing the Internet.

* Private Endpoint (`Microsoft.Network/privateEndpoints`)

#### Builder Keywords

| Applies To | Keyword | Purpose |
|-|-|-|
| privateEndpoint | name | Specifies the name of the private endpoint. |
| privateEndpoint | subnet_reference | Attaches the private endpoint to a referenced subnet. |
| privateEndpoint | link_to_subnet | Attaches the private endpoint to a subnet deployed in the same deployment. |
| privateEndpoint | link_to_unmanaged_subnet | Attaches the private endpoint to an existing subnet. |
| privateEndpoint | resource | Specifies the ARM resource ID of the service it is connecting to. |
| privateEndpoint | custom_nic_name | Optionally specify the name for the NIC generated for the private endpoint. |
| privateEndpoint | add_group_ids | Specify one or more group IDs the private link service provides. |

#### Configuration Members

| Member | Purpose |
|-|-|
| CustomNicEndpointIP <index> | If the `custom_nic_name` is set, this gets an ARM Expression to get the private endpoint IP address by 0-based index. |
| CustomNicFirstEndpointIP | If the `custom_nic_name` is set, this gets an ARM Expression to get the first private endpoint IP address. |

#### Example

```fsharp
open Farmer
open Farmer.Builders

let myPrivateEndpoint = privateEndpoint {
    name "private-endpoint"
    custom_nic_name "private-endpoint-nic"
    link_to_subnet (subnets.resourceId (ResourceName "my-net", ResourceName "priv-endpoints" ))
    resource (Unmanaged existingPrivateLinkId)
}
```
