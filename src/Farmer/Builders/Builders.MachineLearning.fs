[<AutoOpen>]
module Farmer.Builders.MachineLearning

open Farmer
open Farmer.Arm

// TODO: Implement AML workspace config
type WorkspaceConfig = 
    { ResourceName: ResourceName}

// TODO: Implement AML workspace builder
type WorkspaceBuilder() = 
    member _.Yield _ = {||}
    member _.Run (state:WorkspaceConfig) = 
        state
    
    [<CustomOperation "workspace_name">]
    member _.WorkspaceName(state:WorkspaceConfig) = 
        state

let machineLearning = WorkspaceBuilder()