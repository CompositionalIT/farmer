// Example: Production-Ready Azure Functions with Farmer
// This example demonstrates the new production features added to Farmer

#r "nuget: Farmer"

open Farmer
open Farmer.Builders
open Farmer.ProductionValidation
open Farmer.ProductionDefaults

// ============================================================================
// Example 1: Simple Production-Ready Function
// ============================================================================

printfn ""
printfn "=== Example 1: Simple Production-Ready Function ==="
printfn ""

let simpleProductionFunction = functions {
    name "simple-production-api"
    use_runtime FunctionsRuntime.DotNet80
    service_plan_sku Sku.EP1
    
    production_defaults  // One keyword applies all production defaults
    // AlwaysOn enabled (Premium plan)
    // HTTPS enforced
    // Scale limit set to 100
    // App Insights auto-created
    // Storage auto-created
}
|> Functions.validateAndWarn  // Show validation results

// ============================================================================
// Example 2: Consumption Plan with Cost Controls
// ============================================================================

printfn "\n=== Example 2: Consumption Plan with Cost Controls ===\n"

let consumptionFunction = functions {
    name "cost-optimized-api"
    use_runtime FunctionsRuntime.DotNet80
    service_plan_sku Sku.Y1
    
    max_scale_out_limit 50  // Limit scaling to control costs
    https_only
    enable_managed_identity
}
|> Functions.validateAndWarn

// ============================================================================
// Example 3: High-Traffic API with Optimized App Insights
// ============================================================================

printfn "\n=== Example 3: High-Traffic API with Optimized App Insights ===\n"

let highTrafficInsights = appInsights {
    name "high-traffic-api-ai"
    production_sampling  // 20% sampling - reduces costs
}
|> AppInsights.validateAndWarn

let highTrafficFunction = functions {
    name "high-traffic-api"
    use_runtime FunctionsRuntime.Python38
    service_plan_sku Sku.EP2
    
    link_to_app_insights highTrafficInsights
    max_scale_out_limit 200
    always_on
    https_only
    enable_managed_identity
}
|> Functions.validateAndWarn

// ============================================================================
// Example 4: Development Environment with Lower Limits
// ============================================================================

printfn "\n=== Example 4: Development Environment ===\n"

let devFunction = functions {
    name "dev-api"
    use_runtime FunctionsRuntime.DotNet80
    service_plan_sku Sku.Y1
    
    max_scale_out_limit 10  // Lower limit for dev
    https_only
}
|> Functions.validateAndWarn

let devInsights = appInsights {
    name "dev-api-ai"
    development_sampling  // 100% sampling for dev
}

// ============================================================================
// Example 5: Using Production Defaults Module
// ============================================================================

printfn "\n=== Example 5: Using Production Defaults Module ===\n"

// Standard production preset
let standardProd = Presets.standardProduction {
    name "standard-api"
    use_runtime FunctionsRuntime.DotNet80
}
|> Functions.validateAndWarn

// High-traffic preset
let highTrafficProd = Presets.highTrafficApi {
    name "high-traffic-preset-api"
    use_runtime FunctionsRuntime.Node14
}
|> Functions.validateAndWarn

// Development preset
let devPreset = Presets.development {
    name "dev-preset-api"
    use_runtime FunctionsRuntime.Python38
}
|> Functions.validateAndWarn

// ============================================================================
// Example 6: Multi-Environment Deployment
// ============================================================================

printfn "\n=== Example 6: Multi-Environment Deployment ===\n"

let createEnvironment envName =
    let (sku, scaleLimit) =
        match envName with
        | "prod" -> Sku.EP1, 200
        | "staging" -> Sku.Y1, 50
        | "dev" -> Sku.Y1, 10
        | _ -> Sku.Y1, 10
    
    functions {
        name $"payment-api-{envName}"
        use_runtime FunctionsRuntime.DotNet80
        service_plan_sku sku
        max_scale_out_limit scaleLimit
        https_only
        enable_managed_identity
        
        setting "ENVIRONMENT" envName
    }
    |> Functions.validateAndWarn

let prodDeployment = createEnvironment "prod"
let stagingDeployment = createEnvironment "staging"
let devDeployment = createEnvironment "dev"

// ============================================================================
// Example 7: Intentionally Problematic Function (to show warnings)
// ============================================================================

printfn "\n=== Example 7: Function with Production Issues (demonstrates warnings) ===\n"

let problematicFunction = functions {
    name "problematic-api"
    use_runtime FunctionsRuntime.DotNet80
    service_plan_sku Sku.Y1
    // Issue: No scale limit
    // Issue: No HTTPS enforcement
    // Issue: AlwaysOn not set
}
|> Functions.validateAndWarn  // Will show warnings

// ============================================================================
// Example 8: Full Production ARM Template
// ============================================================================

printfn "\n=== Example 8: Complete ARM Template ===\n"

let productionDeployment = arm {
    location Location.EastUS
    
    add_resources [
        // Premium Functions with production defaults
        functions {
            name "payment-processor"
            use_runtime FunctionsRuntime.DotNet80
            service_plan_sku Sku.EP1
            production_defaults
            
            setting "DATABASE_CONNECTION" "connection-string-here"
            setting "API_KEY" "api-key-here"
        }
        
        // Optimized App Insights
        appInsights {
            name "payment-processor-ai"
            production_sampling
        }
    ]
}

printfn "\nSuccess: All examples completed!"
printfn "\nKey Takeaways:"
printfn "  1. Use 'production_defaults' for one-line production setup"
printfn "  2. Set 'max_scale_out_limit' to control costs"
printfn "  3. Use 'production_sampling' for App Insights in high-traffic scenarios"
printfn "  4. Call 'validateAndWarn' to catch issues before deployment"
printfn "  5. Farmer auto-creates App Insights and Storage - you're already production-ready!\n"
