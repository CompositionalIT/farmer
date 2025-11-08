---
title: "Azure Policy"
date: 2025-11-08
chapter: false
weight: 15
---

#### Overview
The Azure Policy builder creates policy definitions and policy assignments that help enforce organizational standards and assess compliance at scale.

* Policy Definition (`Microsoft.Authorization/policyDefinitions`)
* Policy Assignment (`Microsoft.Authorization/policyAssignments`)

> Azure Policy helps enforce organizational standards and assess compliance at-scale. Through its compliance dashboard, it provides an aggregated view to evaluate the overall state of the environment, with the ability to drill down to per-resource, per-policy granularity.

#### Builder Keywords

##### Policy Definition

| Keyword | Purpose |
|-|-|
| name | Sets the name of the policy definition |
| display_name | Sets the display name of the policy definition |
| description | Sets the description of the policy definition |
| mode | Sets the policy mode (PolicyMode.All or PolicyMode.Indexed) |
| policy_rule | Sets the policy rule as a JSON string |
| parameters | Sets the parameters for the policy definition |

##### Policy Assignment

| Keyword | Purpose |
|-|-|
| name | Sets the name of the policy assignment |
| display_name | Sets the display name of the policy assignment |
| description | Sets the description of the policy assignment |
| link_to_policy | Links to a policy definition config built in this deployment |
| link_to_policy_id | Links to an existing policy definition by resource ID |
| enforcement_mode | Sets the enforcement mode (EnforcementMode.Default or EnforcementMode.DoNotEnforce) |
| parameters | Sets the parameters for the policy assignment |
| scope | Sets the scope for the policy assignment |
| not_scopes | Adds resource scopes to exclude from this policy assignment |
| add_dependency | Adds a dependency to this policy assignment |

#### Examples

##### Creating a Policy Definition

```fsharp
open Farmer
open Farmer.Builders
open Farmer.Arm.Policy

// Define a policy that restricts deployment locations
let locationPolicy = policyDefinition {
    name "allowed-locations-policy"
    display_name "Allowed Azure Regions"
    description "This policy restricts resource deployments to specific Azure regions"
    mode PolicyMode.All
    policy_rule """{
        "if": {
            "not": {
                "field": "location",
                "in": ["eastus", "westus", "northeurope"]
            }
        },
        "then": {
            "effect": "deny"
        }
    }"""
}

let deployment = arm {
    location Location.EastUS
    add_resource locationPolicy
}
```

##### Creating a Policy Assignment

```fsharp
open Farmer
open Farmer.Builders
open Farmer.Arm.Policy

// Define and assign a policy to audit storage accounts without HTTPS
let storagePolicy = policyDefinition {
    name "require-https-storage-policy"
    display_name "Require HTTPS for Storage Accounts"
    description "This policy audits storage accounts that don't enforce HTTPS"
    mode PolicyMode.All
    policy_rule """{
        "if": {
            "allOf": [
                {
                    "field": "type",
                    "equals": "Microsoft.Storage/storageAccounts"
                },
                {
                    "field": "Microsoft.Storage/storageAccounts/supportsHttpsTrafficOnly",
                    "notEquals": "true"
                }
            ]
        },
        "then": {
            "effect": "audit"
        }
    }"""
}

let storageAssignment = policyAssignment {
    name "require-https-storage-assignment"
    display_name "Audit Storage HTTPS Compliance"
    description "Audits all storage accounts for HTTPS enforcement"
    link_to_policy storagePolicy
    enforcement_mode EnforcementMode.Default
}

let deployment = arm {
    location Location.EastUS
    add_resources [ storagePolicy; storageAssignment ]
}
```

##### Testing a Policy (DoNotEnforce Mode)

```fsharp
open Farmer
open Farmer.Builders
open Farmer.Arm.Policy

// Create a policy in test mode to evaluate impact without enforcement
let tagPolicy = policyDefinition {
    name "require-environment-tag-policy"
    display_name "Require Environment Tag"
    description "This policy requires all resources to have an environment tag"
    mode PolicyMode.Indexed
    policy_rule """{
        "if": {
            "field": "tags.environment",
            "exists": "false"
        },
        "then": {
            "effect": "deny"
        }
    }"""
}

let tagAssignment = policyAssignment {
    name "require-env-tag-test"
    display_name "Environment Tag Test Assignment"
    link_to_policy tagPolicy
    enforcement_mode EnforcementMode.DoNotEnforce  // Test mode - logs violations but doesn't block
}

let deployment = arm {
    location Location.EastUS
    add_resources [ tagPolicy; tagAssignment ]
}
```

