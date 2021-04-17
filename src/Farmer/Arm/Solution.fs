[<AutoOpen>]
module Farmer.Arm.Solution

open Farmer

let solution = ResourceType("Microsoft.OperationalInsights/workspaces", "2020-03-01-preview")

type Soluton =
    { Name : ResourceName
      Location : Location
      WorkspaceResourceId : ResourceId Set
      ContainedResources : ResourceId Set
      ReferencedResources : ResourceId Set
      Tags : Map<string,string> }
      

    interface IArmResource with
        member  this.ResourceId = solution.resourceId this.Name
        member  this.JsonModel =
            {| solution.Create(this.Name, this.Location, tags = this.Tags ) with
                plan = 
                    {| name = ""
                       publisher = ""
                       promotionCode = ""
                       product = ""
                    
                    |}
                properties = 
                    {| workspaceResourceId = this.WorkspaceResourceId
                       containedResources = this.ContainedResources
                       referencedResources = this.ReferencedResources |}

            
            |} :> _




