---
title: "Recovery Services Vault"
date: 2025-11-08
chapter: false
weight: 17
---

#### Overview
The Recovery Services Vault builder creates Azure Recovery Services Vaults for backup and disaster recovery, along with backup policies for virtual machines.

* Recovery Services Vault (`Microsoft.RecoveryServices/vaults`)
* VM Backup Policy (`Microsoft.RecoveryServices/vaults/backupPolicies`)

> Recovery Services Vaults provide centralized backup and disaster recovery for Azure resources. They support Azure VMs, SQL databases, file shares, and on-premises workloads, with features like cross-region restore, soft delete, and ransomware protection.

#### Builder Keywords

##### Recovery Services Vault

| Keyword | Purpose |
|-|-|
| name | Sets the name of the Recovery Services Vault |
| sku | Sets the SKU (RS0 for free tier, Standard for production). Default is Standard |
| add_tag | Adds a tag to the vault |
| add_tags | Adds multiple tags to the vault |

##### VM Backup Policy

| Keyword | Purpose |
|-|-|
| name | Sets the name of the backup policy |
| link_to_vault | Links to a Recovery Services Vault config |
| vault_name | Sets the vault name directly (for existing vaults) |
| schedule_frequency | Sets backup frequency (Daily or Weekly). Default is Daily |
| schedule_time | Sets backup time in ISO format. Default is 3 AM UTC |
| retention_days | Sets daily retention (7-9999 days). Default is 30 days |
| weekly_retention_weeks | Sets weekly retention (1-5163 weeks) |
| monthly_retention_months | Sets monthly retention (1-1188 months) |
| add_dependency | Adds a dependency to the backup policy |

#### Examples

##### Basic Recovery Services Vault

```fsharp
open Farmer
open Farmer.Builders

let vault = recoveryServicesVault {
    name "production-backup-vault"
    add_tags [
        "environment", "production"
        "backup-tier", "critical"
    ]
}

let deployment = arm {
    location Location.EastUS
    add_resource vault
}
```

##### VM Backup Policy with Daily Backups

```fsharp
open Farmer
open Farmer.Builders
open Farmer.Arm.RecoveryServices

let vault = recoveryServicesVault {
    name "backup-vault"
}

let dailyBackup = vmBackupPolicy {
    name "daily-vm-backup"
    link_to_vault vault
    schedule_frequency BackupScheduleFrequency.Daily
    schedule_time "2023-01-01T02:00:00Z"  // 2 AM UTC
    retention_days 30
}

let deployment = arm {
    location Location.EastUS
    add_resources [ vault; dailyBackup ]
}
```

##### Comprehensive Backup Policy (Daily + Weekly + Monthly)

```fsharp
open Farmer
open Farmer.Builders
open Farmer.Arm.RecoveryServices

let vault = recoveryServicesVault {
    name "enterprise-backup-vault"
    add_tags [ "compliance", "required"; "retention", "long-term" ]
}

let comprehensivePolicy = vmBackupPolicy {
    name "comprehensive-backup"
    link_to_vault vault
    schedule_frequency BackupScheduleFrequency.Daily
    schedule_time "2023-01-01T03:00:00Z"
    retention_days 30              // Keep daily backups for 30 days
    weekly_retention_weeks 52      // Keep weekly backups for 1 year
    monthly_retention_months 24    // Keep monthly backups for 2 years
}

let deployment = arm {
    location Location.EastUS
    add_resources [ vault; comprehensivePolicy ]
}
```

##### Multiple Backup Policies for Different Workloads

```fsharp
open Farmer
open Farmer.Builders
open Farmer.Arm.RecoveryServices

let vault = recoveryServicesVault { name "shared-backup-vault" }

// Production VMs: Long retention
let productionPolicy = vmBackupPolicy {
    name "production-backup"
    link_to_vault vault
    retention_days 90
    weekly_retention_weeks 52
    monthly_retention_months 12
}

// Development VMs: Short retention
let devPolicy = vmBackupPolicy {
    name "dev-backup"
    link_to_vault vault
    retention_days 7
}

let deployment = arm {
    location Location.EastUS
    add_resources [ vault; productionPolicy; devPolicy ]
}
```

#### Cost Considerations

**Recovery Services Vault Costs:**

