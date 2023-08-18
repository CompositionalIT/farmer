[<AutoOpen>]
module Farmer.Arm.OperationsManagement

open Farmer

let oms =
    ResourceType("Microsoft.OperationsManagement/solutions", "2015-11-01-preview")

type OMS = {
    Name: ResourceName
    Location: Location
    Plan: {|
        Name: string
        Product: string
        Publisher: string
    |}
    Properties: {|
        ContainedResources: ResourceId list
        ReferencedResources: ResourceId list
        WorkspaceResourceId: ResourceId
    |}
    Tags: Map<string, string>
} with

    interface IArmResource with
        member this.ResourceId = oms.resourceId this.Name

        member this.JsonModel = {|
            oms.Create(this.Name, this.Location, [ this.Properties.WorkspaceResourceId ], tags = this.Tags) with
                plan = {|
                    name = this.Plan.Name
                    publisher = this.Plan.Publisher
                    product = this.Plan.Product
                    promotionCode = ""
                |}
                properties = {|
                    workspaceResourceId = this.Properties.WorkspaceResourceId.Eval()
                    containedResources = this.Properties.ContainedResources |> List.map (fun cr -> cr.Eval())
                    referencedResources = this.Properties.ReferencedResources |> List.map (fun rr -> rr.Eval())
                |}
        |}
