[<AutoOpen>]
module Farmer.Arm.Solution

open Farmer

let solution = ResourceType("Microsoft.OperationsManagement/solutions", "2015-11-01-preview")

type Solution =
    { Name : ResourceName
      Location : Location
      WorkspaceResourceId : ResourceId
      ContainedResources : ResourceId Set
      Publisher : string
      PromotionCode : string option
      ReferencedResources : ResourceId Set
      Product : string 
      Dependencies : ResourceId Set
      Tags : Map<string,string> }
      

    interface IArmResource with
        member  this.ResourceId = solution.resourceId this.Name
        member  this.JsonModel =
            {| solution.Create(this.Name, this.Location,this.Dependencies, tags = this.Tags ) with
                plan = 
                    {| 
                       name = this.Name.Value
                       publisher = this.Publisher
                       promotionCode = this.PromotionCode |> Option.toObj
                       product = this.Product
                    
                    |}
                properties = 
                    {| workspaceResourceId = this.WorkspaceResourceId.ArmExpression.Value
                       containedResources = this.ContainedResources
                       referencedResources = this.ReferencedResources |}
            |} :> _




