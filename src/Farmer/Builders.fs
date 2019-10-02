namespace Farmer

open Farmer.Internal

[<AutoOpen>]
module Helpers =
    module AppInsights =
        let instrumentationKey (ResourceName accountName) =
            sprintf "[reference('Microsoft.Insights/components/%s').InstrumentationKey]" accountName
            |> ArmExpression
    [<AutoOpen>]
    module Locations =
        let EastAsia = "eastasia"
        let SoutheastAsia = "southeastasia"
        let CentralUS = "centralus"
        let EastUS = "eastus"
        let EastUS2 = "eastus2"
        let WestUS = "westus"
        let NorthCentralUS = "northcentralus"
        let SouthCentralUS = "southcentralus"
        let NorthEurope = "northeurope"
        let WestEurope = "westeurope"
        let JapanWest = "japanwest"
        let JapanEast = "japaneast"
        let BrazilSouth = "brazilsouth"
        let AustraliaEast = "australiaeast"
        let AustraliaSoutheast = "australiasoutheast"
        let SouthIndia = "southindia"
        let CentralIndia = "centralindia"
        let WestIndia = "westindia"

[<AutoOpen>]
module private InternalHelpers =
    let sanitise filters maxLength (resourceName:ResourceName) =
        resourceName.Value.ToLower()
        |> Seq.filter(fun c -> Seq.exists(fun filter -> filter c) filters)
        |> Seq.truncate maxLength
        |> Seq.toArray
        |> System.String

    let sanitiseStorage = sanitise [ System.Char.IsLetterOrDigit ] 16
    let sanitiseSearch = sanitise [ System.Char.IsLetterOrDigit; (=) '-' ] 60

[<AutoOpen>]
module Storage =
    module Sku =
        let StandardLRS = "Standard_LRS"
        let StandardGRS = "Standard_GRS"
        let StandardRAGRS = "Standard_RAGRS"
        let StandardZRS = "Standard_ZRS"
        let StandardGZRS = "Standard_GZRS"
        let StandardRAGZRS = "Standard_RAGZRS"
        let PremiumLRS = "Premium_LRS"
        let PremiumZRS = "Premium_ZRS"
    let buildKey (ResourceName name) =
        sprintf
            "[concat('DefaultEndpointsProtocol=https;AccountName=%s;AccountKey=', listKeys('%s', '2017-10-01').keys[0].value)]"
                name
                name
        |> ArmExpression

    type StorageAccountConfig =
        { /// The name of the storage account.
          Name : ResourceName
          /// The sku of the storage account.
          Sku : string }
        /// Gets the ARM expression path to the key of this storage account.
        member this.Key = buildKey this.Name
    type StorageAccountBuilder() =
        member __.Yield _ = { Name = ResourceName.Empty; Sku = Sku.StandardLRS }
        [<CustomOperation "name">]
        /// Sets the name of the storage account.
        member __.Name(state:StorageAccountConfig, name) = { state with Name = name }
        member this.Name(state:StorageAccountConfig, name) = this.Name(state, ResourceName name)
        [<CustomOperation "sku">]
        /// Sets the sku of the storage account.
        member __.Sku(state:StorageAccountConfig, sku) = { state with Sku = sku }
    let storageAccount = StorageAccountBuilder()