#### Policy Modes

Azure Policy supports two modes:

* **PolicyMode.All**: Evaluates all resource types including resource groups and subscriptions
* **PolicyMode.Indexed**: Only evaluates resource types that support tags and location (default for most policies)

Use `All` mode when your policy needs to evaluate resource groups or subscription-level properties. Use `Indexed` mode for policies focused on individual resources.

#### Policy Effects

Common policy effects you can use in your policy rules:

* **Audit**: Creates a warning event in the activity log but doesn't stop the request
* **Deny**: Blocks non-compliant resource creation or updates
* **DeployIfNotExists**: Deploys additional resources if a condition isn't met
* **Modify**: Adds, updates, or removes tags or properties on resources
* **Append**: Adds additional fields to the resource during creation
* **Disabled**: Useful for testing or temporarily disabling a policy

#### Best Practices

1. **Start with Audit**: Begin with `audit` effect to understand impact before enforcing with `deny`
2. **Use DoNotEnforce Mode**: Test policy assignments in `DoNotEnforce` mode before enabling enforcement
3. **Descriptive Names**: Use clear display names and descriptions for easier management in Azure Portal
4. **Scope Carefully**: Apply policies at the appropriate scope (management group, subscription, or resource group)
5. **Tag Policies**: Use tags on policy definitions to organize and track them
6. **Exemptions**: Use `not_scopes` to exclude specific resources from policy enforcement when needed
7. **Built-in Policies**: Consider using Azure's built-in policies before creating custom ones

#### Common Policy Patterns

##### Enforce Resource Naming Convention

```fsharp
let namingPolicy = policyDefinition {
    name "enforce-naming-convention"
    display_name "Enforce Resource Naming Convention"
    mode PolicyMode.Indexed
    policy_rule """{
        "if": {
            "not": {
                "field": "name",
                "match": "[parameters('namePattern')]"
            }
        },
        "then": {
            "effect": "deny"
        }
    }"""
    parameters (
        Map.ofList [
            "namePattern", box {|
                type = "String"
                metadata = {| displayName = "Name Pattern"; description = "Pattern for resource names" |}
            |}
        ]
    )
}
```

##### Require Specific Tags

```fsharp
let tagRequirementPolicy = policyDefinition {
    name "require-cost-center-tag"
    display_name "Require Cost Center Tag"
    mode PolicyMode.Indexed
    policy_rule """{
        "if": {
            "field": "tags['cost-center']",
            "exists": "false"
        },
        "then": {
            "effect": "deny"
        }
    }"""
}
```

##### Enforce Network Security

```fsharp
let nsgPolicy = policyDefinition {
    name "deny-rdp-from-internet"
    display_name "Deny RDP from Internet"
    description "Denies NSG rules that allow RDP (port 3389) from the Internet"
    mode PolicyMode.All
    policy_rule """{
        "if": {
            "allOf": [
                {
                    "field": "type",
                    "equals": "Microsoft.Network/networkSecurityGroups/securityRules"
                },
                {
                    "field": "Microsoft.Network/networkSecurityGroups/securityRules/access",
                    "equals": "Allow"
                },
                {
                    "field": "Microsoft.Network/networkSecurityGroups/securityRules/direction",
                    "equals": "Inbound"
                },
                {
                    "anyOf": [
                        {
                            "field": "Microsoft.Network/networkSecurityGroups/securityRules/destinationPortRange",
                            "equals": "3389"
                        },
                        {
                            "field": "Microsoft.Network/networkSecurityGroups/securityRules/destinationPortRange",
                            "equals": "*"
                        }
                    ]
                },
                {
                    "anyOf": [
                        {
                            "field": "Microsoft.Network/networkSecurityGroups/securityRules/sourceAddressPrefix",
                            "equals": "*"
                        },
                        {
                            "field": "Microsoft.Network/networkSecurityGroups/securityRules/sourceAddressPrefix",
                            "equals": "Internet"
                        }
                    ]
                }
            ]
        },
        "then": {
            "effect": "deny"
        }
    }"""
}
```

#### Cost Considerations

**Azure Policy is FREE!**

Azure Policy has no direct costs:

| Feature | Cost |
|---------|------|
| **Policy Definitions** | FREE (unlimited) |
| **Policy Assignments** | FREE (unlimited) |
| **Compliance Evaluation** | FREE |
| **Remediation Tasks** | FREE |
| **Guest Configuration** | FREE* |

