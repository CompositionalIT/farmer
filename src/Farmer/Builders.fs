namespace Farmer

open Farmer.Internal

[<AutoOpen>]
module Helpers =
    module AppInsights =
        let instrumentationKey (ResourceName accountName) =
            sprintf "[reference('Microsoft.Insights/components/%s').InstrumentationKey]" accountName
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
    let sanitiseStorage (resourceName:ResourceName) =
        resourceName.Value.ToLower()
        |> Seq.filter System.Char.IsLetterOrDigit
        |> Seq.truncate 16
        |> Seq.toArray
        |> System.String



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

    type StorageAccountConfig =
        { /// The name of the storage account.
          Name : ResourceName
          /// The sku of the storage account.
          Sku : string }
        member this.Key = buildKey this.Name
    type StorageAccountBuilder() =
        member __.Yield _ = { Name = ResourceName.Empty; Sku = Sku.StandardLRS }
        [<CustomOperation "name">]
        member __.Name(state:StorageAccountConfig, name) = { state with Name = name }
        member this.Name(state:StorageAccountConfig, name) = this.Name(state, ResourceName name)
        [<CustomOperation "sku">]
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
    type WebAppConfig =
        { Name : ResourceName
          ServicePlanName : ResourceName
          Sku : WebAppSku
          WorkerSize : WorkerSize
          WorkerCount : int
          AppInsightsName : ResourceName option
          RunFromPackage : bool
          WebsiteNodeDefaultVersion : string option
          Settings : Map<string, string>
          Dependencies : ResourceName list }
        member this.PublishingPassword = publishingPassword this.Name
       
    type FunctionsConfig =
        { Name : ResourceName
          ServicePlanName : ResourceName
          StorageAccountName : ResourceName
          AutoCreateStorageAccount : bool
          AppInsightsName : ResourceName option
          WorkerRuntime : WorkerRuntime
          OperatingSystem : OS }
        member this.PublishingPassword = publishingPassword this.Name
        member this.StorageAccountKey = Storage.buildKey this.StorageAccountName
        member this.AppInsightsKey = this.AppInsightsName |> Option.map Helpers.AppInsights.instrumentationKey

    type WebAppBuilder() =
        member __.Yield _ =
            { Name = ResourceName.Empty
              ServicePlanName = ResourceName.Empty
              AppInsightsName = Some ResourceName.Empty
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
                    state.ServicePlanName.IfEmpty (sprintf "%s-plan" state.Name.Value)
                AppInsightsName =
                    state.AppInsightsName
                    |> Option.map (fun name -> name.IfEmpty (sprintf "%s-ai" state.Name.Value))
            }

        /// Sets the name of the web app; use the `name` keyword.
        [<CustomOperation "name">]
        member __.Name(state:WebAppConfig, name) = { state with Name = name }
        member this.Name(state:WebAppConfig, name:string) = this.Name(state, ResourceName name)
        /// Sets the name of service plan of the web app; use the `service_plan_name` keyword.
        [<CustomOperation "service_plan_name">]
        member __.ServicePlanName(state:WebAppConfig, name) = { state with ServicePlanName = name }
        member this.ServicePlanName(state:WebAppConfig, name:string) = this.ServicePlanName(state, ResourceName name)
        /// Sets the sku of the web app; use the `sku` keyword.
        [<CustomOperation "sku">]
        member __.Sku(state:WebAppConfig, sku) = { state with Sku = sku }
        [<CustomOperation "worker_size">]
        member __.WorkerSize(state:WebAppConfig, workerSize) = { state with Sku = workerSize }
        [<CustomOperation "number_of_workers">]
        member __.NumberOfWorkers(state:WebAppConfig, workerCount) = { state with WorkerCount = workerCount }
        /// Creates a fully-configured application insights resource linked to this web app; use the `use_app_insights` keyword.
        [<CustomOperation "app_insights_name">]
        member __.UseAppInsights(state:WebAppConfig, name) = { state with AppInsightsName = Some name }
        member this.UseAppInsights(state:WebAppConfig, name:string) = this.UseAppInsights(state, ResourceName name)
        /// Sets the web app to use run from package mode; use the `run_from_package` keyword.
        [<CustomOperation "no_app_insights">]
        member __.DeactivateAppInsights(state:FunctionsConfig) = { state with AppInsightsName = None }
        [<CustomOperation "run_from_package">]
        member __.RunFromPackage(state:WebAppConfig) = { state with RunFromPackage = true }
        /// Sets the node version of the web app; use the `website_node_default_version` keyword.
        [<CustomOperation "website_node_default_version">]
        member __.NodeVersion(state:WebAppConfig, version) = { state with WebsiteNodeDefaultVersion = Some version }
        /// Sets an app setting of the web app; use the `setting` keyword.
        [<CustomOperation "setting">]
        member __.AddSetting(state:WebAppConfig, key, value) = { state with Settings = state.Settings.Add(key, value) }
        /// Sets a dependency for the web app; use the `depends_on` keyword.
        [<CustomOperation "depends_on">]
        member __.DependsOn(state:WebAppConfig, resourceName) =
            { state with Dependencies = resourceName :: state.Dependencies }
    
    type FunctionsBuilder() =
        member __.Yield _ =
            { Name = ResourceName.Empty
              ServicePlanName = ResourceName.Empty
              AppInsightsName = Some ResourceName.Empty
              StorageAccountName = ResourceName.Empty
              AutoCreateStorageAccount = true
              WorkerRuntime = DotNet
              OperatingSystem = Windows }
        member __.Run (state:FunctionsConfig) =
            { state with
                ServicePlanName = state.ServicePlanName.IfEmpty (sprintf "%s-plan" state.Name.Value)
                StorageAccountName = state.Name |> sanitiseStorage |> sprintf "%sstorage" |> state.StorageAccountName.IfEmpty
                AppInsightsName =
                    state.AppInsightsName
                    |> Option.map (fun name -> name.IfEmpty (sprintf "%s-ai" state.Name.Value))
            }
        [<CustomOperation "name">]
        member __.Name(state:FunctionsConfig, name) = { state with Name = ResourceName name }
        [<CustomOperation "service_plan_name">]
        member __.ServicePlanName(state:FunctionsConfig, name) = { state with ServicePlanName = ResourceName name }
        [<CustomOperation "storage_account_name">]
        member __.StorageAccountName(state:FunctionsConfig, name) = { state with StorageAccountName = ResourceName name; AutoCreateStorageAccount = false }
        [<CustomOperation "app_insights_name">]
        member __.AppInsightsName(state:FunctionsConfig, name) = { state with AppInsightsName = Some (ResourceName name) }
        [<CustomOperation "no_app_insights">]
        member __.DeactivateAppInsights(state:FunctionsConfig) = { state with AppInsightsName = None }
        [<CustomOperation "use_runtime">]
        member __.Runtime(state:FunctionsConfig, runtime) = { state with WorkerRuntime = runtime }
        [<CustomOperation "operating_system">]
        member __.OperatingSystem(state:FunctionsConfig, os) = { state with OperatingSystem = os }

    let webApp = WebAppBuilder()
    let functions = FunctionsBuilder()

