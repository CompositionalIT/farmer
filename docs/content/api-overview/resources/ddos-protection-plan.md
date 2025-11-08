---
title: "DDoS Protection Plan"
date: 2025-11-08
chapter: false
weight: 14
---

#### Overview
The DDoS Protection Plan builder creates Azure DDoS Protection Plans that provide enhanced DDoS mitigation capabilities for virtual networks.

* DDoS Protection Plan (`Microsoft.Network/ddosProtectionPlans`)

> DDoS Protection Plans provide always-on traffic monitoring and automatic mitigation of DDoS attacks. They can be shared across multiple virtual networks in the same subscription or across subscriptions in the same Azure AD tenant, providing cost-effective protection at scale.

#### Builder Keywords

| Keyword | Purpose |
|-|-|
| name | Sets the name of the DDoS Protection Plan |
| add_tag | Adds a tag to the DDoS Protection Plan |
| add_tags | Adds multiple tags to the DDoS Protection Plan |

#### Example

```fsharp
open Farmer
open Farmer.Builders

let myDdosPlan = ddosProtectionPlan {
    name "my-ddos-protection-plan"
    add_tags [
        "environment", "production"
        "cost-center", "security"
    ]
}

let deployment = arm {
    location Location.EastUS
    add_resource myDdosPlan
}
```

#### Cost Considerations

DDoS Protection Plan is a premium service with a fixed monthly cost (~$3,000 USD/month) plus data transfer charges. However, a single DDoS Protection Plan can be:

* Shared across all virtual networks in a subscription
* Shared across subscriptions in the same Azure AD tenant

This makes it cost-effective to deploy a single DDoS Protection Plan at the tenant or management group level and share it across all virtual networks.

#### Best Practices

1. **Centralized Deployment**: Create one DDoS Protection Plan per tenant and share it across all virtual networks
2. **Tagging**: Use tags to track the cost center and ownership
3. **Virtual Network Association**: After creating a DDoS Protection Plan, associate it with virtual networks using the `link_to_ddos_protection_plan` operation on virtual networks

#### Linking to Virtual Networks

Once a DDoS Protection Plan is created, it must be linked to virtual networks to provide protection:

```fsharp
let ddosPlan = ddosProtectionPlan {
    name "shared-ddos-plan"
}

let vnet = vnet {
    name "my-vnet"
    add_address_spaces [ "10.0.0.0/16" ]
    link_to_ddos_protection_plan ddosPlan
}

let deployment = arm {
    location Location.EastUS
    add_resources [ ddosPlan; vnet ]
}
```

> Note: The `link_to_ddos_protection_plan` operation for virtual networks will be available in a future Farmer release.

#### Security Benefits

DDoS Protection Plan provides:

* **Always-on traffic monitoring**: Continuous monitoring of application traffic patterns
* **Automatic attack mitigation**: Instant attack detection and mitigation without user intervention
* **Attack analytics**: Detailed metrics and diagnostics during and after attacks
* **Adaptive tuning**: Machine learning-based traffic profiling for more accurate detection
* **Protection for Azure resources**: Covers public IP addresses, Application Gateways, and Azure Load Balancers
* **DDoS rapid response support**: Access to DDoS experts during an active attack
* **Cost protection**: Service credits for scale-out costs during documented attacks

#### Compliance

DDoS Protection Plans help meet compliance requirements from security frameworks including:

* **NIST Cybersecurity Framework**: SC-5 (Denial of Service Protection)
* **ISO 27001**: A.14.1.2 (Securing application services)
* **PCI DSS**: Requirement 5 (Protect all systems against malware and regularly update anti-virus software)
* **SOC 2**: CC7.2 (System monitoring)