*Guest Configuration (policies that audit/configure VM settings) is free for Azure VMs. For Arc-enabled servers (on-premises or other clouds), standard Arc billing applies.

**Indirect Costs to Consider:**

While Azure Policy itself is free, be aware of these related costs:

1. **Remediation Resources**: 
   - If you use `DeployIfNotExists` or `Modify` effects, the deployed resources have their own costs
   - Example: A policy that deploys Azure Monitor agents to VMs incurs Log Analytics costs
   - **Cost**: Varies by resource type

2. **Activity Log Storage**:
   - Policy compliance events are logged to Azure Activity Log
   - Activity Log is retained for 90 days free, longer retention requires Log Analytics
   - **Cost**: $2.30/GB for Log Analytics if extended retention is needed

3. **Evaluation Performance**:
   - Complex policies with many assignments may slightly slow deployment times
   - **Cost**: Negligible, but can impact deployment duration

4. **Management Overhead**:
   - Staff time to develop, test, and maintain policies
   - **Cost**: Operational/labor cost

**Cost Optimization Tips:**

1. **Test Before Enforcing**: Use `DoNotEnforce` mode to validate policies before applying them (avoids deployment failures)
2. **Use Audit First**: Start with `audit` effect to understand impact before switching to `deny`
3. **Leverage Built-in Policies**: Azure provides 1000+ built-in policies that are pre-tested and maintained
4. **Scope Carefully**: Apply policies at appropriate scope (management group vs. subscription vs. resource group) to avoid redundant evaluations
5. **Avoid Over-Remediation**: Be selective with `DeployIfNotExists` policies to avoid deploying unnecessary resources

**ROI and Value:**

Despite being free, Azure Policy provides immense value:

* **Prevents Costly Mistakes**: Blocks insecure configurations before resources are deployed
* **Reduces Audit Costs**: Automates compliance checking that would otherwise require manual audits
* **Improves Security Posture**: Enforces security standards consistently, reducing breach risk
* **Accelerates Compliance**: Makes it easier to demonstrate compliance during audits
* **Operational Efficiency**: Automates governance that would require manual processes

**Typical organizational savings from Azure Policy:**
- **Small Organization** (10-50 resources): 5-10 hours/month of manual compliance checking = ~$500-1000/month saved
- **Medium Organization** (100-500 resources): 20-40 hours/month = ~$2,000-4,000/month saved  
- **Large Enterprise** (1000+ resources): 100+ hours/month = ~$10,000+/month saved

#### Security Benefits

Azure Policy provides critical security governance capabilities:

* **Preventive Controls**: Block non-compliant resources before they're created
* **Detective Controls**: Audit existing resources for compliance violations
* **Automated Remediation**: Automatically fix non-compliant resources with DeployIfNotExists and Modify effects
* **Compliance Reporting**: Dashboard showing compliance state across your environment
* **Defense in Depth**: Works alongside RBAC to provide comprehensive access control
* **Consistent Enforcement**: Policies apply consistently across all resources in scope

#### Compliance

Azure Policy is essential for meeting compliance requirements from major security frameworks:

* **NIST SP 800-53**: AC-1 (Access Control Policy), CM-2 (Baseline Configuration)
* **ISO 27001**: A.8.1.1 (Inventory of assets), A.12.1.1 (Documented operating procedures)
* **PCI DSS**: Requirement 2.2 (Develop configuration standards)
* **CIS Azure Foundations Benchmark**: Multiple controls across all sections
* **HIPAA**: Administrative Safeguards (Security Management Process)
* **SOC 2**: CC7.2 (System monitoring), CC8.1 (Change management)
* **FedRAMP**: CM-2 (Baseline Configuration), CM-6 (Configuration Settings)

Azure Policy helps demonstrate continuous compliance and can generate evidence for audit purposes.

#### Integration with Azure Security Center

Policy assignments automatically integrate with Microsoft Defender for Cloud (formerly Azure Security Center), providing:

* Centralized compliance dashboard
* Security score calculation
* Automated remediation recommendations
* Integration with Azure Security Benchmark

#### Limitations

* Policy evaluation can take up to 30 minutes for new or updated policy assignments
* Some Azure services may not be fully supported by Azure Policy
* Complex policies may impact deployment performance
* Policy effects like DeployIfNotExists require managed identity with appropriate permissions
