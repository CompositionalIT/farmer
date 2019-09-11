namespace Farmer

open Farmer.Internal

module Helpers =
    module AppInsights =
        let instrumentationKey (ResourceName accountName) =
            sprintf "[reference(concat('Microsoft.Insights/components/', %s)).InstrumentationKey]" accountName.QuotedValue
    module Locations =
        let ``East Asia`` = Literal "eastasia"
        let ``Southeast Asia`` = Literal "southeastasia"
        let ``Central US`` = Literal "centralus"
        let ``East US`` = Literal "eastus"
        let ``East US 2`` = Literal "eastus2"
        let ``West US`` = Literal "westus"
        let ``North Central US`` = Literal "northcentralus"
        let ``South Central US`` = Literal "southcentralus"
        let ``North Europe`` = Literal "northeurope"
        let ``West Europe`` = Literal "westeurope"
        let ``Japan West`` = Literal "japanwest"
        let ``Japan East`` = Literal "japaneast"
        let ``Brazil South`` = Literal "brazilsouth"
        let ``Australia East`` = Literal "australiaeast"
        let ``Australia Southeast`` = Literal "australiasoutheast"
        let ``South India`` = Literal "southindia"
        let ``Central India`` = Literal "centralindia"
        let ``West India`` = Literal "westindia"

[<AutoOpen>]
module WebApp =
    module Sku =
        let F1 = Literal "F1"
        let B1 = Literal "B1"
        let B2 = Literal "B2"
        let B3 = Literal "B3"
        let S1 = Literal "S1"
        let S2 = Literal "S2"
        let S3 = Literal "S3"
        let P1 = Literal "P1"
        let P2 = Literal "P2"
        let P3 = Literal "P3"
        let P1V2 = Literal "P1V2"
        let P2V2 = Literal "P2V2"
        let P3V2 = Literal "P3V2"
        let I1 = Literal "I1"
        let I2 = Literal "I2"
        let I3 = Literal "I3"

    let publishingPassword (ResourceName websiteName) =
        sprintf "[list(resourceId('Microsoft.Web/sites/config', %s, 'publishingcredentials'), '2014-06-01').properties.publishingPassword]" websiteName.QuotedValue

    module AppSettings =
        let WebsiteNodeDefaultVersion version = "WEBSITE_NODE_DEFAULT_VERSION", version
        let RunFromPackage = "WEBSITE_RUN_FROM_PACKAGE", Literal "1"
    type WebAppConfig =
        { Name : ResourceName
          ServicePlanName : ResourceName
          Sku : Value
          AppInsightsName : ResourceName option
          RunFromPackage : bool
          WebsiteNodeDefaultVersion : Value option
          Settings : Map<string, Value>
          Dependencies : ResourceName list }
        member this.PublishingPassword = publishingPassword this.Name
    type WebAppBuilder() =
        member __.Yield _ =
            { Name = ResourceName (Literal "")
              ServicePlanName = ResourceName (Literal "")
              Sku = Sku.F1
              AppInsightsName = None
              RunFromPackage = false
              WebsiteNodeDefaultVersion = None
              Settings = Map.empty
              Dependencies = [] }
        member __.Run (state:WebAppConfig) =
            { state with
                Dependencies = state.ServicePlanName :: state.Dependencies }
        /// Sets the name of the web app; use the `name` keyword.
        [<CustomOperation "name">]
        member __.Name(state:WebAppConfig, name) = { state with Name = ResourceName name }
        member this.Name(state:WebAppConfig, name:string) = this.Name(state, Literal name)
        /// Sets the name of service plan of the web app; use the `service_plan_name` keyword.
        [<CustomOperation "service_plan_name">]
        member __.ServicePlanName(state:WebAppConfig, name:Value) = { state with ServicePlanName = ResourceName name }
        member this.ServicePlanName(state:WebAppConfig, name:string) = this.ServicePlanName(state, Literal name)
        /// Sets the sku of the web app; use the `sku` keyword.
        [<CustomOperation "sku">]
        member __.Sku(state:WebAppConfig, sku:Value) = { state with Sku = sku }
        /// Creates a fully-configured application insights resource linked to this web app; use the `use_app_insights` keyword.
        [<CustomOperation "use_app_insights">]
        member __.UseAppInsights(state:WebAppConfig, name) = { state with AppInsightsName = Some name }
        member this.UseAppInsights(state:WebAppConfig, name) = this.UseAppInsights(state, ResourceName name)
        member this.UseAppInsights(state:WebAppConfig, name:string) = this.UseAppInsights(state, ResourceName(Literal name))
        /// Sets the web app to use run from package mode; use the `run_from_package` keyword.
        [<CustomOperation "run_from_package">]
        member __.RunFromPackage(state:WebAppConfig) = { state with RunFromPackage = true }
        /// Sets the node version of the web app; use the `website_node_default_version` keyword.
        [<CustomOperation "website_node_default_version">]
        member __.NodeVersion(state:WebAppConfig, version) = { state with WebsiteNodeDefaultVersion = Some version }
        member this.NodeVersion(state:WebAppConfig, version) = this.NodeVersion(state, Literal version)
        /// Sets an app setting of the web app; use the `setting` keyword.
        [<CustomOperation "setting">]
        member __.AddSetting(state:WebAppConfig, key, value) = { state with Settings = state.Settings.Add(key, value) }
        member this.AddSetting(state:WebAppConfig, key, value:string) = this.AddSetting(state, key, Literal value)
        /// Sets a dependency for the web app; use the `depends_on` keyword.
        [<CustomOperation "depends_on">]
        member __.DependsOn(state:WebAppConfig, resourceName) =
            { state with Dependencies = resourceName :: state.Dependencies }
    let webApp = WebAppBuilder()

