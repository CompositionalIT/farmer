module Farmer.Arm.ResourceGroup

open Farmer
open Farmer.CoreTypes

let schema = "https://schema.management.azure.com/schemas/2019-04-01/deploymentTemplate.json#"
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
    { Name: ResourceName
      ResourceGroupName: ResourceName
      Location: Location
      Resources: IArmResource list
      Outputs: Map<string,string>
      Tags: Map<string,string>
      DeploymentMode: DeploymentMode}
    member this.ResourceName = this.Name
    member this.DependsOn = [
        ResourceId.create (resourceGroups, this.ResourceGroupName)
    ]
    interface IArmResource with
        member this.ResourceName = this.Name
        member this.JsonModel = 
            {| resourceGroupDeployments.Create (this.ResourceName, dependsOn=this.DependsOn, tags=this.Tags) with
                 resourceGroup = this.ResourceGroupName.Value
                 properties = 
                    {| mode = this.DeploymentMode.ArmValue 
                       expressionEvaluationOptions = {| scope = "inner" |}
                       template =
                           {| ``$schema`` = schema
                              contentVersion = "1.0.0.0"
                              parameters = {||}
                              variables = {||}
                              resources = this.Resources |> List.map(fun r -> r.JsonModel)
                              outputs = this.Outputs |> Map.map (fun _ v -> {| ``type`` = "string"; value = v |})|}
                    |}
            |} :> _
