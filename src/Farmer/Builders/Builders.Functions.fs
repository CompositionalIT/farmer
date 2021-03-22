[<AutoOpen>]
module rec Farmer.Builders.Functions

open Farmer
open Farmer.Helpers
open Farmer.Identity
open Farmer.WebApp
open Farmer.Arm.Web
open Farmer.Arm.Insights
open Farmer.Arm.Storage
open Farmer.Arm.KeyVault
open Farmer.Arm.KeyVault.Vaults
open System

type FunctionsRuntime = DotNet | Node | Java | Python
type FunctionsExtensionVersion = V1 | V2 | V3

module private FunctionsConfig =
    let ToCommon state =
        { Name = state.Name
          ServicePlan = state.ServicePlan
          AppInsights = state.AppInsights
          OperatingSystem = state.OperatingSystem
          Settings = state.Settings
          Cors = state.Cors
          Identity = state.Identity
          SecretStore = state.SecretStore
          ZipDeployPath = state.ZipDeployPath
          AlwaysOn = state.AlwaysOn
          WorkerProcess = state.WorkerProcess }
    let FromCommon state (config: CommonWebConfig): FunctionsConfig =
        { state with
            AlwaysOn = config.AlwaysOn
            Name = config.Name
            ServicePlan = config.ServicePlan
            AppInsights = config.AppInsights
            OperatingSystem = config.OperatingSystem
            Settings = config.Settings
            Cors = config.Cors
            Identity = config.Identity
            SecretStore = config.SecretStore
            ZipDeployPath = config.ZipDeployPath
            WorkerProcess = config.WorkerProcess }

