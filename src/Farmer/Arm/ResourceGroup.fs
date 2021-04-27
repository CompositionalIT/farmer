[<AutoOpen>]
module Farmer.Arm.ResourceGroup

open Farmer

let resourceGroupDeployment = ResourceType ("Microsoft.Resources/deployments","2020-10-01")

type DeploymentMode = Incremental|Complete

/// Represents all configuration information to generate an ARM template.
type ResourceGroupDeployment =
    { Name: ResourceName
      Parameters : string Set
      Outputs : Map<string, string>
      Location : Location
      Resources : IArmResource list
      Mode: DeploymentMode
      Tags: Map<string,string> }
    interface IArmResource with
        member this.ResourceId = resourceGroupDeployment.resourceId this.Name
        member this.JsonModel = 
            {| resourceGroupDeployment.Create(this.Name, this.Location, tags = this.Tags ) with
                resourceGroup = this.Name.Value
                properties = 
                    {|  template = 
                        parameters = {||}
                        mode = 
                          match this.Mode with
                          | Incremental -> "Incremental"
                          | Complete -> "Complete"
                        expressionEvaluationOptions = {| scope = "Inner" |}
                    |}
            |} :> _