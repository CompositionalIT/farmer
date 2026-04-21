---
title: "Azure Security Best Practices"
date: 2025-01-15
chapter: false
weight: 10
---

## Azure Security Best Practices

This guide covers security best practices for Azure infrastructure deployed with Farmer, based on the Microsoft Security Best Practices and the OWASP Top 10.

### OWASP Top 10 Applied to Azure Infrastructure

The OWASP Top 10 (https://owasp.org/www-project-top-ten/) defines critical web application security risks. Here's how they map to Azure infrastructure and Farmer protections:

| OWASP Risk | Azure Context | Farmer Protection |
|------------|---------------|-------------------|
| **A01: Broken Access Control** | Overly permissive RBAC, public storage | Role-based access control, private storage defaults |
| **A02: Cryptographic Failures** | Unencrypted SQL, Storage, Cosmos DB | Encryption enabled by default, HTTPS enforced |
| **A03: Injection** | SQL injection, command injection | Parameterized queries, Managed Identity for DB auth |
| **A05: Security Misconfiguration** | Public storage, weak NSG rules | Secure defaults, deny-by-default NSG rules |
| **A07: Broken Authentication** | Weak passwords, no MFA | Azure AD B2C with strong password policies |
| **A08: Integrity Failures** | Compromised container images | Container Registry scanning, signed deployments |
| **A09: Logging Failures** | No Application Insights, missing logs | Application Insights integration, diagnostic settings |

**Troy Hunt's Guidance on Authentication:** "Passwords are fundamentally broken. Validate new passwords against Pwned Passwords API containing 600M+ breached passwords from real-world attacks."

Integrate Have I Been Pwned (https://haveibeenpwned.com/) into Azure AD B2C custom policies for password validation. Troy Hunt, Microsoft Regional Director and creator of Have I Been Pwned, maintains the definitive database of compromised credentials used by Microsoft and major tech companies.

Reference: OWASP Top 10 2021 (https://owasp.org/www-project-top-ten/)

### Zero Trust Security Model

Azure security is built on the Zero Trust model, which assumes breach and verifies each request as if it originated from an untrusted network. The Zero Trust model is based on three principles from Microsoft Security:

**Verify Explicitly**: Always authenticate and authorize based on all available data points including user identity, location, device health, service or workload, data classification, and anomalies.

**Use Least Privilege Access**: Limit user access with Just-In-Time and Just-Enough-Access (JIT/JEA), risk-based adaptive policies, and data protection.

**Assume Breach**: Minimize blast radius and segment access. Verify end-to-end encryption. Use analytics to get visibility, drive threat detection, and improve defenses.

Reference: Azure Security Best Practices - Zero Trust (https://learn.microsoft.com/en-us/security/zero-trust/)

### Privileged Identity Management (PIM)

Privileged Identity Management provides time-based and approval-based role activation to mitigate the risks of excessive, unnecessary, or misused access permissions. PIM is essential for implementing Just-In-Time access and meeting SOX/SOC 2 compliance requirements.

**Key PIM Capabilities:**

1. **Time-based role activation**: Users activate privileged roles for a limited time (1-8 hours)
2. **Approval workflows**: Require approval before activating sensitive roles
3. **Multi-factor authentication**: Enforce MFA for role activation
4. **Access reviews**: Regular audits of who has privileged access
5. **Audit history**: Complete trail of all role activations

**PIM Configuration (Portal-based):**

While Farmer doesn't directly support PIM configuration, you should configure PIM for the following Azure roles:

- **Owner**: Full access to all resources
- **Contributor**: Can create and manage resources but can't grant access
- **User Access Administrator**: Can manage user access to Azure resources
- **Custom roles with sensitive permissions**: Such as Key Vault administrator

**PIM Best Practices:**

1. Require approval for highly privileged roles (Owner, User Access Administrator)
2. Set maximum activation duration to 4 hours for production environments
3. Enable MFA for all privileged role activations
4. Configure access reviews every 90 days
5. Require justification for all role activations
6. Enable alerts for suspicious activation patterns

Reference: Microsoft Entra Privileged Identity Management (https://learn.microsoft.com/en-us/entra/id-governance/privileged-identity-management/)

### Conditional Access Policies

Conditional Access is the policy engine that enforces Zero Trust by evaluating signals from identity, device, location, and risk before allowing access. This is critical for preventing credential-based attacks.

**Common Conditional Access Policies:**

1. **Require MFA for all users**: Block access unless MFA is completed
2. **Block legacy authentication**: Prevent attacks using old protocols (SMTP, POP3)
3. **Require compliant devices**: Only allow access from managed devices
4. **Block access from untrusted locations**: Restrict access based on geographic location
5. **Require password change for high-risk users**: Force password reset when risk is detected

**Conditional Access Configuration (Portal-based):**

Farmer doesn't directly support Conditional Access configuration. Configure these policies in the Azure Portal under Microsoft Entra ID → Security → Conditional Access:

```text
Policy: Require MFA for All Users
- Users: All users
- Cloud apps: All cloud apps
- Conditions: Any location
- Access controls: Grant access, Require MFA

Policy: Block Legacy Authentication
- Users: All users
- Cloud apps: All cloud apps
- Conditions: Client apps (Exchange ActiveSync, Other clients)
- Access controls: Block access

Policy: Require Compliant Device
- Users: All users
- Cloud apps: Office 365, Azure Portal
- Conditions: Any location
- Access controls: Grant access, Require device to be marked as compliant
```

**Conditional Access Best Practices:**

1. Start with report-only mode to test policy impact
2. Exclude break-glass accounts from all policies
3. Implement policies in stages (pilot → production)
4. Monitor sign-in logs for blocked users
5. Combine multiple conditions for defense-in-depth

Reference: Microsoft Entra Conditional Access (https://learn.microsoft.com/en-us/entra/identity/conditional-access/)

### Azure Active Directory B2C Security

Implement strong authentication with Azure AD B2C.

```fsharp
open Farmer
open Farmer.Builders

let b2cTenant = b2cTenant {
    name "myb2ctenant"
    sku B2c.Sku.PremiumP1
    
    // Configure in Azure Portal:
    // 1. Enable MFA for all users
    // 2. Password complexity: minimum 12 characters
    // 3. Integrate with Have I Been Pwned API via custom policy
    // 4. Implement account lockout after 5 failed attempts
    // 5. Use conditional access policies
}
```

**Have I Been Pwned Integration:**

Azure AD B2C supports custom policies that can call the Pwned Passwords API to reject compromised passwords:

1. Create REST API to call Have I Been Pwned k-anonymity API
2. Configure custom policy with validation technical profile
3. Reject passwords found in breach database

Reference: Integrate Have I Been Pwned with Azure AD B2C (https://docs.microsoft.com/en-us/azure/active-directory-b2c/custom-policy-password-complexity-haveibeenpwned)

### Key Vault Secrets Management

Never hardcode secrets. Always use Azure Key Vault.

```fsharp
open Farmer.KeyVault

let vault = keyVault {
    name "mysecurevault"
    sku Sku.Standard
    
    // Enable soft delete and purge protection
    enable_soft_delete_with_purge_protection
    
    // Restrict network access
    add_ip_rule "203.0.113.0"
    add_ip_rule "203.0.113.1"
    restrict_to_ip_rules
    
    // Add secrets
    add_secret "database-password"
    add_secret "api-key"
}

// Grant web app access using Managed Identity
let app = webApp {
    name "secure-webapp"
    system_identity
    
    // Reference Key Vault secret
    secret_setting "DbPassword" vault.ResourceId "database-password"
}
```

**Key Vault Security Best Practices:**

1. Enable soft delete (14-day recovery window)
2. Enable purge protection (prevents permanent deletion)
3. Use Managed Identity instead of service principals
4. Implement network restrictions (private endpoints or firewall)
5. Enable audit logging to Log Analytics
6. Rotate secrets regularly (90 days recommended)
7. Use separate Key Vaults per environment (dev, staging, prod)

Reference: Azure Key Vault Security Overview (https://docs.microsoft.com/en-us/azure/key-vault/general/security-features)

### SQL Database Security

Protect SQL databases with multiple layers of security.

```fsharp
open Sql

let database = sqlServer {
    name "secureserver"
    admin_username "sqladmin"
    
    add_databases [
        sqlDb {
            name "productiondb"
            sku DtuSku.S1
            use_encryption
        }
    ]
    
    // Enable Azure services access
    enable_azure_firewall
    add_firewall_rule "AllowAzureServices" "0.0.0.0" "0.0.0.0"
    
    // Only allow specific IPs
    add_firewall_rule "Office" "203.0.113.10" "203.0.113.20"
}
```

**SQL Security Checklist:**

1. Enable Azure Defender for SQL (Advanced Threat Protection)
2. Enable Transparent Data Encryption (TDE) - enabled by default
3. Use Azure AD authentication instead of SQL authentication
4. Enable auditing to Log Analytics or Storage
5. Restrict network access via firewall rules or private endpoints
6. Implement row-level security (RLS) for multi-tenant databases
7. Use Always Encrypted for sensitive columns
8. Enable dynamic data masking for PII fields

**OWASP SQL Injection Prevention:**

Always use parameterized queries in your application code:

```fsharp
// ❌ BAD: String interpolation in SQL queries (vulnerable to injection)
let badQuery userEmail = 
    $"SELECT * FROM Users WHERE Email = '{userEmail}'"

// ✅ GOOD: Use parameterized queries in your application code
// This is handled by your data access library (Dapper, Entity Framework, etc.)
// Example with type-safe F# query builders or stored procedures
```

Reference: Azure SQL Security Best Practices (https://docs.microsoft.com/en-us/azure/azure-sql/database/security-best-practice)

### Storage Account Security

Secure blob storage against unauthorized access.

```fsharp
let storage = storageAccount {
    name "securestorage"
    sku Storage.Sku.Standard_LRS
    
    // Require HTTPS
    enable_data_lake_storage
    min_tls_version Tls12
    
    // Disable public access
    disable_public_network_access
    
    // Enable soft delete
    enable_blob_soft_delete 30<Days>
    
    // Lifecycle management
    add_lifecycle_rule "archive-old-data" [
        Storage.CoolAfter 30<Days>
        Storage.ArchiveAfter 90<Days>
        Storage.DeleteAfter 365<Days>
    ] Storage.NoRuleFilters
}
```

**Storage Security Best Practices:**

1. Block public blob access unless absolutely necessary
2. Use Shared Access Signatures (SAS) with short expiration times
3. Enable soft delete (7-30 days retention)
4. Require HTTPS (disable HTTP completely)
5. Enable versioning for critical blobs
6. Use Azure AD authentication instead of shared keys
7. Enable Storage Analytics logging
8. Implement network restrictions (firewall rules or private endpoints)
9. Enable Azure Defender for Storage

Reference: Azure Storage Security Guide (https://docs.microsoft.com/en-us/azure/storage/blobs/security-recommendations)

### Private Endpoints for Data Security

Private Endpoints provide secure connectivity to Azure PaaS services over a private IP address from your VNet, eliminating exposure to the public internet. This is critical for Zero Trust architecture and compliance requirements (HIPAA, PCI DSS).

**Private Endpoint Benefits:**

1. **Eliminates public internet exposure**: Services are accessed via private IP addresses
2. **Protection against data exfiltration**: Traffic stays within Azure backbone
3. **Simplified network security**: No need for service-specific firewall rules
4. **Compliance enablement**: Required for many regulatory frameworks

**Private Endpoint Implementation:**

Farmer currently has limited support for private endpoints. Configure via Azure Portal or ARM templates:

```fsharp
// Example: Storage account with network restrictions
let storage = storageAccount {
    name "securestorage"
    sku Storage.Sku.Standard_LRS
    
    // Disable public network access - force private endpoint usage
    disable_public_network_access
    
    // Configure private endpoint in Azure Portal:
    // 1. Create private endpoint in your VNet
    // 2. Select target sub-resource (blob, file, table, queue)
    // 3. Configure private DNS integration
    // 4. Approve private endpoint connection
}
```

**Services Supporting Private Endpoints:**

- **Storage Accounts**: Blob, File, Table, Queue, Data Lake Gen2
- **SQL Database**: Database server, SQL Managed Instance
- **Cosmos DB**: SQL API, MongoDB API, Cassandra API
- **Key Vault**: Secure vault access from VNet only
- **Web Apps**: Inbound traffic to web app
- **Container Registry**: Secure image pull operations
- **Azure Cache for Redis**: Database access

**Private Endpoint Best Practices:**

1. Always use private endpoints for production PaaS services handling sensitive data
2. Configure Private DNS zones for automatic name resolution
3. Use Network Security Groups to control traffic to private endpoints
4. Disable public network access after private endpoint is validated
5. Document private endpoint connections for audit compliance
6. Use Azure Private Link service for custom applications

Reference: Azure Private Endpoint Documentation (https://learn.microsoft.com/en-us/azure/private-link/private-endpoint-overview)

### Azure Bastion for Secure VM Access

Azure Bastion provides secure RDP and SSH connectivity to Azure VMs without exposing RDP/SSH ports to the public internet. This eliminates a major attack vector for brute-force attacks and is essential for production environments.

**Azure Bastion Architecture:**

Azure Bastion is a fully managed PaaS service that acts as a jump server (bastion host) deployed inside your VNet. It provides secure connectivity over TLS from the Azure Portal directly to your VMs without needing public IP addresses on VMs.

**Security Benefits:**

1. **No public IP addresses required on VMs**: Eliminates exposure to port scanning
2. **No NSG rules for RDP/SSH**: Reduces attack surface
3. **Hardened by Microsoft**: Platform automatically patched and updated
4. **MFA integration**: Works with Azure AD authentication
5. **Audit logs**: All sessions logged for compliance

**Azure Bastion Deployment:**

Farmer currently doesn't support Azure Bastion builder. Deploy via Azure Portal or Azure CLI:

```fsharp
// Example: VNet with dedicated Bastion subnet
let vnet = vnet {
    name "production-vnet"
    add_address_spaces [
        "10.0.0.0/16"
    ]
    add_subnets [
        subnet {
            name "AzureBastionSubnet"  // Required name
            prefix "10.0.0.0/26"  // Minimum /26 CIDR
            // Reserve for Azure Bastion only
        }
        subnet {
            name "web-tier"
            prefix "10.0.1.0/24"
        }
        subnet {
            name "data-tier"
            prefix "10.0.2.0/24"
        }
    ]
}

// Deploy Azure Bastion via Azure CLI:
// az network bastion create \
//   --name MyBastion \
//   --resource-group production-rg \
//   --vnet-name production-vnet \
//   --location eastus \
//   --sku Standard
```

**Azure Bastion SKUs:**

| Feature | Basic SKU | Standard SKU |
|---------|-----------|--------------|
| RDP/SSH via Azure Portal | ✓ | ✓ |
| Native RDP/SSH client support | ✗ | ✓ |
| File transfer | ✗ | ✓ |
| IP-based connection | ✗ | ✓ |
| Host scaling | ✗ | ✓ |
| Shareable links | ✗ | ✓ |

**Azure Bastion Best Practices:**

1. Use Standard SKU for production workloads requiring native client support
2. Create dedicated `/26` subnet named `AzureBastionSubnet`
3. Enable diagnostic logging to Log Analytics workspace
4. Use Azure RBAC to control who can connect via Bastion
5. Consider hub-and-spoke topology with centralized Bastion
6. Document Bastion access procedures in runbooks

Reference: Azure Bastion Documentation (https://learn.microsoft.com/en-us/azure/bastion/)

### DDoS Protection

Azure DDoS Protection defends against Distributed Denial of Service attacks that can make your applications unavailable. DDoS attacks can cost thousands of dollars per hour in lost revenue and resource scaling charges.

**DDoS Attack Types:**

1. **Volumetric attacks**: Saturate network bandwidth (DNS amplification, UDP floods)
2. **Protocol attacks**: Exploit weaknesses in Layer 3/4 protocols (SYN floods, fragmented packets)
3. **Application layer attacks**: Target web applications (HTTP floods, slowloris)

**Azure DDoS Protection Tiers:**

**Basic (Free)**: 
- Automatic protection for all Azure resources
- Always-on traffic monitoring
- Real-time attack mitigation
- No cost

**Standard (Paid)**:
- Tuned protection policies specific to your resources
- DDoS rapid response team support during active attacks
- Cost protection (service credits during attacks)
- Attack analytics and metrics
- Attack mitigation reports

**DDoS Protection Implementation:**

```fsharp
// DDoS Protection Standard is configured at the subscription level
// Deploy via Azure CLI or Portal

// Example: VNet ready for DDoS Protection
let vnet = vnet {
    name "protected-vnet"
    add_address_spaces [ "10.0.0.0/16" ]
    add_subnets [
        subnet {
            name "web-tier"
            prefix "10.0.1.0/24"
        }
    ]
}

// Create DDoS Protection Plan via CLI:
// az network ddos-protection create \
//   --resource-group production-rg \
//   --name production-ddos-plan \
//   --location eastus
//
// Then associate it with VNet:
// az network vnet update \
//   --resource-group production-rg \
//   --name protected-vnet \
//   --ddos-protection-plan production-ddos-plan \
//   --ddos-protection true
```

**DDoS Protection Standard Costs:**

- **Fixed monthly fee**: ~$2,944/month for first 100 public IPs
- **Additional IPs**: ~$30/IP/month beyond 100
- **Cost protection**: Azure credits for scaling costs during attacks

**DDoS Protection Best Practices:**

1. Enable DDoS Protection Standard for mission-critical applications
2. Configure DDoS alerts in Azure Monitor (attack detection, mitigation)
3. Test DDoS protection with simulation partners (BreakingPoint Cloud)
4. Combine with Azure Firewall and WAF for layered protection
5. Enable diagnostic logs for forensic analysis
6. Document DDoS response procedures

**DDoS Attack Response:**

1. Azure automatically detects and mitigates attacks (Basic and Standard)
2. Standard tier: DDoS Rapid Response team available during active attacks
3. Post-attack: Review mitigation reports and attack analytics
4. Adjust protection policies based on attack patterns

Reference: Azure DDoS Protection (https://learn.microsoft.com/en-us/azure/ddos-protection/)

### Web Application Firewall (WAF)

Protect web applications from OWASP Top 10 attacks using Azure WAF.

```fsharp
open Farmer.ApplicationGateway

let appGateway = appGateway {
    name "secure-gateway"
    
    // Enable WAF with OWASP rules
    sku_capacity 2
    sku ApplicationGatewaySku.WAF_v2
    
    // Configure in Azure Portal:
    // 1. Enable OWASP 3.2 rule set
    // 2. Set to Prevention mode (not just Detection)
    // 3. Enable bot protection
    // 4. Configure custom rules for rate limiting
}
```

**Azure WAF Protection Against OWASP Top 10:**

- **SQL Injection (A03)**: OWASP rule 942xxx series
- **Cross-Site Scripting (XSS)**: OWASP rule 941xxx series  
- **Security Misconfiguration (A05)**: Protocol enforcers
- **Broken Authentication (A07)**: Rate limiting, bot protection
- **Server-Side Request Forgery (A10)**: OWASP rule 944xxx series

Reference: Azure WAF on Application Gateway (https://docs.microsoft.com/en-us/azure/web-application-firewall/ag/ag-overview)

### Network Security Groups (NSG)

Implement defense-in-depth with NSG rules.

```fsharp
let webNsg = nsg {
    name "web-tier-nsg"
    
    add_rules [
        securityRule {
            name "AllowHTTPS"
            description "Allow HTTPS from Internet"
            services [ NetworkService ("https", 443) ]
            add_source_tag TCP "Internet"
            add_destination_any
            direction Inbound
        }
        securityRule {
            name "DenyAll"
            description "Deny all other inbound"
            services [ anyProtocol ]
            add_source_any
            add_destination_any
            direction Inbound
            priority 4096
            access Deny
        }
    ]
}

// Database tier - only allow from web tier
let dbNsg = nsg {
    name "data-tier-nsg"
    
    add_rules [
        securityRule {
            name "AllowSQL"
            description "Allow SQL from web tier only"
            services [ NetworkService ("mssql", 1433) ]
            add_source_address TCP "10.0.1.0/24"  // Web subnet
            add_destination_address "10.0.2.0/24"  // DB subnet
            direction Inbound
        }
    ]
}
```

**NSG Best Practices:**

1. Default deny all inbound traffic
2. Explicitly allow only required ports and protocols
3. Use service tags instead of IP addresses when possible
4. Implement network segmentation (web, app, data tiers)
5. Enable NSG flow logs for security analysis
6. Use Application Security Groups for dynamic rule management
7. Regular audit of NSG rules (remove unused rules)

Reference: Azure NSG Security Best Practices (https://docs.microsoft.com/en-us/azure/virtual-network/network-security-groups-overview)

### Microsoft Defender for Cloud

Microsoft Defender for Cloud (formerly Azure Security Center) is a unified infrastructure security management system that provides advanced threat protection across hybrid cloud workloads. Defender for Cloud is essential for maintaining security posture and detecting threats in real-time.

**Core Capabilities:**

1. **Cloud Security Posture Management (CSPM)**: Continuous assessment of security configuration
2. **Cloud Workload Protection (CWP)**: Threat detection and protection for specific workload types
3. **Regulatory Compliance Dashboard**: Track compliance with frameworks (PCI DSS, HIPAA, SOC 2, ISO 27001)
4. **Secure Score**: Quantified security posture measurement
5. **Security Recommendations**: Actionable remediation guidance

**Defender CSPM (Cloud Security Posture Management):**

Defender CSPM provides free and premium capabilities for security posture management:

**Free Tier (Foundational CSPM):**
- Secure score
- Security recommendations
- Azure security benchmark assessment
- Basic regulatory compliance

**Paid Tier (Defender CSPM Plan):**
- Attack path analysis (identifies exploitation routes)
- Cloud security graph (contextual risk assessment)
- Agentless scanning for VMs
- Data-aware security posture
- Governance and regulatory compliance dashboard
- DevOps security (GitHub/Azure DevOps integration)

**Enabling Defender CSPM:**

```bash
# Enable Defender CSPM plan via Azure CLI
az security pricing create \
  --name CloudPosture \
  --tier Standard

# Enable foundational CSPM (free)
# Automatic when you access Defender for Cloud
```

**Attack Path Analysis:**

Attack paths show how attackers could potentially exploit your environment by chaining vulnerabilities. Example attack path:

```
Internet-exposed VM with vulnerabilities
  → Has managed identity
    → Identity has Contributor role
      → Can access sensitive storage account
        → Contains customer PII data
```

**Cloud Security Graph:**

The security graph analyzes relationships between resources to identify risks based on:
- **Exploitability**: What can be compromised (CVEs, misconfigurations)
- **Business impact**: What sensitive data or critical systems are at risk
- **Lateral movement potential**: What other resources can be reached

**Defender for Cloud Workload Protection Plans:**

| Plan | Protects | Key Features | Typical Cost |
|------|----------|--------------|--------------|
| Defender for Servers | VMs, VMSS | Vulnerability assessment, JIT access, file integrity monitoring | ~$15/server/month |
| Defender for App Service | Web apps, Function apps | Runtime threat protection, dependency scanning | ~$15/plan/month |
| Defender for Storage | Blob, File, Data Lake | Malware scanning, anomaly detection, sensitive data discovery | ~$10/storage account/month |
| Defender for SQL | SQL Database, SQL MI | Vulnerability assessment, threat detection, sensitive data discovery | ~$15/server/month |
| Defender for Containers | AKS, ACR, Arc K8s | Image scanning, runtime protection, Kubernetes audit logs | ~$7/vCore/month |
| Defender for Key Vault | Key Vault | Anomaly detection, suspicious operations | ~$0.02/10K operations |
| Defender CSPM | All resources | Attack paths, security graph, compliance | ~$5/resource/month |

**Microsoft Defender for Cloud Best Practices:**

1. **Enable Defender CSPM for all subscriptions**: Provides attack path analysis and security graph
2. **Enable workload-specific plans**: Defender for Servers, Storage, SQL for production workloads
3. **Integrate with Azure Sentinel**: Export alerts for advanced threat hunting
4. **Configure email notifications**: Alert security team of high-severity findings
5. **Implement security governance**: Assign owners and due dates to recommendations
6. **Track secure score trends**: Monitor improvement over time
7. **Use Just-In-Time VM access**: Reduce VM attack surface
8. **Enable adaptive application controls**: Whitelist applications on VMs
9. **Review regulatory compliance dashboard**: Ensure adherence to industry standards
10. **Automate remediation**: Use Azure Policy and Logic Apps for automatic fixes

**Secure Score Optimization:**

Secure score is calculated based on healthy resources / total resources. Focus on high-impact recommendations first:

**High-Impact Actions (10+ points each):**
- Enable MFA for all users
- Enable Defender plans for workload types
- Remove deprecated accounts
- Restrict network access to storage accounts
- Enable Azure Backup for VMs

**Medium-Impact Actions (5-9 points):**
- Update outdated operating systems
- Enable disk encryption
- Configure NSG rules
- Implement Just-In-Time VM access

**Security Governance:**

Assign owners and due dates to security recommendations to track remediation progress:

```bash
# Assign recommendation owner via Azure CLI
az security task create \
  --name "Enable MFA for all users" \
  --recommendation-id <recommendation-id> \
  --assigned-to security-team@company.com \
  --due-date 2025-03-01
```

**Integration with CI/CD:**

Defender for DevOps integrates with GitHub Actions and Azure DevOps:
- Infrastructure-as-code scanning (Terraform, ARM, Bicep)
- Container image scanning in CI/CD pipelines
- Secrets scanning in source code repositories
- Code-to-cloud security contextualization

Reference: Microsoft Defender for Cloud Documentation (https://learn.microsoft.com/en-us/azure/defender-for-cloud/)

### Monitoring and Threat Detection

Enable comprehensive security monitoring across your Azure environment.

```fsharp
let insights = appInsights {
    name "security-monitoring"
    retention_days 90  // Compliance requirement
    log_analytics_workspace_linking_mode Workspace
}

// Enable Microsoft Defender for Cloud
// Configure in Azure Portal:
// 1. Enable Defender CSPM and workload protection plans
// 2. Configure Security Center alerts via email
// 3. Enable Just-In-Time VM access
// 4. Configure adaptive application controls
// 5. Set up continuous export to Log Analytics
```

**Security Monitoring Stack:**

1. **Azure Defender for Cloud**: Threat protection across all services
2. **Azure Sentinel**: SIEM for advanced threat detection
3. **Application Insights**: Application-level security events
4. **Log Analytics**: Centralized log aggregation
5. **Network Watcher**: Network traffic analysis
6. **Azure Monitor**: Alerts and automation

**Critical Security Alerts to Configure:**

- Root account sign-in attempts
- Multiple failed authentication attempts
- Changes to NSG rules
- Public IP address assignments
- Key Vault access anomalies
- SQL injection attempts (via WAF)
- Unusual data egress patterns

Reference: Azure Security Center Best Practices (https://docs.microsoft.com/en-us/azure/defender-for-cloud/security-center-introduction)

### Compliance Frameworks

Map your Azure security controls to compliance requirements:

**OWASP Top 10 Compliance:**
- Addressed through WAF, secure coding practices, and Azure PaaS defaults

**PCI DSS (Payment Card Industry):**
- Requirement 3.4: Encryption (Azure Storage, SQL TDE)
- Requirement 8.2: Strong authentication (Azure AD MFA)
- Requirement 10.1: Audit trails (Azure Monitor, Log Analytics)

**HIPAA (Healthcare):**
- §164.312(a)(2)(iv): Encryption (Key Vault, TDE)
- §164.308(a)(5)(ii)(C): Log-in monitoring (Azure AD logs)
- §164.312(b): Audit controls (Azure Monitor)

**GDPR (Data Privacy):**
- Article 32: Security of processing (encryption, access controls)
- Article 33: Breach notification (Azure Security Center alerts)
- Article 25: Data protection by design (Farmer secure defaults)

**SOC 2 (Service Organization Control):**
- CC6.1: Logical access controls (Azure RBAC, Managed Identity)
- CC7.2: System monitoring (Application Insights, Log Analytics)
- CC6.6: Encryption (Key Vault, TDE, Storage encryption)

Reference: Azure Compliance Offerings (https://docs.microsoft.com/en-us/azure/compliance/)

### Security Best Practices Summary

1. **Identity and Access**:
   - Use Azure AD with MFA enabled for all users
   - Implement Managed Identity for Azure resources
   - Check passwords against Have I Been Pwned database
   - Follow principle of least privilege with RBAC

2. **Data Protection**:
   - Enable encryption at rest (SQL TDE, Storage encryption)
   - Use Key Vault for secrets management
   - Enforce HTTPS/TLS 1.2+ for all data in transit
   - Implement row-level security for multi-tenant databases

3. **Network Security**:
   - Use NSG rules with default deny
   - Implement network segmentation (VNet, subnets)
   - Deploy Azure WAF for OWASP protection
   - Use private endpoints for PaaS services

4. **Application Security**:
   - Follow OWASP secure coding practices
   - Use parameterized queries (prevent SQL injection)
   - Validate and sanitize all inputs
   - Implement rate limiting and bot protection

5. **Monitoring and Response**:
   - Enable Azure Defender for all resource types
   - Configure Security Center alerts
   - Implement centralized logging with Log Analytics
   - Regular security audits and penetration testing

6. **Compliance**:
   - Document security controls for audit requirements
   - Regular compliance assessments
   - Maintain audit trail for all administrative actions
   - Implement data retention policies per framework

### Additional Security Resources

**Microsoft Official:**
- [Azure Security Best Practices](https://docs.microsoft.com/en-us/azure/security/fundamentals/best-practices-and-patterns)
- [Azure Security Benchmark](https://docs.microsoft.com/en-us/security/benchmark/azure/)
- [Microsoft Security Response Center](https://www.microsoft.com/en-us/msrc)

**OWASP Resources:**
- [OWASP Top 10 2021](https://owasp.org/www-project-top-ten/)
- [OWASP Azure Security](https://owasp.org/www-community/Azure_Security)
- [OWASP Cheat Sheet Series](https://cheatsheetseries.owasp.org/)

**Troy Hunt Resources:**
- [Have I Been Pwned](https://haveibeenpwned.com/)
- [Pwned Passwords API](https://haveibeenpwned.com/API/v3#PwnedPasswords)
- [Troy Hunt's Blog](https://www.troyhunt.com/)

**Azure Security Community:**
- [Azure Security & Compliance Blog](https://techcommunity.microsoft.com/t5/azure-security/bg-p/AzureSecurityBlog)
- [Azure Security on Twitter](https://twitter.com/AzureSecurity)
- [Azure Security YouTube Channel](https://www.youtube.com/c/MicrosoftSecurityCommunity)

---

This guide reflects Azure security best practices as of 2025. Always refer to the latest Microsoft documentation and your organization's compliance requirements when implementing security controls.
