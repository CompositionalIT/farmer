[<AutoOpen>]
module Farmer.Builders.OperationsManagement

open Farmer
open Farmer.Arm.OperationsManagement

type SolutionProperties =
    { Workspace: IBuilder option
      // todo - should these be IBuilders to access ResourceIds?
      ContainedResources: string list
      ReferencedResources : string list
    } with
        static member Empty = {
          Workspace = None
          ContainedResources = []
          ReferencedResources = []
        }

type SolutionPlan =
    { Name: string
      Publisher: string
      Product: string
    } with
        static member Empty = {
            Name = ""
            Publisher = ""
            Product = ""
        }

type SolutionConfig =
    { Name: ResourceName
      Properties: SolutionProperties
      Plan: SolutionPlan
      Tags: Map<string, string> }
    interface IBuilder with
        member this.ResourceId = solutions.resourceId this.Name
        member this.BuildResources location = [
          match this.Properties.Workspace with
          | Some workspace ->
              { Name = this.Name
                Location = location
                Plan = 
                    {|  Name = this.Plan.Name
                        Product = this.Plan.Product
                        Publisher = this.Plan.Publisher
                    |}
                Properties =
                    {|  WorkspaceResourceId = workspace.ResourceId
                        ContainedResources = this.Properties.ContainedResources
                        ReferencedResources = this.Properties.ReferencedResources
                    |}
                Tags = this.Tags
              }
          | None ->
              ()
        ]

type SolutionPropertiesBuilder() =
    member _.Yield _ =
        { Workspace = None
          ContainedResources = []
          ReferencedResources = [] }

    /// Sets the workspace resource id of the Solution Properties
    [<CustomOperation "workspace">]
    member _.WorkspaceResourceId(state: SolutionProperties, workspace) = { state with Workspace = Some workspace }

    /// Sets the contained resources of the Solution Properties
    [<CustomOperation "contains">]
    member _.ContainedResources(state: SolutionProperties, contained) = { state with ContainedResources = contained }

    /// Sets the referenced resources of the Solution Properties
    [<CustomOperation "references">]
    member _.ReferencedResources(state: SolutionProperties, referenced) = { state with ReferencedResources = referenced }

let solutionProperties = SolutionPropertiesBuilder()

type SolutionPlanBuilder() =
    member _.Yield _ =
        { Name = ""
          Publisher = "Microsoft"
          Product = "" }

    /// Sets the name of the Solution Plan
    [<CustomOperation "name">]
    member _.Name(state: SolutionPlan, name) = { state with Name = name }

    /// Sets the publisher of the Solution Plan
    [<CustomOperation "publisher">]
    member _.Publisher(state: SolutionPlan, publisher) = { state with Publisher = publisher }

    /// Sets the product of the Solution Plan
    [<CustomOperation "product">]
    member _.Product(state: SolutionPlan, product) = { state with Product = product }

let solutionPlan = SolutionPlanBuilder()

type SolutionBuilder() =
    member _.Yield _ =
        { Name = ResourceName.Empty
          Plan = SolutionPlan.Empty
          Properties = SolutionProperties.Empty
          Tags = Map.empty }

    /// Sets the name of the Solution
    [<CustomOperation "name">]
    member _.Name(state: SolutionConfig, name) = { state with Name = ResourceName name }

    /// Sets the Solution Plan
    [<CustomOperation "plan">]
    member _.Plan(state: SolutionConfig, plan : SolutionPlan) = { state with Plan = plan}

    /// Sets the Solution Plan
    [<CustomOperation "properties">]
    member _.Properties(state: SolutionConfig, properties : SolutionProperties) = { state with Properties = properties}

    interface ITaggable<WorkspaceConfig> with member _.Add state tags = { state with Tags = state.Tags |> Map.merge tags }

let solution = SolutionBuilder()

