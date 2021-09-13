[<AutoOpen>]
module rec Farmer.Builders.WebApp

open Farmer
open Farmer.Arm
open Farmer.WebApp
open Farmer.Arm.KeyVault.Vaults
open Sites
open System
open Farmer.Identity

type JavaHost =
    | JavaSE | WildFly14 | Tomcat of string
    static member Tomcat85 = Tomcat "8.5"
    static member Tomcat90 = Tomcat "9.0"
type JavaRuntime =
    | Java8 | Java11
    member this.Version = match this with Java8 -> 8 | Java11 -> 11
    member this.Jre = match this with Java8 -> "jre8" | Java11 -> "java11"
type Runtime =
    | DotNetCore of string
    | DotNet of version:string
    | Node of string
    | Php of string
    | Ruby of string
    | AspNet of version:string
    | Java of JavaRuntime * JavaHost
    | Python of linuxVersion:string * windowsVersion:string
    static member Php73 = Php "7.3"
    static member Php72 = Php "7.2"
    static member Php71 = Php "7.1"
    static member Php70 = Php "7.0"
    static member Php56 = Php "5.6"
    static member DotNetCore21 = DotNetCore "2.1"
    static member DotNetCore31 = DotNetCore "3.1"
    static member DotNetCoreLts = DotNetCore "LTS"
    static member DotNetCoreLatest = DotNetCore "Latest"
    static member Node6 = Node "6-lts"
    static member Node8 = Node "8-lts"
    static member Node10 = Node "10-lts"
    static member Node12 = Node "12-lts"
    static member NodeLts = Node "lts"
    static member Ruby26 = Ruby "2.6"
    static member Ruby25 = Ruby "2.5"
    static member Ruby24 = Ruby "2.4"
    static member Ruby23 = Ruby "2.3"
    static member Java11 = Java (Java11, JavaSE)
    static member Java11Tomcat90 = Java (Java11, JavaHost.Tomcat90)
    static member Java11Tomcat85 = Java (Java11, JavaHost.Tomcat85)
    static member Java8 = Java (Java8, JavaSE)
    static member Java8WildFly14 = Java (Java8, WildFly14)
    static member Java8Tomcat90 = Java (Java8, JavaHost.Tomcat90)
    static member Java8Tomcat85 = Java (Java8, JavaHost.Tomcat85)
    static member DotNet50 = DotNet "5.0"
    static member AspNet47 = AspNet "4.0"
    static member AspNet35 = AspNet "2.0"
    static member Python27 = Python ("2.7", "2.7")
    static member Python36 = Python ("3.6", "3.4") // not typo, really version 3.4
    static member Python37 = Python ("3.7", "3.7")

module AppSettings =
    let WebsiteNodeDefaultVersion version = "WEBSITE_NODE_DEFAULT_VERSION", version
    let RunFromPackage = "WEBSITE_RUN_FROM_PACKAGE", "1"

let publishingPassword (name:ResourceName) =
    let resourceId = config.resourceId (name, ResourceName "publishingCredentials")
    let expr = $"list({resourceId.ArmExpression.Value}, '2014-06-01').properties.publishingPassword"
    ArmExpression.create(expr, resourceId)

type SecretStore =
    | AppService
    | KeyVault of ResourceRef<CommonWebConfig>

type SlotConfig =
    { Name: string
      AutoSwapSlotName: string option
      AppSettings: Map<string,Setting>
      ConnectionStrings: Map<string,(Setting * ConnectionStringKind)>
      Identity: ManagedIdentity
      KeyVaultReferenceIdentity: UserAssignedIdentity option
      Tags: Map<string,string>
      Dependencies: ResourceId Set}
    member this.ToSite (owner: Arm.Web.Site) =
        { owner with
            SiteType = SiteType.Slot (owner.Name/this.Name)
            Dependencies = owner.Dependencies |> Set.add (owner.ResourceType.resourceId owner.Name)
            AutoSwapSlotName = this.AutoSwapSlotName
            AppSettings = owner.AppSettings |> Map.merge ( this.AppSettings |> Map.toList)
            ConnectionStrings = owner.ConnectionStrings |> Map.merge (this.ConnectionStrings |> Map.toList)
            Identity = this.Identity + owner.Identity
            KeyVaultReferenceIdentity = this.KeyVaultReferenceIdentity |> Option.orElse owner.KeyVaultReferenceIdentity}

