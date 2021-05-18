[<AutoOpen>]
module Farmer.Builders.Soltuion

open Farmer
open Farmer.Arm.Solution
type SolutionConfig = 
    { Name : ResourceName
      WorkspaceResourceId : ResourceId 
      ContainedResources : ResourceId Set
      ReferencedResources :ResourceId Set
      Publisher : string
      PromotionCode : string option 
      Product : string 
      Dependencies : ResourceId Set
      Tags: Map<string,string> }
    interface IBuilder with 
        member this.ResourceId = solution.resourceId this.Name
        member this.BuildResources location = [
            { Name = this.Name
              Location = location
              Publisher = this.Publisher
              PromotionCode = this.PromotionCode
              Product = this.Product
              WorkspaceResourceId = this.WorkspaceResourceId
              ContainedResources = this.ContainedResources
              ReferencedResources = this.ReferencedResources
              Dependencies = this.Dependencies
              Tags = this.Tags }
        ]

type SolutionBuilder() =
    member _.Yield _ =
        { Name = ResourceName.Empty
          WorkspaceResourceId = ResourceId.create(ResourceType("", ""), ResourceName "")
          ContainedResources =  Set.empty
          ReferencedResources = Set.empty
          Publisher = ""
          PromotionCode = None
          Product = ""
          Dependencies = Set.empty
          Tags = Map.empty
        }

    member _.Run (state:SolutionConfig) = 
        match state with
        | {Name = name } when name.Value = "" -> failwith "You must specify a name."
        | {WorkspaceResourceId = workspaceResourceId} when workspaceResourceId.Name.Value = "" -> failwith "You must specify a workspace."
        | {Publisher = ""} ->  failwith "You must specify a publisher."
        | {Product = ""} ->  failwith "You must specify a product."
        | _ -> state

    [<CustomOperation "name">]
    member _.Name(state: SolutionConfig, name) = { state with Name = ResourceName name }

    [<CustomOperation "workspace">]
    member _.WorkspaceResourceId(state: SolutionConfig, workspace:WorkspaceConfig) = { state with WorkspaceResourceId  = (workspace :> IBuilder).ResourceId}

    [<CustomOperation "containedResources">]
    member _.ContainedResources(state: SolutionConfig, containedResources ) = { state with ContainedResources = state.ContainedResources.Add(containedResources)}

    [<CustomOperation "referencedResources">]
    member _.ReferencedResources(state: SolutionConfig, referencedResources) = { state with ReferencedResources = state.ReferencedResources.Add(referencedResources)}

    [<CustomOperation "product">]
    member _.Product(state: SolutionConfig, product) = { state with Product = product}

    [<CustomOperation "publisher">]
    member _.Publisher(state: SolutionConfig, publisher) = { state with Publisher = publisher}

    [<CustomOperation "promotionCode">]
    member _.PromotionCode(state: SolutionConfig, promotionCode) = { state with PromotionCode = promotionCode}
    
    interface IDependable<SolutionConfig> with member _.Add state newDeps = { state with Dependencies = state.Dependencies + newDeps }
    interface ITaggable<SolutionConfig> with member _.Add state tags = { state with Tags = state.Tags |> Map.merge tags }

      

let solution = SolutionBuilder()

