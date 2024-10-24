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
open Farmer.Arm

type FunctionsRuntime =
    | DotNet
    | DotNetIsolated
    | Node
    | Java
    | Python

type VersionedFunctionsRuntime = FunctionsRuntime * string option

type FunctionsRuntime with
    // These values are defined on FunctionsRuntime to reduce the need for users to be aware of the distinction
    // between FunctionsRuntime and VersionedFunctionsRuntime as well as to provide parity with WebApp runtime
    static member DotNetFramework48 = DotNet, Some "4.0"
    static member DotNetCore31 = DotNet, Some "3.1"
    static member DotNet50 = DotNet, Some "5.0"
    static member DotNet50Isolated = DotNetIsolated, Some "5.0"
    static member DotNet60 = DotNet, Some "6.0"
    static member DotNet60Isolated = DotNetIsolated, Some "6.0"
    static member DotNet70Isolated = DotNetIsolated, Some "7.0"
    static member DotNet80Isolated = DotNetIsolated, Some "8.0"
    static member Node14 = Node, Some "14-lts"
    static member Node12 = Node, Some "12-lts"
    static member Node10 = Node, Some "10-lts"
    static member Node10_1 = Node, Some "10.1"
    static member Node10_6 = Node, Some "10.6"
    static member Node10_14 = Node, Some "10.14"
    static member Java8 = Java, Some "8-jre8"
    static member Java11 = Java, Some "11-java11"
    static member Python38 = Python, Some "3.8"
    static member Python37 = Python, Some "3.7"
    static member Python36 = Python, Some "3.6"

type DockerInfo = {
    User: string
    Password: SecureParameter
    Url: Uri
    StartupCommand: string
}

type PublishAs =
    | Code
    | DockerContainer of DockerInfo

type FunctionsExtensionVersion =
    | V1
    | V2
    | V3
    | V4

    member internal this.ArmValue =
        match this with
        | V1 -> "~1"
        | V2 -> "~2"
        | V3 -> "~3"
        | V4 -> "~4"

