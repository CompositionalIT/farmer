#r "../../src/Farmer/bin/Debug/netstandard2.0/Farmer.dll"

open Farmer
open Farmer.Builders

let sentinelWorkspace = logAnalytics {
    name "my-sentinel-workspace"
    retention_period 30<Days>
    enable_query
    daily_cap 5<Gb>
}

let omsName = $"SecurityInsights({sentinelWorkspace.Name.Value})"

let sentinelSolution = oms {
    name omsName

    plan (
        omsPlan {
            name omsName
            publisher "Microsoft"
            product "OMSGallery/SecurityInsights"
        }
    )

    properties (omsProperties { workspace sentinelWorkspace })
}

let deployment = arm {
    location Location.NorthCentralUS
    add_resource sentinelWorkspace
    add_resource sentinelSolution
}

deployment |> Writer.quickWrite "operationsManagement"
