[<AutoOpen>]
module Farmer.ArmBuilder

type ArmConfig =
    { Parameters : string Set
      Outputs : (string * string) list
      Location : string
      Resources : obj list }
type ArmBuilder() =
    member __.Yield _ =
        { Parameters = Set.empty
          Outputs = List.empty
          Resources = List.empty
          Location = WestEurope }

    member __.Run (state:ArmConfig) =
        state.Location, {
        Parameters = state.Parameters |> Set.toList
        Outputs = state.Outputs
        Resources =
            state.Resources
            |> List.collect(function
            | :? StorageAccountConfig as config ->
                [ StorageAccount (Converters.storage state.Location config) ]
            | :? WebAppConfig as config ->
                let outputs = Converters.webApp state.Location config
                [ yield WebApp outputs.WebApp
                  yield ServerFarm outputs.ServerFarm
                  match outputs.Ai with (Some ai) -> yield AppInsights ai | None -> () ]
            | :? FunctionsConfig as config ->
                let outputs = config |> Converters.functions state.Location
                [ yield WebApp outputs.WebApp
                  yield ServerFarm outputs.ServerFarm
                  match outputs.Ai with (Some ai) -> yield AppInsights ai | None -> ()
                  match outputs.Storage with (Some storage) -> yield StorageAccount storage | None -> () ]
            | :? CosmosDbConfig as config ->
                let outputs = config |> Converters.cosmosDb state.Location
                [ yield CosmosAccount outputs.Account
                  yield CosmosSqlDb outputs.SqlDb
                  yield! outputs.Containers |> List.map CosmosContainer ]
            | :? SqlAzureConfig as config ->
                [ SqlServer (Converters.sql state.Location config) ]
            | :? VmConfig as config ->
                let output = Converters.vm state.Location config
                [ yield Vm output.Vm
                  yield Vnet output.Vnet
                  yield Ip output.Ip
                  yield Nic output.Nic
                  match output.Storage with Some storage -> yield StorageAccount storage | None -> () ]
            | :? SearchConfig as search ->
                [ AzureSearch (Converters.search state.Location search) ]
            | :? AppInsightsConfig as aiConfig ->
                [ AppInsights (Converters.appInsights state.Location aiConfig) ]
            | r ->
                failwithf "Sorry, I don't know how to handle this resource of type '%s'." (r.GetType().FullName))
            |> List.distinctBy(fun r -> r.ResourceName)
    }

    /// Creates an output; use the `output` keyword.
    [<CustomOperation "output">]
    member __.Output (state, outputName, outputValue) : ArmConfig = { state with Outputs = (outputName, outputValue) :: state.Outputs }
    member this.Output (state:ArmConfig, outputName:string, (ResourceName outputValue)) = this.Output(state, outputName, outputValue)
    member this.Output (state:ArmConfig, outputName:string, outputValue:string option) =
        match outputValue with
        | Some outputValue -> this.Output(state, outputName, outputValue)
        | None -> state

    /// Sets the default location of all resources; use the `location` keyword.
    [<CustomOperation "location">]
    member __.Location (state, location) : ArmConfig = { state with Location = location }

    /// Adds a resource to the template; use the `add_resource` keyword.
    [<CustomOperation "add_resource">]
    member __.AddResource(state, resource) : ArmConfig =
        { state with Resources = box resource :: state.Resources }

let arm = ArmBuilder()