type FunctionsConfig = {
    CommonWebConfig: CommonWebConfig
    Tags: Map<string, string>
    Dependencies: ResourceId Set
    StorageAccount: ResourceRef<FunctionsConfig>
    VersionedRuntime: VersionedFunctionsRuntime
    PublishAs: PublishAs
    ExtensionVersion: FunctionsExtensionVersion
    FunctionAppScaleLimit: int option
} with

    member this.Name = this.CommonWebConfig.Name
    member this.Runtime = fst this.VersionedRuntime
    /// Gets the system-created managed principal for the functions instance. It must have been enabled using enable_managed_identity.
    member this.SystemIdentity = SystemIdentity(sites.resourceId this.Name.ResourceName)
    /// Gets the ARM expression path to the publishing password of this functions app.
    member this.PublishingPassword = publishingPassword this.Name.ResourceName

    /// Gets the ARM expression path to the storage account key of this functions app.
    member this.StorageAccountKey =
        StorageAccount.getConnectionString this.StorageAccountId

    /// Gets the ARM expression path to the app insights key of this functions app, if it exists.
    member this.AppInsightsKey =
        this.AppInsightsId |> Option.map AppInsights.getInstrumentationKey

    /// Gets the default key for the functions site
    member this.DefaultKey =
        $"listkeys(concat(resourceId('Microsoft.Web/sites', '{this.Name.ResourceName.Value}'), '/host/default/'),'2016-08-01').functionKeys.default"
        |> ArmExpression.create

    /// Gets the master key for the functions site
    member this.MasterKey =
        $"listkeys(concat(resourceId('Microsoft.Web/sites', '{this.Name.ResourceName.Value}'), '/host/default/'),'2016-08-01').masterKey"
        |> ArmExpression.create

    /// Gets this web app's Server Plan's full resource ID.
    member this.ServicePlanId =
        this.CommonWebConfig.ServicePlan.resourceId this.Name.ResourceName

    /// Gets the Service Plan name for this web app.
    member this.ServicePlanName = this.ServicePlanId.Name

    /// Gets the App Insights name for this functions app, if it exists.
    [<Obsolete("Prefer AppInsightsId instead as this property ignores resource groups")>]
    member this.AppInsightsName: ResourceName option =
        this.CommonWebConfig.AppInsights
        |> Option.map (fun ai -> ai.resourceId(this.Name.ResourceName).Name)

    /// Gets the Storage Account name for this functions app.
    [<Obsolete("Prefer StorageAccountId instead as this property ignores resource groups")>]
    member this.StorageAccountName: Storage.StorageAccountName =
        this.StorageAccount.resourceId(this).Name
        |> Storage.StorageAccountName.Create
        |> Result.get

    /// Gets the App Insights resourceId for this functions app, if it exists.
    member this.AppInsightsId: ResourceId option =
        this.CommonWebConfig.AppInsights
        |> Option.map (fun ai -> ai.resourceId (this.Name.ResourceName))

    /// Gets the Storage Account resourceId for this functions app.
    member this.StorageAccountId: ResourceId = this.StorageAccount.resourceId (this)
    /// Gets the Resource Id for this functions app
    member this.ResourceId = sites.resourceId this.Name.ResourceName

    interface IBuilder with
        member this.ResourceId = sites.resourceId this.Name.ResourceName

        member this.BuildResources location = [
            let keyVault, secrets =
                match this.CommonWebConfig.SecretStore with
                | KeyVault(DeployableResource (this.CommonWebConfig) vaultName) ->
                    let store = keyVault {
                        name vaultName.Name

                        add_access_policy (
                            AccessPolicy.create (this.SystemIdentity.PrincipalId, [ KeyVault.Secret.Get ])
                        )

                        add_secrets [
                            for setting in this.CommonWebConfig.Settings do
                                match setting.Value with
                                | LiteralSetting _ -> ()
                                | ParameterSetting _ -> SecretConfig.create (setting.Key)
                                | ExpressionSetting expr -> SecretConfig.create (setting.Key, expr)
                        ]
                    }

                    Some store, []
                | KeyVault(ExternalResource vaultName) ->
                    let secrets = [
                        for setting in this.CommonWebConfig.Settings do
                            let secret =
                                match setting.Value with
                                | LiteralSetting _ -> None
                                | ParameterSetting _ -> SecretConfig.create setting.Key |> Some
                                | ExpressionSetting expr -> SecretConfig.create (setting.Key, expr) |> Some

                            match secret with
                            | Some secret ->
                                {
                                    Secret.Name = vaultName.Name / secret.SecretName
                                    Value = secret.Value
                                    ContentType = secret.ContentType
                                    Enabled = secret.Enabled
                                    ActivationDate = secret.ActivationDate
                                    ExpirationDate = secret.ExpirationDate
                                    Location = location
                                    Dependencies = secret.Dependencies.Add vaultName
                                    Tags = secret.Tags
                                }
                                :> IArmResource
                            | None -> ()
                    ]

                    None, secrets
                | KeyVault _
                | AppService -> None, []

            yield! secrets

            match keyVault with
            | Some keyVault ->
                let builder = keyVault :> IBuilder
                yield! builder.BuildResources location
            | None -> ()

            let functionsRuntime =
                match this.Runtime with
                | DotNetIsolated -> "dotnet-isolated"
                | DotNet -> "dotnet"
                | other -> (string other).ToLower()

            let basicSettings = [
                "FUNCTIONS_WORKER_RUNTIME", functionsRuntime
                "WEBSITE_NODE_DEFAULT_VERSION", "10.14.1"
                "FUNCTIONS_EXTENSION_VERSION", this.ExtensionVersion.ArmValue
                "AzureWebJobsStorage", StorageAccount.getConnectionString this.StorageAccountId |> ArmExpression.Eval

                yield!
                    this.AppInsightsKey
                    |> Option.mapList (fun key -> "APPINSIGHTS_INSTRUMENTATIONKEY", key |> ArmExpression.Eval)

                if this.ExtensionVersion = V1 then
                    "AzureWebJobsDashboard",
                    StorageAccount.getConnectionString this.StorageAccountId |> ArmExpression.Eval

                if this.CommonWebConfig.OperatingSystem = Windows then
                    "WEBSITE_CONTENTAZUREFILECONNECTIONSTRING",
                    StorageAccount.getConnectionString this.StorageAccountId |> ArmExpression.Eval

                    "WEBSITE_CONTENTSHARE", this.Name.ResourceName.Value.ToLower()
                match this.PublishAs with
                | DockerContainer {
                                      User = us
                                      Password = pass
                                      Url = url
                                  } ->
                    yield! [
                        "DOCKER_REGISTRY_SERVER_URL", url.ToString()
                        "DOCKER_REGISTRY_SERVER_USERNAME", us
                        "DOCKER_REGISTRY_SERVER_PASSWORD", pass.ArmExpression.Eval()
                    ]

                | _ -> ()
            ]

            let functionsSettings =
                basicSettings
                |> List.map Setting.AsLiteral
                |> List.append (
                    (match this.CommonWebConfig.SecretStore with
                     | AppService -> this.CommonWebConfig.Settings
                     | KeyVault r ->
                         let name = r.resourceId (this.CommonWebConfig)

                         [
                             for setting in this.CommonWebConfig.Settings do
                                 match setting.Value with
                                 | LiteralSetting _ -> setting.Key, setting.Value
                                 | ParameterSetting _
                                 | ExpressionSetting _ ->
                                     setting.Key,
                                     LiteralSetting
                                         $"@Microsoft.KeyVault(SecretUri=https://{name.Name.Value}.vault.azure.net/secrets/{setting.Key})"
                         ]
                         |> Map.ofList)
                    |> Map.toList
                )
                |> Map

            let site = {
                SiteType = Site this.Name
                ServicePlan = this.ServicePlanId
                Location = location
                Cors = this.CommonWebConfig.Cors
                Tags = this.Tags
                ConnectionStrings = Some this.CommonWebConfig.ConnectionStrings
                AppSettings = Some functionsSettings
                Identity = this.CommonWebConfig.Identity
                KeyVaultReferenceIdentity = this.CommonWebConfig.KeyVaultReferenceIdentity
                Kind =
                    match this.CommonWebConfig.OperatingSystem with
                    | Windows -> "functionapp"
                    | Linux -> "functionapp,linux"
                Dependencies =
                    Set [
                        yield! this.Dependencies

                        match this.CommonWebConfig.AppInsights with
                        | Some(DependableResource this.Name.ResourceName resourceId) -> resourceId
                        | _ -> ()

                        for setting in this.CommonWebConfig.Settings do
                            match setting.Value with
                            | ExpressionSetting e -> yield! Option.toList e.Owner
                            | ParameterSetting _
                            | LiteralSetting _ -> ()

                        match this.CommonWebConfig.ServicePlan with
                        | DependableResource this.Name.ResourceName resourceId -> resourceId
                        | _ -> ()

                        match this.StorageAccount with
                        | DependableResource this resourceId -> resourceId
                        | _ -> ()

                        match this.CommonWebConfig.SecretStore with
                        | AppService ->
                            for setting in this.CommonWebConfig.Settings do
                                match setting.Value with
                                | ExpressionSetting expr -> yield! Option.toList expr.Owner
                                | ParameterSetting _
                                | LiteralSetting _ -> ()
                        | KeyVault _ -> ()
                    ]
                HTTPSOnly = this.CommonWebConfig.HTTPSOnly
                FTPState = this.CommonWebConfig.FTPState
                AlwaysOn = this.CommonWebConfig.AlwaysOn
                HTTP20Enabled = None
                ClientAffinityEnabled = None
                WebSocketsEnabled = None
                LinuxFxVersion =
                    match this.CommonWebConfig.OperatingSystem with
                    | Windows -> None
                    | Linux ->
                        match this.VersionedRuntime with
                        | DotNet, Some version ->
                            match Double.TryParse(version) with
                            | true, versionNo when versionNo < 4.0 -> Some $"DOTNETCORE|{version}"
                            | _ -> Some $"DOTNET|{version}"
                        | DotNetIsolated, Some version -> Some $"DOTNET-ISOLATED|{version}"
                        | _, Some version -> Some $"{functionsRuntime.ToUpper()}|{version}"
                        | _, None -> None
                NetFrameworkVersion =
                    let possibleVersions = [ "4.0"; "6.0"; "7.0"; "8.0" ]

                    match this.VersionedRuntime with
                    | (DotNet | DotNetIsolated), Some version when possibleVersions |> List.contains version ->
                        Some $"v{version}"
                    | _, _ -> None
                JavaVersion = None
                JavaContainer = None
                JavaContainerVersion = None
                PhpVersion = None
                PythonVersion = None
                Metadata = []
                AutoSwapSlotName = None
                ZipDeployPath =
                    this.CommonWebConfig.ZipDeployPath
                    |> Option.map (fun (path, slot) -> path, ZipDeploy.ZipDeployTarget.FunctionApp, slot)
                AppCommandLine =
                    match this.PublishAs with
                    | DockerContainer { StartupCommand = sc } -> Some sc
                    | _ -> None
                WorkerProcess = this.CommonWebConfig.WorkerProcess
                HealthCheckPath = this.CommonWebConfig.HealthCheckPath
                IpSecurityRestrictions = this.CommonWebConfig.IpSecurityRestrictions
                LinkToSubnet = this.CommonWebConfig.IntegratedSubnet
                VirtualApplications = Map []
                FunctionAppScaleLimit = this.FunctionAppScaleLimit
            }

            match this.CommonWebConfig.ServicePlan with
            | DeployableResource this.Name.ResourceName resourceId -> {
                Name = resourceId.Name
                Location = location
                Sku = this.CommonWebConfig.Sku
                WorkerSize = Serverless
                WorkerCount = 0
                MaximumElasticWorkerCount = None
                OperatingSystem = this.CommonWebConfig.OperatingSystem
                ZoneRedundant = None
                Tags = this.Tags
              }
            | _ -> ()

            match this.StorageAccount with
            | DeployableResource this resourceId -> {
                Name = Storage.StorageAccountName.Create(resourceId.Name).OkValue
                Location = location
                Sku = Storage.Sku.Standard_LRS
                Dependencies = []
                NetworkAcls = None
                StaticWebsite = None
                EnableHierarchicalNamespace = None
                MinTlsVersion = None
                SupportsHttpsTrafficOnly = None
                Tags = this.Tags
                DnsZoneType = None
                DisablePublicNetworkAccess = None
                DisableBlobPublicAccess = None
                DisableSharedKeyAccess = None
                DefaultToOAuthAuthentication = None
                ImmutableStorageWithVersioning = None
                RequireInfrastructureEncryption = None
              }
            | _ -> ()

            match this.CommonWebConfig.AppInsights with
            | Some(DeployableResource this.Name.ResourceName resourceId) -> {
                Name = resourceId.Name
                Location = location
                DisableIpMasking = false
                SamplingPercentage = 100
                Dependencies = Set.empty
                InstanceKind = Classic
                LinkedWebsite =
                    match this.CommonWebConfig.OperatingSystem with
                    | Windows -> Some this.Name.ResourceName
                    | Linux -> None
                Tags = this.Tags
              }
            | Some _
            | None -> ()

            match this.CommonWebConfig.IntegratedSubnet with
            | None -> ()
            | Some subnetRef -> {
                Site = site
                Subnet = subnetRef.ResourceId
                Dependencies = subnetRef.Dependency |> Option.toList
              }

            yield! (PrivateEndpoint.create location this.ResourceId [ "sites" ] this.CommonWebConfig.PrivateEndpoints)

            if Map.isEmpty this.CommonWebConfig.Slots then
                site
            else
                {
                    site with
                        AppSettings = None
                        ConnectionStrings = None
                }

                for (_, slot) in this.CommonWebConfig.Slots |> Map.toSeq do
                    slot.ToSite site
        ]

