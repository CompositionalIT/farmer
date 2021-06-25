[<AutoOpen>]
module Farmer.Arm.ResourceGroup

open Farmer
open Farmer.Writer

let resourceGroupDeployment = ResourceType ("Microsoft.Resources/deployments","2020-10-01")

type DeploymentMode = Incremental|Complete

/// Represents all configuration information to generate an ARM template.
type ResourceGroupDeployment =
    { Name: ResourceName
      Dependencies: ResourceId Set
      Outputs : Map<string, string>
      Location : Location
      Resources : IArmResource list
      Mode: DeploymentMode
      Tags: Map<string,string> }
    member this.ResourceId = resourceGroupDeployment.resourceId this.Name
    member this.Parameters = 
          [ for resource in this.Resources do
                match resource with
                | :? IParameters as p -> yield! p.SecureParameters
                | _ -> ()
          ] |> List.distinct
    member this.RequiredResourceGroups = 
        this.Name.Value :: (this.Resources |> List.collect (function | :? ResourceGroupDeployment as rg -> rg.RequiredResourceGroups | _ ->  []))
        |> List.distinct
    member this.Template = 
        { Parameters = this.Parameters
          Outputs = this.Outputs |> Map.toList
          Resources = this.Resources }
    interface IParameters with 
        member this.SecureParameters = this.Parameters
    interface IArmResource with
        member this.ResourceId = resourceGroupDeployment.resourceId this.Name
        member this.JsonModel = 
            {| resourceGroupDeployment.Create(this.Name, this.Location, dependsOn = this.Dependencies, tags = this.Tags ) with
                location = null // location is not supported for nested resource groups
                resourceGroup = this.Name.Value
                properties = 
                    {|  template = TemplateGeneration.processTemplate this.Template
                        parameters = 
                            this.Parameters
                            |> Seq.map(fun (SecureParameter s) -> s, {| ``value`` = $"[parameters('%s{s}')]" |})
                            |> Map.ofSeq
                        mode = 
                          match this.Mode with
                          | Incremental -> "Incremental"
                          | Complete -> "Complete"
                        expressionEvaluationOptions = {| scope = "Inner" |}
                    |}
            |} :> _