type SlotBuilder() =
    member this.Yield _ =
        { Name = "staging"
          AutoSwapSlotName = None
          AppSettings = Map.empty
          ConnectionStrings = Map.empty
          Identity = ManagedIdentity.Empty
          KeyVaultReferenceIdentity =  None
          Tags = Map.empty
          Dependencies = Set.empty}

    [<CustomOperation "name">]
    member this.Name (state,name) : SlotConfig = {state with Name = name}

    [<CustomOperation "autoSlotSwapName">]
    member this.AutoSlotSwapName (state,autoSlotSwapName) : SlotConfig = {state with AutoSwapSlotName = Some autoSlotSwapName}

    /// Sets an app setting of the web app in the form "key" "value".
    [<CustomOperation "add_identity">]
    member this.AddIdentity (state: SlotConfig, identity:UserAssignedIdentity) =
        { state with
            Identity = (state.Identity + identity)
            AppSettings = state.AppSettings.Add("AZURE_CLIENT_ID", Setting.ExpressionSetting identity.ClientId) }
    member this.AddIdentity (state, identity:UserAssignedIdentityConfig) = this.AddIdentity(state, identity.UserAssignedIdentity)

    [<CustomOperation "system_identity">]
    member this.SystemIdentity (state: SlotConfig) =
        { state with Identity = {state.Identity with SystemAssigned = Enabled } }

    [<CustomOperation "keyvault_identity">]
    member this.AddKeyVaultIdentity (state: SlotConfig, identity:UserAssignedIdentity) =
        { state with
            Identity = state.Identity + identity
            KeyVaultReferenceIdentity = Some identity
            AppSettings = state.AppSettings.Add("AZURE_CLIENT_ID", Setting.ExpressionSetting identity.ClientId) }
    member this.AddKeyVaultIdentity (state: SlotConfig, identity:UserAssignedIdentityConfig) = this.AddKeyVaultIdentity(state, identity.UserAssignedIdentity)

    [<CustomOperation "setting">]
    /// Adds an AppSetting to this deployment slot
    member this.AddSetting (state, key, value):SlotConfig =
        { state with AppSettings = state.AppSettings.Add(key, value) }
    member this.AddSetting (state, key, value) = this.AddSetting(state, key, LiteralSetting value)
    member this.AddSetting (state, key, resourceName:ResourceName) = this.AddSetting(state, key, resourceName.Value)
    member this.AddSetting (state, key, value:ArmExpression) = this.AddSetting(state, key, ExpressionSetting value)

    /// Sets a list of app setting of the web app in the form "key" "value".
    [<CustomOperation "settings">]
    member this.AddSettings(state, settings: (string*Setting) list):SlotConfig =
        {state with AppSettings = Map.merge settings state.AppSettings }
    member this.AddSettings(state, settings: (string*string) list) =
        this.AddSettings( state, List.map(fun (k,v) -> k,LiteralSetting v) settings)

    /// Creates a set of connection strings of the web app whose values will be supplied as secret parameters.
    [<CustomOperation "connection_string">]
    member _.AddConnectionString(state, key) :SlotConfig=
        { state with ConnectionStrings = state.ConnectionStrings.Add(key, (ParameterSetting (SecureParameter key), Custom)) }
    member _.AddConnectionString(state, (key, value:ArmExpression)) :SlotConfig =
        { state with ConnectionStrings = state.ConnectionStrings.Add(key, (ExpressionSetting value, Custom)) }

    /// Creates a set of connection strings of the web app whose values will be supplied as secret parameters.
    [<CustomOperation "connection_strings">]
    member this.AddConnectionStrings(state, connectionStrings:string list) :SlotConfig =
        connectionStrings
        |> List.fold (fun state key -> this.AddConnectionString(state, key)) state
    interface ITaggable<SlotConfig> with member _.Add state tags = { state with Tags = state.Tags |> Map.merge tags }
    interface IDependable<SlotConfig> with member _.Add state newDeps = { state with Dependencies = state.Dependencies + newDeps }

let appSlot = SlotBuilder()

/// Common fields between WebApp and Functions
type CommonWebConfig =
    { Name : WebAppName
      AlwaysOn : bool
      AppInsights : ResourceRef<ResourceName> option
      Cors : Cors option
      HTTPSOnly : bool
      Identity : Identity.ManagedIdentity
      KeyVaultReferenceIdentity: UserAssignedIdentity Option
      OperatingSystem : OS
      SecretStore : SecretStore
      ServicePlan : ResourceRef<ResourceName>
      Settings : Map<string, Setting>
      Slots : Map<string,SlotConfig>
      WorkerProcess : Bitness option
      ZipDeployPath : (string*ZipDeploy.ZipDeploySlot) option
      HealthCheckPath: string option }

