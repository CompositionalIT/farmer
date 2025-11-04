---
title: "Production Best Practices"
date: 2025-11-04T00:00:00+00:00
weight: 2
---

This guide covers production-ready defaults and best practices when deploying Azure infrastructure with Farmer.

## What Farmer Provides Automatically

Farmer includes several production-safe defaults out of the box:

### Azure Functions
- **Application Insights** - Auto-created and linked (named `{function-name}-ai`)
- **Storage Account** - Auto-created with sanitized names
- **Managed Identity** - Easy to enable with `enable_managed_identity`
- **Key Vault Integration** - Built-in secret store support

### Application Insights
- **Instrumentation Key** - Automatically linked to Functions
- **Connection String** - Modern connection string support

## Quick Start: Production-Ready Functions

### One-Line Production Setup

Use the `production_defaults` keyword to apply all production defaults:

```fsharp
open Farmer
open Farmer.Builders

let productionFunction = functions {
    name "payment-api"
    use_runtime FunctionsRuntime.DotNet80
    service_plan_sku WebApp.Sku.EP1  // Premium plan
    production_defaults  // ← Applies all production defaults
}
```

**What `production_defaults` does:**
- Enables `always_on` (for Premium/Dedicated plans only) - reduces cold starts
- Enforces `https_only` - security best practice
- Sets `max_scale_out_limit` to 100 (for Consumption plan) - prevents unexpected costs during development/testing

> **Note**: For production workloads with high traffic on Consumption plans, you may want to increase or remove the scale limit. Azure recommends removing scale limits in production for critical workloads, but setting a limit helps prevent unexpected costs during testing.

### Customize Production Settings

```fsharp
let customFunction = functions {
    name "payment-api"
    use_runtime FunctionsRuntime.DotNet80
    service_plan_sku WebApp.Sku.Y1  // Consumption plan
    
    max_scale_out_limit 50      // Prevent runaway costs
    https_only                  // Enforce HTTPS
    enable_managed_identity     // Secure resource access
}
```

## Production Checklist

### Azure Functions

| Setting | Recommended | How to Set | Why |
|---------|-------------|------------|-----|
| **App Insights** | Always | Auto-created by default | Observability is critical |
| **HTTPS Only** | Always | `https_only` or `production_defaults` | Security best practice |
| **Scale Limit** | Consider carefully | `max_scale_out_limit 100` | Prevents unexpected costs in dev/test. For production, consider higher limits or no limit for critical workloads. |
| **Managed Identity** | Recommended | `enable_managed_identity` | No connection strings in config |
| **AlwaysOn** | Premium/Dedicated | `always_on` or `production_defaults` | Prevent cold starts |
| **Storage** | Always | Auto-created by default | Required for Functions |

### Application Insights

| Setting | Recommended | How to Set | Why |
|---------|-------------|------------|-----|
| **Sampling** | Adjust for traffic | `production_sampling` (20%) or custom | Reduce costs while maintaining visibility. Azure samples intelligently - all errors are always captured. |
| **Log Analytics** | Recommended | `log_analytics_workspace` | Better retention and queries |

## Cost Optimization

### Consumption Plan (Y1)
Best for: Variable workloads, cost-sensitive scenarios

```fsharp
// Development/Testing - with cost controls
let devFunction = functions {
    name "dev-processor"
    service_plan_sku WebApp.Sku.Y1
    use_runtime FunctionsRuntime.Python38
    max_scale_out_limit 20        // Lower limit for dev/test cost control
    https_only
    enable_managed_identity
}

// Production - carefully consider scale limits
let prodFunction = functions {
    name "prod-processor"
    service_plan_sku WebApp.Sku.Y1
    use_runtime FunctionsRuntime.Python38
    max_scale_out_limit 200       // Higher limit for production, or consider removing
    https_only
    enable_managed_identity
}

let insights = appInsights {
    name "processor-ai"
    production_sampling  // 20% sampling reduces costs, errors always captured
}
```

> **Azure Recommendation**: For critical production workloads, consider removing scale limits entirely to ensure availability. Scale limits are most useful during development and testing to prevent unexpected costs.

