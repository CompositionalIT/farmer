---
title: "Azure Sentinel"
date: 2025-11-08
chapter: false
weight: 18
---

#### Overview
The Sentinel builder enables Azure Sentinel (cloud-native SIEM) on a Log Analytics Workspace.

* Sentinel Onboarding (`Microsoft.SecurityInsights/onboardingStates`)

> Azure Sentinel is Microsoft's cloud-native SIEM and SOAR solution. It provides intelligent security analytics and threat intelligence across the enterprise, providing a single solution for alert detection, threat visibility, proactive hunting, and threat response.

#### Builder Keywords

| Keyword | Purpose |
|-|-|
| link_to_workspace | Links to a Log Analytics Workspace to enable Sentinel on |
| workspace_name | Sets the workspace name directly (for existing workspaces) |
| add_dependency | Adds a dependency to Sentinel onboarding |

#### Examples

##### Enable Sentinel on New Workspace

```fsharp
open Farmer
open Farmer.Builders

let workspace = logAnalytics {
    name "security-operations-workspace"
    sku LogAnalytics.PerGB2018
}

let siem = sentinel {
    link_to_workspace workspace
}

let deployment = arm {
    location Location.EastUS
    add_resources [ workspace; siem ]
}
```

##### Enable Sentinel on Existing Workspace

```fsharp
open Farmer
open Farmer.Builders

let siem = sentinel {
    workspace_name "existing-security-workspace"
}

let deployment = arm {
    location Location.EastUS
    add_resource siem
}
```

#### Cost Considerations

**Azure Sentinel Pricing** (Pay-as-you-go):

| Tier | Included | Price per GB* |
|------|----------|---------------|
| **Pay-as-you-go** | None | **$2.30/GB** (same as Log Analytics) |
| **Commitment Tier** (100 GB/day) | 100 GB/day | **$2.00/GB** (~13% savings) |
| **Commitment Tier** (500 GB/day) | 500 GB/day | **$1.65/GB** (~28% savings) |

*Approximate costs as of 2025. First 90 days free for new workspaces.

**Example Monthly Costs:**

| Scenario | Daily Ingestion | Commitment | **Monthly Cost** |
|----------|----------------|------------|------------------|
| **Small (Dev)** | 5 GB/day | Pay-as-you-go | **~$345/month** |
| **Medium** | 50 GB/day | Pay-as-you-go | **~$3,450/month** |
| **Large** | 100 GB/day | 100 GB tier | **~$6,000/month** |
| **Enterprise** | 500 GB/day | 500 GB tier | **~$24,750/month** |

**Free Tier:**
- **First 90 days FREE** (up to 10 GB/day) for new Sentinel workspaces
- Good for POC and evaluation

**What Sentinel provides for the cost:**
- Unlimited threat hunting queries
- Built-in analytics rules
- Automated threat response (SOAR)
- UEBA (User Entity Behavior Analytics)
- Threat intelligence integration
- Incident management

**Cost Optimization:**

1. **Use Basic Logs**: For high-volume, low-priority logs (~50% cheaper)
2. **Data Retention**: Keep 90 days in hot storage, archive rest
3. **Connector Selection**: Only enable needed data connectors
4. **Commitment Tiers**: Save 13-48% for predictable volumes
5. **Filter at Source**: Don't ingest unnecessary logs

**Typical breakdown:**
- Security logs: 60% of ingestion
- Audit logs: 25%
- Diagnostic logs: 15%

#### Security Benefits

Azure Sentinel provides comprehensive SIEM/SOAR capabilities:

* **Threat Detection**: AI-powered analytics detect threats across entire estate
* **Incident Response**: Automated playbooks respond to threats in real-time
* **Threat Hunting**: KQL-based hunting across petabytes of data
* **UEBA**: Detect insider threats and compromised accounts
* **Threat Intelligence**: Integration with Microsoft and third-party feeds
* **SOAR**: Security orchestration and automated response
* **Compliance**: Meet audit requirements for security monitoring

#### Next Steps

After enabling Sentinel:

1. **Enable Data Connectors**: Azure AD, Office 365, Azure Activity, etc.
2. **Configure Analytics Rules**: Enable built-in detection rules
3. **Set Up Automation**: Create playbooks for automated response
4. **Configure UEBA**: Enable user and entity behavior analytics
5. **Integrate Threat Intelligence**: Connect threat feeds

**Note:** This builder only enables Sentinel on the workspace. Data connectors, analytics rules, and playbooks must be configured separately.

#### Compliance

Azure Sentinel helps meet SOC requirements for:
- **NIST CSF**: DE (Detect), RS (Respond)
- **ISO 27001**: A.12.4 (Logging and monitoring), A.16.1 (Incident management)
- **PCI DSS**: Requirement 10 (Log monitoring), 11 (Security testing)
- **SOC 2**: CC7 (System monitoring)
