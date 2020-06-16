[<AutoOpen>]
module Farmer.Builders.WebApp

open Farmer
open Farmer.CoreTypes
open Farmer.WebApp
open Farmer.Arm.Web
open Farmer.Arm.Insights
open System

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
    static member AspNet47 = AspNet "4.0"
    static member AspNet35 = AspNet "2.0"
    static member Python27 = Python ("2.7", "2.7")
    static member Python36 = Python ("3.6", "3.4") // not typo, really version 3.4
    static member Python37 = Python ("3.7", "3.7")

module AppSettings =
    let WebsiteNodeDefaultVersion version = "WEBSITE_NODE_DEFAULT_VERSION", version
    let RunFromPackage = "WEBSITE_RUN_FROM_PACKAGE", "1"

let publishingPassword (ResourceName name) =
    sprintf "list(resourceId('Microsoft.Web/sites/config', '%s', 'publishingcredentials'), '2014-06-01').properties.publishingPassword" name
    |> ArmExpression

type WebAppConfig =
    { Name : ResourceName
      ServicePlanName : ResourceRef
      HTTPSOnly : bool
      HTTP20Enabled : bool option
      ClientAffinityEnabled : bool option
      WebSocketsEnabled: bool option
      AppInsightsName : ResourceRef option
      OperatingSystem : OS
      Settings : Map<string, Setting>
      Dependencies : ResourceName list

      Cors : Cors option
      Sku : Sku
      WorkerSize : WorkerSize
      WorkerCount : int
      RunFromPackage : bool
      WebsiteNodeDefaultVersion : string option
      AlwaysOn : bool
      Runtime : Runtime

      Identity : FeatureFlag option

      ZipDeployPath : string option

      DockerImage : (string * string) option
      DockerCi : bool
      DockerAcrCredentials : {| RegistryName : string; Password : SecureParameter |} option }

    /// Gets the ARM expression path to the publishing password of this web app.
    member this.PublishingPassword = publishingPassword this.Name
    /// Gets the Service Plan name for this web app.
    member this.ServicePlan = this.ServicePlanName.ResourceName
    /// Gets the App Insights name for this web app, if it exists.
    member this.AppInsights = this.AppInsightsName |> Option.map (fun ai -> ai.ResourceName)
    /// Gets the system-created managed principal for the web app. It must have been enabled using enable_managed_identity.
    member this.SystemIdentity =
        sprintf "reference(resourceId('Microsoft.Web/sites', '%s'), '2019-08-01', 'full').identity.principalId" this.Name.Value
        |> ArmExpression
        |> PrincipalId

    interface IBuilder with
        member this.DependencyName = this.ServicePlanName.ResourceName
        member this.BuildResources location _ = [
            let webApp =
                { Name = this.Name
                  Location = location
                  ServicePlan = this.ServicePlanName.ResourceName
                  HTTPSOnly = this.HTTPSOnly
                  HTTP20Enabled = this.HTTP20Enabled
                  ClientAffinityEnabled = this.ClientAffinityEnabled
                  WebSocketsEnabled = this.WebSocketsEnabled
                  Identity = this.Identity
                  Cors = this.Cors
                  AppSettings =
                    let literalSettings = [
                        if this.RunFromPackage then AppSettings.RunFromPackage

                        match this.WebsiteNodeDefaultVersion with
                        | Some v -> AppSettings.WebsiteNodeDefaultVersion v
                        | None -> ()

                        match this.OperatingSystem, this.AppInsightsName with
                        | Windows, Some (External resourceName)
                        | Windows, Some (AutomaticallyCreated resourceName) ->
                            "APPINSIGHTS_INSTRUMENTATIONKEY", instrumentationKey resourceName |> ArmExpression.Eval
                            "APPINSIGHTS_PROFILERFEATURE_VERSION", "1.0.0"
                            "APPINSIGHTS_SNAPSHOTFEATURE_VERSION", "1.0.0"
                            "ApplicationInsightsAgent_EXTENSION_VERSION", "~2"
                            "DiagnosticServices_EXTENSION_VERSION", "~3"
                            "InstrumentationEngine_EXTENSION_VERSION", "~1"
                            "SnapshotDebugger_EXTENSION_VERSION", "~1"
                            "XDT_MicrosoftApplicationInsights_BaseExtensions", "~1"
                            "XDT_MicrosoftApplicationInsights_Mode", "recommended"
                        | Windows, Some AutomaticPlaceholder
                        | Windows, None
                        | Linux, _ ->
                            ()
                        if this.DockerCi then "DOCKER_ENABLE_CI", "true"
                    ]

                    let dockerSettings = [
                        match this.DockerAcrCredentials with
                        | Some credentials ->
                            "DOCKER_REGISTRY_SERVER_PASSWORD", ParameterSetting credentials.Password
                            Setting.AsLiteral ("DOCKER_REGISTRY_SERVER_URL", sprintf "https://%s.azurecr.io" credentials.RegistryName)
                            Setting.AsLiteral ("DOCKER_REGISTRY_SERVER_USERNAME", credentials.RegistryName)
                        | None ->
                            ()
                    ]
                    literalSettings
                    |> List.map Setting.AsLiteral
                    |> List.append dockerSettings
                    |> List.append (this.Settings |> Map.toList)
                  Kind = [
                    "app"
                    match this.OperatingSystem with Linux -> "linux" | Windows -> ()
                    match this.DockerImage with Some _ -> "container" | _ -> ()
                  ] |> String.concat ","
                  Dependencies = [
                    this.ServicePlanName.ResourceName
                    yield! this.Dependencies
                    match this.AppInsightsName with
                    | Some (AutomaticallyCreated appInsightsName)
                    | Some (External appInsightsName) ->
                        appInsightsName
                    | Some AutomaticPlaceholder
                    | None ->
                        ()
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
                            | DotNetCore version -> Some ("DOTNETCORE|" + version)
                            | Node version -> Some ("NODE|" + version)
                            | Php version -> Some ("PHP|" + version)
                            | Ruby version -> Some ("RUBY|" + version)
                            | Java (runtime, JavaSE) -> Some (sprintf "JAVA|%d-%s" runtime.Version runtime.Jre)
                            | Java (runtime, (Tomcat version)) -> Some (sprintf "TOMCAT|%s-%s" version runtime.Jre)
                            | Java (Java8, WildFly14) -> Some (sprintf "WILDFLY|14-%s" Java8.Jre)
                            | Python (linuxVersion, _) -> Some (sprintf "PYTHON|%s" linuxVersion)
                            | _ -> None
                  NetFrameworkVersion =
                    match this.Runtime with
                    | AspNet version -> Some (sprintf "v%s" version)
                    | _ -> None
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
                    | AspNet _, _ -> Some "dotnet"
                    | _ -> None
                    |> Option.map(fun stack -> "CURRENT_STACK", stack)
                    |> Option.toList
                  AppCommandLine = this.DockerImage |> Option.map snd
                  ZipDeployPath = this.ZipDeployPath
                }

            let ai =
                match this.AppInsightsName with
                | Some (AutomaticallyCreated resourceName) ->
                    { Name = resourceName
                      Location = location
                      LinkedWebsite =
                        match this.OperatingSystem with
                        | Windows -> Some this.Name
                        | Linux -> None }
                    |> Some
                | Some AutomaticPlaceholder
                | Some (External _)
                | None ->
                    None

            let serverFarm =
                match this.ServicePlanName with
                | External _
                | AutomaticPlaceholder ->
                    None
                | AutomaticallyCreated name ->
                    { Name = name
                      Location = location
                      Sku = this.Sku
                      WorkerSize = this.WorkerSize
                      WorkerCount = this.WorkerCount
                      OperatingSystem = this.OperatingSystem }
                    |> Some
            webApp
            match ai with Some ai -> ai | None -> ()
            match serverFarm with Some serverFarm -> serverFarm | None -> ()
        ]

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
          AlwaysOn = false
          HTTPSOnly = false
          HTTP20Enabled = None
          ClientAffinityEnabled = None
          WebSocketsEnabled = None
          Settings = Map.empty
          Dependencies = []
          Identity = None
          Runtime = Runtime.DotNetCoreLts
          OperatingSystem = Windows
          ZipDeployPath = None
          DockerImage = None
          DockerCi = false
          Cors = None
          DockerAcrCredentials = None }
    member __.Run(state:WebAppConfig) =
        let operatingSystem =
            match state.DockerImage with
            | None -> state.OperatingSystem
            | Some _ -> Linux
        { state with
            ServicePlanName =
                match state.ServicePlanName with
                | AutomaticPlaceholder -> AutomaticallyCreated (ResourceName (sprintf "%s-farm" state.Name.Value))
                | AutomaticallyCreated x -> AutomaticallyCreated x
                | External r -> External r
            OperatingSystem =
                operatingSystem
            AppInsightsName =
                tryCreateAppInsightsName state.AppInsightsName state.Name.Value
            DockerImage =
                match state.DockerImage, state.DockerAcrCredentials with
                | Some (image, tag), Some credentials when not (image.Contains "azurecr.io") ->
                    Some (sprintf "%s.azurecr.io/%s" credentials.RegistryName image, tag)
                | Some x, _ ->
                    Some x
                | None, _ ->
                    None
        }
    /// Sets the name of the web app.
    [<CustomOperation "name">]
    member __.Name(state:WebAppConfig, name) = { state with Name = name }
    member this.Name(state:WebAppConfig, name:string) = this.Name(state, ResourceName name)
    /// Sets the name of the service plan.
    [<CustomOperation "service_plan_name">]
    member _.ServicePlanName(state:WebAppConfig, name) = { state with ServicePlanName = name }
    member this.ServicePlanName(state:WebAppConfig, name) = this.ServicePlanName(state, AutomaticallyCreated name)
    member this.ServicePlanName(state:WebAppConfig, name:string) = this.ServicePlanName(state, ResourceName name)
    /// Do not create a service plan for this web app. Instead, link to another pre-defined one.
    [<CustomOperation "link_to_service_plan">]
    member __.LinkToServicePlan(state:WebAppConfig, name) = { state with ServicePlanName = External name }
    member this.LinkToServicePlan(state:WebAppConfig, name:string) = this.LinkToServicePlan (state, ResourceName name)
    member this.LinkToServicePlan(state:WebAppConfig, config:ServicePlanConfig) = this.LinkToServicePlan (state, config.Name)
    /// Sets the sku of the service plan.
    [<CustomOperation "sku">]
    member __.Sku(state:WebAppConfig, sku) = { state with Sku = sku }
    /// Sets the size of the service plan worker.
    [<CustomOperation "worker_size">]
    member __.WorkerSize(state:WebAppConfig, workerSize) = { state with WorkerSize = workerSize }
    /// Sets the number of instances on the service plan.
    [<CustomOperation "number_of_workers">]
    member __.NumberOfWorkers(state:WebAppConfig, workerCount) = { state with WorkerCount = workerCount }
    /// Sets the name of the automatically-created app insights instance.
    [<CustomOperation "app_insights_auto_name">]
    member __.UseAppInsights(state:WebAppConfig, name) = { state with AppInsightsName = Some (AutomaticallyCreated name) }
    member this.UseAppInsights(state:WebAppConfig, name:string) = this.UseAppInsights(state, ResourceName name)
    /// Removes any automatic app insights creation, configuration and settings for this webapp.
    [<CustomOperation "app_insights_off">]
    member __.DeactivateAppInsights(state:WebAppConfig) = { state with AppInsightsName = None }
    /// Instead of creating a new AI instance, configure this webapp to point to another AI instance that you are managing
    /// yourself.
    [<CustomOperation "link_to_app_insights">]
    member __.LinkAppInsights(state:WebAppConfig, name) = { state with AppInsightsName = Some(External name) }
    member this.LinkAppInsights(state:WebAppConfig, name) = this.LinkAppInsights(state, ResourceName name)
    member __.LinkAppInsights(state:WebAppConfig, name) = { state with AppInsightsName = name |> Option.map External }
    /// Sets the web app to use "run from package" deployment capabilities.
    [<CustomOperation "run_from_package">]
    member __.RunFromPackage(state:WebAppConfig) = { state with RunFromPackage = true }
    /// Sets the node version of the web app.
    [<CustomOperation "website_node_default_version">]
    member __.NodeVersion(state:WebAppConfig, version) = { state with WebsiteNodeDefaultVersion = Some version }
    /// Sets an app setting of the web app in the form "key" "value".
    [<CustomOperation "setting">]
    member __.AddSetting(state:WebAppConfig, key, value) =
        { state with Settings = state.Settings.Add(key, LiteralSetting value) }
    member this.AddSetting(state:WebAppConfig, key, value:ArmExpression) =
        this.AddSetting(state, key, value.Eval())
    /// Sets a list of app setting of the web app in the form "key" "value".
    [<CustomOperation "settings">]
    member this.AddSettings(state:WebAppConfig, settings: (string*string) list) =
        settings
        |> List.fold (fun state (key, value: string) -> this.AddSetting(state, key, value)) state
    /// Creates an app setting of the web app whose value will be supplied as a secret parameter.
    [<CustomOperation "secret_setting">]
    member __.AddSecret(state:WebAppConfig, key) =
        { state with Settings = state.Settings.Add(key, ParameterSetting (SecureParameter key)) }
    /// Sets a dependency for the web app.
    [<CustomOperation "depends_on">]
    member __.DependsOn(state:WebAppConfig, resourceName) = { state with Dependencies = resourceName :: state.Dependencies }
    member __.DependsOn(state:WebAppConfig, builder:IBuilder) = { state with Dependencies = builder.DependencyName :: state.Dependencies }
    member __.DependsOn(state:WebAppConfig, resource:IArmResource) = { state with Dependencies = resource.ResourceName :: state.Dependencies }
    /// Sets "Always On" flag
    [<CustomOperation "always_on">]
    member __.AlwaysOn(state:WebAppConfig) = { state with AlwaysOn = true }
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
    [<CustomOperation "operating_system">]
    /// Sets the operating system
    member __.OperatingSystem(state:WebAppConfig, os) = { state with OperatingSystem = os }
    [<CustomOperation "zip_deploy">]
    /// Specifies a folder path or a zip file containing the web application to install as a post-deployment task.
    member __.ZipDeploy(state:WebAppConfig, path) = { state with ZipDeployPath = Some path }
    [<CustomOperation "docker_image">]
    /// Specifies a docker image to use from the registry (linux only), and the startup command to execute.
    member __.DockerImage(state:WebAppConfig, registryPath, startupFile) = { state with DockerImage = Some (registryPath, startupFile) }
    [<CustomOperation "docker_ci">]
    /// Have your custom Docker image automatically re-deployed when a new version is pushed to e.g. Docker hub.
    member __.DockerCI(state:WebAppConfig) = { state with DockerCi = true }
    [<CustomOperation "docker_use_azure_registry">]
    /// Have your custom Docker image automatically re-deployed when a new version is pushed to e.g. Docker hub.
    member __.DockerAcrCredentials(state:WebAppConfig, registryName) =
        { state with
            DockerAcrCredentials =
                Some {| RegistryName = registryName
                        Password = SecureParameter (sprintf "docker-password-for-%s" registryName) |} }
    [<CustomOperation "enable_managed_identity">]
    member _.EnableManagedIdentity(state:WebAppConfig) =
        { state with Identity = Some Enabled }
    [<CustomOperation "disable_managed_identity">]
    member _.DisableManagedIdentity(state:WebAppConfig) =
        { state with Identity = Some Disabled }
    [<CustomOperation "enable_cors">]
    member _.EnableCors (state:WebAppConfig, origins) = { state with Cors = Some (SpecificOrigins (List.map Uri origins)) }
    member _.EnableCors (state:WebAppConfig, cors) = { state with Cors = Some cors }

let webApp = WebAppBuilder()