### Premium Plan (EP1)
Best for: Production workloads requiring consistent performance

```fsharp
let production = functions {
    name "payment-processor"
    service_plan_sku WebApp.Sku.EP1
    use_runtime FunctionsRuntime.DotNet80
    production_defaults  // Applies all production defaults
}
```

## Application Insights Best Practices

### High-Traffic Production
```fsharp
let prodInsights = appInsights {
    name "high-traffic-api-ai"
    production_sampling  // 20% - Azure intelligently samples (all errors always captured)
    log_analytics_workspace myWorkspace
}
```

> **Note**: Application Insights uses adaptive sampling that always captures errors and exceptions, regardless of the sampling percentage. The sampling applies primarily to successful requests. For more control, see [Configure sampling](https://learn.microsoft.com/en-us/azure/azure-monitor/app/sampling).

### Development
```fsharp
let devInsights = appInsights {
    name "dev-api-ai"
    development_sampling  // 100% - see everything
}
```

## Helpful Warnings

Farmer provides proactive warnings to catch production issues:

### Missing Scale Limit Warning
```fsharp
let noLimit = functions {
    name "my-api"
    service_plan_sku WebApp.Sku.Y1  // Consumption
    use_runtime FunctionsRuntime.DotNet80
}
// Warning: [my-api] Consider setting 'max_scale_out_limit 100' to prevent runaway costs
```

### High Sampling Warning
```fsharp
let fullSampling = appInsights {
    name "my-ai"
    sampling_percentage 100
}
// Warning: [my-ai] App Insights sampling at 100%. For high-traffic production apps, consider 'production_sampling'
```

## Multi-Environment Setup

```fsharp
let createEnvironment envName =
    let (sku, scaleLimit) =
        match envName with
        | "prod" -> WebApp.Sku.EP1, 200
        | "staging" -> WebApp.Sku.Y1, 50
        | "dev" -> WebApp.Sku.Y1, 10
        | _ -> WebApp.Sku.Y1, 10
    
    functions {
        name $"payment-api-{envName}"
        use_runtime FunctionsRuntime.DotNet80
        service_plan_sku sku
        max_scale_out_limit scaleLimit
        https_only
        enable_managed_identity
        setting "ENVIRONMENT" envName
    }

let deployments = [
    createEnvironment "prod"
    createEnvironment "staging"
    createEnvironment "dev"
]
```

## Important Considerations

### Scale Limits and Production Workloads

The `production_defaults` keyword sets `max_scale_out_limit` to 100 for Consumption plans. This is a **sensible default for development and testing** to prevent unexpected costs.

However, **for production workloads**, consider these Azure recommendations:

- **Critical workloads**: Consider increasing the limit (e.g., 200) or removing it entirely
- **Non-critical workloads**: The default of 100 is often sufficient
- **Development/Testing**: Lower limits (10-50) help control costs

Azure's guidance: *"Check for a Daily Usage Quota (GB-Sec) limit set during development and testing. Consider removing this limit in production environments."*

### Sampling and Observability

The `production_sampling` helper sets sampling to 20%, which is appropriate for many high-traffic scenarios. Remember:

- Application Insights **always captures all errors** regardless of sampling
- Sampling applies primarily to successful requests
- For critical production systems, you may want higher sampling (50-100%)
- You can always adjust: `sampling_percentage 50` for custom values

## Summary

Farmer makes production deployments easy while following Azure best practices:

1. **Sensible defaults** - App Insights and Storage auto-created
2. **Production helpers** - `production_defaults` keyword applies best practices as a starting point
3. **Flexibility** - Easy to adjust scale limits and sampling for your specific needs
4. **Security** - HTTPS enforcement and managed identity support
5. **Proactive warnings** - Catch issues before deployment

Start with `production_defaults` and customize for your production requirements!

## Further Reading

- [Functions Resource Documentation](../api-overview/resources/functions/)
- [App Insights Resource Documentation](../api-overview/resources/app-insights/)
- [Azure Functions Best Practices](https://docs.microsoft.com/en-us/azure/azure-functions/functions-best-practices)
