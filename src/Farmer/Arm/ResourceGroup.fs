[<AutoOpen>]
module Farmer.Arm.ResourceGroup

open Farmer
open Farmer.CoreTypes
open Farmer.Identity
open System

let resourceGroups = ResourceType ("Microsoft.Resources/resourceGroups", "2020-06-01")
let deployments = ResourceType ("Microsoft.Resources/deployments", "2020-06-01")

let private getDeploymentNumber =
    let r = Random()
    fun () -> r.Next 10000

type ResourceGroup = 
    { Name : ResourceName 
      Location: Location }
    interface IArmResource with
        member this.ResourceName = this.Name
        member this.JsonModel =
            {| ``type`` = resourceGroups.Type
               apiVersion= resourceGroups.ApiVersion
               name= this.Name.Value
               location= this.Location.ArmValue |} :> _

type ResourceGroupDeployment = 
    { Name : ResourceName 
      Location: Location 
      Template: ArmTemplate }
    interface IArmResource with
        member this.ResourceName = this.Name
        member this.JsonModel =
            {| ``type`` = deployments.Type
               apiVersion= deployments.ApiVersion
               name = sprintf "%s-deployment-%d" this.Name.Value (getDeploymentNumber ())
               resourceGroup = this.Name.Value
               dependsOn = 
                 [ sprintf "[%s]" (ResourceId.create (resourceGroups, this.Name)).ArmExpression.Value ]
               properties = 
                 {| mode = "Incremental"
                    template = this.Template |> Writer.TemplateGeneration.processTemplate
                 |}
            |} :> _