type WebAppConfig =
    { CommonWebConfig: CommonWebConfig
      HTTP20Enabled : bool option
      ClientAffinityEnabled : bool option
      WebSocketsEnabled: bool option
      ConnectionStrings : Map<string, (Setting * ConnectionStringKind)>
      Dependencies : ResourceId Set
      Tags : Map<string,string>
      Sku : Sku
      WorkerSize : WorkerSize
      WorkerCount : int
      MaximumElasticWorkerCount : int option
      RunFromPackage : bool
      WebsiteNodeDefaultVersion : string option
      Runtime : Runtime
      SourceControlSettings : {| Repository : Uri; Branch : string; ContinuousIntegration : FeatureFlag |} option
      DockerImage : (string * string) option
      DockerCi : bool
      DockerAcrCredentials : {| RegistryName : string; Password : SecureParameter |} option
      AutomaticLoggingExtension : bool
      SiteExtensions : ExtensionName Set
      PrivateEndpoints: (LinkedResource * string option) Set 
      CustomDomain : DomainConfig }
    member this.Name = this.CommonWebConfig.Name
    /// Gets this web app's Server Plan's full resource ID.
    member this.ServicePlanId = this.CommonWebConfig.ServicePlan.resourceId this.Name.ResourceName
    /// Gets the Service Plan name for this web app.
    member this.ServicePlanName = this.ServicePlanId.Name
    /// Gets the App Insights name for this web app, if it exists.
    member this.AppInsightsName = this.CommonWebConfig.AppInsights |> Option.map (fun ai -> ai.resourceId(this.Name.ResourceName).Name)
    /// Gets the ARM expression path to the publishing password of this web app.
    member this.PublishingPassword = publishingPassword this.Name.ResourceName
    member this.Endpoint = $"{this.Name.ResourceName.Value}.azurewebsites.net"
    member this.SystemIdentity = SystemIdentity this.ResourceId
    member this.ResourceId = sites.resourceId this.Name.ResourceName
    interface IBuilder with
        member this.ResourceId = this.ResourceId
        member this.BuildResources location = [
            let keyVault, secrets =
                match this.CommonWebConfig.SecretStore with
                | KeyVault (DeployableResource (this.CommonWebConfig) vaultName) ->
                    let store = keyVault {
                        name vaultName.Name
                        add_access_policy (AccessPolicy.create (this.SystemIdentity.PrincipalId, [ KeyVault.Secret.Get ]))
                        add_secrets [
                            for setting in this.CommonWebConfig.Settings do
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
                        for setting in this.CommonWebConfig.Settings do
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

            let siteSettings =
                let literalSettings = [
                    if this.RunFromPackage then AppSettings.RunFromPackage
                    yield! this.WebsiteNodeDefaultVersion |> Option.mapList AppSettings.WebsiteNodeDefaultVersion
                    yield! this.CommonWebConfig.AppInsights |> Option.mapList (fun resource -> "APPINSIGHTS_INSTRUMENTATIONKEY", AppInsights.getInstrumentationKey(resource.resourceId this.Name.ResourceName).Eval())
                    match this.CommonWebConfig.OperatingSystem, this.CommonWebConfig.AppInsights with
                    | Windows, Some _ ->
                        "APPINSIGHTS_PROFILERFEATURE_VERSION", "1.0.0"
                        "APPINSIGHTS_SNAPSHOTFEATURE_VERSION", "1.0.0"
                        "ApplicationInsightsAgent_EXTENSION_VERSION", "~2"
                        "DiagnosticServices_EXTENSION_VERSION", "~3"
                        "InstrumentationEngine_EXTENSION_VERSION", "~1"
                        "SnapshotDebugger_EXTENSION_VERSION", "~1"
                        "XDT_MicrosoftApplicationInsights_BaseExtensions", "~1"
                        "XDT_MicrosoftApplicationInsights_Mode", "recommended"
                    | Linux, Some _
                    | _ , None ->
                        ()

                    if this.DockerCi then "DOCKER_ENABLE_CI", "true"
                ]

                let dockerSettings = [
                    match this.DockerAcrCredentials with
                    | Some credentials ->
                        "DOCKER_REGISTRY_SERVER_PASSWORD", ParameterSetting credentials.Password
                        Setting.AsLiteral ("DOCKER_REGISTRY_SERVER_URL", $"https://{credentials.RegistryName}.azurecr.io")
                        Setting.AsLiteral ("DOCKER_REGISTRY_SERVER_USERNAME", credentials.RegistryName)
                    | None ->
                        ()
                ]
                literalSettings
                |> List.map Setting.AsLiteral
                |> List.append dockerSettings
                |> List.append (
                    (match this.CommonWebConfig.SecretStore with
                     | AppService ->
                         this.CommonWebConfig.Settings
                     | KeyVault r ->
                        let name = r.resourceId (this.CommonWebConfig)
                        [ for setting in this.CommonWebConfig.Settings do
                            match setting.Value with
                            | LiteralSetting _ ->
                                setting.Key, setting.Value
                            | ParameterSetting _
                            | ExpressionSetting _ ->
                                setting.Key, LiteralSetting $"@Microsoft.KeyVault(SecretUri=https://{name.Name.Value}.vault.azure.net/secrets/{setting.Key})"
                        ] |> Map.ofList
                    ) |> Map.toList)
                |> Map

            let site =
                { SiteType = Site this.Name
                  Location = location
                  ServicePlan = this.ServicePlanId
                  HTTPSOnly = this.CommonWebConfig.HTTPSOnly
                  HTTP20Enabled = this.HTTP20Enabled
                  ClientAffinityEnabled = this.ClientAffinityEnabled
                  WebSocketsEnabled = this.WebSocketsEnabled
                  Identity = this.CommonWebConfig.Identity
                  KeyVaultReferenceIdentity = this.CommonWebConfig.KeyVaultReferenceIdentity
                  Cors = this.CommonWebConfig.Cors
                  Tags = this.Tags
                  ConnectionStrings = this.ConnectionStrings
                  WorkerProcess = this.CommonWebConfig.WorkerProcess
                  AppSettings = siteSettings
                  Kind = [
                    "app"
                    match this.CommonWebConfig.OperatingSystem with Linux -> "linux" | Windows -> ()
                    match this.DockerImage with Some _ -> "container" | _ -> ()
                  ] |> String.concat ","
                  Dependencies = Set [
                    match this.CommonWebConfig.ServicePlan with
                    | DependableResource this.Name.ResourceName resourceId -> resourceId
                    | _ -> ()

                    yield! this.Dependencies

                    match this.CommonWebConfig.SecretStore with
                    | AppService ->
                        for setting in this.CommonWebConfig.Settings do
                            match setting.Value with
                            | ExpressionSetting expr ->
                                yield! Option.toList expr.Owner
                            | ParameterSetting _
                            | LiteralSetting _ ->
                                ()
                    | KeyVault _ ->
                        ()

                    match this.CommonWebConfig.AppInsights with
                    | Some (DependableResource this.Name.ResourceName resourceId) -> resourceId
                    | Some _ | None -> ()
                  ]
                  AlwaysOn = this.CommonWebConfig.AlwaysOn
                  LinuxFxVersion =
                    match this.CommonWebConfig.OperatingSystem with
                    | Windows ->
                        None
                    | Linux ->
                        match this.DockerImage with
                        | Some (image, _) ->
                            Some ("DOCKER|" + image)
                        | None ->
                            match this.Runtime with
                            | DotNetCore version -> Some $"DOTNETCORE|{version}"
                            | Node version -> Some $"NODE|{version}"
                            | Php version -> Some $"PHP|{version}"
                            | Ruby version -> Some $"RUBY|{version}"
                            | Java (runtime, JavaSE) -> Some $"JAVA|{runtime.Version}-{runtime.Jre}"
                            | Java (runtime, (Tomcat version)) -> Some $"TOMCAT|{version}-{runtime.Jre}"
                            | Java (Java8, WildFly14) -> Some $"WILDFLY|14-{Java8.Jre}"
                            | Python (linuxVersion, _) -> Some $"PYTHON|{linuxVersion}"
                            | _ -> None
                  NetFrameworkVersion =
                    match this.Runtime with
                    | AspNet version
                    | DotNet ("5.0" as version) ->
                        Some $"v{version}"
                    | _ ->
                        None
                  JavaVersion =
                    match this.Runtime, this.CommonWebConfig.OperatingSystem with
                    | Java (Java11, Tomcat _), Windows -> Some "11"
                    | Java (Java8, Tomcat _), Windows -> Some "1.8"
                    | _ -> None
                  JavaContainer =
                    match this.Runtime, this.CommonWebConfig.OperatingSystem with
                    | Java (_, Tomcat _), Windows -> Some "Tomcat"
                    | _ -> None
                  JavaContainerVersion =
                    match this.Runtime, this.CommonWebConfig.OperatingSystem with
                    | Java (_, Tomcat version), Windows -> Some version
                    | _ -> None
                  PhpVersion =
                    match this.Runtime, this.CommonWebConfig.OperatingSystem with
                    | Php version, Windows -> Some version
                    | _ -> None
                  PythonVersion =
                    match this.Runtime, this.CommonWebConfig.OperatingSystem with
                    | Python (_, windowsVersion), Windows -> Some windowsVersion
                    | _ -> None
                  Metadata =
                    match this.Runtime, this.CommonWebConfig.OperatingSystem with
                    | Java (_, Tomcat _), Windows -> Some "java"
                    | Php _, _ -> Some "php"
                    | Python _, Windows -> Some "python"
                    | DotNetCore _, Windows -> Some "dotnetcore"
                    | AspNet _, _ | DotNet "5.0", Windows -> Some "dotnet"
                    | _ -> None
                    |> Option.map(fun stack -> "CURRENT_STACK", stack)
                    |> Option.toList
                  AppCommandLine = this.DockerImage |> Option.map snd
                  AutoSwapSlotName = None
                  ZipDeployPath = this.CommonWebConfig.ZipDeployPath |> Option.map (fun (path,slot) -> path, ZipDeploy.ZipDeployTarget.WebApp, slot )
                  HealthCheckPath = this.CommonWebConfig.HealthCheckPath
                }

            match keyVault with
            | Some keyVault ->
                let builder = keyVault :> IBuilder
                yield! builder.BuildResources location
            | None ->
                ()

            match this.SourceControlSettings with
            | Some settings ->
                { Website = this.Name.ResourceName
                  Location = location
                  Repository = settings.Repository
                  Branch = settings.Branch
                  ContinuousIntegration = settings.ContinuousIntegration }
            | None ->
                ()

            match this.CommonWebConfig.AppInsights with
            | Some (DeployableResource this.Name.ResourceName resourceId) ->
                { Name = resourceId.Name
                  Location = location
                  DisableIpMasking = false
                  SamplingPercentage = 100
                  LinkedWebsite =
                    match this.CommonWebConfig.OperatingSystem with
                    | Windows -> Some this.Name.ResourceName
                    | Linux -> None
                  Tags = this.Tags }
            | Some _
            | None ->
                ()

            match this.CommonWebConfig.ServicePlan with
            | DeployableResource this.Name.ResourceName resourceId ->
                { Name = resourceId.Name
                  Location = location
                  Sku = this.Sku
                  WorkerSize = this.WorkerSize
                  WorkerCount = this.WorkerCount
                  MaximumElasticWorkerCount = this.MaximumElasticWorkerCount
                  OperatingSystem = this.CommonWebConfig.OperatingSystem
                  Tags = this.Tags}
            | _ ->
                ()

            for (ExtensionName extension) in this.SiteExtensions do
                { Name = ResourceName extension
                  SiteName = this.Name.ResourceName
                  Location = location }

            if Map.isEmpty this.CommonWebConfig.Slots then
                site
            else
                { site with AppSettings = Map.empty; ConnectionStrings = Map.empty }
                for (_,slot) in this.CommonWebConfig.Slots |> Map.toSeq do
                    slot.ToSite site

            match this.CustomDomain with
            | SecureDomain (customDomain, certOptions) ->
                let hostNameBinding =
                    { Location = location
                      SiteId =  Managed (Arm.Web.sites.resourceId this.Name.ResourceName)
                      DomainName = customDomain
                      SslState = SslDisabled } // Initially create non-secure host name binding, we link the certificate in a nested deployment below
                let cert =
                    { Location = location
                      SiteId = this.ResourceId
                      ServicePlanId = this.ServicePlanId
                      DomainName = customDomain }
                hostNameBinding
                cert
                let resourceLocation = location

                // nested deployment to update hostname binding with specified SSL options
                yield! (resourceGroup { 
                    name "[resourceGroup().name]"
                    location resourceLocation
                    add_resource { hostNameBinding with
                                    SiteId =
                                        match hostNameBinding.SiteId with 
                                        | Managed id -> Unmanaged id
                                        | x -> x 
                                    SslState = 
                                        match certOptions with
                                        | AppManagedCertificate -> SniBased cert.Thumbprint
                                        | CustomCertificate thumbprint -> SniBased thumbprint
                                  }
                    depends_on [ Arm.Web.certificates.resourceId cert.ResourceName
                                 hostNameBinding.ResourceId ]
                } :> IBuilder).BuildResources location
            | InsecureDomain customDomain -> 
                { Location = location
                  SiteId =  Managed (Arm.Web.sites.resourceId this.Name.ResourceName)
                  DomainName = customDomain
                  SslState = SslDisabled }
            | NoDomain -> ()

            yield! (PrivateEndpoint.create location this.ResourceId ["sites"] this.PrivateEndpoints)
        ]

type WebAppBuilder() =
    member _.Yield _ =
        { CommonWebConfig =
            { Name = WebAppName.Empty
              AlwaysOn = false
              AppInsights = Some (derived (fun name -> components.resourceId (name-"ai")))
              Cors = None
              HTTPSOnly = false
              Identity = ManagedIdentity.Empty
              KeyVaultReferenceIdentity = None
              OperatingSystem = Windows
              SecretStore = AppService
              ServicePlan = derived (fun name -> serverFarms.resourceId (name-"farm"))
              Settings = Map.empty
              Slots = Map.empty
              WorkerProcess = None
              ZipDeployPath = None
              HealthCheckPath = None }
          Sku = Sku.F1
          WorkerSize = Small
          WorkerCount = 1
          MaximumElasticWorkerCount = None
          RunFromPackage = false
          WebsiteNodeDefaultVersion = None
          HTTP20Enabled = None
          ClientAffinityEnabled = None
          WebSocketsEnabled = None
          ConnectionStrings = Map.empty
          Tags = Map.empty
          Dependencies = Set.empty
          Runtime = Runtime.DotNetCoreLts
          DockerImage = None
          DockerCi = false
          SourceControlSettings = None
          DockerAcrCredentials = None
          AutomaticLoggingExtension = true
          SiteExtensions = Set.empty
          PrivateEndpoints = Set.empty
          CustomDomain = NoDomain}
    member _.Run(state:WebAppConfig) =
        if state.Name.ResourceName = ResourceName.Empty then raiseFarmer "No Web App name has been set."
        { state with
            SiteExtensions =
                match state with
                // its important to only add this extension if we're not using Web App for Containers - if we are
                // then this will generate an error during deployment:
                // No route registered for '/api/siteextensions/Microsoft.AspNetCore.AzureAppServices.SiteExtension'
                | { Runtime = Runtime.DotNetCore _; AutomaticLoggingExtension = true ; DockerImage = None } ->
                    state.SiteExtensions.Add WebApp.Extensions.Logging
                | _ ->
                    state.SiteExtensions
            DockerImage =
                match state.DockerImage, state.DockerAcrCredentials with
                | Some (image, tag), Some credentials when not (image.Contains "azurecr.io") ->
                    Some ($"{credentials.RegistryName}.azurecr.io/{image}", tag)
                | Some x, _ ->
                    Some x
                | None, _ ->
                    None
        }

    [<CustomOperation "sku">]
    member _.Sku(state:WebAppConfig, sku) = { state with Sku = sku }
    /// Sets the size of the service plan worker.
    [<CustomOperation "worker_size">]
    member _.WorkerSize(state:WebAppConfig, workerSize) = { state with WorkerSize = workerSize }
    /// Sets the number of instances on the service plan.
    [<CustomOperation "number_of_workers">]
    member _.NumberOfWorkers(state:WebAppConfig, workerCount) = { state with WorkerCount = workerCount }
    /// Sets the web app to use "run from package" deployment capabilities.
    [<CustomOperation "run_from_package">]
    member _.RunFromPackage(state:WebAppConfig) = { state with RunFromPackage = true }
    /// Sets the node version of the web app.
    [<CustomOperation "website_node_default_version">]
    member _.NodeVersion(state:WebAppConfig, version) = { state with WebsiteNodeDefaultVersion = Some version }
    /// Creates a set of connection strings of the web app whose values will be supplied as secret parameters.
    [<CustomOperation "connection_string">]
    member _.AddConnectionString(state:WebAppConfig, key) =
        { state with ConnectionStrings = state.ConnectionStrings.Add(key, (ParameterSetting (SecureParameter key), Custom)) }
    member _.AddConnectionString(state:WebAppConfig, (key, value:ArmExpression)) =
        { state with ConnectionStrings = state.ConnectionStrings.Add(key, (ExpressionSetting value, Custom)) }
    /// Creates a set of connection strings of the web app whose values will be supplied as secret parameters.
    [<CustomOperation "connection_strings">]
    member this.AddConnectionStrings(state:WebAppConfig, connectionStrings) =
        connectionStrings
        |> List.fold (fun (state:WebAppConfig) (key:string) -> this.AddConnectionString(state, key)) state
    /// Enables HTTP 2.0 for this webapp.
    [<CustomOperation "enable_http2">]
    member _.Http20Enabled(state:WebAppConfig) = { state with HTTP20Enabled = Some true }
    /// Disables client affinity for this webapp.
    [<CustomOperation "disable_client_affinity">]
    member _.ClientAffinityEnabled(state:WebAppConfig) = { state with ClientAffinityEnabled = Some false }
    /// Enables websockets for this webapp.
    [<CustomOperation "enable_websockets">]
    member _.WebSockets(state:WebAppConfig) = { state with WebSocketsEnabled = Some true }
    /// Sets the runtime stack
    [<CustomOperation "runtime_stack">]
    member _.RuntimeStack(state:WebAppConfig, runtime) = { state with Runtime = runtime }
    [<CustomOperation "docker_image">]
    /// Specifies a docker image to use from the registry (linux only), and the startup command to execute.
    member _.DockerImage(state:WebAppConfig, registryPath, startupFile) =
        { state with
            CommonWebConfig = { state.CommonWebConfig with OperatingSystem = Linux }
            DockerImage = Some (registryPath, startupFile) }
    [<CustomOperation "docker_ci">]
    /// Have your custom Docker image automatically re-deployed when a new version is pushed to e.g. Docker hub.
    member _.DockerCI(state:WebAppConfig) = { state with DockerCi = true }
    [<CustomOperation "docker_use_azure_registry">]
    /// Have your custom Docker image automatically re-deployed when a new version is pushed to e.g. Docker hub.
    member _.DockerAcrCredentials(state:WebAppConfig, registryName) =
        { state with
            DockerAcrCredentials =
                Some {| RegistryName = registryName
                        Password = SecureParameter $"docker-password-for-{registryName}" |} }
    [<CustomOperation "source_control">]
    member _.SourceControl(state:WebAppConfig, url, branch) =
        { state with
            SourceControlSettings =
                Some {| Repository = Uri url
                        Branch = branch
                        ContinuousIntegration = Enabled |} }
    member _.SourceControlCi(state:WebAppConfig, featureFlag) =
        { state with
            SourceControlSettings =
                state.SourceControlSettings
                |> Option.map(fun s -> {| s with ContinuousIntegration = featureFlag |}) }
    [<CustomOperation "enable_source_control_ci">]
    member this.EnableCi(state:WebAppConfig) = this.SourceControlCi(state, Enabled)
    [<CustomOperation "disable_source_control_ci">]
    member this.DisableCi(state:WebAppConfig) = this.SourceControlCi(state, Disabled)
    [<CustomOperation "add_extension">]
    member _.AddExtension (state:WebAppConfig, extension) = { state with SiteExtensions = state.SiteExtensions.Add extension }
    member this.AddExtension (state:WebAppConfig, name) = this.AddExtension (state, ExtensionName name)
    /// Automatically add the ASP.NET Core logging extension.
    [<CustomOperation "automatic_logging_extension">]
    member _.DefaultLogging (state:WebAppConfig, setting) = { state with AutomaticLoggingExtension = setting }
    //Add Custom domain to you web app
    [<CustomOperation "custom_domain">]
    member _.CustomDomain(state:WebAppConfig, domainConfig) = { state with CustomDomain = domainConfig }
    member _.CustomDomain(state:WebAppConfig, customDomain) = { state with CustomDomain = SecureDomain (customDomain,AppManagedCertificate) }
    member _.CustomDomain(state:WebAppConfig, (customDomain,thumbprint)) = { state with CustomDomain = SecureDomain (customDomain,CustomCertificate thumbprint) }

    interface IPrivateEndpoints<WebAppConfig> with member _.Add state endpoints = { state with PrivateEndpoints = state.PrivateEndpoints |> Set.union endpoints}
    interface ITaggable<WebAppConfig> with member _.Add state tags = { state with Tags = state.Tags |> Map.merge tags }
    interface IDependable<WebAppConfig> with member _.Add state newDeps = { state with Dependencies = state.Dependencies + newDeps }
    interface IServicePlanApp<WebAppConfig> with
        member _.Get state = state.CommonWebConfig
        member _.Wrap state config = {state with CommonWebConfig = config}

let webApp = WebAppBuilder()

/// Allow adding storage accounts directly to CDNs
type EndpointBuilder with
    member this.Origin(state:EndpointConfig, webApp:WebAppConfig) =
        let state = this.Origin(state, webApp.Endpoint)
        this.DependsOn(state, webApp.ResourceId)

/// An interface for shared capabilities between builders that work with Service Plan-style apps.
/// In other words, Web Apps or Functions.
type IServicePlanApp<'T> =
    abstract member Get : 'T -> CommonWebConfig
    abstract member Wrap : 'T -> CommonWebConfig -> 'T

// Common keywords for IServicePlanApp live here.
[<AutoOpen>]
module Extensions =
    type IServicePlanApp<'T> with
        member private this.Map (state:'T) f = this.Wrap state ( f (this.Get state) )
        /// Sets the name of the web app.
        [<CustomOperation "name">]
        member this.Name (state:'T, name) = { this.Get state with Name = name } |> this.Wrap state
        member this.Name (state:'T, name:ResourceName) = this.Name (state, (WebAppName.Create name |> Result.get))
        member this.Name (state:'T, name:string) = this.Name(state, ResourceName name)
        /// Sets the name of the service plan.
        [<CustomOperation "service_plan_name">]
        member this.SetServicePlanName (state:'T, name) = { this.Get state with ServicePlan = named serverFarms name } |> this.Wrap state
        member this.SetServicePlanName (state:'T, name:string) = this.SetServicePlanName(state, ResourceName name)
        /// Instead of creating a new service plan instance, configure this webapp to point to another Farmer-managed service plan instance.
        /// A dependency will automatically be set for this instance.
        [<CustomOperation "link_to_service_plan">]
        member this.LinkToServicePlan (state:'T, name) = { this.Get state with ServicePlan = managed serverFarms name } |> this.Wrap state
        member this.LinkToServicePlan (state:'T, name:string) = this.LinkToServicePlan (state, ResourceName name)
        member this.LinkToServicePlan (state:'T, config:ServicePlanConfig) = this.LinkToServicePlan (state, config.Name)
        /// Instead of creating a new service plan instance, configure this webapp to point to another unmanaged service plan instance.
        /// A dependency will automatically be set for this instance.
        [<CustomOperation "link_to_unmanaged_service_plan">]
        member this.LinkToUnmanagedServicePlan (state:'T, resourceId) = { this.Get state with ServicePlan = unmanaged resourceId } |> this.Wrap state
        /// Sets the name of the automatically-created app insights instance.
        [<CustomOperation "app_insights_name">]
        member this.UseAppInsights (state:'T, name) = { this.Get state with AppInsights = Some (named components name) } |> this.Wrap state
        member this.UseAppInsights (state:'T, name:string) = this.UseAppInsights(state, ResourceName name)
        /// Removes any automatic app insights creation, configuration and settings for this webapp.
        [<CustomOperation "app_insights_off">]
        member this.DeactivateAppInsights (state:'T) = { this.Get state with AppInsights = None } |> this.Wrap state
        /// Instead of creating a new AI instance, configure this webapp to point to another Farmer-managed AI instance.
        /// A dependency will automatically be set for this instance.
        [<CustomOperation "link_to_app_insights">]
        member this.LinkToAi (state:'T, name) = { this.Get state with AppInsights = Some (managed components name) } |> this.Wrap state
        member this.LinkToAi (state:'T, name) = this.LinkToAi (state, ResourceName name)
        member this.LinkToAi (state:'T, name:ResourceName option) = match name with Some name -> this.LinkToAi (state, name) | None -> state
        member this.LinkToAi (state:'T, config:AppInsightsConfig) = this.LinkToAi (state, config.Name)
        /// Instead of creating a new AI instance, configure this webapp to point to an unmanaged AI instance.
        /// A dependency will not be set for this instance.
        [<CustomOperation "link_to_unmanaged_app_insights">]
        member this.LinkUnmanagedAppInsights (state:'T, resourceId) = { this.Get state with AppInsights = Some (unmanaged resourceId) } |> this.Wrap state
        /// Sets an app setting of the web app in the form "key" "value".
        [<CustomOperation "setting">]
        member this.AddSetting (state:'T, key, value) =
            let current = this.Get state
            { current with Settings = current.Settings.Add(key, LiteralSetting value) }
            |> this.Wrap state
        member this.AddSetting (state:'T, key, resourceName:ResourceName) = this.AddSetting(state, key, resourceName.Value)
        member this.AddSetting (state:'T, key, value:ArmExpression) =
            let current = this.Get state
            { current with Settings = current.Settings.Add(key, ExpressionSetting value) }
            |> this.Wrap state
        /// Sets a list of app setting of the web app in the form "key" "value".
        [<CustomOperation "settings">]
        member this.AddSettings(state:'T, settings: (string*string) list) =
            let current = this.Get state
            settings
            |> List.fold (fun (state:CommonWebConfig) (key, value: string) -> { state with Settings = state.Settings.Add(key, LiteralSetting value) }) current
            |> this.Wrap state
        member this.AddSettings(state:'T, settings) =
            let current = this.Get state
            settings
            |> List.fold (fun (state:CommonWebConfig) (key, value:ArmExpression) -> { state with Settings = state.Settings.Add(key, ExpressionSetting value) }) current
            |> this.Wrap state
        /// Sets an app setting of the web app in the form "key" "value".
        [<CustomOperation "add_identity">]
        member this.AddIdentity (state:'T, identity:UserAssignedIdentity) =
            let current = this.Get state
            { current with
                Identity = current.Identity + identity
                Settings = current.Settings.Add("AZURE_CLIENT_ID", Setting.ExpressionSetting identity.ClientId) }
            |> this.Wrap state
        member this.AddIdentity (state, identity:UserAssignedIdentityConfig) = this.AddIdentity(state, identity.UserAssignedIdentity)
        [<CustomOperation "keyvault_identity">]
        member this.AddKeyVaultIdentity (state:'T, identity:UserAssignedIdentity) =
            let current = this.Get state
            { current with
                Identity = current.Identity + identity
                KeyVaultReferenceIdentity = Some identity
                Settings = current.Settings.Add("AZURE_CLIENT_ID", Setting.ExpressionSetting identity.ClientId) }
            |> this.Wrap state
        member this.AddKeyVaultIdentity (state, identity:UserAssignedIdentityConfig) = this.AddKeyVaultIdentity(state, identity.UserAssignedIdentity)
        [<CustomOperation "system_identity">]
        member this.SystemIdentity (state:'T) =
            let current = this.Get state
            { current with Identity = { current.Identity with SystemAssigned = Enabled } }
            |> this.Wrap state

        /// sets the list of origins that should be allowed to make cross-origin calls. Use AllOrigins to allow all.
        [<CustomOperation "enable_cors">]
        member this.EnableCors (state:'T, origins) =
            { this.Get state with
                Cors =
                    match origins with
                    | [ "*" ] -> Some AllOrigins
                    | origins -> Some (SpecificOrigins (List.map Uri origins, None)) }
            |> this.Wrap state
        member this.EnableCors (state:'T, origins) = { this.Get state with Cors = Some origins } |> this.Wrap state
        /// Allows CORS requests with credentials.
        [<CustomOperation "enable_cors_credentials">]
        member this.EnableCorsCredentials (state:'T) =
            let current = this.Get state
            { current with
                Cors =
                    current.Cors
                    |> Option.map (function
                    | SpecificOrigins (origins, _) -> SpecificOrigins (origins, Some true)
                    | AllOrigins -> raiseFarmer "You cannot enable CORS Credentials if you have already set CORS to AllOrigins.") }
            |> this.Wrap state
        /// Sets the operating system
        [<CustomOperation "operating_system">]
        member this.OperatingSystem (state:'T, os) = { this.Get state with OperatingSystem = os } |> this.Wrap state
        /// Specifies a folder path or a zip file containing the web application to install as a post-deployment task.
        [<CustomOperation "zip_deploy">]
        member this.ZipDeploy (state:'T, path) = { this.Get state with ZipDeployPath = Some (path,ZipDeploy.ProductionSlot) } |> this.Wrap state
        /// Specifies a folder path or a zip file containing the web application to install as a post-deployment task.
        [<CustomOperation "zip_deploy_slot">]
        member this.ZipDeploySlot (state:'T, slotName,path) = { this.Get state with ZipDeployPath = Some (path, ZipDeploy.NamedSlot slotName) } |> this.Wrap state
        /// Creates an app setting of the web app whose value will be supplied as a secret parameter.
        [<CustomOperation "secret_setting">]
        member this.AddSecret (state:'T, key) =
            let current = this.Get state
            { current with Settings = current.Settings.Add(key, ParameterSetting (SecureParameter key)) }
            |> this.Wrap state
        /// Sets "Always On" flag
        [<CustomOperation "always_on">]
        member this.AlwaysOn(state:'T) = { this.Get state with AlwaysOn = true } |> this.Wrap state
        ///Chooses the bitness (32 or 64) of the worker process
        [<CustomOperation "worker_process">]
        member this.WorkerProcess (state:'T, bitness) = { this.Get state with WorkerProcess = Some bitness } |> this.Wrap state
        /// Creates a key vault instance. All secret settings will automatically be mapped into key vault.
        [<CustomOperation "use_keyvault">]
        member this.UseKeyVault (state:'T) =
            let current = this.Get state
            { current with
                Identity = { current.Identity with SystemAssigned = Enabled }
                SecretStore = KeyVault (derived(fun c -> vaults.resourceId (ResourceName (c.Name.ResourceName.Value + "vault")))) }
            |> this.Wrap state
        /// Links your application to a Farmer-managed key vault instance. All secret settings will automatically be mapped into key vault.
        [<CustomOperation "link_to_keyvault">]
        member this.LinkToKeyVault (state:'T, vaultName:ResourceName) =
            let current = this.Get state
            { current with
                Identity = { current.Identity with SystemAssigned = Enabled }
                SecretStore = KeyVault (managed vaults vaultName) }
            |> this.Wrap state
        /// Links your application to an existing key vault instance. All secret settings will automatically be mapped into key vault.
        [<CustomOperation "link_to_unmanaged_keyvault">]
        member this.LinkToExternalKeyVault(state:'T, resourceId) =
            let current = this.Get state
            { current with
                Identity = { current.Identity with SystemAssigned = Enabled }
                SecretStore = KeyVault (unmanaged resourceId) }
            |> this.Wrap state
        /// Adds a deployment slot to the app
        [<CustomOperation "add_slot">]
        member this.AddSlot (state:'T, slot:SlotConfig) =
            let current = this.Get state
            { current with Slots = current.Slots |> Map.add slot.Name slot}
            |> this.Wrap state
        member this.AddSlot (state:'T, slotName:string) = this.AddSlot(state, appSlot{ name slotName })
        /// Adds deployment slots to the app
        [<CustomOperation "add_slots">]
        member this.AddSlots (state:'T, slots:SlotConfig list) =
            let current = this.Get state
            { current with Slots = slots |> List.fold (fun m s -> Map.add s.Name s m) current.Slots}
            |> this.Wrap state

        /// Disables http for this webapp so that only https is used.
        [<CustomOperation "https_only">]
        member this.HttpsOnly(state:'T) = this.Map state (fun x -> { x with HTTPSOnly = true })

        [<CustomOperation "health_check_path">]
        /// Specifies the path Azure load balancers will ping to check for unhealthy instances.
        member this.HealthCheckPath(state:'T, healthCheckPath:string) = this.Map state (fun x -> {x with HealthCheckPath = Some(healthCheckPath)})