| Component | Cost Model | Approx. Cost* |
|-----------|-----------|---------------|
| **Vault (Standard SKU)** | FREE | **$0** |
| **Protected Instance** | Per VM/month | **~$5-10/VM** (depends on size) |
| **Backup Storage (LRS)** | Per GB/month | **$0.05/GB** (first 50 GB free per vault) |
| **Backup Storage (GRS)** | Per GB/month | **$0.10/GB** (geo-redundant) |
| **Snapshot Storage** | Per GB/month | **$0.05/GB** |
| **Restore** | Per GB | **Free** (data transfer may apply) |
| **Cross-Region Restore** | Data transfer | **$0.02/GB** (outbound) |

*Approximate costs as of 2025. Actual costs vary by region.

**Example Monthly Cost Calculations:**

| Scenario | VMs | Avg VM Size | Storage (GRS) | **Total/Month** |
|----------|-----|-------------|---------------|-----------------|
| **Small (Dev/Test)** | 5 VMs | 50 GB each | 250 GB | **$25-75** |
| **Medium (Production)** | 20 VMs | 100 GB each | 2 TB | **$300-500** |
| **Large (Enterprise)** | 100 VMs | 150 GB each | 15 TB | **$2,000-3,500** |

**Free Tier:**
- First 50 GB of backup storage per vault is **FREE** (Standard SKU)
- Good for small dev/test environments

**Cost Optimization Strategies:**

1. **Right-Size Retention**:
   - Dev/test: 7 days ($minimal)
   - Production: 30 days ($moderate)
   - Compliance: 90+ days ($higher)
   - Don't over-retain "just in case"

2. **Use LRS for Non-Critical Workloads**:
   - LRS: $0.05/GB (single region)
   - GRS: $0.10/GB (geo-redundant)
   - 50% savings for workloads that don't need geo-redundancy

3. **Instant Restore vs. Standard**:
   - Instant Restore (snapshot-based): Faster but costs more
   - Standard (vault-based): Slower but cheaper
   - Keep instant restore snapshots for 1-5 days only

4. **Policy Segregation**:
   - Create separate policies for different workload tiers
   - Critical VMs: Long retention
   - Non-critical VMs: Short retention
   - Development VMs: Minimal retention

5. **Backup Compression**:
   - Azure automatically compresses backups (saves ~50% storage)
   - No configuration needed

6. **Monitor Unused Backups**:
   - Delete backups for decommissioned VMs
   - Use soft-delete (14-day retention) to prevent accidental deletion

**Cost vs. Risk Tradeoff:**

| Retention Period | Cost Level | Suitable For | Compliance |
|-----------------|------------|--------------|------------|
| **7 days** | Very Low (~$10/VM/mo) | Dev/test | None |
| **30 days** | Low (~$15/VM/mo) | Production | Basic |
| **90 days** | Medium (~$30/VM/mo) | Critical | PCI DSS, HIPAA |
| **365 days** | High (~$100/VM/mo) | Compliance | SOX, regulatory |

**When to Use Each SKU:**

| SKU | Cost | When to Use |
|-----|------|-------------|
| **Standard** | Backup costs only | Production (recommended) |
| **RS0** | FREE (deprecated) | Legacy only, use Standard instead |

**ROI Analysis:**

Backup costs are typically **1-5% of infrastructure costs** but prevent:
- Ransomware recovery costs ($100K-$1M+)
- Data loss from accidental deletions
- Compliance fines ($10K-$1M+)
- Business downtime ($5K-$500K per hour)

**Cost is minimal compared to risk avoided.**

#### Retention Policies

Azure Backup supports multiple retention tiers:

| Tier | Purpose | Max Duration | Typical Use |
|------|---------|--------------|-------------|
| **Daily** | Short-term recovery | 9999 days | All backups |
| **Weekly** | Medium-term recovery | 5163 weeks (~99 years) | Weekly checkpoints |
| **Monthly** | Long-term archival | 1188 months (~99 years) | Monthly archives |
| **Yearly** | Compliance archival | 99 years | Regulatory requirements |

**Compliance Retention Requirements:**

| Framework | Minimum Retention | Recommendation |
|-----------|------------------|----------------|
| **PCI DSS** | 90 days | 90 days daily + 1 year weekly |
| **HIPAA** | 6 years | 30 days daily + 7 years monthly |
| **SOX** | 7 years | 30 days daily + 7 years monthly |
| **GDPR** | Varies | 30 days (right to be forgotten applies) |
| **ISO 27001** | Define in policy | 90 days minimum recommended |

#### Backup Schedule

**Schedule Frequency:**
- **Daily**: Most common, recommended for production
- **Weekly**: For non-critical workloads

