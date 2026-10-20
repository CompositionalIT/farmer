---
title: "Network Watcher"
date: 2025-11-08
chapter: false
weight: 16
---

#### Overview
The Network Watcher builder creates Azure Network Watcher instances and Flow Logs for network monitoring and diagnostics.

* Network Watcher (`Microsoft.Network/networkWatchers`)
* Flow Log (`Microsoft.Network/networkWatchers/flowLogs`)

> Network Watcher provides tools to monitor, diagnose, view metrics, and enable or disable logs for resources in an Azure virtual network. Flow Logs capture information about IP traffic flowing through Network Security Groups (NSGs), enabling security analysis, compliance auditing, and traffic pattern monitoring.

#### Builder Keywords

##### Network Watcher

| Keyword | Purpose |
|-|-|
| name | Sets the name of the Network Watcher |
| add_tag | Adds a tag to the Network Watcher |
| add_tags | Adds multiple tags to the Network Watcher |

##### Flow Log

| Keyword | Purpose |
|-|-|
| name | Sets the name of the flow log |
| link_to_network_watcher | Links to a Network Watcher instance (required) |
| link_to_nsg | Links to the NSG to monitor (required) |
| link_to_storage_account | Links to the Storage Account for storing logs (required) |
| retention_days | Sets log retention period in days (0 = unlimited, default 7) |
| enable_traffic_analytics | Enables Traffic Analytics with Log Analytics Workspace |

#### Examples

##### Basic Network Watcher

```fsharp
open Farmer
open Farmer.Builders

let watcher = networkWatcher {
    name "network-watcher-eastus"
    add_tags [
        "environment", "production"
        "cost-center", "network-operations"
    ]
}

let deployment = arm {
    location Location.EastUS
    add_resource watcher
}
```

##### Flow Log with Storage

```fsharp
open Farmer
open Farmer.Builders

let storageAccount = storageAccount {
    name "flowlogsstorage"
    sku Storage.Standard_LRS
}

let nsg = nsg {
    name "web-tier-nsg"
    add_rules [
        securityRule {
            name "allow-https"
            services [ NetworkService ("https", 443) ]
            add_source_tag NetworkSecurity.Tag.Internet
            add_destination_network "10.0.1.0/24"
        }
    ]
}

let watcher = networkWatcher {
    name "network-watcher-eastus"
}

let flowLog = flowLog {
    name "web-tier-flow-logs"
    link_to_network_watcher watcher
    link_to_nsg nsg.ResourceId
    link_to_storage_account storageAccount.ResourceId
    retention_days 30
}

let deployment = arm {
    location Location.EastUS
    add_resources [ storageAccount; nsg; watcher; flowLog ]
}
```

##### Flow Log with Traffic Analytics

```fsharp
open Farmer
open Farmer.Builders

let logAnalytics = logAnalytics {
    name "security-workspace"
    sku LogAnalytics.PerGB2018
}

let storageAccount = storageAccount {
    name "flowlogsstorage"
    sku Storage.Standard_LRS
}

let nsg = nsg {
    name "app-tier-nsg"
}

let watcher = networkWatcher {
    name "network-watcher-eastus"
}

let flowLogWithAnalytics = flowLog {
    name "app-tier-flow-logs-analytics"
    link_to_network_watcher watcher
    link_to_nsg nsg.ResourceId
    link_to_storage_account storageAccount.ResourceId
    retention_days 90
    enable_traffic_analytics logAnalytics.ResourceId
}

let deployment = arm {
    location Location.EastUS
    add_resources [ logAnalytics; storageAccount; nsg; watcher; flowLogWithAnalytics ]
}
```

#### Cost Considerations

**Network Watcher Costs:**

| Component | Cost Model | Approx. Cost* |
|-----------|-----------|---------------|
| **Network Watcher Instance** | Per region | **FREE** (automatically deployed) |
| **Flow Logs** | Per GB processed | **$0.50 per GB** |
| **Storage (Flow Logs)** | Standard storage rates | **~$0.02 per GB/month** (LRS) |
| **Traffic Analytics** | Per GB analyzed | **$0.125 per GB** |
| **Log Analytics Ingestion** | Per GB ingested | **$2.30 per GB** (Pay-as-you-go) |

*Approximate costs as of 2025. Actual costs vary by region and usage.

**Example Monthly Cost Calculations:**

| Scenario | Flow Log Volume | Storage | Traffic Analytics | Log Analytics | **Total/Month** |
|----------|----------------|---------|-------------------|---------------|-----------------|
| **Small (Dev/Test)** | 10 GB | $0.20 | $1.25 | $23 | **~$30** |
| **Medium (Production)** | 100 GB | $2 | $12.50 | $230 | **~$295** |
| **Large (Enterprise)** | 1 TB | $20 | $125 | $2,300 | **~$2,950** |

**Cost Optimization Strategies:**

1. **Selective Monitoring**: Only enable Flow Logs on critical NSGs (not every NSG in your environment)
2. **Shorter Retention**: Use 7-30 day retention instead of 90+ days for most workloads
3. **Storage Tiers**: 
   - Use **Cool tier** for archived logs (access infrequent, ~40% cheaper storage)
   - Use **Hot tier** only for recent logs requiring frequent access
