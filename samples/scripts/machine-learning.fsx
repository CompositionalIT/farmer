#r "../../src/Farmer/bin/Debug/netstandard2.0/Farmer.dll"
#r "nuget:Newtonsoft.Json,12.0.0"

open Farmer
open Farmer.Builders
open Farmer.MachineLearning

let workspaces = ResourceType("Microsoft.MachineLearningServices/workspaces","2020-08-01")

type AzureMachineLearningWorkspace = 
    { 
      Name: ResourceName
      Location: Location
      Tags: Map<string,string>
    }
    interface IArmResource with
        member this.ResourceId = workspaces.resourceId this.Name
        member this.JsonModel = 
          {| workspaces.Create(this.Name, this.Location, tags = this.Tags) with 
              identity = {| ``type``= "systemAssigned" |}
              properties = {||} 
          |} :> _


type WorkspaceConfig = 
  { 
    Name: ResourceName
    KeyVaultId: ResourceName
    Tags: Map<string,string>
  }
  interface IBuilder with
    member this.ResourceId = workspaces.resourceId this.Name
    member this.BuildResources location = [
      { Name= this.Name
        Location= location
        Tags= this.Tags }
    ]

type AzureMLWorkspaceBuilder() = 
  member _.Yield _ = 
    { Name= ResourceName.Empty
      Tags= Map.empty }
  
  [<CustomOperation "name">]
  member _.Name (state:WorkspaceConfig, name) = 
    {state with Name = ResourceName name }

  [<CustomOperation "add_tags">]
  member _.Tags (state:WorkspaceConfig, pairs) = 
    { state with
        Tags = pairs |> List.fold (fun map (key,value) -> Map.add key value map) state.Tags }

  [<CustomOperation "add_tag">]
  member this.Tag(state:WorkspaceConfig, key, value) = this.Tags(state, [ (key,value) ])
  
let azuremlworkspace = AzureMLWorkspaceBuilder()

let amlws = azuremlworkspace {
  name "myws"
}

let deployment = arm {
  location Location.EastUS
  add_resource amlws
}

deployment |> Writer.quickWrite "amlworkspace"

deployment
|> Deploy.execute "test-aml-farmer-rg" Deploy.NoParameters







