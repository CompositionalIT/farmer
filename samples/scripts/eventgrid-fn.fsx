#r "../../src/Tests/bin/Debug/net6.0/Farmer.dll"

open Farmer
open Farmer.Builders

/// Send events to this function app
let fnApp = functions { name "gridFnApp" }
/// And this specific function
let fnName = ResourceName "eventHandler"

/// The source will default to the resourceGroup() and event grid target will be the function handler.
let grid = eventGrid {
    topic_name "src-rg-events"
    add_function_subscriber fnApp fnName 
        { MaxEventsPerBatch = 1u; PreferredBatchSizeInKilobytes = 64u }
        [ SystemEvents.Resources.ResourceWriteSuccess; SystemEvents.Resources.ResourceActionSuccess ]
}

let deployment = arm {
    add_resources [
        grid
        fnApp
    ]
}

// Generate the ARM template here...
deployment
|> Writer.quickWrite "farmer-deploy"