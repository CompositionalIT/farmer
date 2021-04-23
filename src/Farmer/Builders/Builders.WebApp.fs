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

module private WebAppConfig =
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
    let FromCommon state (config: CommonWebConfig): WebAppConfig =
        { state with
            Name = config.Name
            ServicePlan = config.ServicePlan
            AppInsights = config.AppInsights
            OperatingSystem = config.OperatingSystem
            Settings = config.Settings
            Cors = config.Cors
            Identity = config.Identity
            SecretStore = config.SecretStore
            ZipDeployPath = config.ZipDeployPath
            AlwaysOn = config.AlwaysOn
            WorkerProcess = config.WorkerProcess }

/// Common fields between WebApp and Functions
type CommonWebConfig =
    { Name : ResourceName
      ServicePlan : ResourceRef<ResourceName>
      AppInsights : ResourceRef<ResourceName> option
      OperatingSystem : OS
      Settings : Map<string, Setting>
      Cors : Cors option
      Identity : Identity.ManagedIdentity
      SecretStore : SecretStore
      ZipDeployPath : string option
      AlwaysOn : bool
      WorkerProcess : Bitness option }

type WebAppConfig =
    { Name : ResourceName
      ServicePlan : ResourceRef<ResourceName>
      AppInsights : ResourceRef<ResourceName> option
      OperatingSystem : OS
      Settings : Map<string, Setting>
      Cors : Cors option
      Identity : Identity.ManagedIdentity
      ZipDeployPath : string option
      HTTPSOnly : bool
      HTTP20Enabled : bool option
      ClientAffinityEnabled : bool option
      WebSocketsEnabled: bool option
      ConnectionStrings : Map<string, (Setting * ConnectionStringKind)>
      Dependencies : ResourceId Set
      Tags : Map<string,string>
      Sku : Sku
      WorkerSize : WorkerSize
      WorkerCount : int
      RunFromPackage : bool
      WebsiteNodeDefaultVersion : string option
      AlwaysOn : bool
      Runtime : Runtime
      SourceControlSettings : {| Repository : Uri; Branch : string; ContinuousIntegration : FeatureFlag |} option
      DockerImage : (string * string) option
      DockerCi : bool
      DockerAcrCredentials : {| RegistryName : string; Password : SecureParameter |} option
      SecretStore : SecretStore
      AutomaticLoggingExtension : bool
      SiteExtensions : ExtensionName Set
      WorkerProcess : Bitness option }
    /// Gets this web app's Server Plan's full resource ID.
    member this.ServicePlanId = this.ServicePlan.resourceId this.Name
    /// Gets the Service Plan name for this web app.
    member this.ServicePlanName = this.ServicePlanId.Name
    /// Gets the App Insights name for this web app, if it exists.
    member this.AppInsightsName = this.AppInsights |> Option.map (fun ai -> ai.resourceId(this.Name).Name)
    /// Gets the ARM expression path to the publishing password of this web app.
    member this.PublishingPassword = publishingPassword (this.Name)
    member this.Endpoint = $"{this.Name.Value}.azurewebsites.net"
    member this.SystemIdentity = SystemIdentity this.ResourceId
    member this.ResourceId = sites.resourceId this.Name
    interface IBuilder with
        member this.ResourceId = this.ResourceId
        member this.BuildResources location = [
            let keyVault, secrets =
                match this.SecretStore with
                | KeyVault (DeployableResource (WebAppConfig.ToCommon this) vaultName) ->
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

            { Name = this.Name
              Location = location
              ServicePlan = this.ServicePlanId
              HTTPSOnly = this.HTTPSOnly
              HTTP20Enabled = this.HTTP20Enabled
              ClientAffinityEnabled = this.ClientAffinityEnabled
              WebSocketsEnabled = this.WebSocketsEnabled
              Identity = this.Identity
              Cors = this.Cors
              Tags = this.Tags
              ConnectionStrings = this.ConnectionStrings
              WorkerProcess = this.WorkerProcess
              AppSettings =
                let literalSettings = [
                    if this.RunFromPackage then AppSettings.RunFromPackage
                    yield! this.WebsiteNodeDefaultVersion |> Option.mapList AppSettings.WebsiteNodeDefaultVersion
                    yield! this.AppInsights |> Option.mapList (fun resource -> "APPINSIGHTS_INSTRUMENTATIONKEY", AppInsights.getInstrumentationKey(resource.resourceId this.Name).Eval())
                    match this.OperatingSystem, this.AppInsights with
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
                    (match this.SecretStore with
                     | AppService ->
                         this.Settings
                     | KeyVault r ->
                        let name = r.resourceId (WebAppConfig.ToCommon this)
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
              Kind = [
                "app"
                match this.OperatingSystem with Linux -> "linux" | Windows -> ()
                match this.DockerImage with Some _ -> "container" | _ -> ()
              ] |> String.concat ","
              Dependencies = Set [
                match this.ServicePlan with
                | DependableResource this.Name resourceId -> resourceId
                | _ -> ()

                yield! this.Dependencies

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

                match this.AppInsights with
                | Some (DependableResource this.Name resourceId) -> resourceId
                | Some _ | None -> ()
              ]
              AlwaysOn = this.AlwaysOn
              LinuxFxVersion =
                match this.OperatingSystem with
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
                match this.Runtime, this.OperatingSystem with
                | Java (Java11, Tomcat _), Windows -> Some "11"
                | Java (Java8, Tomcat _), Windows -> Some "1.8"
                | _ -> None
              JavaContainer =
                match this.Runtime, this.OperatingSystem with
                | Java (_, Tomcat _), Windows -> Some "Tomcat"
                | _ -> None
              JavaContainerVersion =
                match this.Runtime, this.OperatingSystem with
                | Java (_, Tomcat version), Windows -> Some version
                | _ -> None
              PhpVersion =
                match this.Runtime, this.OperatingSystem with
                | Php version, Windows -> Some version
                | _ -> None
              PythonVersion =
                match this.Runtime, this.OperatingSystem with
                | Python (_, windowsVersion), Windows -> Some windowsVersion
                | _ -> None
              Metadata =
                match this.Runtime, this.OperatingSystem with
                | Java (_, Tomcat _), Windows -> Some "java"
                | Php _, _ -> Some "php"
                | Python _, Windows -> Some "python"
                | DotNetCore _, Windows -> Some "dotnetcore"
                | AspNet _, _ | DotNet "5.0", Windows -> Some "dotnet"
                | _ -> None
                |> Option.map(fun stack -> "CURRENT_STACK", stack)
                |> Option.toList
              AppCommandLine = this.DockerImage |> Option.map snd
              ZipDeployPath = this.ZipDeployPath |> Option.map (fun x -> x, ZipDeploy.ZipDeployTarget.WebApp)
            }

            match keyVault with
            | Some keyVault ->
                let builder = keyVault :> IBuilder
                yield! builder.BuildResources location
            | None ->
                ()

            match this.SourceControlSettings with
            | Some settings ->
                { Website = this.Name
                  Location = location
                  Repository = settings.Repository
                  Branch = settings.Branch
                  ContinuousIntegration = settings.ContinuousIntegration }
            | None ->
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

            match this.ServicePlan with
            | DeployableResource this.Name resourceId ->
                { Name = resourceId.Name
                  Location = location
                  Sku = this.Sku
                  WorkerSize = this.WorkerSize
                  WorkerCount = this.WorkerCount
                  OperatingSystem = this.OperatingSystem
                  Tags = this.Tags}
            | _ ->
                ()

            for (ExtensionName extension) in this.SiteExtensions do
                { Name = ResourceName extension
                  SiteName = this.Name
                  Location = location }
        ]

type WebAppBuilder() =
    member __.Yield _ =
        { Name = ResourceName.Empty
          ServicePlan = derived (fun name -> serverFarms.resourceId (name-"farm"))
          AppInsights = Some (derived (fun name -> components.resourceId (name-"ai")))
          Settings = Map.empty
          Identity = ManagedIdentity.Empty
          Cors = None
          OperatingSystem = Windows
          ZipDeployPath = None
          Sku = Sku.F1
          WorkerSize = Small
          WorkerCount = 1
          RunFromPackage = false
          WebsiteNodeDefaultVersion = None
          AlwaysOn = false
          HTTPSOnly = false
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
          SecretStore = AppService
          AutomaticLoggingExtension = true
          SiteExtensions = Set.empty
          WorkerProcess = None }
    member __.Run(state:WebAppConfig) =
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

    /// Disables http for this webapp so that only https is used.
    [<CustomOperation "https_only">]
    member __.HttpsOnly(state:WebAppConfig) = { state with HTTPSOnly = true }
    /// Enables HTTP 2.0 for this webapp.
    [<CustomOperation "enable_http2">]
    member __.Http20Enabled(state:WebAppConfig) = { state with HTTP20Enabled = Some true }
    /// Disables client affinity for this webapp.
    [<CustomOperation "disable_client_affinity">]
    member __.ClientAffinityEnabled(state:WebAppConfig) = { state with ClientAffinityEnabled = Some false }
    /// Enables websockets for this webapp.
    [<CustomOperation "enable_websockets">]
    member __.WebSockets(state:WebAppConfig) = { state with WebSocketsEnabled = Some true }
    /// Sets the runtime stack
    [<CustomOperation "runtime_stack">]
    member __.RuntimeStack(state:WebAppConfig, runtime) = { state with Runtime = runtime }
    [<CustomOperation "docker_image">]
    /// Specifies a docker image to use from the registry (linux only), and the startup command to execute.
    member __.DockerImage(state:WebAppConfig, registryPath, startupFile) =
        { state with
            OperatingSystem = Linux
            DockerImage = Some (registryPath, startupFile) }
    [<CustomOperation "docker_ci">]
    /// Have your custom Docker image automatically re-deployed when a new version is pushed to e.g. Docker hub.
    member __.DockerCI(state:WebAppConfig) = { state with DockerCi = true }
    [<CustomOperation "docker_use_azure_registry">]
    /// Have your custom Docker image automatically re-deployed when a new version is pushed to e.g. Docker hub.
    member __.DockerAcrCredentials(state:WebAppConfig, registryName) =
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
    interface ITaggable<WebAppConfig> with member _.Add state tags = { state with Tags = state.Tags |> Map.merge tags }
    interface IDependable<WebAppConfig> with member _.Add state newDeps = { state with Dependencies = state.Dependencies + newDeps }
    interface IServicePlanApp<WebAppConfig> with
        member _.Get state = WebAppConfig.ToCommon state
        member _.Wrap state config = WebAppConfig.FromCommon state config

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
        /// Sets the name of the web app.
        [<CustomOperation "name">]
        member this.Name (state:'T, name) = { this.Get state with Name = name } |> this.Wrap state
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
        [<CustomOperation "add_identity">]
        member this.AddIdentity (state:'T, identity:UserAssignedIdentity) =
            let current = this.Get state
            { current with
                Identity = current.Identity + identity
                Settings = current.Settings.Add("AZURE_CLIENT_ID", Setting.ExpressionSetting identity.ClientId) }
            |> this.Wrap state
        member this.AddIdentity (state, identity:UserAssignedIdentityConfig) = this.AddIdentity(state, identity.UserAssignedIdentity)
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
                    | AllOrigins -> failwith "You cannot enable CORS Credentials if you have already set CORS to AllOrigins.") }
            |> this.Wrap state
        [<CustomOperation "operating_system">]
        /// Sets the operating system
        member this.OperatingSystem (state:'T, os) = { this.Get state with OperatingSystem = os } |> this.Wrap state
        [<CustomOperation "zip_deploy">]
        /// Specifies a folder path or a zip file containing the web application to install as a post-deployment task.
        member this.ZipDeploy (state:'T, path) = { this.Get state with ZipDeployPath = Some path } |> this.Wrap state
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
                SecretStore = KeyVault (derived(fun c -> vaults.resourceId (ResourceName (c.Name.Value + "vault")))) }
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