#r "../../src/Farmer/bin/Debug/netstandard2.0/Farmer.dll"

open Farmer
open Farmer.MachineLearning

let workspaces = ResourceType("Microsoft.MachineLearningServices/workspaces","2020-08-01")
let vaults = ResourceType ("Microsoft.KeyVault/vaults", "2018-02-14")

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

// 
let wsProperties = {
    Description = "Test farmer workspace"
    FriendlyName = "myws"
    KeyVault = vaults.resourceId "kvId"
    ApplicationInsights = workspaces.resourceId "aiId"
    ContainerRegistry = workspaces.resourceId "crId"
    StorageAccount = workspaces.resourceId "saId"
}

let ws = {
  Name = ResourceName "myws"
  Location = Location.EastUS
  Properties = wsProperties
  Tags = Map.empty
}