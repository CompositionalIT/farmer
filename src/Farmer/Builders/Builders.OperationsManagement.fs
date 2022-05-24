[<AutoOpen>]
module Farmer.Builders.OperationsManagement

open Farmer
open Farmer.Arm.OperationsManagement

/// OMS = Operations Management Solution

type OMSProperties =
    { Workspace: IBuilder option
      ContainedResources: IBuilder list
      ReferencedResources: IBuilder list }
    static member Empty =
        { Workspace = None
          ContainedResources = []
          ReferencedResources = [] }

type OMSPlan =
    { Name: string
      Publisher: string
      Product: string }
    static member Empty =
        { Name = ""
          Publisher = ""
          Product = "" }

type OMSConfig =
    { Name: ResourceName
      Properties: OMSProperties
      Plan: OMSPlan
      Tags: Map<string, string> }
    interface IBuilder with
        member this.ResourceId = oms.resourceId this.Name

        member this.BuildResources location =
            [ match this.Properties.Workspace with
              | Some workspace ->
                  { Name = this.Name
                    Location = location
                    Plan =
                      {| Name = this.Plan.Name
                         Product = this.Plan.Product
                         Publisher = this.Plan.Publisher |}
                    Properties =
                      {| WorkspaceResourceId = workspace.ResourceId
                         ContainedResources =
                          this.Properties.ContainedResources
                          |> List.map (fun cr -> cr.ResourceId)
                         ReferencedResources =
                          this.Properties.ReferencedResources
                          |> List.map (fun rr -> rr.ResourceId) |}
                    Tags = this.Tags }
              | None -> () ]

type OMSPropertiesBuilder() =
    member _.Yield _ =
        { Workspace = None
          ContainedResources = []
          ReferencedResources = [] }

    /// Sets the workspace resource id of the OMS Properties
    [<CustomOperation "workspace">]
    member _.WorkspaceResourceId(state: OMSProperties, workspace) =
        { state with Workspace = Some workspace }

    /// Adds a contained resource.
    [<CustomOperation "add_contained_resource">]
    member _.AddContainedResource(state: OMSProperties, contained) =
        { state with ContainedResources = contained :: state.ContainedResources }

    /// Adds a collection of contained resources.
    [<CustomOperation "add_contained_resources">]
    member _.AddContainedResources(state: OMSProperties, contained) =
        { state with ContainedResources = contained @ state.ContainedResources }

    /// Adds a referenced resource.
    [<CustomOperation "add_referenced_resource">]
    member _.AddReferencedResource(state: OMSProperties, referenced) =
        { state with ReferencedResources = referenced :: state.ReferencedResources }

    /// Adds a collection of referenced resources.
    [<CustomOperation "add_referenced_resources">]
    member _.AddReferencedResources(state: OMSProperties, referenced) =
        { state with ReferencedResources = referenced @ state.ReferencedResources }

let omsProperties = OMSPropertiesBuilder()

type OMSPlanBuilder() =
    member _.Yield _ =
        { Name = ""
          Publisher = "Microsoft"
          Product = "" }

    /// Sets the name of the OMS Plan
    [<CustomOperation "name">]
    member _.Name(state: OMSPlan, name) = { state with Name = name }

    /// Sets the publisher of the OMS Plan
    [<CustomOperation "publisher">]
    member _.Publisher(state: OMSPlan, publisher) = { state with Publisher = publisher }

    /// Sets the product of the OMS Plan
    [<CustomOperation "product">]
    member _.Product(state: OMSPlan, product) = { state with Product = product }

let omsPlan = OMSPlanBuilder()

type OMSBuilder() =
    member _.Yield _ =
        { Name = ResourceName.Empty
          Plan = OMSPlan.Empty
          Properties = OMSProperties.Empty
          Tags = Map.empty }

    /// Sets the name of the OMS
    [<CustomOperation "name">]
    member _.Name(state: OMSConfig, name) = { state with Name = ResourceName name }

    /// Sets the OMS Plan
    [<CustomOperation "plan">]
    member _.Plan(state: OMSConfig, plan: OMSPlan) = { state with Plan = plan }

    /// Sets the OMS Properties
    [<CustomOperation "properties">]
    member _.Properties(state: OMSConfig, properties: OMSProperties) = { state with Properties = properties }

    interface ITaggable<WorkspaceConfig> with
        member _.Add state tags =
            { state with Tags = state.Tags |> Map.merge tags }

let oms = OMSBuilder()