4. **Sampling**: For very high-volume environments, consider sampling traffic instead of logging every flow
5. **Traffic Analytics**: Only enable on critical workloads where you need deep insights
6. **Log Analytics**: 
   - Use **Commitment Tiers** if ingesting >100 GB/day (up to 30% savings)
   - Set **data retention** to minimum required (31 days default, first 31 days free)
7. **Regional Placement**: Use same region for Network Watcher, NSG, and Storage to avoid data transfer costs

**When to Use Each Feature:**

| Feature | Use For | Skip If |
|---------|---------|---------|
| **Basic Flow Logs** | Compliance requirements, security investigations, troubleshooting | Cost-sensitive dev/test environments |
| **Traffic Analytics** | Threat detection, capacity planning, usage analysis | Basic compliance logging only |
| **Long Retention** | Regulatory compliance (e.g., 90+ days) | No specific retention requirements |

**Cost vs. Security Tradeoff:**
- Flow Logs are essential for security incident investigation and compliance
- For production workloads handling sensitive data, the cost (~$30-500/month per environment) is typically justified
- Development/test environments may skip Flow Logs to reduce costs

#### Network Watcher Per Region

Azure automatically deploys one Network Watcher instance per region when you enable it. You typically don't need to create Network Watcher instances manually unless:

1. You're using Infrastructure as Code and want explicit control
2. You need to set specific tags for cost tracking
3. You're deploying to a new region for the first time

#### Flow Log Retention

Retention settings control how long flow logs are kept in storage:

- **0 days**: Unlimited retention (data kept until manually deleted)
- **1-365 days**: Logs automatically deleted after specified period
- **Default**: 7 days
- **Compliance**: Set based on regulatory requirements (e.g., PCI DSS requires 90 days)

#### Traffic Analytics Benefits

Traffic Analytics provides advanced insights beyond basic Flow Logs:

1. **Security Threat Detection**: Identify suspicious traffic patterns, malicious IPs
2. **Compliance Reporting**: Generate reports on traffic flows for audits
3. **Capacity Planning**: Identify bandwidth hotspots and underutilized resources
4. **Application Mapping**: Visualize which applications are communicating
5. **Geo-mapping**: See traffic patterns across regions and countries
6. **Top Talkers**: Identify VMs generating most traffic

**When to Enable:**
- Production environments requiring active threat monitoring
- Environments subject to compliance audits
- Complex multi-tier applications where traffic patterns aren't obvious

**When to Skip:**
- Simple dev/test environments
- Cost-sensitive deployments
- When basic flow logs suffice for compliance

#### Best Practices

1. **Regional Deployment**: Deploy one Network Watcher per Azure region you use
2. **Storage Account Location**: Place storage account in same region as NSG to avoid egress charges
3. **Retention Policy**: Set retention based on compliance requirements, not "just in case"
4. **Centralized Storage**: Use one storage account per region for all flow logs
5. **Monitoring**: Set up alerts for unusual traffic patterns in Log Analytics
6. **Cost Alerts**: Configure Azure Cost Management alerts when flow log costs exceed budget
7. **Tagging**: Tag Network Watchers and Flow Logs for cost allocation and tracking
8. **Regular Review**: Periodically review which NSGs have flow logs enabled and disable unnecessary ones

#### Security Benefits

Network Watcher and Flow Logs provide essential security capabilities:

* **Threat Detection**: Identify anomalous traffic patterns and potential attacks
* **Forensic Analysis**: Investigate security incidents with historical traffic data
* **Compliance Auditing**: Demonstrate network traffic logging for regulatory requirements
* **Baseline Monitoring**: Establish normal traffic patterns to detect deviations
* **Incident Response**: Quickly understand network activity during security events
* **Lateral Movement Detection**: Identify unauthorized internal network traversal
* **Data Exfiltration Detection**: Detect unusual outbound traffic patterns

#### Compliance

Network Watcher Flow Logs help meet requirements from major security frameworks:

* **NIST SP 800-53**: AU-2 (Audit Events), AU-6 (Audit Review), SI-4 (Information System Monitoring)
* **ISO 27001**: A.12.4.1 (Event logging), A.12.4.2 (Protection of log information)
* **PCI DSS**: Requirement 10 (Track and monitor all access to network resources)
* **HIPAA**: §164.312(b) (Audit controls)
* **SOC 2**: CC7.2 (System monitoring), CC7.3 (Logging and monitoring)
* **CIS Azure Foundations**: 6.5 (Ensure that Network Watcher is 'Enabled')
* **FedRAMP**: AU-2 (Audit Events), SI-4 (Information System Monitoring)

Flow Logs are particularly important for demonstrating:
- Continuous monitoring of network traffic
- Audit trails for security investigations
- Compliance with data residency requirements (logs stored in specific regions)

#### Integration with Azure Security Center

Network Watcher integrates with Microsoft Defender for Cloud to provide:

* Automatic threat detection based on flow log analysis
* Security recommendations for network configuration
* Network map visualization
* Just-in-time VM access monitoring

#### Limitations and Considerations

* Flow Logs only capture NSG-level traffic (not individual packet content)
* Processing delay: Flow logs typically appear within 5-15 minutes
* Storage costs can accumulate quickly in high-traffic environments
* Traffic Analytics requires Log Analytics workspace (additional cost)
* Flow Logs v2 is the current version (v1 is being deprecated)
* Maximum retention: 365 days (use storage lifecycle policies for longer retention)