[<AutoOpen>]
module Storage =
    let accountKey (ResourceName accountName) =
        sprintf
            "[concat('DefaultEndpointsProtocol=https;AccountName=', %s, ';AccountKey=', listKeys(resourceId('Microsoft.Storage/storageAccounts/', %s), '2017-10-01').keys[0].value)]"
            accountName.QuotedValue
            accountName.QuotedValue

    module Sku =
        let StandardLRS = Literal "Standard_LRS"
        let StandardGRS = Literal "Standard_GRS"
        let StandardRAGRS = Literal "Standard_RAGRS"
        let StandardZRS = Literal "Standard_ZRS"
        let StandardGZRS = Literal "Standard_GZRS"
        let StandardRAGZRS = Literal "Standard_RAGZRS"
        let PremiumLRS = Literal "Premium_LRS"
        let PremiumZRS = Literal "Premium_ZRS"
    type StorageAccountConfig =
        { /// The name of the storage account.
          Name : ResourceName
          /// The sku of the storage account.
          Sku : Value }
        member this.Key = accountKey this.Name
    type StorageAccountBuilder() =
        member __.Yield _ = { Name = ResourceName (Literal ""); Sku = Sku.StandardLRS }
        [<CustomOperation "name">]
        member __.Name(state:StorageAccountConfig, name) = { state with Name = name }
        member this.Name(state:StorageAccountConfig, name) = this.Name(state, ResourceName name)
        member this.Name(state:StorageAccountConfig, name) = this.Name(state, ResourceName(Literal name))
        [<CustomOperation "sku">]
        member __.Sku(state:StorageAccountConfig, sku) = { state with Sku = sku }
        member this.Sku(state:StorageAccountConfig, sku) = this.Sku(state, Literal sku)
    let storageAccount = StorageAccountBuilder()

    open WebApp
    type WebAppBuilder with
        member this.DependsOn(state:WebAppConfig, storageAccountConfig:StorageAccountConfig) =
            this.DependsOn(state, storageAccountConfig.Name)

