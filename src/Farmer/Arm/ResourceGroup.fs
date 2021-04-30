[<AutoOpen>]
module Farmer.Arm.ResourceGroup

open Farmer
open Farmer.Writer

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
    member this.ResourceId = resourceGroupDeployment.resourceId this.Name
    member this.Template = 
        { Parameters = [
            for resource in this.Resources do
                match resource with
                | :? IParameters as p -> yield! p.SecureParameters
                | _ -> ()
          ] |> List.distinct
          Outputs = this.Outputs |> Map.toList
          Resources = this.Resources }
    interface IArmResource with
        member this.ResourceId = resourceGroupDeployment.resourceId this.Name
        member this.JsonModel = 
            {| resourceGroupDeployment.Create(this.Name, this.Location, tags = this.Tags ) with
                resourceGroup = this.Name.Value
                properties = 
                    {|  template = TemplateGeneration.processTemplate this.Template
                        parameters = 
                            this.Parameters
                            |> Seq.map(fun s -> s, {| ``type`` = "securestring" |})
                            |> Map.ofSeq
                        mode = 
                          match this.Mode with
                          | Incremental -> "Incremental"
                          | Complete -> "Complete"
                        expressionEvaluationOptions = {| scope = "Inner" |}
                    |}
            |} :> _