type FunctionsConfig =
    { Name : ResourceName
      ServicePlan : ResourceRef<ResourceName>
      HTTPSOnly : bool
      AppInsights : ResourceRef<ResourceName> option
      OperatingSystem : OS
      Settings : Map<string, Setting>
      Tags : Map<string, string>
      Dependencies : ResourceId Set
      Cors : Cors option
      StorageAccount : ResourceRef<FunctionsConfig>
      Runtime : FunctionsRuntime
      ExtensionVersion : FunctionsExtensionVersion
      Identity : ManagedIdentity
      SecretStore : SecretStore
      ZipDeployPath : string option
      AlwaysOn : bool
      WorkerProcess : Bitness option }

    /// Gets the system-created managed principal for the functions instance. It must have been enabled using enable_managed_identity.
    member this.SystemIdentity = SystemIdentity (sites.resourceId this.Name)
    /// Gets the ARM expression path to the publishing password of this functions app.
    member this.PublishingPassword = publishingPassword this.Name
    /// Gets the ARM expression path to the storage account key of this functions app.
    member this.StorageAccountKey = StorageAccount.getConnectionString this.StorageAccountName
    /// Gets the ARM expression path to the app insights key of this functions app, if it exists.
    member this.AppInsightsKey = this.AppInsightsName |> Option.map AppInsights.getInstrumentationKey
    /// Gets the default key for the functions site
    member this.DefaultKey =
        $"listkeys(concat(resourceId('Microsoft.Web/sites', '{this.Name.Value}'), '/host/default/'),'2016-08-01').functionKeys.default"
        |> ArmExpression.create
    /// Gets the master key for the functions site
    member this.MasterKey =
        $"listkeys(concat(resourceId('Microsoft.Web/sites', '{this.Name.Value}'), '/host/default/'),'2016-08-01').masterKey"
        |> ArmExpression.create
    /// Gets this web app's Server Plan's full resource ID.
    member this.ServicePlanId = this.ServicePlan.resourceId this.Name
    /// Gets the Service Plan name for this web app.
    member this.ServicePlanName = this.ServicePlanId.Name
    /// Gets the App Insights name for this functions app, if it exists.
    member this.AppInsightsName : ResourceName option = this.AppInsights |> Option.map (fun ai -> ai.resourceId(this.Name).Name)
    /// Gets the Storage Account name for this functions app.
    member this.StorageAccountName : Storage.StorageAccountName = this.StorageAccount.resourceId(this).Name |> Storage.StorageAccountName.Create |> Result.get
    interface IBuilder with
        member this.ResourceId = sites.resourceId this.Name
        member this.BuildResources location = [
            let keyVault, secrets =
                match this.SecretStore with
                | KeyVault (DeployableResource (FunctionsConfig.ToCommon this) vaultName) ->
                    let store = keyVault {
                        name vaultName.Name
                        add_access_policy (AccessPolicy.create (this.SystemIdentity.PrincipalId, [ KeyVault.Secret.Get ]))
                        add_secrets [
                            for setting in this.Settings do
                                match setting.Value with
                                | LiteralSetting _ ->
                                    ()
                                | ParameterSetting _ ->
                                    SecretConfig.create (setting.Key)
                                | ExpressionSetting expr ->
                                    SecretConfig.create (setting.Key, expr)
                        ]
                    }
                    Some store, []
                | KeyVault (ExternalResource vaultName) ->
                    let secrets = [
                        for setting in this.Settings do
                            let secret =
                                match setting.Value with
                                | LiteralSetting _ -> None
                                | ParameterSetting _ -> SecretConfig.create setting.Key |> Some
                                | ExpressionSetting expr -> SecretConfig.create (setting.Key, expr) |> Some
                            match secret with
                            | Some secret ->
                                { Secret.Name = vaultName.Name/secret.Key
                                  Value = secret.Value
                                  ContentType = secret.ContentType
                                  Enabled = secret.Enabled
                                  ActivationDate = secret.ActivationDate
                                  ExpirationDate = secret.ExpirationDate
                                  Location = location
                                  Dependencies = secret.Dependencies.Add vaultName
                                  Tags = secret.Tags } :> IArmResource
                            | None ->
                                ()
                    ]
                    None, secrets
                | KeyVault _
                | AppService ->
                    None, []

            yield! secrets

            match keyVault with
            | Some keyVault ->
                let builder = keyVault :> IBuilder
                yield! builder.BuildResources location
            | None ->
                ()

            { Name = this.Name
              ServicePlan = this.ServicePlanId
              Location = location
              Cors = this.Cors
              Tags = this.Tags
              ConnectionStrings = Map.empty
              AppSettings = [
                "FUNCTIONS_WORKER_RUNTIME", (string this.Runtime).ToLower()
                "WEBSITE_NODE_DEFAULT_VERSION", "10.14.1"
                "FUNCTIONS_EXTENSION_VERSION", match this.ExtensionVersion with V1 -> "~1" | V2 -> "~2" | V3 -> "~3"
                "AzureWebJobsStorage", StorageAccount.getConnectionString this.StorageAccountName |> ArmExpression.Eval
                "AzureWebJobsDashboard", StorageAccount.getConnectionString this.StorageAccountName |> ArmExpression.Eval

                yield! this.AppInsightsKey |> Option.mapList (fun key -> "APPINSIGHTS_INSTRUMENTATIONKEY", key |> ArmExpression.Eval)

                if this.OperatingSystem = Windows then
                    "WEBSITE_CONTENTAZUREFILECONNECTIONSTRING", StorageAccount.getConnectionString this.StorageAccountName |> ArmExpression.Eval
                    "WEBSITE_CONTENTSHARE", this.Name.Value.ToLower()
              ]
              |> List.map Setting.AsLiteral
              |> List.append (
                    (match this.SecretStore with
                     | AppService ->
                         this.Settings
                     | KeyVault r ->
                        let name = r.resourceId (FunctionsConfig.ToCommon this)
                        [ for setting in this.Settings do
                            match setting.Value with
                            | LiteralSetting _ ->
                                setting.Key, setting.Value
                            | ParameterSetting _
                            | ExpressionSetting _ ->
                                setting.Key, LiteralSetting $"@Microsoft.KeyVault(SecretUri=https://{name.Name.Value}.vault.azure.net/secrets/{setting.Key})"
                        ] |> Map.ofList
                    ) |> Map.toList)
              |> Map

              Identity = this.Identity
              Kind =
                match this.OperatingSystem with
                | Windows -> "functionapp"
                | Linux -> "functionapp,linux"
              Dependencies = Set [
                yield! this.Dependencies

                match this.AppInsights with
                | Some (DependableResource this.Name resourceId) -> resourceId
                | _ -> ()

                for setting in this.Settings do
                    match setting.Value with
                    | ExpressionSetting e -> yield! Option.toList e.Owner
                    | ParameterSetting _ | LiteralSetting _ -> ()

                match this.ServicePlan with
                | DependableResource this.Name resourceId -> resourceId
                | _ -> ()

                match this.StorageAccount with
                | DependableResource this resourceId -> resourceId
                | _ -> ()

                match this.SecretStore with
                | AppService ->
                    for setting in this.Settings do
                        match setting.Value with
                        | ExpressionSetting expr ->
                            yield! Option.toList expr.Owner
                        | ParameterSetting _
                        | LiteralSetting _ ->
                            ()
                | KeyVault _ ->
                    ()
              ]
              HTTPSOnly = this.HTTPSOnly
              AlwaysOn = this.AlwaysOn
              HTTP20Enabled = None
              ClientAffinityEnabled = None
              WebSocketsEnabled = None
              LinuxFxVersion = None
              NetFrameworkVersion = None
              JavaVersion = None
              JavaContainer = None
              JavaContainerVersion = None
              PhpVersion = None
              PythonVersion = None
              Metadata = []
              ZipDeployPath = this.ZipDeployPath |> Option.map (fun x -> x, ZipDeploy.ZipDeployTarget.FunctionApp)
              AppCommandLine = None
              WorkerProcess = this.WorkerProcess }
            match this.ServicePlan with
            | DeployableResource this.Name resourceId ->
                { Name = resourceId.Name
                  Location = location
                  Sku = Sku.Y1
                  WorkerSize = Serverless
                  WorkerCount = 0
                  OperatingSystem = this.OperatingSystem
                  Tags = this.Tags }
            | _ ->
                ()

            match this.StorageAccount with
            | DeployableResource this resourceId ->
                { Name = Storage.StorageAccountName.Create(resourceId.Name).OkValue
                  Location = location
                  Sku = Storage.Sku.Standard_LRS
                  Dependencies = []
                  StaticWebsite = None
                  EnableHierarchicalNamespace = None
                  Tags = this.Tags }
            | _ ->
                ()

            match this.AppInsights with
            | Some (DeployableResource this.Name resourceId) ->
                { Name = resourceId.Name
                  Location = location
                  DisableIpMasking = false
                  SamplingPercentage = 100
                  LinkedWebsite =
                    match this.OperatingSystem with
                    | Windows -> Some this.Name
                    | Linux -> None
                  Tags = this.Tags }
            | Some _
            | None ->
                ()
        ]