[<AutoOpen>]
module CosmosDb =
    type CosmosDbContainerConfig =
        { Name : ResourceName
          PartitionKey : string list * CosmosDbIndexKind
          Indexes : (string * (CosmosDbIndexDataType * CosmosDbIndexKind) list) list
          ExcludedPaths : string list }

    type CosmosDbConfig =
        { Name : ResourceName
          ServerName : ResourceName          
          ConsistencyPolicy : ConsistencyPolicy
          FailoverPolicy : FailoverPolicy
          Throughput : Value
          Containers : CosmosDbContainerConfig list }    

    type CosmosDbContainer() =
        member __.Yield _ =
            { Name = ResourceName (Literal "")
              PartitionKey = [], Hash
              Indexes = []
              ExcludedPaths = [] }


        [<CustomOperation "name">]
        member __.Name (state:CosmosDbContainerConfig, name) =
            { state with Name = ResourceName(Literal name) }

        [<CustomOperation "partition_key">]
        member __.PartitionKey (state:CosmosDbContainerConfig, partitions, indexKind) =
            { state with PartitionKey = partitions, indexKind }

        [<CustomOperation "include_index">]
        member __.IncludeIndex (state:CosmosDbContainerConfig, path, indexes) =
            { state with Indexes = (path, indexes) :: state.Indexes }

        [<CustomOperation "exclude_path">]
        member __.ExcludePath (state:CosmosDbContainerConfig, path) =
            { state with ExcludedPaths = path :: state.ExcludedPaths }

    type CosmosDbBuilder() =
        member __.Yield _ =
            { Name = ResourceName (Literal "CosmosDatabase")
              ServerName = ResourceName (Literal "CosmosServer")              
              ConsistencyPolicy = Eventual
              FailoverPolicy = NoFailover
              Throughput = Literal "400"
              Containers = [] }
        /// Sets the name of cosmos db server; use the `server_name` keyword.
        [<CustomOperation "server_name">]
        member __.ServerName(state:CosmosDbConfig, serverName) = { state with ServerName = ResourceName serverName }
        member this.ServerName(state:CosmosDbConfig, serverName:string) = this.ServerName(state, Literal serverName)
        /// Sets the name of the web app; use the `name` keyword.
        [<CustomOperation "name">]
        member __.Name(state:CosmosDbConfig, name:Value) = { state with Name = ResourceName name }
        member this.Name(state:CosmosDbConfig, name:string) = this.Name(state, Literal name)
        /// Sets the sku of the web app; use the `sku` keyword.
        [<CustomOperation "consistency_policy">]
        member __.ConsistencyPolicy(state:CosmosDbConfig, consistency:ConsistencyPolicy) = { state with ConsistencyPolicy = consistency }
        [<CustomOperation "failover_policy">]
        member __.FailoverPolicy(state:CosmosDbConfig, failoverPolicy:FailoverPolicy) = { state with FailoverPolicy = failoverPolicy }
        [<CustomOperation "throughput">]
        member __.Throughput(state:CosmosDbConfig, throughput) = { state with Throughput = throughput }
        member this.Throughput(state:CosmosDbConfig, throughput:int) = this.Throughput(state, Literal(string throughput))
        [<CustomOperation "add_containers">]
        member __.AddContainers(state:CosmosDbConfig, containers) =
            { state with Containers = state.Containers @ containers }

    open WebApp
    type WebAppBuilder with
        member this.DependsOn(state:WebAppConfig, cosmosDbConfig:CosmosDbConfig) =
            this.DependsOn(state, cosmosDbConfig.Name)

    let cosmosDb = CosmosDbBuilder()
    let container = CosmosDbContainer()

type ArmConfig =
    { Parameters : string Set
      Variables : (string * string) list
      Outputs : (string * Value) list
      Location : Value
      Resources : obj list }

