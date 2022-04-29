#r "../../src/Farmer/bin/Debug/netstandard2.0/Farmer.dll"

open Farmer
open Farmer.Builders

let sentinelWorkspace = logAnalytics {
    name "my-sentinel-workspace"
    retention_period 30<Days>
    enable_query
    daily_cap 5<Gb>
}

let solutionName = $"SecurityInsights({sentinelWorkspace.Name.Value})"

let sentinelSolution = solution {
    name solutionName
    plan (solutionPlan {
        name solutionName
        publisher "Microsoft"
        product "OMSGallery/SecurityInsights"
    })
    properties(solutionProperties {
        workspace sentinelWorkspace
    })
}

let deployment = arm {
  location Location.NorthCentralUS
  add_resource sentinelWorkspace
  add_resource sentinelSolution
}

deployment
|> Writer.quickWrite "operationsManagement"
