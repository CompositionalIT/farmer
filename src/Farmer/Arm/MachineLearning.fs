[<AutoOpen>]
module Farmer.Arm.MachineLearning

open Farmer
open Farmer.MachineLearning

//https://docs.microsoft.com/en-us/azure/templates/microsoft.machinelearningservices/2020-08-01/workspaces

let workspaces = ResourceType("Microsoft.MachineLearningServices/workspaces","2020-08-01")

type AzureMachineLearningWorkspace = 
    { 
      Name: ResourceName
      Location: Location
      Properties: WorkspaceProperties
      Tags: Map<string,string>
    }
    interface IArmResource with
        member this.ResourceId = workspaces.resourceId this.Name
        member this.JsonModel = 
          {| workspaces.Create(this.Name, this.Location, tags = this.Tags) with 
              properties = this.Properties |} :> _