[<AutoOpen>]
module ArmBuilder =
    type ArmBuilder() =
        member __.Yield _ =
            { Parameters = Set.empty
              Variables = List.empty
              Outputs = List.empty
              Resources = List.empty
              Location = Helpers.Locations.``West Europe`` }

        member __.Run (state:ArmConfig) = {
            Parameters = state.Parameters |> Set.toList
            Outputs = state.Outputs
            Variables = state.Variables
            Resources =
                state.Resources
                |> List.collect(function
                | :? StorageAccountConfig as sac ->
                    [ { Location = state.Location; Name = sac.Name; Sku = sac.Sku } ]
                | :? WebAppConfig as wac -> [
                    let webApp =
                        { Name = wac.Name
                          AppSettings = [
                            yield! Map.toList wac.Settings
                            if wac.RunFromPackage then yield WebApp.AppSettings.RunFromPackage

                            match wac.WebsiteNodeDefaultVersion with
                            | Some v -> yield WebApp.AppSettings.WebsiteNodeDefaultVersion v
                            | None -> ()

                            match wac.AppInsightsName with
                            | Some v ->
                                yield "APPINSIGHTS_INSTRUMENTATIONKEY", Literal (Helpers.AppInsights.instrumentationKey v)
                                yield "APPINSIGHTS_PROFILERFEATURE_VERSION", Literal "1.0.0"
                                yield "APPINSIGHTS_SNAPSHOTFEATURE_VERSION", Literal "1.0.0"
                                yield "ApplicationInsightsAgent_EXTENSION_VERSION", Literal "~2"
                                yield "DiagnosticServices_EXTENSION_VERSION", Literal "~3"
                                yield "InstrumentationEngine_EXTENSION_VERSION", Literal "~1"
                                yield "SnapshotDebugger_EXTENSION_VERSION", Literal "~1"
                                yield "XDT_MicrosoftApplicationInsights_BaseExtensions", Literal "~1"
                                yield "XDT_MicrosoftApplicationInsights_Mode", Literal "recommended"
                            | None ->
                                ()
                          ]

                          Extensions =
                            match wac.AppInsightsName with
                            | Some _ -> Set [ AppInsightsExtension ]
                            | None -> Set.empty

                          Dependencies = [
                            yield! wac.Dependencies
                            match wac.AppInsightsName with
                            | Some appInsightsame -> yield appInsightsame
                            | None -> ()
                          ]
                        }

                    let serverFarm =
                        { Location = state.Location
                          Name = wac.ServicePlanName
                          Sku = wac.Sku
                          WebApps = [ webApp ] }

                    yield serverFarm
                    match wac.AppInsightsName with
                    | Some ai ->
                        yield { Name = ai; Location = state.Location; LinkedWebsite = wac.Name }
                    | None ->
                        () ]
                | :? CosmosDbConfig as cosmos -> [
                    let server =
                        { Name = cosmos.ServerName
                          Location = state.Location
                          ConsistencyPolicy = cosmos.ConsistencyPolicy
                          WriteModel = cosmos.FailoverPolicy
                          Databases =
                            [ { Name = cosmos.Name
                                Dependencies = [ ResourcePath.makeCosmosDb cosmos.ServerName ]
                                Throughput = cosmos.Throughput
                                Containers =
                                    cosmos.Containers
                                    |> List.map(fun c ->
                                        { CosmosDbContainer.Name = c.Name
                                          PartitionKey =
                                            {| Paths = fst c.PartitionKey
                                               Kind = snd c.PartitionKey |}
                                          IndexingPolicy =
                                            {| ExcludedPaths = c.ExcludedPaths
                                               IncludedPaths =
                                                   c.Indexes
                                                   |> List.map(fun index ->
                                                     {| Path = fst index
                                                        Indexes =
                                                            index
                                                            |> snd
                                                            |> List.map(fun (dataType, kind) ->
                                                                {| DataType = dataType
                                                                   Kind = kind |})
                                                     |})
                                            |}
                                        })
                              }
                            ]
                        }
                    yield server ]
                | _ ->
                    failwith "Sorry, I don't know how to handle this resource.")
        }

        /// Creates a variable; use the `variable` keyword.
        [<CustomOperation "variable">]
        member __.AddVariable (state, key, value) : ArmConfig =
            { state with
                Variables = (key, value) :: state.Variables }

        /// Creates a parameter; use the `parameter` keyword.
        [<CustomOperation "parameter">]
        member __.AddParameter (state, parameter) : ArmConfig =
            { state with
                Parameters = state.Parameters.Add parameter }

        /// Creates a list of parameters; use the `parameters` keyword.
        [<CustomOperation "parameters">]
        member __.AddParameters (state, parameters) : ArmConfig =
            { state with
                Parameters = state.Parameters + (Set.ofList parameters) }

        /// Creates an output; use the `output` keyword.
        [<CustomOperation "output">]
        member __.Output (state, outputName, outputValue) : ArmConfig = { state with Outputs = (outputName, outputValue) :: state.Outputs }
        member this.Output (state:ArmConfig, outputName:string, outputValue:string) = this.Output(state, outputName, Literal outputValue)
        member this.Output (state:ArmConfig, outputName:string, (ResourceName outputValue)) = this.Output(state, outputName, outputValue)

        /// Sets the default location of all resources; use the `location` keyword.
        [<CustomOperation "location">]
        member __.Location (state, location) : ArmConfig = { state with Location = location }

        /// Adds a resource to the template; use the `resource` keyword.
        [<CustomOperation "resource">]
        member __.AddResource(state, resource) : ArmConfig =
            { state with Resources = box resource :: state.Resources }
    let arm = ArmBuilder()

