module Farmer.Arm.ResourceGroup

open Farmer
open Farmer.CoreTypes

let resourceGroups = ResourceType ("Microsoft.Resources/resourceGroups", "2020-06-01")
let resourceGroupDeployments = ResourceType ("Microsoft.Resources/deployments", "2020-06-01")

type DeploymentMode =
    | Incremental
    | Complete
    member this.ArmValue = 
        match this with
        | Incremental -> "Incremental"
        | Complete -> "Complete"

type ResourceGroup =
    { Name: ResourceName
      Location: Location
      Tags: Map<string,string> }
    interface IArmResource with
        member this.ResourceName = this.Name
        member this.JsonModel = 
            resourceGroups.Create(this.Name, this.Location, tags = this.Tags)
            :> _

type ResourceGroupDeployment =
    { ResourceGroupName: ResourceName
      Location: Location
      Resources: IArmResource list
      Tags: Map<string,string>
      DeploymentMode: DeploymentMode}
    member this.ResourceName = 
        let rg = this.ResourceGroupName.Value
        sprintf "%s-deployment" rg
        |> ResourceName
    member this.DependsOn = [
        ResourceId.create (resourceGroups, this.ResourceGroupName)
    ]
    interface IArmResource with
        member this.ResourceName = this.ResourceName
        member this.JsonModel = 
            {| resourceGroupDeployments.Create (this.ResourceName, this.Location, this.DependsOn, this.Tags) with
                 properties = {|
                   template = {|
                     ``$schema`` = "https://schema.management.azure.com/schemas/2019-04-01/deploymentTemplate.json#"
                     contentVersion = "1.0.0.0"
                     resources = this.Resources |> List.map(fun r -> r.JsonModel)
                   |}
                   mode = this.DeploymentMode.ArmValue
                 |}
            |} :> _
