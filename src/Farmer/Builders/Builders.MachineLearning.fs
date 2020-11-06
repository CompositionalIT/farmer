[<AutoOpen>]
module Farmer.Builders.MachineLearning

open Farmer
open Farmer.Arm
open Farmer.CoreTypes

// TODO: Implement AML workspace config
type WorkspaceConfig = 
    { Name: ResourceName }
    interface IBuilder with
        member this.DependencyName = this.Name
        member this.BuildResources location = []

// TODO: Implement AML workspace builder
type WorkspaceBuilder() = 
    member _.Yield _ = { Name = ResourceName.Empty }
    member _.Run (state:WorkspaceConfig) = 
        state
    
    [<CustomOperation "name">]
    member _.Name(state:WorkspaceConfig) = 
        state

let machineLearning = WorkspaceBuilder()