[<AutoOpen>]
module Extensions =
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
        member __.Name (state:CosmosDbContainerConfig, name) =
            { state with Name = ResourceName name }

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
            { DbName = ResourceName "CosmosDatabase"
              ServerName = ResourceName "CosmosServer"            
              ServerConsistencyPolicy = Eventual
              ServerFailoverPolicy = NoFailover
              DbThroughput = "400"
              Containers = [] }
        /// Sets the name of cosmos db server; use the `server_name` keyword.
        [<CustomOperation "server_name">]
        member __.ServerName(state:CosmosDbConfig, serverName) = { state with ServerName = serverName }
        member this.ServerName(state:CosmosDbConfig, serverName:string) = this.ServerName(state, ResourceName serverName)
        /// Sets the name of the web app; use the `name` keyword.
        [<CustomOperation "name">]
        member __.Name(state:CosmosDbConfig, name) = { state with DbName = name }
        member this.Name(state:CosmosDbConfig, name:string) = this.Name(state, ResourceName name)
        /// Sets the sku of the web app; use the `sku` keyword.
        [<CustomOperation "consistency_policy">]
        member __.ConsistencyPolicy(state:CosmosDbConfig, consistency:ConsistencyPolicy) = { state with ServerConsistencyPolicy = consistency }
        [<CustomOperation "failover_policy">]
        member __.FailoverPolicy(state:CosmosDbConfig, failoverPolicy:FailoverPolicy) = { state with ServerFailoverPolicy = failoverPolicy }
        [<CustomOperation "throughput">]
        member __.Throughput(state:CosmosDbConfig, throughput) = { state with DbThroughput = throughput }
        member this.Throughput(state:CosmosDbConfig, throughput:int) = this.Throughput(state, string throughput)
        [<CustomOperation "add_containers">]
        member __.AddContainers(state:CosmosDbConfig, containers) =
            { state with Containers = state.Containers @ containers }

    open WebApp
    type WebAppBuilder with
        member this.DependsOn(state:WebAppConfig, cosmosDbConfig:CosmosDbConfig) =
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
        member this.FullyQualifiedDomainName =
            sprintf "[reference(concat('Microsoft.Sql/servers/', variables('%s'))).fullyQualifiedDomainName]" this.ServerName.Value
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
        member __.ServerName(state:SqlAzureConfig, serverName) = { state with ServerName = serverName }
        member this.ServerName(state:SqlAzureConfig, serverName:string) = this.ServerName(state, ResourceName serverName)
        [<CustomOperation "db_name">]
        member __.Name(state:SqlAzureConfig, name) = { state with DbName = name }
        member this.Name(state:SqlAzureConfig, name:string) = this.Name(state, ResourceName name)
        [<CustomOperation "db_edition">]
        member __.DatabaseEdition(state:SqlAzureConfig, edition:Edition) = { state with DbEdition = edition }
        [<CustomOperation "collation">]
        member __.Collation(state:SqlAzureConfig, collation:string) = { state with DbCollation = collation }
        [<CustomOperation "use_encryption">]
        member __.Encryption(state:SqlAzureConfig) = { state with Encryption = Enabled }
        [<CustomOperation "firewall_rule">]
        member __.AddFirewallWall(state:SqlAzureConfig, name, startRange, endRange) =
            { state with
                FirewallRules =
                    {| Name = name
                       Start = makeIp startRange
                       End = makeIp endRange |}
                    :: state.FirewallRules }
        [<CustomOperation "use_azure_firewall">]
        member this.UseAzureFirewall(state:SqlAzureConfig) =
            this.AddFirewallWall(state, "AllowAllMicrosoftAzureIps", "0.0.0.0", "0.0.0.0")
        [<CustomOperation "admin_username">]
        member __.AdminUsername(state:SqlAzureConfig, username) =
            { state with
                AdministratorCredentials =
                    {| state.AdministratorCredentials with
                        UserName = username |} }
    open WebApp
    type WebAppBuilder with
        member this.DependsOn(state:WebAppConfig, sqlDb:SqlAzureConfig) =
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
    let makeName (vmName:ResourceName) prefix = sprintf "%s-%s" prefix vmName.Value
    let makeResourceName vmName = makeName vmName >> ResourceName
    type VmConfig =
        { Name : ResourceName
          AutoCreateStorageAccount : bool
          StorageAccountName : ResourceName
          
          Username : string
          Image : {| Publisher : string; Offer : string; Sku : string |}
          Size : string
          DataDisks : int list
          
          DomainNamePrefix : string option
          AddressPrefix : string
          SubnetPrefix : string }

        member this.NicName = makeResourceName this.Name "nic"
        member this.VnetName = makeResourceName this.Name "vnet"
        member this.SubnetName = makeResourceName this.Name "subnet"
        member this.IpName = makeResourceName this.Name "ip"
        member this.Hostname = sprintf "[reference('%s').dnsSettings.fqdn]" this.IpName.Value

          
    type VirtualMachineBuilder() =
        member __.Yield _ =
            { Name = ResourceName.Empty
              AutoCreateStorageAccount = true
              StorageAccountName = ResourceName.Empty
              Size = Size.Basic_A0
              Username = "admin"
              Image = CommonImages.WindowsServer_2012Datacenter
              DataDisks = [ ]
              DomainNamePrefix = None
              AddressPrefix = "10.0.0.0/16"
              SubnetPrefix = "10.0.0.0/24" }

        member __.Run (state:VmConfig) =
            { state with
                StorageAccountName = state.Name |> sanitiseStorage |> sprintf "%sstorage" |> state.StorageAccountName.IfEmpty
                DataDisks = match state.DataDisks with [] -> [ 1024 ] | other -> other
            }

        [<CustomOperation "name">]
        member __.Name(state:VmConfig, name) = { state with Name = name }
        member this.Name(state:VmConfig, name) = this.Name(state, ResourceName name)
        [<CustomOperation "storage_account_name">]
        member __.StorageAccountName(state:VmConfig, name) = { state with StorageAccountName = ResourceName name; AutoCreateStorageAccount = false }
        [<CustomOperation "vm_size">]
        member __.VmSize(state:VmConfig, size) = { state with Size = size }
        [<CustomOperation "username">]
        member __.Username(state:VmConfig, username) = { state with Username = username }
        [<CustomOperation "image">]
        member __.Image(state:VmConfig, image) = { state with Image = image }
        [<CustomOperation "add_disk">]
        member __.AddDisk(state:VmConfig, disk) = { state with DataDisks = disk :: state.DataDisks }
        [<CustomOperation "domain_name_prefix">]
        member __.DomainNamePrefix(state:VmConfig, prefix) = { state with DomainNamePrefix = prefix }
        [<CustomOperation "address_prefix">]
        member __.AddressPrefix(state:VmConfig, prefix) = { state with AddressPrefix = prefix }
        [<CustomOperation "subnet_prefix">]
        member __.SubnetPrefix(state:VmConfig, prefix) = { state with SubnetPrefix = prefix }        

    let vm = VirtualMachineBuilder()