[<AutoOpen>]
module WebApp =
    type WorkerSize = Small | Medium | Large
    type WebAppSku = Shared | Free | Basic of string | Standard of string | Premium of string | PremiumV2 of string | Isolated of string
    type WorkerRuntime = DotNet | Node | Java | Python
    type OS = Windows | Linux
    module Sku =
        let D1 = Shared
        let F1 = Free
        let B1 = Basic "B1"
        let B2 = Basic "B2"
        let B3 = Basic "B3"
        let S1 = Standard "S1"
        let S2 = Standard "S2"
        let S3 = Standard "S3"
        let P1 = Premium "P1"
        let P2 = Premium "P2"
        let P3 = Premium "P3"
        let P1V2 = PremiumV2 "P1V2"
        let P2V2 = PremiumV2 "P2V2"
        let P3V2 = PremiumV2 "P3V2"
        let I1 = Isolated "I1"
        let I2 = Isolated "I2"
        let I3 = Isolated "I3"
    module AppSettings =
        let WebsiteNodeDefaultVersion version = "WEBSITE_NODE_DEFAULT_VERSION", version
        let RunFromPackage = "WEBSITE_RUN_FROM_PACKAGE", "1"
    let publishingPassword (ResourceName name) =
        sprintf "[list(resourceId('Microsoft.Web/sites/config', '%s', 'publishingcredentials'), '2014-06-01').properties.publishingPassword]" name
        |> ArmExpression


    /// A ResourceRef represents a linked resource; typically this will be for two resources that have a relationship
    /// such as AppInsights on WebApp. WebApps can automatically create and configure an AI instance for the webapp,
    /// or configure the web app to an existing AI instance, or do nothing.
    type ResourceRef =
        | External of ResourceName
        | AutomaticPlaceholder
        | AutomaticallyCreated of ResourceName
        member this.ResourceNameOpt = match this with External r | AutomaticallyCreated r -> Some r | AutomaticPlaceholder -> None
        member this.ResourceName = this.ResourceNameOpt |> Option.defaultValue ResourceName.Empty
        member this.ReplacePlaceholder name =
            match this with
            | AutomaticPlaceholder ->
                AutomaticallyCreated(ResourceName name)
            | External _
            | AutomaticallyCreated _ ->
                this

    type WebAppConfig =
        { Name : ResourceName
          ServicePlanName : ResourceRef
          Sku : WebAppSku
          WorkerSize : WorkerSize
          WorkerCount : int
          AppInsightsName : ResourceRef option
          RunFromPackage : bool
          WebsiteNodeDefaultVersion : string option
          Settings : Map<string, string>
          Dependencies : ResourceName list }
        /// Gets the ARM expression path to the publishing password of this web app.
        member this.PublishingPassword = publishingPassword this.Name      
    type FunctionsConfig =
        { Name : ResourceName
          ServicePlanName : ResourceRef
          StorageAccountName : ResourceRef
          AppInsightsName : ResourceRef option
          WorkerRuntime : WorkerRuntime
          OperatingSystem : OS
          Settings : Map<string, string>
          Dependencies : ResourceName list }
        /// Gets the ARM expression path to the publishing password of this functions app.
        member this.PublishingPassword = publishingPassword this.Name
        /// Gets the ARM expression path to the storage account key of this functions app.
        member this.StorageAccountKey =
            Storage.buildKey this.StorageAccountName.ResourceName            
        /// Gets the ARM expression path to the app insights key of this functions app, if it exists.
        member this.AppInsightsKey =
            this.AppInsightsName
            |> Option.bind (fun r -> r.ResourceNameOpt)
            |> Option.map Helpers.AppInsights.instrumentationKey
    type AppInsightsConfig =
        { Name : ResourceName }
        /// Gets the ARM expression path to the instrumentation key of this App Insights instance.
        member this.InstrumentationKey = Helpers.AppInsights.instrumentationKey this.Name

    let tryCreateAppInsightsName (aiName:ResourceRef option) rootName =
        aiName
        |> Option.map(fun aiName ->
            aiName.ReplacePlaceholder (sprintf "%s-ai" rootName))

    type WebAppBuilder() =
        member __.Yield _ =
            { Name = ResourceName.Empty
              ServicePlanName = AutomaticPlaceholder
              AppInsightsName = Some AutomaticPlaceholder
              Sku = Sku.F1
              WorkerSize = Small
              WorkerCount = 1
              RunFromPackage = false
              WebsiteNodeDefaultVersion = None
              Settings = Map.empty
              Dependencies = [] }
        member __.Run(state:WebAppConfig) =
            { state with
                ServicePlanName =
                    state.ServicePlanName.ReplacePlaceholder (sprintf "%s-plan" state.Name.Value)
                AppInsightsName =
                    tryCreateAppInsightsName state.AppInsightsName state.Name.Value
            }
        [<CustomOperation "name">]
        /// Sets the name of the web app.
        member __.Name(state:WebAppConfig, name:string) = { state with Name = ResourceName name }
        [<CustomOperation "service_plan_name">]
        /// Sets the name of the service plan.
        member __.ServicePlanName(state:WebAppConfig, name:string) = { state with ServicePlanName = AutomaticallyCreated(ResourceName name) }
        [<CustomOperation "sku">]
        /// Sets the sku of the service plan.
        member __.Sku(state:WebAppConfig, sku) = { state with Sku = sku }
        [<CustomOperation "worker_size">]
        /// Sets the size of the service plan worker.
        member __.WorkerSize(state:WebAppConfig, workerSize) = { state with Sku = workerSize }
        [<CustomOperation "number_of_workers">]
        /// Sets the number of instances on the service plan.
        member __.NumberOfWorkers(state:WebAppConfig, workerCount) = { state with WorkerCount = workerCount }
        [<CustomOperation "app_insights_auto_name">]
        /// Sets the name of the automatically-created app insights instance.
        member __.UseAppInsights(state:WebAppConfig, name:string) = { state with AppInsightsName = Some (AutomaticallyCreated (ResourceName name)) }
        [<CustomOperation "app_insights_off">]
        /// Removes any automatic app insights creation, configuration and settings for this webapp.
        member __.DeactivateAppInsights(state:WebAppConfig) = { state with AppInsightsName = None }
        [<CustomOperation "app_insights_manual">]
        /// Instead of creating a new AI instance, configure this webapp to point to another AI instance that you are managing
        /// yourself.
        member __.LinkAppInsights(state:WebAppConfig, name) = { state with AppInsightsName = Some(External name) }
        [<CustomOperation "run_from_package">]
        /// Sets the web app to use "run from package" deployment capabilities.
        member __.RunFromPackage(state:WebAppConfig) = { state with RunFromPackage = true }
        [<CustomOperation "website_node_default_version">]
        /// Sets the node version of the web app.
        member __.NodeVersion(state:WebAppConfig, version) = { state with WebsiteNodeDefaultVersion = Some version }
        [<CustomOperation "setting">]
        /// Sets an app setting of the web app in the form "key" "value".
        member __.AddSetting(state:WebAppConfig, key, value) = { state with Settings = state.Settings.Add(key, value) }
        member this.AddSetting(state:WebAppConfig, key, ArmExpression value) = this.AddSetting(state, key, value)
        [<CustomOperation "depends_on">]
        /// Sets a dependency for the web app.
        member __.DependsOn(state:WebAppConfig, resourceName) =
            { state with Dependencies = resourceName :: state.Dependencies }
    type FunctionsBuilder() =
        member __.Yield _ =
            { Name = ResourceName.Empty
              ServicePlanName = AutomaticPlaceholder
              AppInsightsName = Some AutomaticPlaceholder
              StorageAccountName = AutomaticPlaceholder
              WorkerRuntime = DotNet
              OperatingSystem = Windows
              Settings = Map.empty
              Dependencies = [] }
        member __.Run (state:FunctionsConfig) =
            { state with
                ServicePlanName = state.ServicePlanName.ReplacePlaceholder (sprintf "%s-plan" state.Name.Value)
                StorageAccountName =
                    match state.StorageAccountName with
                    | AutomaticPlaceholder ->
                        state.Name
                        |> sanitiseStorage
                        |> sprintf "%sstorage"
                        |> ResourceName
                        |> AutomaticallyCreated
                    | AutomaticallyCreated _
                    | External _ ->
                        state.StorageAccountName
                AppInsightsName =
                    tryCreateAppInsightsName state.AppInsightsName state.Name.Value
            }
        [<CustomOperation "name">]
        /// Sets the name of the functions instance.
        member __.Name(state:FunctionsConfig, name) = { state with Name = ResourceName name }
        [<CustomOperation "service_plan_name">]
        /// Sets the name of the service plan hosting the function instance.
        member __.ServicePlanName(state:FunctionsConfig, name) = { state with ServicePlanName = AutomaticallyCreated(ResourceName name) }
        [<CustomOperation "storage_account_link">]
        /// Do not create an automatic storage account; instead, link to a storage account that is created outside of this Functions instance.
        member __.StorageAccountName(state:FunctionsConfig, name) = { state with StorageAccountName = External (ResourceName name) }
        [<CustomOperation "app_insights_auto_name">]
        /// Sets the name of the automatically-created app insights instance.
        member __.UseAppInsights(state:FunctionsConfig, name:string) = { state with AppInsightsName = Some (AutomaticallyCreated (ResourceName name)) }
        [<CustomOperation "app_insights_off">]
        /// Removes any automatic app insights creation, configuration and settings for this webapp.
        member __.DeactivateAppInsights(state:FunctionsConfig) = { state with AppInsightsName = None }
        [<CustomOperation "app_insights_manual">]
        /// Instead of creating a new AI instance, configure this webapp to point to another AI instance that you are managing
        /// yourself.
        member __.LinkAppInsights(state:FunctionsConfig, name) = { state with AppInsightsName = Some(External name) }
        [<CustomOperation "use_runtime">]
        /// Sets the runtime of the Functions host.
        member __.Runtime(state:FunctionsConfig, runtime) = { state with WorkerRuntime = runtime }
        [<CustomOperation "operating_system">]
        /// Sets the operating system of the Functions host.
        member __.OperatingSystem(state:FunctionsConfig, os) = { state with OperatingSystem = os }
        /// Sets an app setting of the web app in the form "key" "value".
        [<CustomOperation "setting">]
        member __.AddSetting(state:FunctionsConfig, key, value) = { state with Settings = state.Settings.Add(key, value) }
        member this.AddSetting(state:FunctionsConfig, key, ArmExpression value) = this.AddSetting(state, key, value)
        /// Sets a dependency for the web app.
        [<CustomOperation "depends_on">]
        member __.DependsOn(state:FunctionsConfig, resourceName) =
            { state with Dependencies = resourceName :: state.Dependencies }
    type AppInsightsBuilder() =
        member __.Yield _ =
            { Name = ResourceName.Empty }
        [<CustomOperation "name">]
        /// Sets the name of the App Insights instance.
        member __.Name(state:AppInsightsConfig, name) = { state with Name = ResourceName name }

    let appInsights = AppInsightsBuilder()
    let webApp = WebAppBuilder()
    let functions = FunctionsBuilder()

