---
title: "Defender for Cloud"
date: 2025-11-08
chapter: false
weight: 19
---

#### Overview
The Defender for Cloud builder enables Microsoft Defender plans for continuous security posture management and threat protection.

* Defender Pricing (`Microsoft.Security/pricings`)

> Microsoft Defender for Cloud (formerly Azure Security Center) provides unified security management and advanced threat protection across hybrid cloud workloads. It helps strengthen security posture, protect against threats, and meet compliance requirements.

#### Builder Keywords

| Keyword | Purpose |
|-|-|
| plan | Sets the Defender plan to enable (VirtualMachines, SqlServers, AppServices, etc.) |
| tier | Sets the pricing tier (Standard for enabled, Free for disabled). Default is Standard |
| enable | Explicitly enables the plan (sets tier to Standard) |
| disable | Disables the plan (sets tier to Free) |

#### Available Defender Plans

| Plan | Protects | Monthly Cost* |
|------|----------|---------------|
| **VirtualMachines** | Azure VMs | **$15/VM** |
| **AppServices** | Web Apps, Functions | **$15/instance** |
| **SqlServers** | Azure SQL, SQL MI | **$15/server** |
| **SqlServerVirtualMachines** | SQL on VMs | **$15/VM** |
| **StorageAccounts** | Blob, Files | **$10/million transactions** |
| **KubernetesService** | AKS clusters | **$7/vCore/month** |
| **ContainerRegistry** | ACR images | **$0.29/image** |
| **KeyVaults** | Key Vaults | **$0.02/10K transactions** |
| **Dns** | DNS queries | **$0.70/million queries** |
| **Arm** | ARM operations | **FREE** |
| **Containers** | Container security | **$7/vCore/month** |
| **OpenSourceRelationalDatabases** | PostgreSQL, MySQL | **$15/server** |
| **CosmosDbs** | Cosmos DB | **$0.0012/100 RU/s/hour** |
| **CloudPosture** | CSPM | **FREE** |

*Approximate costs as of 2025. Actual costs vary.

#### Examples

##### Enable Defender for VMs

```fsharp
open Farmer
open Farmer.Builders
open Farmer.Arm.Security

let vmDefender = defenderForCloud {
    plan DefenderPlan.VirtualMachines
}

let deployment = arm {
    add_resource vmDefender
}
```

##### Enable Multiple Defender Plans

```fsharp
open Farmer
open Farmer.Builders
open Farmer.Arm.Security

let vmDefender = defenderForCloud { plan DefenderPlan.VirtualMachines }
let sqlDefender = defenderForCloud { plan DefenderPlan.SqlServers }
let storageDefender = defenderForCloud { plan DefenderPlan.StorageAccounts }
let aksDefender = defenderForCloud { plan DefenderPlan.KubernetesService }

let deployment = arm {
    add_resources [
        vmDefender
        sqlDefender
        storageDefender
        aksDefender
    ]
}
```

##### Enable Free Cloud Posture Management

```fsharp
open Farmer
open Farmer.Builders
open Farmer.Arm.Security

// Cloud Security Posture Management (CSPM) is FREE
let cspm = defenderForCloud {
    plan DefenderPlan.CloudPosture
}

let deployment = arm {
    add_resource cspm
}
```

##### Disable a Defender Plan

```fsharp
open Farmer
open Farmer.Builders
open Farmer.Arm.Security

let disableVmDefender = defenderForCloud {
    plan DefenderPlan.VirtualMachines
    disable  // Sets tier to Free
}

let deployment = arm {
    add_resource disableVmDefender
}
```

#### Cost Optimization

**Free Features:**
- Cloud Security Posture Management (CSPM)
- Azure Resource Manager (ARM) protection
- Secure Score
- Security recommendations
- Regulatory compliance dashboard

**Recommended Plans by Priority:**

| Priority | Plan | Why |
|----------|------|-----|
| **Critical** | VirtualMachines | Most attack surface |
| **Critical** | SqlServers | Data protection |
| **High** | AppServices | Public-facing apps |
| **High** | KubernetesService | Container security |
| **Medium** | StorageAccounts | Data protection |
| **Medium** | KeyVaults | Secrets protection |
| **Low** | ContainerRegistry | Image scanning |

**Start Small:**
- Enable FREE Cloud Posture first
- Add VM Defender for production VMs
- Expand to other services as needed

#### Security Benefits

Defender for Cloud provides:

* **Continuous Assessment**: Real-time security posture evaluation
* **Secure Score**: Quantified security posture with actionable recommendations
* **Threat Protection**: Advanced threat detection using Microsoft threat intelligence
* **Just-In-Time VM Access**: Reduce attack surface with temporary VM access
* **Adaptive Application Controls**: Whitelist applications on VMs
* **File Integrity Monitoring**: Detect unauthorized changes
* **Vulnerability Assessment**: Built-in scanner for VMs and containers
* **Compliance Dashboard**: Track compliance with standards (PCI, HIPAA, ISO, etc.)
* **Security Alerts**: Real-time alerts for detected threats
* **Automated Response**: Integration with Logic Apps for automation

#### Best Practices

1. **Enable Cloud Posture First**: It's free and provides immediate value
2. **Start with Critical Resources**: VMs and databases first
3. **Review Recommendations Daily**: Act on high-severity findings
4. **Enable JIT Access**: Reduce VM attack surface
5. **Configure Email Alerts**: Get notified of security incidents
6. **Integrate with Sentinel**: Send alerts to SIEM for investigation
7. **Regular Compliance Reviews**: Track regulatory compliance monthly
8. **Test Response Procedures**: Simulate security incidents quarterly

#### Compliance

Defender for Cloud helps meet requirements from:

* **PCI DSS**: Multiple requirements (vulnerability management, monitoring)
* **HIPAA**: §164.308(a)(1) (Security management), §164.308(a)(5) (Security awareness)
* **ISO 27001**: A.12.6 (Technical vulnerability management), A.18.2 (Compliance reviews)
* **NIST CSF**: ID.RA (Risk assessment), DE.CM (Continuous monitoring)
* **SOC 2**: CC7 (System monitoring), CC9 (Risk mitigation)
* **CIS Controls**: Control 3 (Continuous vulnerability management)

#### What You Get

**All Plans Include:**
- Security alerts for detected threats
- Integration with Sentinel
- Compliance dashboard
- Secure Score recommendations
- Integration with Azure Policy

**VM Defender Adds:**
- Just-In-Time VM access
- Adaptive application controls
- File integrity monitoring
- Vulnerability scanner

**Container Defender Adds:**
- Image vulnerability scanning
- Runtime threat protection
- Kubernetes workload protection

#### Next Steps

After enabling Defender:

1. **Review Secure Score**: Check security posture
2. **Act on Recommendations**: Fix high-severity issues
3. **Enable JIT Access**: Configure for production VMs
4. **Set Up Alerts**: Configure email notifications
5. **Integrate with Sentinel**: Send alerts to SIEM
6. **Review Compliance**: Check regulatory compliance status
7. **Test Incident Response**: Simulate and respond to alerts

**Note:** This builder enables Defender plans. Additional configuration (JIT policies, alert rules, etc.) must be done through Azure Portal or CLI.