**Schedule Time:**
- Use ISO 8601 format: `"2023-01-01T03:00:00Z"`
- Choose off-peak hours to minimize performance impact
- Typical production time: 2-4 AM local time
- All times in UTC

**Backup Windows:**
- Daily backups take 30 minutes - 2 hours (depending on VM size and change rate)
- First backup (full) takes longer than incremental backups
- Plan for 2-4 hour backup window

#### Best Practices

1. **Use Standard SKU**: RS0 is deprecated, always use Standard
2. **Enable Soft Delete**: 14-day recovery window for accidentally deleted backups (enabled by default)
3. **Tag Everything**: Use tags for cost tracking and compliance reporting
4. **Separate Vaults by Environment**: Dev, test, prod in different vaults
5. **Regional Placement**: Place vault in same region as VMs to avoid data transfer costs
6. **Geo-Redundancy for Production**: Use GRS for critical workloads
7. **Test Restores**: Regularly test restore procedures (monthly recommended)
8. **Monitor Backup Jobs**: Set up alerts for failed backups
9. **Document Retention Policies**: Tie retention to business requirements
10. **Use Azure Policy**: Enforce backup on all production VMs

#### Security Benefits

Recovery Services Vaults provide critical business continuity and security capabilities:

* **Ransomware Protection**: Immutable backups protected from encryption attacks
* **Soft Delete**: 14-day recovery window for deleted backups
* **Role-Based Access Control**: Granular permissions for backup operators
* **Encryption**: All data encrypted at rest and in transit
* **Air-Gapped Backups**: Isolated from production environment
* **Cross-Region Restore**: Disaster recovery to secondary region
* **Audit Logs**: Full backup/restore activity tracking
* **Point-in-Time Recovery**: Restore VMs to specific backup points

#### Compliance

Recovery Services Vaults help meet backup requirements from major frameworks:

* **PCI DSS**: Requirement 3.1 (Keep cardholder data retention to minimum), 9.5 (Backup data)
* **HIPAA**: §164.308(a)(7) (Contingency plan), §164.310(d) (Data backup)
* **SOX**: Section 404 (Data retention and recoverability)
* **ISO 27001**: A.12.3.1 (Information backup)
* **NIST SP 800-53**: CP-9 (Information System Backup)
* **SOC 2**: CC7.5 (System availability)
* **GDPR**: Article 32 (Security of processing - resilience)

**Audit Evidence:**
- Backup success/failure reports
- Restore test results
- Retention policy documentation
- Recovery time objective (RTO) testing

#### Integration with Azure Services

Recovery Services Vaults integrate with:

* **Azure VMs**: Full VM backup and restore
* **Azure Policy**: Enforce backup on resources
* **Azure Monitor**: Backup job monitoring and alerts
* **Log Analytics**: Centralized backup reporting
* **Azure Security Center**: Backup recommendations
* **Azure Site Recovery**: Combined backup + DR strategy

#### Limitations and Considerations

* **Backup Frequency**: Maximum once per day (24-hour minimum interval)
* **Instant Restore**: Snapshots retained for maximum 5 days
* **Vault Location**: Must be in same Azure geography as backup source
* **First Backup**: Takes longer (full backup), subsequent backups are incremental
* **Bandwidth**: Initial backups may take time depending on VM size
* **Restore Time**: Depends on data size (typically 2-6 hours for full VM)
* **Cross-Region Restore**: Only available with GRS vaults
* **Soft Delete**: Cannot be disabled during first 14 days after enabling

#### What's Not Covered (Out of Scope)

This builder focuses on **VM backup only**. For complete backup coverage, you'll also need:

- SQL/PostgreSQL database backups (built into database builders)
- File share backups (separate Azure Backup for Files)
- On-premises backup (requires Azure Backup Server)
- Azure Site Recovery (disaster recovery orchestration)

These require additional configuration beyond this builder.

#### Next Steps

After creating a vault and policy:

1. **Associate VMs with Policy**: Use Azure Portal or CLI to assign policy to VMs
2. **Run Initial Backup**: Trigger first backup manually to verify configuration
3. **Test Restore**: Perform test restore to secondary location
4. **Set Up Monitoring**: Configure backup job alerts in Azure Monitor
5. **Document RTO/RPO**: Define recovery time objectives for business continuity plan

**Note:** Farmer creates the vault and policies. Associating specific VMs with backup policies is typically done through Azure Portal, CLI, or Azure Policy enforcement.