[<AutoOpen>]
module Extensions =
    open WebApp
    type WebAppBuilder with
        member this.DependsOn(state:WebAppConfig, functionsConfig:FunctionsConfig) =
            this.DependsOn(state, functionsConfig.Name)
        member this.DependsOn(state:WebAppConfig, storageAccountConfig:StorageAccountConfig) =
            this.DependsOn(state, storageAccountConfig.Name)
        member this.DependsOn(state:WebAppConfig, appInsightsConfig:AppInsightsConfig) =
            this.DependsOn(state, appInsightsConfig.Name)
    type FunctionsBuilder with
        member this.DependsOn(state:FunctionsConfig, storageAccountConfig:StorageAccountConfig) =
            this.DependsOn(state, storageAccountConfig.Name)
        member this.DependsOn(state:FunctionsConfig, webAppConfig:WebAppConfig) =
            this.DependsOn(state, webAppConfig.Name)
        member this.DependsOn(state:FunctionsConfig, appInsightsConfig:AppInsightsConfig) =
            this.DependsOn(state, appInsightsConfig.Name)

[<AutoOpen>]
module CosmosDb =
    type CosmosDbContainerConfig =
        { Name : ResourceName
          PartitionKey : string list * CosmosDbIndexKind
          Indexes : (string * (CosmosDbIndexDataType * CosmosDbIndexKind) list) list
          ExcludedPaths : string list }
    type CosmosDbConfig =
        { ServerName : ResourceName
          ServerConsistencyPolicy : ConsistencyPolicy
          ServerFailoverPolicy : FailoverPolicy
          DbName : ResourceName          
          DbThroughput : string
          Containers : CosmosDbContainerConfig list }    

    type CosmosDbContainer() =
        member __.Yield _ =
            { Name = ResourceName ""
              PartitionKey = [], Hash
              Indexes = []
              ExcludedPaths = [] }

        [<CustomOperation "name">]
        /// Sets the name of the container.
        member __.Name (state:CosmosDbContainerConfig, name) =
            { state with Name = ResourceName name }

        [<CustomOperation "partition_key">]
        /// Sets the partition key of the container.
        member __.PartitionKey (state:CosmosDbContainerConfig, partitions, indexKind) =
            { state with PartitionKey = partitions, indexKind }

        [<CustomOperation "add_index">]
        /// Adds an index to the container.
        member __.AddIndex (state:CosmosDbContainerConfig, path, indexes) =
            { state with Indexes = (path, indexes) :: state.Indexes }

        [<CustomOperation "exclude_path">]
        /// Excludes a path from the container index.
        member __.ExcludePath (state:CosmosDbContainerConfig, path) =
            { state with ExcludedPaths = path :: state.ExcludedPaths }
    type CosmosDbBuilder() =
        member __.Yield _ =
            { DbName = ResourceName "CosmosDatabase"
              ServerName = ResourceName "CosmosServer"            
              ServerConsistencyPolicy = Eventual
              ServerFailoverPolicy = NoFailover
              DbThroughput = "400"
              Containers = [] }
        [<CustomOperation "server_name">]
        /// Sets the name of the CosmosDB server.
        member __.ServerName(state:CosmosDbConfig, serverName) = { state with ServerName = serverName }
        member this.ServerName(state:CosmosDbConfig, serverName:string) = this.ServerName(state, ResourceName serverName)
        [<CustomOperation "name">]
        /// Sets the name of the database.
        member __.Name(state:CosmosDbConfig, name) = { state with DbName = name }
        member this.Name(state:CosmosDbConfig, name:string) = this.Name(state, ResourceName name)
        [<CustomOperation "consistency_policy">]
        /// Sets the consistency policy of the database.
        member __.ConsistencyPolicy(state:CosmosDbConfig, consistency:ConsistencyPolicy) = { state with ServerConsistencyPolicy = consistency }
        [<CustomOperation "failover_policy">]
        /// Sets the failover policy of the database.
        member __.FailoverPolicy(state:CosmosDbConfig, failoverPolicy:FailoverPolicy) = { state with ServerFailoverPolicy = failoverPolicy }
        [<CustomOperation "throughput">]
        /// Sets the throughput of the server.
        member __.Throughput(state:CosmosDbConfig, throughput) = { state with DbThroughput = throughput }
        member this.Throughput(state:CosmosDbConfig, throughput:int) = this.Throughput(state, string throughput)
        [<CustomOperation "add_containers">]
        /// Adds a list of containers to the database.
        member __.AddContainers(state:CosmosDbConfig, containers) =
            { state with Containers = state.Containers @ containers }

    open WebApp
    type WebAppBuilder with
        member this.DependsOn(state:WebAppConfig, cosmosDbConfig:CosmosDbConfig) =
            this.DependsOn(state, cosmosDbConfig.DbName)
    type FunctionsBuilder with
        member this.DependsOn(state:FunctionsConfig, cosmosDbConfig:CosmosDbConfig) =
            this.DependsOn(state, cosmosDbConfig.DbName)

    let cosmosDb = CosmosDbBuilder()
    let container = CosmosDbContainer()