type ArmConfig =
    { Parameters : string Set
      Outputs : (string * string) list
      Location : string
      Resources : obj list }

[<AutoOpen>]
module ArmBuilder =
    open Internal.VM
    type ArmBuilder() =
        member __.Yield _ =
            { Parameters = Set.empty
              Outputs = List.empty
              Resources = List.empty
              Location = Helpers.Locations.WestEurope }

        member __.Run (state:ArmConfig) = {
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
                          ServerFarm = wac.ServicePlanName
                          AppSettings = [
                            yield! Map.toList wac.Settings
                            if wac.RunFromPackage then yield WebApp.AppSettings.RunFromPackage

                            match wac.WebsiteNodeDefaultVersion with
                            | Some v -> yield WebApp.AppSettings.WebsiteNodeDefaultVersion v
                            | None -> ()

                            match wac.AppInsightsName with
                            | Some v ->
                                yield "APPINSIGHTS_INSTRUMENTATIONKEY", Helpers.AppInsights.instrumentationKey v
                                yield "APPINSIGHTS_PROFILERFEATURE_VERSION", "1.0.0"
                                yield "APPINSIGHTS_SNAPSHOTFEATURE_VERSION", "1.0.0"
                                yield "ApplicationInsightsAgent_EXTENSION_VERSION", "~2"
                                yield "DiagnosticServices_EXTENSION_VERSION", "~3"
                                yield "InstrumentationEngine_EXTENSION_VERSION", "~1"
                                yield "SnapshotDebugger_EXTENSION_VERSION", "~1"
                                yield "XDT_MicrosoftApplicationInsights_BaseExtensions", "~1"
                                yield "XDT_MicrosoftApplicationInsights_Mode", "recommended"
                            | None ->
                                ()
                          ]

                          Extensions =
                            match wac.AppInsightsName with
                            | Some _ -> Set [ AppInsightsExtension ]
                            | None -> Set.empty
                          Kind = None
                          Dependencies = [
                            yield wac.ServicePlanName
                            yield! wac.Dependencies
                            match wac.AppInsightsName with
                            | Some appInsightsame -> yield appInsightsame
                            | None -> ()
                          ]
                        }

                    let serverFarm =
                        { Location = state.Location
                          Name = wac.ServicePlanName
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
                    yield WebApp webApp
                    match wac.AppInsightsName with
                    | Some ai ->
                        yield { Name = ai
                                Location = state.Location
                                LinkedWebsite = wac.Name }
                              |> AppInsights
                    | None ->
                        () ]
                | :? FunctionsConfig as fns -> [
                    let webApp =
                        { Name = fns.Name
                          ServerFarm = fns.ServicePlanName
                          Location = state.Location
                          AppSettings = [
                            yield "FUNCTIONS_WORKER_RUNTIME", string fns.WorkerRuntime
                            yield "WEBSITE_NODE_DEFAULT_VERSION", "10.14.1"
                            yield "FUNCTIONS_EXTENSION_VERSION", "~2"
                            yield "AzureWebJobsStorage", Storage.buildKey fns.StorageAccountName
                            yield "AzureWebJobsDashboard", Storage.buildKey fns.StorageAccountName

                            match fns.AppInsightsName with
                            | Some v -> yield "APPINSIGHTS_INSTRUMENTATIONKEY", Helpers.AppInsights.instrumentationKey v
                            | None -> ()

                            if fns.OperatingSystem = Windows then
                                yield "WEBSITE_CONTENTAZUREFILECONNECTIONSTRING", Storage.buildKey fns.StorageAccountName
                                yield "WEBSITE_CONTENTSHARE", fns.Name.Value.ToLower()
                          ]

                          Kind =
                            match fns.OperatingSystem with
                            | Windows -> Some "functionapp"
                            | Linux -> Some "functionapp,linux"
                          Extensions = Set.empty
                          Dependencies = [
                            match fns.AppInsightsName with
                            | Some appInsightsame -> yield appInsightsame
                            | None -> ()
                            yield fns.ServicePlanName
                            yield fns.StorageAccountName
                          ]
                        }                    

                    let serverFarm =
                        { Location = state.Location
                          Name = fns.ServicePlanName
                          Sku = "Y1"
                          WorkerSize = "Y1"
                          IsDynamic = true
                          Tier = "Dynamic"
                          WorkerCount = 0 }

                    yield ServerFarm serverFarm
                    yield WebApp webApp

                    if fns.AutoCreateStorageAccount then
                        yield
                            { StorageAccount.Name = fns.StorageAccountName
                              Location = state.Location
                              Sku = Storage.Sku.StandardLRS }
                            |> StorageAccount

                    match fns.AppInsightsName with
                    | Some ai ->
                        yield
                            { Name = ai
                              Location = state.Location
                              LinkedWebsite = fns.Name }
                            |> AppInsights
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
                    if vm.AutoCreateStorageAccount then
                        yield StorageAccount
                            { StorageAccount.Name = vm.StorageAccountName
                              Location = state.Location
                              Sku = Storage.Sku.StandardLRS }
                    yield Vm
                        { Name = vm.Name
                          Location = state.Location
                          StorageAccountName = vm.StorageAccountName
                          NetworkInterfaceName = vm.NicName
                          Size = vm.Size
                          Credentials = {| Username = vm.Username; Password = SecureParameter (makeName vm.Name "password-for") |}
                          Image = vm.Image
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
                | r ->
                    failwithf "Sorry, I don't know how to handle this resource of type '%s'." (r.GetType().FullName))
        }

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
        member this.Output (state:ArmConfig, outputName:string, (ResourceName outputValue)) = this.Output(state, outputName, outputValue)
        member this.Output (state:ArmConfig, outputName:string, outputValue:string option) =
            match outputValue with
            | Some outputValue -> this.Output(state, outputName, outputValue)
            | None -> state

        /// Sets the default location of all resources; use the `location` keyword.
        [<CustomOperation "location">]
        member __.Location (state, location) : ArmConfig = { state with Location = location }

        /// Adds a resource to the template; use the `resource` keyword.
        [<CustomOperation "resource">]
        member __.AddResource(state, resource) : ArmConfig =
            { state with Resources = box resource :: state.Resources }
    let arm = ArmBuilder()