[<AutoOpen>]
module Farmer.Builders.Soltuion

open Farmer
open Farmer.Arm.Solution

type SolutionConfig = 
    { Name : ResourceName
      WorkspaceResourceId : ResourceId Set
      ContainedResources : ResourceId Set
      ReferencedResources :ResourceId Set
      Tags: Map<string,string> }
    interface IBuilder with 
        member this.ResourceId = solution.resourceId this.Name
        member this.BuildResources location = [
            { Name = this.Name
              Location = location
              WorkspaceResourceId = this.WorkspaceResourceId
              ContainedResources = this.ContainedResources
              ReferencedResources = this.ReferencedResources
              Tags = this.Tags }
        ]

type SolutionBuilder() =
    member _.Yield _ =
        { Name = ResourceName.Empty
          WorkspaceResourceId = Set.empty
          ContainedResources =  Set.empty
          ReferencedResources = Set.empty
          Tags = Map.empty
        }

    member _.Run (state:SolutionConfig) = 
        state

    [<CustomOperation "name">]
       member _.Name(state: SolutionConfig, name) = { state with Name = ResourceName name }

    [<CustomOperation "workspaceResourceId">]
       member _.WorkspaceResourceId(state: SolutionConfig, name) = { state with Name = ResourceName name }

    [<CustomOperation "containedResources">]
       member _.ContainedResources(state: SolutionConfig, containedResources ) = { state with ContainedResources = containedResources}

    [<CustomOperation "referencedResources">]
       member _.ReferencedResources(state: SolutionConfig, referencedResources) = { state with ReferencedResources = referencedResources}

    interface ITaggable<SolutionConfig> with member _.Add state tags = { state with Tags = state.Tags |> Map.merge tags }

let solution = SolutionBuilder()

