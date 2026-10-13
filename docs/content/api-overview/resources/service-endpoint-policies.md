---
title: "Service Endpoint Policies"
date: 2026-09-28T12:00:00-04:00
chapter: false
weight: 6
---

#### Overview
The `serviceEndpointPolicy` builder creates an Azure service endpoint policy that can be attached to a subnet. This lets you restrict a subnet's service endpoint access to specific Azure resources such as storage accounts. To learn more, reference the [Azure Docs](https://learn.microsoft.com/en-us/azure/virtual-network/virtual-network-service-endpoint-policies-overview).

* Service Endpoint Policy (`Microsoft.Network/serviceEndpointPolicies`)

#### Builder Keywords

| Applies To | Keyword | Purpose |
|-|-|-|
| serviceEndpointPolicy | name | Name of the service endpoint policy resource |
| serviceEndpointPolicy | add_definitions | Adds one or more policy definitions |
| serviceEndpointPolicy | add_tag / add_tags | Adds tags to the policy resource |
| serviceEndpointPolicy | depends_on | Adds explicit resource dependencies |
| serviceEndpointPolicyDefinition | name | Name of the policy definition |
| serviceEndpointPolicyDefinition | description | Optional description for the policy definition |
| serviceEndpointPolicyDefinition | service | The Azure service protected by the policy definition, for example `EndpointServiceType.Storage` |
| serviceEndpointPolicyDefinition | add_service_resources | Adds Farmer-managed service resources to the definition |
| serviceEndpointPolicyDefinition | link_to_service_resources | Adds externally managed service resources to the definition |
| subnet | associate_service_endpoint_policies | Associates one or more service endpoint policies with a manually defined subnet |
| subnetSpec | add_service_endpoint_policies | Associates one or more service endpoint policies with an automatically carved subnet |

#### Example

```fsharp
#r "nuget:Farmer"

open Farmer
open Farmer.Builders
open Farmer.Network

let storage = storageAccount { name "policyteststorage" }

let storagePolicy =
    serviceEndpointPolicy {
        name "storage-policy"

        add_definitions [
            serviceEndpointPolicyDefinition {
                name "allow-storage"
                description "Allow access to a specific storage account"
                service EndpointServiceType.Storage
                add_service_resources [ storage ]
            }
        ]
    }

let network =
    vnet {
        name "my-vnet"
        add_address_spaces [ "10.28.0.0/16" ]

        add_subnets [
            subnet {
                name "services"
                prefix "10.28.0.0/24"
                add_service_endpoints [ EndpointServiceType.Storage, [ Location.EastUS ] ]
                associate_service_endpoint_policies [ storagePolicy ]
            }
        ]
    }

arm {
    location Location.EastUS
    add_resources [ storage; storagePolicy; network ]
}
```