[<AutoOpen>]
module SqlAzure =
    type Edition = Free | Basic | Standard of string | Premium of string
    module Sku =
        let ``Free`` = Free
        let ``Basic`` = Basic
        let ``S0`` = Standard "S0"
        let ``S1`` = Standard "S1"
        let ``S2`` = Standard "S2"
        let ``S3`` = Standard "S3"
        let ``S4`` = Standard "S4"
        let ``S6`` = Standard "S6"
        let ``S7`` = Standard "S7"
        let ``S9`` = Standard "S9"
        let ``S12`` =Standard "S12"
        let ``P1`` = Premium "P1"
        let ``P2`` = Premium "P2"
        let ``P4`` = Premium "P4"
        let ``P6`` = Premium "P6"
        let ``P11`` = Premium "P11"
        let ``P15`` = Premium "P15"
    type SqlAzureConfig =
        { ServerName : ResourceName
          AdministratorCredentials : {| UserName : string; Password : SecureParameter |}
          DbName : ResourceName
          DbEdition : Edition
          DbCollation : string
          Encryption : FeatureFlag
          FirewallRules : {| Name : string; Start : System.Net.IPAddress; End : System.Net.IPAddress |} list }
        /// Gets the ARM expression path to the FQDN of this VM.
        member this.FullyQualifiedDomainName =
            sprintf "[reference(concat('Microsoft.Sql/servers/', variables('%s'))).fullyQualifiedDomainName]" this.ServerName.Value
            |> ArmExpression
    type SqlBuilder() =
        let makeIp = System.Net.IPAddress.Parse
        member __.Yield _ =
            { ServerName = ResourceName ""
              AdministratorCredentials = {| UserName = ""; Password = SecureParameter "" |}
              DbName = ResourceName ""
              DbEdition = Free
              DbCollation = "SQL_Latin1_General_CP1_CI_AS"
              Encryption = Disabled
              FirewallRules = [] }
        member __.Run(state) =
            { state with
                AdministratorCredentials =
                    {| state.AdministratorCredentials with
                        Password = SecureParameter (sprintf "password-for-%s" state.ServerName.Value) |} }
        [<CustomOperation "server_name">]
        /// Sets the name of the SQL server.
        member __.ServerName(state:SqlAzureConfig, serverName) = { state with ServerName = serverName }
        member this.ServerName(state:SqlAzureConfig, serverName:string) = this.ServerName(state, ResourceName serverName)
        [<CustomOperation "db_name">]
        /// Sets the name of the database.
        member __.Name(state:SqlAzureConfig, name) = { state with DbName = name }
        member this.Name(state:SqlAzureConfig, name:string) = this.Name(state, ResourceName name)
        [<CustomOperation "sku">]
        /// Sets the sku of the database.
        member __.DatabaseEdition(state:SqlAzureConfig, edition:Edition) = { state with DbEdition = edition }
        [<CustomOperation "collation">]
        /// Sets the collation of the database.
        member __.Collation(state:SqlAzureConfig, collation:string) = { state with DbCollation = collation }
        [<CustomOperation "use_encryption">]
        /// Enables encryption of the database.
        member __.Encryption(state:SqlAzureConfig) = { state with Encryption = Enabled }
        [<CustomOperation "add_firewall_rule">]
        /// Adds a custom firewall rule given a name, start and end IP address range.
        member __.AddFirewallWall(state:SqlAzureConfig, name, startRange, endRange) =
            { state with
                FirewallRules =
                    {| Name = name
                       Start = makeIp startRange
                       End = makeIp endRange |}
                    :: state.FirewallRules }
        [<CustomOperation "enable_azure_firewall">]
        /// Adds a firewall rule that enables access to other Azure services.
        member this.UseAzureFirewall(state:SqlAzureConfig) =
            this.AddFirewallWall(state, "AllowAllMicrosoftAzureIps", "0.0.0.0", "0.0.0.0")
        [<CustomOperation "admin_username">]
        /// Sets the admin username of the server (note: the password is supplied as a securestring parameter to the generated ARM template).
        member __.AdminUsername(state:SqlAzureConfig, username) =
            { state with
                AdministratorCredentials =
                    {| state.AdministratorCredentials with
                        UserName = username |} }

    open WebApp
    type WebAppBuilder with
        member this.DependsOn(state:WebAppConfig, sqlDb:SqlAzureConfig) =
            this.DependsOn(state, sqlDb.ServerName)
    type FunctionsBuilder with
        member this.DependsOn(state:FunctionsConfig, sqlDb:SqlAzureConfig) =
            this.DependsOn(state, sqlDb.ServerName)            

    let sql = SqlBuilder()

