#r "../../src/Farmer/bin/Debug/netstandard2.0/Farmer.dll"
#r "nuget:Newtonsoft.Json,12.0.0"

open Farmer
open Farmer.Builders

let id = "" // subscription id
let workspaces = ResourceType("Microsoft.MachineLearningServices/workspaces","2020-08-01")

type AzureMachineLearningWorkspace = 
    { 
      Name: ResourceName
      Location: Location
      KeyVault: string
      StorageAccount: string
      AppInsights: string
      Tags: Map<string,string>
    }
    interface IArmResource with
        member this.ResourceId = workspaces.resourceId this.Name
        member this.JsonModel = 
          {| workspaces.Create(this.Name, this.Location, tags = this.Tags) with 
              identity = {| ``type`` = "SystemAssigned" |}
              properties = {| applicationInsights = this.AppInsights
                              keyVault = this.KeyVault
                              storageAccount = this.StorageAccount |} 
              resources = [||]
          |} :> _

type WorkspaceConfig = 
  { 
    Name: ResourceName
    KeyVault: string
    StorageAccount: string
    AppInsights: string
    Tags: Map<string,string>
  }
  interface IBuilder with
    member this.ResourceId = workspaces.resourceId this.Name
    member this.BuildResources location = [
      { Name= this.Name
        Location= location
        Tags= this.Tags 
        KeyVault = this.AppInsights
        StorageAccount = this.StorageAccount
        AppInsights= this.AppInsights
        }
    ]

type AzureMLWorkspaceBuilder() = 
  member _.Yield _ = 
    { Name= ResourceName.Empty
      KeyVault = ""
      StorageAccount = ""
      AppInsights= ""
      Tags= Map.empty } 
  
  [<CustomOperation "name">]
  member _.Name (state:WorkspaceConfig, name) = 
    {state with Name = ResourceName name }

  [<CustomOperation "keyvault_name">]
  member _.KeyVault (state:WorkspaceConfig, kvname) = 
    {state with KeyVault = kvname }

  [<CustomOperation "storage_account_name">]
  member _.StorageAccount (state:WorkspaceConfig, saname) = 
    {state with StorageAccount = saname }

  [<CustomOperation "app_insights_name">]
  member _.AppInsights (state:WorkspaceConfig, ainame) = 
    {state with AppInsights = ainame }

  [<CustomOperation "add_tags">]
  member _.Tags (state:WorkspaceConfig, pairs) = 
    { state with
        Tags = pairs |> List.fold (fun map (key,value) -> Map.add key value map) state.Tags }

  [<CustomOperation "add_tag">]
  member this.Tag(state:WorkspaceConfig, key, value) = this.Tags(state, [ (key,value) ])
  
let azuremlworkspace = AzureMLWorkspaceBuilder()

let unwrapRN (rn:ResourceName):string = 
  match rn with
  | ResourceName r -> r
  | _ -> ""

// Create app insights, keyvault, storage account
let sa = storageAccount {
    name "farmersa10232020"
}

let kv = keyVault {
  name "farmerkv10232020"
}

let ai = appInsights {
  name "farmerai10232020"
}

let amlws = azuremlworkspace {
  name "my-wkspc"
  keyvault_name (sprintf "/subscriptions/%s/%s" id (unwrapRN kv.Name))
  app_insights_name (sprintf "/subscriptions/%s/%s" id (unwrapRN ai.Name))
  storage_account_name (sprintf "/subscriptions/%s/%s" id (unwrapRN sa.ResourceId.Name))
}

let deployment = arm {
  location Location.EastUS
  add_resources [
    sa
    ai
    kv
    amlws
  ]
}

deployment |> Writer.quickWrite "amlworkspace"

deployment
|> Deploy.execute "test-aml-farmer-rg" Deploy.NoParameters







