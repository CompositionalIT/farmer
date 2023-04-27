#r "../../src/Farmer/bin/Debug/netstandard2.0/Farmer.dll"

open Farmer
open Farmer.Builders

/// Send events to this function that was deployed separately
let fnRef = 
    { Arm.Web.siteFunctions.resourceId(ResourceName "gridFnApp", ResourceName "eventHandler") with 
        ResourceGroup = Some "fn-rg" }
    |> Unmanaged

/// The source will default to the resourceGroup() and event grid target will be the function handler.
let grid = eventGrid {
    topic_name "src-rg-events"
    add_function_subscriber fnRef 
        { MaxEventsPerBatch = 1u; PreferredBatchSizeInKilobytes = 64u }
        [ SystemEvents.Resources.ResourceWriteSuccess; SystemEvents.Resources.ResourceActionSuccess ]
}

// deploy into the resource group that we want to be the source of events
let deployment = arm {
    add_resources [
        grid
    ]
}

// Generate the ARM template here...
deployment
|> Writer.quickWrite "farmer-deploy"