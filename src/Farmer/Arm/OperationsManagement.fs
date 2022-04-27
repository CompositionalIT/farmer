[<AutoOpen>]
module Farmer.Arm.OperationsManagement

open Farmer

let solutions = ResourceType("Microsoft.OperationsManagement/solutions", "2015-11-01-preview")

type Solution =
    { Name: ResourceName
      Location: Location
      Plan: 
          {|
              Name: string
              Product: string
              Publisher: string
          |}
      Properties:
          {|
              ContainedResources: string list
              ReferencedResources: string list
              WorkspaceResourceId: string
          |}
      Tags: Map<string, string>
    }
    interface IArmResource with
        member this.ResourceId = solutions.resourceId this.Name
        member this.JsonModel =
            {|
              solutions.Create(this.Name, this.Location, tags = this.Tags) with
                  plan =
                      {|
                          name = this.Plan.Name
                          publisher = this.Plan.Publisher
                          product = this.Plan.Product
                          promotionCode = ""
                      |}
                  properties =
                      {|
                          workspaceResourceId = this.Properties.WorkspaceResourceId
                          containedResources = this.Properties.ContainedResources
                          referencedResources = this.Properties.ReferencedResources
                      |}
            |}