type FunctionsBuilder() =
    member _.Yield _ =
        { Name = ResourceName.Empty
          ServicePlan = derived (fun name -> serverFarms.resourceId (name-"farm"))
          AppInsights = Some (derived (fun name -> components.resourceId (name-"ai")))
          StorageAccount = derived (fun config ->
            let storage = config.Name.Map (sprintf "%sstorage") |> sanitiseStorage |> ResourceName
            storageAccounts.resourceId storage)
          Runtime = DotNet
          ExtensionVersion = V3
          Cors = None
          HTTPSOnly = false
          AlwaysOn = false
          OperatingSystem = Windows
          Settings = Map.empty
          Dependencies = Set.empty
          Identity = ManagedIdentity.Empty
          SecretStore = AppService
          Tags = Map.empty
          ZipDeployPath = None
          WorkerProcess = None }
    /// Do not create an automatic storage account; instead, link to a storage account that is created outside of this Functions instance.
    [<CustomOperation "link_to_storage_account">]
    member _.LinkToStorageAccount(state:FunctionsConfig, name) = { state with StorageAccount = managed storageAccounts name }
    member this.LinkToStorageAccount(state:FunctionsConfig, name) = this.LinkToStorageAccount(state, ResourceName name)
    [<CustomOperation "link_to_unmanaged_storage_account">]
    member _.LinkToUnmanagedStorageAccount(state:FunctionsConfig, resourceId) = { state with StorageAccount = unmanaged resourceId }
    /// Set the name of the storage account instead of using an auto-generated one based on the function instance name.
    [<CustomOperation "storage_account_name">]
    member _.StorageAccountName(state:FunctionsConfig, name) = { state with StorageAccount = named storageAccounts (ResourceName name) }
    /// Disables http for this webapp so that only https is used.
    [<CustomOperation "https_only">]
    member _.HttpsOnly(state:FunctionsConfig) = { state with HTTPSOnly = true }
    /// Sets the runtime of the Functions host.
    [<CustomOperation "use_runtime">]
    member _.Runtime(state:FunctionsConfig, runtime) = { state with Runtime = runtime }
    [<CustomOperation "use_extension_version">]
    member _.ExtensionVersion(state:FunctionsConfig, version) = { state with ExtensionVersion = version }

    interface ITaggable<FunctionsConfig> with member _.Add state tags = { state with Tags = state.Tags |> Map.merge tags }
    interface IDependable<FunctionsConfig> with member _.Add state newDeps = { state with Dependencies = state.Dependencies + newDeps }
    interface IServicePlanApp<FunctionsConfig> with
        member _.Get state = FunctionsConfig.ToCommon state
        member _.Wrap state config = FunctionsConfig.FromCommon state config

let functions = FunctionsBuilder()