type FunctionsBuilder() =
    member _.Yield _ = {
        FunctionsConfig.CommonWebConfig = {
            Name = WebAppName.Empty
            AlwaysOn = false
            AppInsights = Some(derived (fun name -> components.resourceId (name - "ai")))
            ConnectionStrings = Map.empty
            Cors = None
            FTPState = None
            HTTPSOnly = false
            Identity = ManagedIdentity.Empty
            KeyVaultReferenceIdentity = None
            OperatingSystem = Windows
            SecretStore = AppService
            ServicePlan = derived (fun name -> serverFarms.resourceId (name - "farm"))
            Settings = Map.empty
            Sku = Sku.Y1
            Slots = Map.empty
            WorkerProcess = None
            ZipDeployPath = None
            HealthCheckPath = None
            IpSecurityRestrictions = []
            IntegratedSubnet = None
            PrivateEndpoints = Set.empty
        }
        StorageAccount =
            derived (fun config ->
                let storage =
                    config.Name.ResourceName.Map(sprintf "%sstorage")
                    |> sanitiseStorage
                    |> ResourceName

                storageAccounts.resourceId storage)
        VersionedRuntime = FunctionsRuntime.DotNetCore31
        ExtensionVersion = V4
        Dependencies = Set.empty
        PublishAs = Code
        Tags = Map.empty
        FunctionAppScaleLimit = None
    }

    member _.Run(state: FunctionsConfig) =
        if state.Name.ResourceName = ResourceName.Empty then
            raiseFarmer "No Functions instance name has been set."

        match state.FunctionAppScaleLimit with
        | Some limit when limit < 1 || limit > 200 -> raiseFarmer "Max scale out limit can only be 1-200"
        | _ -> ()

        state.CommonWebConfig.Validate()
        state

    /// Do not create an automatic storage account; instead, link to a storage account that is created outside of this Functions instance.
    [<CustomOperation "link_to_storage_account">]
    member _.LinkToStorageAccount(state: FunctionsConfig, name) = {
        state with
            StorageAccount = managed storageAccounts name
    }

    member this.LinkToStorageAccount(state: FunctionsConfig, name) =
        this.LinkToStorageAccount(state, ResourceName name)

    [<CustomOperation "link_to_unmanaged_storage_account">]
    member _.LinkToUnmanagedStorageAccount(state: FunctionsConfig, resourceId) = {
        state with
            StorageAccount = unmanaged resourceId
    }

    /// Set the name of the storage account instead of using an auto-generated one based on the function instance name.
    [<CustomOperation "storage_account_name">]
    member _.StorageAccountName(state: FunctionsConfig, name) = {
        state with
            StorageAccount = named storageAccounts (ResourceName name)
    }

    /// Sets the runtime of the Functions host.
    [<CustomOperation "use_runtime">]
    member _.Runtime(state: FunctionsConfig, runtime) = {
        state with
            VersionedRuntime = runtime, None
    }

    member _.Runtime(state: FunctionsConfig, runtime) = {
        state with
            VersionedRuntime = runtime
    }

    /// Sets the Publish as Code or Docker container information.
    [<CustomOperation "publish_as">]
    member _.PublishAs(state: FunctionsConfig, publishAs) = { state with PublishAs = publishAs }

    [<CustomOperation "use_extension_version">]
    member _.ExtensionVersion(state: FunctionsConfig, version) = {
        state with
            ExtensionVersion = version
    }

    /// Sets the maximum scale out limit for Azure Functions
    [<CustomOperation "max_scale_out_limit">]
    member _.MaxScaleOutLimit(state: FunctionsConfig, limit) = {
        state with
            FunctionAppScaleLimit = Some limit
    }

    interface ITaggable<FunctionsConfig> with
        member _.Add state tags = {
            state with
                Tags = state.Tags |> Map.merge tags
        }

    interface IDependable<FunctionsConfig> with
        member _.Add state newDeps = {
            state with
                Dependencies = state.Dependencies + newDeps
        }

    interface IServicePlanApp<FunctionsConfig> with
        member _.Get state = state.CommonWebConfig
        member _.Wrap state config = { state with CommonWebConfig = config }

let functions = FunctionsBuilder()

let docker (server: Uri) (user: string) (command: string) : DockerInfo = {
    User = user
    Password = SecureParameter $"{user}-password"
    Url = server
    StartupCommand = command
}