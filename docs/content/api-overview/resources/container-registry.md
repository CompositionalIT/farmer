---
title: "Container Registry"
date: 2020-04-30T19:10:46+02:00
chapter: false
weight: 3
---

#### Overview
The Container Registry builder is used to create Azure Container Registry (ACR) instances.

* Container Registry (`Microsoft.ContainerRegistry/registries`)

#### Builder Keywords
| Keyword | Purpose |
|-|-|
| name | Sets the name of the Container Registry instance. |
| sku | Sets the SKU of the instance. Defaults to Basic. |
| enable_admin_user | Enables the admin user (not recommended for production). |
| enable_public_network_access | Explicitly enables public network access. |
| disable_public_network_access | Disables public network access (Premium SKU only, recommended for security). |
| add_ip_rule | Adds an IP address or CIDR range to the allow list (Premium SKU only). |
| add_ip_rules | Adds multiple IP addresses or CIDR ranges to the allow list (Premium SKU only). |

#### Configuration Members

| Member | Purpose |
|-|-|
| Password | Gets the ARM expression path to the first admin password of this container registry if the admin user was enabled. |
| Password2 | Gets the ARM expression path to the second admin password of this container registry if the admin user was enabled. |
| Username | Gets the ARM expression path to the admin username of this container registry if the admin user was enabled. |

#### Basic Example
```fsharp
open Farmer
open Farmer.Builders

let myRegistry = containerRegistry {
    name "myRegistry"
    sku ContainerRegistry.Basic
    enable_admin_user
}
```

#### Secure Example with Network Restrictions (Premium SKU)
```fsharp
open Farmer
open Farmer.Builders
open Farmer.ContainerRegistry

let secureRegistry = containerRegistry {
    name "mySecureRegistry"
    sku Premium
    // Disable public network access - use private endpoints only
    disable_public_network_access
}

let restrictedRegistry = containerRegistry {
    name "myRestrictedRegistry"
    sku Premium
    // Allow access only from specific IP addresses/ranges
    add_ip_rules [
        "203.0.113.0/24"  // Corporate network
        "198.51.100.5"     // Build server
    ]
}
```

#### Security Best Practices

1. **Disable Admin User**: The admin user provides a single account with full access to the registry. For production, use Azure AD authentication with managed identities instead.

2. **Use Premium SKU for Production**: Only the Premium SKU supports:
   - Network restrictions (IP rules and private endpoints)
   - Disabling public network access
   - Content trust and image signing
   - Customer-managed keys

3. **Restrict Network Access**: Use one of these strategies:
   - **Disable Public Access**: Use `disable_public_network_access` and access only through private endpoints (most secure)
   - **IP Restrictions**: Use `add_ip_rules` to limit access to known IP addresses

4. **Use Managed Identities**: Instead of admin credentials, authenticate using Azure managed identities from services like AKS, Azure DevOps, or GitHub Actions.

#### Network Security Notes

- **IP Rules require Premium SKU**: Network restrictions are only available with the Premium tier
- **Default Deny**: When you add IP rules, all other IPs are denied by default
- **CIDR Notation**: IP rules support both individual IPs (`203.0.113.5`) and CIDR ranges (`203.0.113.0/24`)
- **Private Endpoints**: For complete isolation, use `disable_public_network_access` and connect via private endpoints

#### Cost Considerations

Azure Container Registry pricing varies significantly by SKU tier:

| SKU | Approx. Monthly Cost* | Storage (Included) | Network Features |
|-----|----------------------|-------------------|------------------|
| **Basic** | ~$5 USD | 10 GB | Public access only |
| **Standard** | ~$20 USD | 100 GB | Public access only |
| **Premium** | ~$500 USD | 500 GB | IP rules, private endpoints, geo-replication |

*Approximate costs as of 2025. Additional charges apply for:
- Storage beyond included amounts: ~$0.10/GB per day
- Build tasks and image pulls
- Geo-replication (Premium only)
- Data egress

**Cost Optimization Tips:**
1. **Start with Basic**: Use Basic SKU for development/testing environments
2. **Standard for Production**: Standard SKU provides better performance and storage for most production workloads
3. **Premium When Needed**: Only upgrade to Premium when you specifically need:
   - Network restrictions (IP rules, private endpoints)
   - Geo-replication for global deployments
   - Enhanced security features (content trust, customer-managed keys)
4. **Clean Up Old Images**: Regularly delete unused images to minimize storage costs
5. **Use Retention Policies**: Premium SKU supports automated image cleanup policies

**Security vs. Cost Tradeoff:**
- Network restrictions (IP rules, private endpoints) require Premium SKU (~$500/month)
- For sensitive workloads, this cost is justified by the enhanced security
- For less sensitive workloads, consider Basic/Standard with Azure AD authentication and proper RBAC

#### Compliance

Container Registry with network restrictions helps meet security requirements from:
- **NIST 800-53**: AC-3 (Access Enforcement), SC-7 (Boundary Protection)
- **CIS Benchmarks**: 6.10 (Restrict container registry network access)
- **SOC 2**: CC6.6 (Logical and Physical Access Controls)