[<AutoOpen>]
module VirtualMachine =
    module Size =
        let Basic_A0 = "Basic_A0"
        let Basic_A1 = "Basic_A1"
        let Basic_A2 = "Basic_A2"
        let Basic_A3 = "Basic_A3"
        let Basic_A4 = "Basic_A4"
        let Standard_A0 = "Standard_A0"
        let Standard_A1 = "Standard_A1"
        let Standard_A2 = "Standard_A2"
        let Standard_A3 = "Standard_A3"
        let Standard_A4 = "Standard_A4"
        let Standard_A5 = "Standard_A5"
        let Standard_A6 = "Standard_A6"
        let Standard_A7 = "Standard_A7"
        let Standard_A8 = "Standard_A8"
        let Standard_A9 = "Standard_A9"
        let Standard_A10 = "Standard_A10"
        let Standard_A11 = "Standard_A11"
        let Standard_A1_v2 = "Standard_A1_v2"
        let Standard_A2_v2 = "Standard_A2_v2"
        let Standard_A4_v2 = "Standard_A4_v2"
        let Standard_A8_v2 = "Standard_A8_v2"
        let Standard_A2m_v2 = "Standard_A2m_v2"
        let Standard_A4m_v2 = "Standard_A4m_v2"
        let Standard_A8m_v2 = "Standard_A8m_v2"
        let Standard_B1s = "Standard_B1s"
        let Standard_B1ms = "Standard_B1ms"
        let Standard_B2s = "Standard_B2s"
        let Standard_B2ms = "Standard_B2ms"
        let Standard_B4ms = "Standard_B4ms"
        let Standard_B8ms = "Standard_B8ms"
        let Standard_D1 = "Standard_D1"
        let Standard_D2 = "Standard_D2"
        let Standard_D3 = "Standard_D3"
        let Standard_D4 = "Standard_D4"
        let Standard_D11 = "Standard_D11"
        let Standard_D12 = "Standard_D12"
        let Standard_D13 = "Standard_D13"
        let Standard_D14 = "Standard_D14"
        let Standard_D1_v2 = "Standard_D1_v2"
        let Standard_D2_v2 = "Standard_D2_v2"
        let Standard_D3_v2 = "Standard_D3_v2"
        let Standard_D4_v2 = "Standard_D4_v2"
        let Standard_D5_v2 = "Standard_D5_v2"
        let Standard_D2_v3 = "Standard_D2_v3"
        let Standard_D4_v3 = "Standard_D4_v3"
        let Standard_D8_v3 = "Standard_D8_v3"
        let Standard_D16_v3 = "Standard_D16_v3"
        let Standard_D32_v3 = "Standard_D32_v3"
        let Standard_D64_v3 = "Standard_D64_v3"
        let Standard_D2s_v3 = "Standard_D2s_v3"
        let Standard_D4s_v3 = "Standard_D4s_v3"
        let Standard_D8s_v3 = "Standard_D8s_v3"
        let Standard_D16s_v3 = "Standard_D16s_v3"
        let Standard_D32s_v3 = "Standard_D32s_v3"
        let Standard_D64s_v3 = "Standard_D64s_v3"
        let Standard_D11_v2 = "Standard_D11_v2"
        let Standard_D12_v2 = "Standard_D12_v2"
        let Standard_D13_v2 = "Standard_D13_v2"
        let Standard_D14_v2 = "Standard_D14_v2"
        let Standard_D15_v2 = "Standard_D15_v2"
        let Standard_DS1 = "Standard_DS1"
        let Standard_DS2 = "Standard_DS2"
        let Standard_DS3 = "Standard_DS3"
        let Standard_DS4 = "Standard_DS4"
        let Standard_DS11 = "Standard_DS11"
        let Standard_DS12 = "Standard_DS12"
        let Standard_DS13 = "Standard_DS13"
        let Standard_DS14 = "Standard_DS14"
        let Standard_DS1_v2 = "Standard_DS1_v2"
        let Standard_DS2_v2 = "Standard_DS2_v2"
        let Standard_DS3_v2 = "Standard_DS3_v2"
        let Standard_DS4_v2 = "Standard_DS4_v2"
        let Standard_DS5_v2 = "Standard_DS5_v2"
        let Standard_DS11_v2 = "Standard_DS11_v2"
        let Standard_DS12_v2 = "Standard_DS12_v2"
        let Standard_DS13_v2 = "Standard_DS13_v2"
        let Standard_DS14_v2 = "Standard_DS14_v2"
        let Standard_DS15_v2 = "Standard_DS15_v2"
        let Standard_DS13_4_v2 = "Standard_DS13-4_v2"
        let Standard_DS13_2_v2 = "Standard_DS13-2_v2"
        let Standard_DS14_8_v2 = "Standard_DS14-8_v2"
        let Standard_DS14_4_v2 = "Standard_DS14-4_v2"
        let Standard_E2_v3_v3 = "Standard_E2_v3"
        let Standard_E4_v3 = "Standard_E4_v3"
        let Standard_E8_v3 = "Standard_E8_v3"
        let Standard_E16_v3 = "Standard_E16_v3"
        let Standard_E32_v3 = "Standard_E32_v3"
        let Standard_E64_v3 = "Standard_E64_v3"
        let Standard_E2s_v3 = "Standard_E2s_v3"
        let Standard_E4s_v3 = "Standard_E4s_v3"
        let Standard_E8s_v3 = "Standard_E8s_v3"
        let Standard_E16s_v3 = "Standard_E16s_v3"
        let Standard_E32s_v3 = "Standard_E32s_v3"
        let Standard_E64s_v3 = "Standard_E64s_v3"
        let Standard_E32_16_v3 = "Standard_E32-16_v3"
        let Standard_E32_8s_v3 = "Standard_E32-8s_v3"
        let Standard_E64_32s_v3 = "Standard_E64-32s_v3"
        let Standard_E64_16s_v3 = "Standard_E64-16s_v3"
        let Standard_F1 = "Standard_F1"
        let Standard_F2 = "Standard_F2"
        let Standard_F4 = "Standard_F4"
        let Standard_F8 = "Standard_F8"
        let Standard_F16 = "Standard_F16"
        let Standard_F1s = "Standard_F1s"
        let Standard_F2s = "Standard_F2s"
        let Standard_F4s = "Standard_F4s"
        let Standard_F8s = "Standard_F8s"
        let Standard_F16s = "Standard_F16s"
        let Standard_F2s_v2 = "Standard_F2s_v2"
        let Standard_F4s_v2 = "Standard_F4s_v2"
        let Standard_F8s_v2 = "Standard_F8s_v2"
        let Standard_F16s_v2 = "Standard_F16s_v2"
        let Standard_F32s_v2 = "Standard_F32s_v2"
        let Standard_F64s_v2 = "Standard_F64s_v2"
        let Standard_F72s_v2 = "Standard_F72s_v2"
        let Standard_G1 = "Standard_G1"
        let Standard_G2 = "Standard_G2"
        let Standard_G3 = "Standard_G3"
        let Standard_G4 = "Standard_G4"
        let Standard_G5 = "Standard_G5"
        let Standard_GS1 = "Standard_GS1"
        let Standard_GS2 = "Standard_GS2"
        let Standard_GS3 = "Standard_GS3"
        let Standard_GS4 = "Standard_GS4"
        let Standard_GS5 = "Standard_GS5"
        let Standard_GS4_8 = "Standard_GS4-8"
        let Standard_GS4_4 = "Standard_GS4-4"
        let Standard_GS5_16 = "Standard_GS5-16"
        let Standard_GS5_8 = "Standard_GS5-8"
        let Standard_H8 = "Standard_H8"
        let Standard_H16 = "Standard_H16"
        let Standard_H8m = "Standard_H8m"
        let Standard_H16m = "Standard_H16m"
        let Standard_H16r = "Standard_H16r"
        let Standard_H16mr = "Standard_H16mr"
        let Standard_L4s = "Standard_L4s"
        let Standard_L8s = "Standard_L8s"
        let Standard_L16s = "Standard_L16s"
        let Standard_L32s = "Standard_L32s"
        let Standard_M64s = "Standard_M64s"
        let Standard_M64ms = "Standard_M64ms"
        let Standard_M128s = "Standard_M128s"
        let Standard_M128ms = "Standard_M128ms"
        let Standard_M64_32ms = "Standard_M64-32ms"
        let Standard_M64_16ms = "Standard_M64-16ms"
        let Standard_M128_64ms = "Standard_M128-64ms"
        let Standard_M128_32ms = "Standard_M128-32ms"
        let Standard_NC6 = "Standard_NC6"
        let Standard_NC12 = "Standard_NC12"
        let Standard_NC24 = "Standard_NC24"
        let Standard_NC24r = "Standard_NC24r"
        let Standard_NC6s_v2 = "Standard_NC6s_v2"
        let Standard_NC12s_v2 = "Standard_NC12s_v2"
        let Standard_NC24s_v2 = "Standard_NC24s_v2"
        let Standard_NC24rs_v2 = "Standard_NC24rs_v2"
        let Standard_NC6s_v3 = "Standard_NC6s_v3"
        let Standard_NC12s_v3 = "Standard_NC12s_v3"
        let Standard_NC24s_v3 = "Standard_NC24s_v3"
        let Standard_NC24rs_v3 = "Standard_NC24rs_v3"
        let Standard_ND6s = "Standard_ND6s"
        let Standard_ND12s = "Standard_ND12s"
        let Standard_ND24s = "Standard_ND24s"
        let Standard_ND24rs = "Standard_ND24rs"
        let Standard_NV6 = "Standard_NV6"
        let Standard_NV12 = "Standard_NV12"
        let Standard_NV24 = "Standard_NV24"
    module CommonImages =
        let CentOS_75 = {| Offer = "CentOS"; Publisher = "OpenLogic"; Sku = "7.5" |}
        let CoreOS_Stable = {| Offer = "CoreOS"; Publisher = "CoreOS"; Sku = "Stable" |}
        let debian_10 = {| Offer = "debian-10"; Publisher = "Debian"; Sku = "10" |}
        let openSUSE_423 = {| Offer = "openSUSE-Leap"; Publisher = "SUSE"; Sku = "42.3" |}
        let RHEL_7RAW = {| Offer = "RHEL"; Publisher = "RedHat"; Sku = "7-RAW" |}
        let SLES_15 = {| Offer = "SLES"; Publisher = "SUSE"; Sku = "15" |}
        let UbuntuServer_1804LTS = {| Offer = "UbuntuServer"; Publisher = "Canonical"; Sku = "18.04-LTS" |}
        let WindowsServer_2019Datacenter = {| Offer = "WindowsServer"; Publisher = "MicrosoftWindowsServer"; Sku = "2019-Datacenter" |}
        let WindowsServer_2016Datacenter = {| Offer = "WindowsServer"; Publisher = "MicrosoftWindowsServer"; Sku = "2016-Datacenter" |}
        let WindowsServer_2012R2Datacenter = {| Offer = "WindowsServer"; Publisher = "MicrosoftWindowsServer"; Sku = "2012-R2-Datacenter" |}
        let WindowsServer_2012Datacenter = {| Offer = "WindowsServer"; Publisher = "MicrosoftWindowsServer"; Sku = "2012-Datacenter" |}
        let WindowsServer_2008R2SP1 = {| Offer = "WindowsServer"; Publisher = "MicrosoftWindowsServer"; Sku = "2008-R2-SP1" |}
    let makeName (vmName:ResourceName) elementType = sprintf "%s-%s" vmName.Value elementType
    let makeResourceName vmName = makeName vmName >> ResourceName
    type VmConfig =
        { Name : ResourceName
          DiagnosticsStorageAccount : ResourceRef option
          
          Username : string
          Image : {| Publisher : string; Offer : string; Sku : string |}
          Size : string
          OsDisk : DiskInfo
          DataDisks : DiskInfo list
          
          DomainNamePrefix : string option
          AddressPrefix : string
          SubnetPrefix : string }

        member this.NicName = makeResourceName this.Name "nic"
        member this.VnetName = makeResourceName this.Name "vnet"
        member this.SubnetName = makeResourceName this.Name "subnet"
        member this.IpName = makeResourceName this.Name "ip"
        member this.Hostname = sprintf "[reference('%s').dnsSettings.fqdn]" this.IpName.Value |> ArmExpression
    type VirtualMachineBuilder() =
        member __.Yield _ =
            { Name = ResourceName.Empty
              DiagnosticsStorageAccount = None
              Size = Size.Basic_A0
              Username = "admin"
              Image = CommonImages.WindowsServer_2012Datacenter
              DataDisks = [ ]
              DomainNamePrefix = None
              OsDisk = { Size = 128; DiskType = Standard_LRS }
              AddressPrefix = "10.0.0.0/16"
              SubnetPrefix = "10.0.0.0/24" }

        member __.Run (state:VmConfig) =
            { state with
                DiagnosticsStorageAccount =
                    state.DiagnosticsStorageAccount
                    |> Option.map(fun account ->
                        match account with
                        | AutomaticPlaceholder ->
                            state.Name
                            |> sanitiseStorage
                            |> sprintf "%sstorage"
                            |> ResourceName
                            |> AutomaticallyCreated
                        | External _
                        | AutomaticallyCreated _ ->
                            account)
                DataDisks = match state.DataDisks with [] -> [ { Size = 1024; DiskType = Standard_LRS } ] | other -> other
            }

        [<CustomOperation "name">]
        /// Sets the name of the VM.
        member __.Name(state:VmConfig, name) = { state with Name = name }
        member this.Name(state:VmConfig, name) = this.Name(state, ResourceName name)
        [<CustomOperation "diagnostics_support">]
        /// Turns on diagnostics support using an automatically created storage account.
        member __.StorageAccountName(state:VmConfig) = { state with DiagnosticsStorageAccount = Some AutomaticPlaceholder }
        [<CustomOperation "diagnostics_support">]
        /// Turns on diagnostics support using an externally managed storage account.
        member __.StorageAccountName(state:VmConfig, name) = { state with DiagnosticsStorageAccount = Some (External name) }
        [<CustomOperation "vm_size">]
        /// Sets the size of the VM.
        member __.VmSize(state:VmConfig, size) = { state with Size = size }
        [<CustomOperation "username">]
        /// Sets the admin username of the VM (note: the password is supplied as a securestring parameter to the generated ARM template).
        member __.Username(state:VmConfig, username) = { state with Username = username }
        [<CustomOperation "operating_system">]
        /// Sets the operating system of the VM. A set of samples is provided in the `CommonImages` module.
        member __.ConfigureOs(state:VmConfig, image) =
            { state with Image = image }
        /// Sets the operating system of the VM.
        member __.ConfigureOs(state:VmConfig, (offer, publisher, sku)) =
            { state with Image = {| Offer = offer; Publisher = publisher; Sku = sku |} }
        [<CustomOperation "os_disk">]
        /// Sets the size and type of the OS disk for the VM.
        member __.OsDisk(state:VmConfig, size, diskType) =
            { state with OsDisk = { Size = size; DiskType = diskType } }
        [<CustomOperation "add_disk">]
        /// Adds a data disk to the VM with a specific size and type.
        member __.AddDisk(state:VmConfig, size, diskType) = { state with DataDisks = { Size = size; DiskType = diskType } :: state.DataDisks }
        [<CustomOperation "add_ssd_disk">]
        /// Adds a SSD data disk to the VM with a specific size.
        member this.AddSsd(state:VmConfig, size) = this.AddDisk(state, size, StandardSSD_LRS)
        [<CustomOperation "add_slow_disk">]
        /// Adds a conventional (non-SSD) data disk to the VM with a specific size.
        member this.AddSlowDisk(state:VmConfig, size) = this.AddDisk(state, size, Standard_LRS)
        [<CustomOperation "domain_name_prefix">]
        /// Sets the prefix for the domain name of the VM.
        member __.DomainNamePrefix(state:VmConfig, prefix) = { state with DomainNamePrefix = prefix }
        [<CustomOperation "address_prefix">]
        /// Sets the IP address prefix of the VM.
        member __.AddressPrefix(state:VmConfig, prefix) = { state with AddressPrefix = prefix }
        [<CustomOperation "subnet_prefix">]
        /// Sets the subnet prefix of the VM.
        member __.SubnetPrefix(state:VmConfig, prefix) = { state with SubnetPrefix = prefix }        

    let vm = VirtualMachineBuilder()

