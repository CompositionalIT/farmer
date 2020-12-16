[<AutoOpen>]
module Farmer.Arm.MachineLearning

open Farmer

//https://docs.microsoft.com/en-us/azure/templates/microsoft.machinelearningservices/2020-08-01/workspaces

let workspaces = ResourceType("Microsoft.MachineLearningServices/workspaces","2020-08-01")

type AzureMachineLearningWorkspace = 
    { 
      Name: string
      Type: string
      ApiVersion: string
      Properties: obj
    }
    interface IArmResource with
        member this.ResourceId = workspaces.resourceId this.WorkspaceName
        member this.JsonModel = {| |} :> _ // TODO : Create JsonModel


