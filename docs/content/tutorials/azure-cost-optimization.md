---
title: "Azure Cost Optimization Strategies"
date: 2025-01-15
chapter: false
weight: 9
---

## Azure Cost Optimization Strategies

This guide provides practical approaches to reduce Azure infrastructure costs while maintaining security, reliability, and performance. Based on the Microsoft Azure Well-Architected Framework Cost Optimization pillar and real-world implementations.

**Note on Pricing**: Cost figures in this guide are approximate as of January 2025 in USD for the East US region. Azure pricing varies by region, currency, and billing agreements. Always verify current pricing at https://azure.microsoft.com/pricing/calculator/ before making budget decisions.

### Understanding Azure Cost Structure

Azure costs follow a consumption-based model. The largest cost drivers are typically compute, storage, and networking. According to Microsoft's Azure Cost Management best practices, organizations can reduce cloud spend by 30-50% through systematic optimization.

Reference: Azure Well-Architected Framework - Cost Optimization (https://learn.microsoft.com/en-us/azure/well-architected/cost-optimization/)

### Environment-Specific Cost Tiers

Different environments require different cost-performance trade-offs. This strategy is used by Azure MVPs and documented in Microsoft's Cloud Adoption Framework.

#### Development Environment

Optimize for minimal costs while maintaining functionality for developers.

```fsharp
open Farmer
open Farmer.Builders
open Sql

// Minimal App Service Plan
let devAppPlan = servicePlan {
    name "dev-app-plan"
    sku WebApp.Sku.F1  // FREE tier
    operating_system OS.Linux
}

let devWebApp = webApp {
    name "dev-web-app"
    link_to_service_plan devAppPlan
    https_only
}

// Basic SQL Database
let devDatabase = sqlServer {
    name "dev-sql-server"
    admin_username "sqladmin"
    add_databases [
        sqlDb {
            name "devdb"
            sku Basic  // ~$5/month
        }
    ]
}

// Standard storage (LRS)
let devStorage = storageAccount {
    name "devstorage"
    sku Storage.Sku.Standard_LRS  // Cheapest replication
}

let devEnvironment = arm {
    location Location.EastUS
    add_resources [
        devAppPlan
        devWebApp
        devDatabase
        devStorage
    ]
}
```

**Development Cost**: $30-80/month

| Component | SKU | Monthly Cost |
|-----------|-----|--------------|
| App Service Plan | F1 (Free) | $0 |
| SQL Database | Basic | ~$5 |
| Storage Account | Standard LRS | ~$2 |
| Application Insights | 5GB free | $0-10 |

#### Staging Environment

Balance cost and production similarity for pre-production testing.

```fsharp
// Shared App Service Plan
let stagingPlan = servicePlan {
    name "staging-plan"
    sku WebApp.Sku.B1  // Basic tier ~$13/month
    operating_system OS.Linux
    number_of_workers 1
}

let stagingDatabase = sqlServer {
    name "staging-sql"
    admin_username "sqladmin"
    add_databases [
        sqlDb {
            name "stagingdb"
            sku DtuSku.S1  // Standard tier ~$30/month
        }
    ]
}

// Geo-redundant storage for testing failover
let stagingStorage = storageAccount {
    name "stagingstorage"
    sku Storage.Sku.Standard_GRS
}

let stagingEnvironment = arm {
    location Location.EastUS
    add_resources [
        stagingPlan
        stagingDatabase
        stagingStorage
    ]
}
```

**Staging Cost**: $100-200/month

#### Production Environment

Optimized for reliability and performance with managed costs.

```fsharp
// Production App Service with scale-out
let prodPlan = servicePlan {
    name "prod-plan"
    sku WebApp.Sku.P1V3  // Premium v3 ~$100/month
    number_of_workers 2
    operating_system OS.Linux
}

let prodInsights = appInsights {
    name "prod-insights"
}

let prodApp = webApp {
    name "prod-web-app"
    link_to_service_plan prodPlan
    always_on
    https_only
    link_to_app_insights prodInsights.Name
}

// Production database with DTU-based pricing
let prodDatabase = sqlServer {
    name "prod-sql"
    admin_username "sqladmin"
    add_databases [
        sqlDb {
            name "proddb"
            sku DtuSku.S3  // 100 DTUs ~$150/month
        }
    ]
    // Geo-replication to West US for disaster recovery
    geo_replicate ({
        NameSuffix = "-replica"
        Location = Location.WestUS
        DbSku = Some DtuSku.S3
    })
}

// Premium storage with geo-redundancy
let prodStorage = storageAccount {
    name "prodstorage"
    sku Storage.Sku.Premium_LRS
    enable_data_lake true
}

let productionEnvironment = arm {
    location Location.EastUS
    add_resources [
        prodPlan
        prodInsights
        prodApp
        prodDatabase
        prodStorage
    ]
}
```

**Production Cost**: $400-800/month

### App Service Cost Optimization

App Service Plans are often the largest compute cost. Strategic optimization can reduce costs by 40-60%.

#### Right-Sizing App Service Plans

According to Microsoft's App Service best practices, most applications are over-provisioned by 30-50%.

```fsharp
// Cost comparison for App Service Plans
(*
F1 (Free):        $0/month      - Dev/test only, 60 min/day limit
B1 (Basic):       ~$13/month    - Small apps, no auto-scale
B2 (Basic):       ~$25/month    - Medium apps
S1 (Standard):    ~$70/month    - Production, auto-scale, 5 slots
P1V3 (Premium):   ~$100/month   - High performance, better CPU
P2V3 (Premium):   ~$200/month   - Scale-intensive workloads
*)

// Use consumption-based Functions for sporadic workloads
let scheduledJob = functions {
    name "scheduled-processor"
    // Consumption plan is the default
    // Schedule configuration is done in the function app code, not infrastructure
}
```

#### App Service Reserved Instances

Azure Reserved Instances provide 30-65% savings for 1-3 year commitments. Purchase through Azure portal, not Farmer deployment.

**Savings Examples:**
- P1V3 (1 year): Save 30% (~$35/month)
- P1V3 (3 years): Save 65% (~$65/month)
- P2V3 (3 years): Save $130/month per instance

Reference: Azure Reserved Instances pricing (https://azure.microsoft.com/en-us/pricing/reserved-vm-instances/)

### Azure SQL Database Cost Optimization

SQL Database offers multiple pricing models. Choosing the right model can reduce costs by 50-70%.

#### DTU vs vCore Pricing Models

```fsharp
open Sql

// DTU Model: Simpler, bundled compute and storage
let dtuDatabase = sqlDb {
    name "dtu-database"
    sku DtuSku.S1  // 20 DTUs, ~$30/month
}

// vCore Model: More control, better for large databases
let vCoreDatabase = sqlDb {
    name "vcore-database"
    sku (GeneralPurpose Gen5_2)  // 2 vCores, ~$500/month
}

// Serverless: Best for intermittent workloads
let serverlessDb = sqlDb {
    name "serverless-db"
    sku (GeneralPurpose(S_Gen5(0.5, 2.0)))  // 0.5-2 vCores serverless
}
```

#### Cost Comparison by Workload

| Workload Type | Recommended SKU | Monthly Cost | Use Case |
|---------------|----------------|--------------|----------|
| Development | Basic (5 DTUs) | ~$5 | Dev/test databases |
| Small Production | S1 (20 DTUs) | ~$30 | <100 users, simple queries |
| Medium Production | S3 (100 DTUs) | ~$150 | 100-1000 users, complex queries |
| Large Production | GP Gen5 4 vCore | ~$700 | >1000 users, always-on |
| Sporadic Use | Serverless 0.5-4 vCore | $90-300 | Intermittent access patterns |

Reference: Azure SQL Database pricing documentation (https://azure.microsoft.com/en-us/pricing/details/azure-sql-database/)

#### Elastic Pools for Multiple Databases

Use elastic pools when running multiple databases with varying load patterns. This is recommended by Microsoft for SaaS applications.

```fsharp
open Sql

let elasticPoolServer = sqlServer {
    name "multi-tenant-server"
    admin_username "pooladmin"
    elastic_pool_name "shared-pool"
    elastic_pool_sku PoolSku.Basic200
    add_databases [
        sqlDb { name "tenant-db-1" }  // No SKU needed, uses pool
        sqlDb { name "tenant-db-2" }
        sqlDb { name "tenant-db-3" }
    ]
}
```

**Cost Savings Example:**
- 10 databases × S1 DTU: $300/month
- Elastic pool (200 eDTUs): $180/month
- **Savings: 40%**

### Storage Account Cost Optimization

Storage costs accumulate over time. Lifecycle management and tier optimization are critical.

#### Storage Tiers and Lifecycle Policies

```fsharp
let optimizedStorage = storageAccount {
    name "optimizedstorage"
    sku Storage.Sku.Standard_LRS
    add_lifecycle_rule "archive-old-data" [
        Storage.DeleteAfter 365<Days>
        Storage.CoolAfter 30<Days>
        Storage.ArchiveAfter 90<Days>
    ] Storage.NoRuleFilters
}
```

**Storage Tier Costs (per GB/month):**
- Hot: $0.0184 - Frequent access
- Cool: $0.01 - Infrequent access (>30 days)
- Archive: $0.002 - Rare access (>180 days)

**Cost Optimization Strategy:**
1. Start all data in Hot tier
2. Move to Cool after 30 days (save 45%)
3. Move to Archive after 90 days (save 89%)
4. Delete after 365 days (save 100%)

#### Replication Strategy by Environment

```fsharp
// Development: Locally Redundant (LRS) - Cheapest
let devStorage = storageAccount {
    name "devstorage"
    sku Storage.Sku.Standard_LRS  // 1x cost
}

// Production: Zone-Redundant (ZRS) - Balanced
let prodStorage = storageAccount {
    name "prodstorage"
    sku Storage.Sku.Standard_ZRS  // 1.25x cost
}

// Critical: Geo-Redundant (GRS) - Most reliable
let criticalStorage = storageAccount {
    name "criticalstorage"
    sku Storage.Sku.Standard_GRS  // 2x cost
}
```

### Virtual Network and Gateway Costs

Networking costs are often overlooked but can reach $500-2000/month in enterprise deployments.

#### VPN Gateway vs ExpressRoute

```fsharp
open Farmer.VirtualNetworkGateway

// VPN Gateway: $27-650/month depending on SKU
let vpnGateway = gateway {
    name "site-to-site-vpn"
    vnet "my-vnet"
    vpn_gateway_sku VpnGatewaySku.VpnGw1  // ~$140/month + data transfer
}

// ExpressRoute: $55-5,200/month for dedicated connection
let expressRouteGateway = gateway {
    name "express-route-gw"
    vnet "my-vnet"
    er_gateway_sku ErGatewaySku.Standard  // Configure capacity via portal
}
```

**Cost Comparison:**
| Connection Type | Monthly Cost | Data Transfer | Use Case |
|----------------|--------------|---------------|----------|
| VPN Gateway Basic | ~$27 | $0.087/GB | Dev/test |
| VPN Gateway VpnGw1 | ~$140 | $0.087/GB | Production |
| ExpressRoute 50 Mbps | ~$55 | First 5TB free | Small enterprise |
| ExpressRoute 1 Gbps | ~$700 | First 5TB free | Large enterprise |

#### NAT Gateway vs Public IPs

For outbound connectivity from private subnets, compare NAT Gateway costs with individual public IPs.

**NAT Gateway**: $32.50/month + $0.045/GB processed
**Public IP**: $3.50/month per IP (no data charges)

Use NAT Gateway when:
- Need >10 outbound IPs
- Require consistent source IP for whitelisting
- High outbound traffic volume (>700 GB/month)

### Application Insights Cost Management

Application Insights charges per GB ingested. Uncontrolled logging can cost thousands per month.

```fsharp
let costOptimizedInsights = appInsights {
    name "optimized-insights"
    retention_days 30  // Default is 90 days
    
    // Note: Sampling is configured in application code via ApplicationInsights SDK
    // Not available as an infrastructure setting in Farmer
}
```

**Typical Costs:**
- First 5 GB/month: FREE
- Beyond 5 GB: $2.30/GB

**Cost Reduction Strategies:**
1. Adjust sampling (50% = 50% cost reduction)
2. Reduce retention (90 to 30 days)
3. Filter verbose telemetry at source
4. Use separate App Insights per environment

Reference: Application Insights pricing (https://azure.microsoft.com/en-us/pricing/details/monitor/)

### Traffic Manager vs Azure Front Door

For global load balancing and routing, choose based on features needed.

```fsharp
open Farmer.TrafficManager

// Traffic Manager: DNS-based, $0.54/million queries
let trafficMgr = trafficManager {
    name "global-traffic"
    routing_method RoutingMethod.Performance
    add_endpoints [
        endpoint {
            name "us-endpoint"
            target_external "app-us.example.com" Location.EastUS
        }
        endpoint {
            name "eu-endpoint"
            target_external "app-eu.example.com" Location.NorthEurope
        }
    ]
}

// Azure Front Door: Application layer, starts at ~$35/month
// Configure via Azure Portal - advanced CDN/WAF features
```

**Cost Comparison:**
| Service | Base Cost | Data Transfer | Use Case |
|---------|-----------|---------------|----------|
| Traffic Manager | $0.54/M queries | Standard egress | DNS routing only |
| Azure Front Door | ~$35/month | $0.11/GB | WAF, caching, SSL offload |

### Cost Monitoring and Alerting

Implement budget alerts to prevent cost overruns. This is critical for development accounts.

```fsharp
// Note: Azure Budgets created via portal or Azure CLI
// Example using Azure CLI:
(*
az consumption budget create \
  --budget-name "dev-environment-budget" \
  --amount 100 \
  --resource-group dev-rg \
  --time-grain Monthly \
  --start-date 2025-01-01T00:00:00Z \
  --end-date 2026-01-01T00:00:00Z
*)
```

### Reserved Instances and Savings Plans

Azure offers significant discounts for long-term commitments. According to Microsoft, customers save an average of 40-65%.

**Reserved Instance Savings:**
- 1 year commitment: 30-40% savings
- 3 year commitment: 50-65% savings

**Purchase Strategy:**
1. Analyze 6-12 months of usage
2. Reserve 60-80% of stable workloads
3. Keep 20-40% on-demand for flexibility
4. Review quarterly and adjust

Reference: Azure Reservations documentation (https://learn.microsoft.com/en-us/azure/cost-management-billing/reservations/)

### Quick Wins Checklist

Implement these changes for immediate cost reduction:

1. **Right-size App Service Plans**: Review CPU and memory metrics, downgrade over-provisioned plans (20-40% savings)
2. **Enable storage lifecycle policies**: Move old blobs to Cool/Archive tiers (40-90% storage savings)
3. **Use Basic tier for dev/test**: Switch non-production to B1/B2 plans instead of S1 (40-80% savings)
4. **Delete unused resources**: Remove orphaned disks, NICs, public IPs (5-15% overall savings)
5. **Enable auto-shutdown for VMs**: Stop dev/test VMs outside business hours (60-75% VM savings)
6. **Review Application Insights**: Adjust sampling and retention (30-50% monitoring savings)
7. **Consolidate databases**: Use elastic pools for multiple databases (30-50% database savings)

### Cost Anti-Patterns to Avoid

Common mistakes that increase Azure costs unnecessarily:

1. **Over-provisioning App Service Plans**: Running S1 when B1 would suffice (400% cost increase)
2. **Premium storage for everything**: Use Standard LRS for non-critical data (75% more expensive)
3. **Always-on VMs**: Running 24/7 when only needed 8-10 hours/day (200% waste)
4. **No lifecycle policies**: Keeping all storage in Hot tier forever (400-900% storage waste)
5. **Separate App Service Plans**: One plan per app instead of consolidating (200-400% more cost)
6. **DTU databases for large workloads**: Not switching to vCore (30-50% overpayment)
7. **Ignoring reserved instances**: Missing 40-65% savings on stable workloads

### Monthly Cost Targets by Environment

Set realistic cost targets based on application complexity:

| Environment | Small App | Medium App | Large App |
|-------------|-----------|------------|-----------|
| Development | $30-80 | $80-200 | $200-500 |
| Staging | $100-200 | $200-500 | $500-1000 |
| Production | $400-1000 | $1000-3000 | $3000-10000+ |

### Additional Resources

**Microsoft Official Documentation:**
- Azure Cost Management and Billing: https://learn.microsoft.com/en-us/azure/cost-management-billing/
- Azure Well-Architected Framework - Cost Optimization: https://learn.microsoft.com/en-us/azure/well-architected/cost-optimization/
- Azure Pricing Calculator: https://azure.microsoft.com/en-us/pricing/calculator/

**Microsoft Learn Paths:**
- Control Azure spending and manage bills: https://learn.microsoft.com/en-us/training/paths/control-spending-manage-bills/
- Design for cost optimization: https://learn.microsoft.com/en-us/training/modules/design-for-cost-optimization/

**Community Resources:**
- Azure Cost Optimization Best Practices (Microsoft Tech Community)
- Azure Friday - Cost Management episodes
- Azure MVPs' cost optimization blog posts

**Books:**
- "Microsoft Azure Architect Technologies Study Guide" by Ben Lee (Sybex)
- "Cloud FinOps" by J.R. Storment and Mike Fuller (O'Reilly) - Cloud-agnostic FinOps principles

This guide provides practical cost optimization strategies based on Microsoft's best practices and real-world implementations. Always validate costs using the Azure Pricing Calculator before deployment.