[<AutoOpen>]
module Search =
    module Sku =
        type HostingMode = Default | HighDensity
        /// The SKU of the search service you want to create. E.g. free or standard.
        type SearchSku =
            | FreeSearch
            | BasicSearch
            | StandardSearch
            | StandardSearch2
            | StandardSearch3 of HostingMode
            | StorageOptimisedSearchL1
            | StorageOptimisedSearchL2

    type SearchConfig =
        { Name : ResourceName
          Sku : Sku.SearchSku
          Replicas : int
          Partitions : int }
        /// Gets an ARM expression for the admin key of the search instance.
        member this.AdminKey =
            sprintf "[listAdminKeys('Microsoft.Search/searchServices/%s', '2015-08-19').primaryKey]" this.Name.Value
            |> ArmExpression
        /// Gets an ARM expression for the query key of the search instance.
        member this.QueryKey =
            sprintf "[listQueryKeys('Microsoft.Search/searchServices/%s', '2015-08-19').value[0].key]" this.Name.Value
            |> ArmExpression

    type SearchBuilder() =
        member __.Yield _ =
            { Name = ResourceName.Empty
              Sku = Sku.StandardSearch
              Replicas = 1
              Partitions = 1 }        
        member __.Run(state:SearchConfig) =
            { state with Name = state.Name |> sanitiseSearch |> ResourceName }
        [<CustomOperation "name">]
        /// Sets the name of the Azure Search instance.
        member __.Name(state:SearchConfig, name) = { state with Name = name }
        member this.Name(state:SearchConfig, name) = this.Name(state, ResourceName name)
        [<CustomOperation "sku">]
        /// Sets the sku of the Azure Search instance.
        member __.Sku(state:SearchConfig, sku) = { state with Sku = sku }
        [<CustomOperation "replicas">]
        /// Sets the replica count of the Azure Search instance.
        member __.ReplicaCount(state:SearchConfig, replicas:int) = { state with Replicas = replicas }
        [<CustomOperation "partitions">]
        /// Sets the number of partitions of the Azure Search instance.
        member __.PartitionCount(state:SearchConfig, partitions:int) = { state with Partitions = partitions }

    open WebApp
    type WebAppBuilder with
        member this.DependsOn(state:WebAppConfig, search:SearchConfig) =
            this.DependsOn(state, search.Name)
    type FunctionsBuilder with
        member this.DependsOn(state:FunctionsConfig, search:SearchConfig) =
            this.DependsOn(state, search.Name)
    

    let search = SearchBuilder()

[<AutoOpen>]
module ArmBuilder =
    open Internal.VM
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
              Location = Helpers.Locations.WestEurope }

        member __.Run (state:ArmConfig) =
            state.Location, {
            Parameters = state.Parameters |> Set.toList
            Outputs = state.Outputs
            Resources =
                state.Resources
                |> List.collect(function
                | :? StorageAccountConfig as sac ->
                    [ StorageAccount { Location = state.Location; Name = sac.Name; Sku = sac.Sku } ]
                | :? WebAppConfig as wac -> [
                    let webApp =
                        { Name = wac.Name
                          Location = state.Location
                          ServerFarm = wac.ServicePlanName.ResourceName
                          AppSettings = [
                            yield! Map.toList wac.Settings
                            if wac.RunFromPackage then yield WebApp.AppSettings.RunFromPackage

                            match wac.WebsiteNodeDefaultVersion with
                            | Some v -> yield WebApp.AppSettings.WebsiteNodeDefaultVersion v
                            | None -> ()

                            match wac.AppInsightsName with
                            | Some (External resourceName)
                            | Some (AutomaticallyCreated resourceName) ->
                                yield "APPINSIGHTS_INSTRUMENTATIONKEY", Helpers.AppInsights.instrumentationKey resourceName |> eval
                                yield "APPINSIGHTS_PROFILERFEATURE_VERSION", "1.0.0"
                                yield "APPINSIGHTS_SNAPSHOTFEATURE_VERSION", "1.0.0"
                                yield "ApplicationInsightsAgent_EXTENSION_VERSION", "~2"
                                yield "DiagnosticServices_EXTENSION_VERSION", "~3"
                                yield "InstrumentationEngine_EXTENSION_VERSION", "~1"
                                yield "SnapshotDebugger_EXTENSION_VERSION", "~1"
                                yield "XDT_MicrosoftApplicationInsights_BaseExtensions", "~1"
                                yield "XDT_MicrosoftApplicationInsights_Mode", "recommended"
                            | Some AutomaticPlaceholder
                            | None ->
                                ()
                          ]

                          Extensions =
                            match wac.AppInsightsName with
                            | Some _ -> Set [ AppInsightsExtension ]
                            | None -> Set.empty
                          Kind = None
                          Dependencies = [
                            yield wac.ServicePlanName.ResourceName
                            yield! wac.Dependencies
                            match wac.AppInsightsName with
                            | Some (AutomaticallyCreated appInsightsName)
                            | Some (External appInsightsName) ->
                                yield appInsightsName
                            | Some AutomaticPlaceholder
                            | None ->
                                ()
                          ]
                        }
                    match wac.ServicePlanName with
                    | AutomaticallyCreated resourceName ->
                        let serverFarm =
                            { Location = state.Location
                              Name = resourceName
                              Sku =
                                match wac.Sku with
                                | WebApp.Free ->
                                    "F1"
                                | Shared ->
                                    "D1"
                                | WebApp.Basic sku
                                | WebApp.Standard sku
                                | WebApp.Premium sku
                                | PremiumV2 sku
                                | Isolated sku ->
                                    sku
                              WorkerSize =
                                match wac.WorkerSize with
                                | Small -> "0"
                                | Medium -> "1"
                                | Large -> "2"
                              IsDynamic = false
                              Tier =
                                match wac.Sku with
                                | WebApp.Free -> "Free"
                                | Shared -> "Shared"
                                | WebApp.Basic _ -> "Basic"
                                | WebApp.Standard _ -> "Standard"
                                | WebApp.Premium _ -> "Premium"
                                | WebApp.PremiumV2 _ -> "PremiumV2"
                                | WebApp.Isolated _ -> "Isolated"
                              WorkerCount = wac.WorkerCount }

                        yield ServerFarm serverFarm
                    | External _
                    | AutomaticPlaceholder ->
                        ()
                    yield WebApp webApp
                    match wac.AppInsightsName with
                    | Some (AutomaticallyCreated resourceName) ->
                        yield { Name = resourceName
                                Location = state.Location
                                LinkedWebsite = Some wac.Name }
                              |> AppInsights
                    | Some AutomaticPlaceholder
                    | Some (External _)
                    | None ->
                        () ]
                | :? FunctionsConfig as fns -> [
                    let webApp =
                        { Name = fns.Name
                          ServerFarm = fns.ServicePlanName.ResourceName
                          Location = state.Location
                          AppSettings = [
                            yield! Map.toList fns.Settings
                            yield "FUNCTIONS_WORKER_RUNTIME", string fns.WorkerRuntime
                            yield "WEBSITE_NODE_DEFAULT_VERSION", "10.14.1"
                            yield "FUNCTIONS_EXTENSION_VERSION", "~2"
                            yield "AzureWebJobsStorage", Storage.buildKey fns.StorageAccountName.ResourceName |> eval
                            yield "AzureWebJobsDashboard", Storage.buildKey fns.StorageAccountName.ResourceName |> eval

                            match fns.AppInsightsName with
                            | Some (External resourceName)
                            | Some (AutomaticallyCreated resourceName) ->
                                yield "APPINSIGHTS_INSTRUMENTATIONKEY", Helpers.AppInsights.instrumentationKey resourceName |> eval
                            | Some AutomaticPlaceholder
                            | None -> ()

                            if fns.OperatingSystem = Windows then
                                yield "WEBSITE_CONTENTAZUREFILECONNECTIONSTRING", Storage.buildKey fns.StorageAccountName.ResourceName |> eval
                                yield "WEBSITE_CONTENTSHARE", fns.Name.Value.ToLower()
                          ]

                          Kind =
                            match fns.OperatingSystem with
                            | Windows -> Some "functionapp"
                            | Linux -> Some "functionapp,linux"
                          Extensions = Set.empty
                          Dependencies = [
                            yield! fns.Dependencies
                            match fns.AppInsightsName with
                            | Some (AutomaticallyCreated appInsightsName)
                            | Some (External appInsightsName) ->
                                yield appInsightsName
                            | Some AutomaticPlaceholder
                            | None ->
                                ()
                            yield fns.ServicePlanName.ResourceName
                            yield fns.StorageAccountName.ResourceName
                          ]
                        }                    

                    match fns.ServicePlanName with
                    | AutomaticallyCreated resourceName ->
                        let serverFarm =
                            { Location = state.Location
                              Name = resourceName
                              Sku = "Y1"
                              WorkerSize = "Y1"
                              IsDynamic = true
                              Tier = "Dynamic"
                              WorkerCount = 0 }
                        yield ServerFarm serverFarm
                    | External _
                    | AutomaticPlaceholder ->
                        ()

                    yield WebApp webApp
                    
                    match fns.StorageAccountName with
                    | AutomaticallyCreated resourceName ->
                        yield
                            { StorageAccount.Name = resourceName 
                              Location = state.Location
                              Sku = Storage.Sku.StandardLRS }
                            |> StorageAccount
                    | AutomaticPlaceholder | External _ ->
                        ()

                    match fns.AppInsightsName with
                    | Some (AutomaticallyCreated resourceName) ->
                        yield
                            { Name = resourceName
                              Location = state.Location
                              LinkedWebsite = Some fns.Name }
                            |> AppInsights
                    | Some (External _)
                    | Some AutomaticPlaceholder
                    | None ->
                        () ]
                | :? CosmosDbConfig as cosmos -> [
                    yield
                        { Name = cosmos.ServerName
                          Location = state.Location
                          ConsistencyPolicy = cosmos.ServerConsistencyPolicy
                          WriteModel = cosmos.ServerFailoverPolicy }
                        |> CosmosAccount
                    yield
                        { Name = cosmos.DbName
                          Account = cosmos.ServerName
                          Throughput = cosmos.DbThroughput }
                        |> CosmosSqlDb
                    yield!
                        cosmos.Containers
                        |> List.map(fun c ->
                            { Name = c.Name
                              Account = cosmos.ServerName
                              Database = cosmos.DbName
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
                            } |> CosmosContainer) ]
                | :? SqlAzureConfig as sql -> [
                    { ServerName = sql.ServerName
                      Location = state.Location
                      Credentials =
                        {| Username = sql.AdministratorCredentials.UserName
                           Password = sql.AdministratorCredentials.Password |}
                      DbName = sql.DbName
                      DbEdition =
                        match sql.DbEdition with
                        | Basic -> "Basic"
                        | Free -> "Free"
                        | Standard _ -> "Standard"
                        | Premium _ -> "Premium"
                      DbObjective =
                        match sql.DbEdition with
                        | Basic -> "Basic"
                        | Free -> "Free"
                        | Standard s -> s
                        | Premium p -> p
                      DbCollation = sql.DbCollation
                      TransparentDataEncryption = sql.Encryption
                      FirewallRules = sql.FirewallRules
                    } |> SqlServer ]
                | :? VmConfig as vm -> [
                    match vm.DiagnosticsStorageAccount with
                    | Some (AutomaticallyCreated account) ->
                        yield StorageAccount
                            { StorageAccount.Name = account
                              Location = state.Location
                              Sku = Storage.Sku.StandardLRS }
                    | Some AutomaticPlaceholder
                    | Some (External _)
                    | None ->
                        ()
                    yield Vm
                        { Name = vm.Name
                          Location = state.Location
                          StorageAccount =
                            vm.DiagnosticsStorageAccount
                            |> Option.bind(function
                                | AutomaticPlaceholder -> None
                                | AutomaticallyCreated r -> Some r
                                | External r -> Some r)
                          NetworkInterfaceName = vm.NicName
                          Size = vm.Size
                          Credentials = {| Username = vm.Username; Password = SecureParameter (sprintf "password-for-%s" vm.Name.Value) |}
                          Image = vm.Image
                          OsDisk = vm.OsDisk
                          DataDisks = vm.DataDisks }
                    yield Nic
                        { Name = vm.NicName
                          Location = state.Location
                          IpConfigs = [
                            {| SubnetName = vm.SubnetName
                               PublicIpName = vm.IpName |} ]
                          VirtualNetwork = vm.VnetName }
                    yield Vnet
                        { Name = vm.VnetName
                          Location = state.Location
                          AddressSpacePrefixes = [ vm.AddressPrefix ]
                          Subnets = [
                              {| Name = vm.SubnetName
                                 Prefix = vm.SubnetPrefix |}
                          ] }
                    yield Ip
                        { Name = vm.IpName
                          Location = state.Location
                          DomainNameLabel = vm.DomainNamePrefix }
                    ]
                | :? SearchConfig as search -> [
                    AzureSearch
                        { Name = search.Name
                          Location = state.Location
                          Sku =
                            match search.Sku with
                            | Sku.FreeSearch -> "free"
                            | Sku.BasicSearch -> "basic"
                            | Sku.StandardSearch -> "standard"
                            | Sku.StandardSearch2 -> "standard2"
                            | Sku.StandardSearch3 _ -> "standard3"
                            | Sku.StorageOptimisedSearchL1 -> "storage_optimized_l1"
                            | Sku.StorageOptimisedSearchL2 -> "storage_optimized_l2"
                          ReplicaCount = search.Replicas
                          PartitionCount = search.Partitions
                          HostingMode = 
                            match search.Sku with
                            | Sku.StandardSearch3 Sku.HighDensity -> "highDensity"
                            | _ -> "default"
                          }
                    ]
                | :? AppInsightsConfig as aiConfig -> [
                    { Name = aiConfig.Name
                      Location = state.Location
                      LinkedWebsite = None }
                    |> AppInsights ]
                | r ->
                    failwithf "Sorry, I don't know how to handle this resource of type '%s'." (r.GetType().FullName))
                |> List.distinctBy(fun r -> r.ResourceName)
        }

        /// Creates an output; use the `output` keyword.
        [<CustomOperation "output">]
        member __.Output (state, outputName, outputValue) : ArmConfig = { state with Outputs = (outputName, outputValue) :: state.Outputs }
        member __.Output (state, outputName, ArmExpression expr) : ArmConfig = { state with Outputs = (outputName, expr) :: state.Outputs }
        member this.Output (state:ArmConfig, outputName:string, (ResourceName outputValue)) = this.Output(state, outputName, outputValue)
        member this.Output (state:ArmConfig, outputName:string, outputValue:ArmExpression option) =
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