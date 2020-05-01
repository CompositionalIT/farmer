[<AutoOpen>]
module Farmer.Resources.WebApp

open Farmer

type ServerFarm =
    { Name : ResourceName
      Location : Location
      Sku: string
      WorkerSize : string
      IsDynamic : bool
      Kind : string option
      Tier : string
      WorkerCount : int
      IsLinux : bool }
    interface IResource with
        member this.ResourceName = this.Name
        member this.ToArmObject() =
            {| ``type`` = "Microsoft.Web/serverfarms"
               sku =
                   {| name = this.Sku
                      tier = this.Tier
                      size = this.WorkerSize
                      family = if this.IsDynamic then "Y" else null
                      capacity = if this.IsDynamic then 0 else this.WorkerCount |}
               name = this.Name.Value
               apiVersion = "2018-02-01"
               location = this.Location.ArmValue
               properties =
                   if this.IsDynamic then
                       box {| name = this.Name.Value
                              computeMode = "Dynamic"
                              reserved = this.IsLinux |}
                   else
                       box {| name = this.Name.Value
                              perSiteScaling = false
                              reserved = this.IsLinux |}
               kind = this.Kind |> Option.toObj
            |} :> _

module ZipDeploy =
    open System.IO
    open System.IO.Compression

    type ZipDeployKind =
        | DeployFolder of string
        | DeployZip of string
        member this.Value = match this with DeployFolder s | DeployZip s -> s
        /// Tries to create a ZipDeployKind from a string path.
        static member TryParse path =
            if (File.GetAttributes path).HasFlag FileAttributes.Directory then
                Some(DeployFolder path)
            else if Path.GetExtension path = ".zip" then
                Some(DeployZip path)
            else
                None
        /// Processes a ZipDeployKind and returns the filename of the zip file.
        /// If the ZipDeployKind is a DeployFolder, the folder will be zipped first and the generated zip file returned.
        member this.GetZipPath targetFolder =
            match this with
            | DeployFolder appFolder ->
                let packageFilename = Path.Combine(targetFolder, (Path.GetFileName appFolder) + ".zip")
                File.Delete packageFilename
                ZipFile.CreateFromDirectory(appFolder, packageFilename)
                packageFilename
            | DeployZip zipFilePath ->
                zipFilePath

type WebApp =
    { Name : ResourceName
      ServicePlan : ResourceName
      Location : Location
      AppSettings : List<string * string>
      AlwaysOn : bool
      HTTPSOnly : bool
      Dependencies : ResourceName list
      Kind : string
      LinuxFxVersion : string option
      AppCommandLine : string option
      NetFrameworkVersion : string option
      JavaVersion : string option
      JavaContainer : string option
      JavaContainerVersion : string option
      PhpVersion : string option
      PythonVersion : string option
      Metadata : List<string * string>
      ZipDeployPath : string option
      Parameters : SecureParameter list }
    interface IParameters with
        member this.SecureParameters = this.Parameters
    interface IPostDeploy with
        member this.Run resourceGroupName =
            match this with
            | { ZipDeployPath = Some path; Name = name } ->
                let path =
                    ZipDeploy.ZipDeployKind.TryParse path
                    |> Option.defaultWith (fun () ->
                        failwithf "Path '%s' must either be a folder to be zipped, or an existing zip." path)
                printfn "Running ZIP deploy for %s" path.Value
                Some(Deploy.Az.zipDeploy name.Value path.GetZipPath resourceGroupName)
            | _ ->
                None
    interface IResource with
        member this.ResourceName = this.Name
        member this.ToArmObject() =
            {| ``type`` = "Microsoft.Web/sites"
               name = this.Name.Value
               apiVersion = "2016-08-01"
               location = this.Location.ArmValue
               dependsOn = this.Dependencies |> List.map(fun p -> p.Value)
               kind = this.Kind
               properties =
                   {| serverFarmId = this.ServicePlan.Value
                      httpsOnly = this.HTTPSOnly
                      siteConfig =
                           [ "alwaysOn", box this.AlwaysOn
                             "appSettings", this.AppSettings |> List.map(fun (k,v) -> {| name = k; value = v |}) |> box
                             match this.LinuxFxVersion with Some v -> "linuxFxVersion", box v | None -> ()
                             match this.AppCommandLine with Some v -> "appCommandLine", box v | None -> ()
                             match this.NetFrameworkVersion with Some v -> "netFrameworkVersion", box v | None -> ()
                             match this.JavaVersion with Some v -> "javaVersion", box v | None -> ()
                             match this.JavaContainer with Some v -> "javaContainer", box v | None -> ()
                             match this.JavaContainerVersion with Some v -> "javaContainerVersion", box v | None -> ()
                             match this.PhpVersion with Some v -> "phpVersion", box v | None -> ()
                             match this.PythonVersion with Some v -> "pythonVersion", box v | None -> ()
                             "metadata", this.Metadata |> List.map(fun (k,v) -> {| name = k; value = v |}) |> box ]
                           |> Map.ofList
                    |}
            |} :> _

type AppInsights =
    { Name : ResourceName
      Location : Location
      LinkedWebsite : ResourceName option }
    interface IResource with
        member this.ResourceName = this.Name
        member this.ToArmObject() =
            {| ``type`` = "Microsoft.Insights/components"
               kind = "web"
               name = this.Name.Value
               location = this.Location.ArmValue
               apiVersion = "2014-04-01"
               tags =
                   [ match this.LinkedWebsite with
                     | Some linkedWebsite -> sprintf "[concat('hidden-link:', resourceGroup().id, '/providers/Microsoft.Web/sites/', '%s')]" linkedWebsite.Value, "Resource"
                     | None -> ()
                     "displayName", "AppInsightsComponent" ]
                   |> Map.ofList
               properties =
                match this.LinkedWebsite with
                | Some linkedWebsite ->
                   box {| name = this.Name.Value
                          Application_Type = "web"
                          ApplicationId = linkedWebsite.Value |}
                | None ->
                   box {| name = this.Name.Value
                          Application_Type = "web" |}
            |} :> _

type WorkerSize = Small | Medium | Large | Serverless
type WebAppSku = Shared | Free | Basic of string | Standard of string | Premium of string | PremiumV2 of string | Isolated of string | Functions
type OS = Windows | Linux

type JavaHost =
    | JavaSE | WildFly14 | Tomcat of string
    static member Tomcat85 = Tomcat "8.5"
    static member Tomcat90 = Tomcat "9.0"
type JavaRuntime =
    | Java8 | Java11
    member this.Version = match this with Java8 -> 8 | Java11 -> 11
    member this.Jre = match this with Java8 -> "jre8" | Java11 -> "java11"
type WebAppRuntime =
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
    let Y1 = Isolated "Y1"

module AppSettings =
    let WebsiteNodeDefaultVersion version = "WEBSITE_NODE_DEFAULT_VERSION", version
    let RunFromPackage = "WEBSITE_RUN_FROM_PACKAGE", "1"

let publishingPassword (ResourceName name) =
    sprintf "list(resourceId('Microsoft.Web/sites/config', '%s', 'publishingcredentials'), '2014-06-01').properties.publishingPassword" name
    |> ArmExpression

module Ai =
    let tryCreateAppInsightsName aiName rootName =
        aiName
        |> Option.map(function
        | AutomaticPlaceholder ->
          AutomaticallyCreated(ResourceName(sprintf "%s-ai" rootName))
        | (External _ as resourceRef)
        | (AutomaticallyCreated _ as resourceRef) ->
            resourceRef)
    let instrumentationKey (ResourceName accountName) =
        sprintf "reference('Microsoft.Insights/components/%s').InstrumentationKey" accountName
        |> ArmExpression

type AppInsightsConfig =
    { Name : ResourceName }
    /// Gets the ARM expression path to the instrumentation key of this App Insights instance.
    member this.InstrumentationKey = Ai.instrumentationKey this.Name
    interface IResourceBuilder with
        member this.BuildResources location _ = [
            NewResource { Name = this.Name
                          Location = location
                          LinkedWebsite = None }
        ]

type ServicePlanConfig =
    { Name : ResourceName
      Sku : WebAppSku
      WorkerSize : WorkerSize
      WorkerCount : int
      OperatingSystem : OS }
    interface IResourceBuilder with
        member this.BuildResources location _ = [
            NewResource
              { Location = location
                Name = this.Name
                Sku =
                  match this.Sku with
                  | Free ->
                      "F1"
                  | Shared ->
                      "D1"
                  | Basic sku
                  | Standard sku
                  | Premium sku
                  | PremiumV2 sku
                  | Isolated sku ->
                      sku
                  | Functions ->
                      "Y1"
                WorkerSize =
                  match this.WorkerSize with
                  | Small -> "0"
                  | Medium -> "1"
                  | Large -> "2"
                  | Serverless -> "Y1"
                IsDynamic =
                  match this.Sku, this.WorkerSize with
                  | Functions, Serverless -> true
                  | _ -> false
                Kind =
                  match this.OperatingSystem with
                  | Linux -> Some "linux"
                  | _ -> None
                Tier =
                  match this.Sku with
                  | Free -> "Free"
                  | Shared -> "Shared"
                  | Basic _ -> "Basic"
                  | Standard _ -> "Standard"
                  | Premium _ -> "Premium"
                  | PremiumV2 _ -> "PremiumV2"
                  | Isolated _ -> "Isolated"
                  | Functions -> "Dynamic"
                IsLinux =
                  match this.OperatingSystem with
                  | Linux -> true
                  | Windows -> false
                WorkerCount =
                  this.WorkerCount }
        ]

type WebAppConfig =
    { Name : ResourceName
      ServicePlanName : ResourceRef
      HTTPSOnly : bool
      AppInsightsName : ResourceRef option
      OperatingSystem : OS
      Settings : Map<string, string>
      Dependencies : ResourceName list

      Sku : WebAppSku
      WorkerSize : WorkerSize
      WorkerCount : int
      RunFromPackage : bool
      WebsiteNodeDefaultVersion : string option
      AlwaysOn : bool
      Runtime : WebAppRuntime
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
    interface IResourceBuilder with
        member this.BuildResources location existingResources = [
            let webApp =
                { Name = this.Name
                  Location = location
                  ServicePlan = this.ServicePlanName.ResourceName
                  HTTPSOnly = this.HTTPSOnly
                  AppSettings = [
                    yield! this.Settings |> Map.toList
                    if this.RunFromPackage then AppSettings.RunFromPackage

                    match this.WebsiteNodeDefaultVersion with
                    | Some v -> AppSettings.WebsiteNodeDefaultVersion v
                    | None -> ()

                    match this.OperatingSystem, this.AppInsightsName with
                    | Windows, Some (External resourceName)
                    | Windows, Some (AutomaticallyCreated resourceName) ->
                        "APPINSIGHTS_INSTRUMENTATIONKEY", Ai.instrumentationKey resourceName |> ArmExpression.Eval
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

                    match this.DockerAcrCredentials with
                    | Some credentials ->
                        "DOCKER_REGISTRY_SERVER_PASSWORD", credentials.Password.AsArmRef.Eval()
                        "DOCKER_REGISTRY_SERVER_URL", sprintf "https://%s.azurecr.io" credentials.RegistryName
                        "DOCKER_REGISTRY_SERVER_USERNAME", credentials.RegistryName
                    | None ->
                      ()
                  ]
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
                  Parameters = [
                      match this.DockerAcrCredentials with
                      | Some credentials -> credentials.Password
                      | None -> ()
                  ]
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
                      Sku = this.Sku
                      WorkerSize = this.WorkerSize
                      WorkerCount = this.WorkerCount
                      OperatingSystem = this.OperatingSystem }
                    |> Some
            NewResource webApp
            match ai with Some ai -> NewResource ai | None -> ()
            match serverFarm with Some serverFarm -> yield! (serverFarm :> IResourceBuilder).BuildResources location existingResources | None -> ()
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
          Settings = Map.empty
          Dependencies = []
          Runtime = WebAppRuntime.DotNetCoreLts
          OperatingSystem = Windows
          ZipDeployPath = None
          DockerImage = None
          DockerCi = false
          DockerAcrCredentials = None }
    member __.Run(state:WebAppConfig) =
        let operatingSystem =
            match state.DockerImage with
            | None -> state.OperatingSystem
            | Some _ -> Linux
        { state with
            ServicePlanName =
                match state.ServicePlanName with
                | AutomaticPlaceholder -> AutomaticallyCreated (ResourceName (sprintf "%s-plan" state.Name.Value))
                | AutomaticallyCreated x -> AutomaticallyCreated x
                | External r -> External r
            OperatingSystem =
                operatingSystem
            AppInsightsName =
                Ai.tryCreateAppInsightsName state.AppInsightsName state.Name.Value
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
    member __.AddSetting(state:WebAppConfig, key, value) = { state with Settings = state.Settings.Add(key, value) }
    member __.AddSetting(state:WebAppConfig, key, value:ArmExpression) = { state with Settings = state.Settings.Add(key, value.Eval()) }
    /// Sets a list of app setting of the web app in the form "key" "value".
    [<CustomOperation "settings">]
    member __.AddSettings(state:WebAppConfig, settings: (string*string) list) =
        settings
        |> List.fold (fun state (key, value: string) -> __.AddSetting(state, key, value)) state
    /// Sets a dependency for the web app.
    [<CustomOperation "depends_on">]
    member __.DependsOn(state:WebAppConfig, resourceName) = { state with Dependencies = resourceName :: state.Dependencies }
    /// Sets "Always On" flag
    [<CustomOperation "always_on">]
    member __.AlwaysOn(state:WebAppConfig) = { state with AlwaysOn = true }
    /// Disables http for this webapp so that only https is used.
    [<CustomOperation "https_only">]
    member __.HttpsOnly(state:WebAppConfig) = { state with HTTPSOnly = true }
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
type AppInsightsBuilder() =
    member __.Yield _ =
        { Name = ResourceName.Empty }
    [<CustomOperation "name">]
    /// Sets the name of the App Insights instance.
    member __.Name(state:AppInsightsConfig, name) = { state with Name = ResourceName name }
type ServicePlanBuilder() =
    member __.Yield _ : ServicePlanConfig=
        { Name = ResourceName.Empty
          Sku = Free
          WorkerSize = Small
          WorkerCount = 1
          OperatingSystem = Windows }
    [<CustomOperation "name">]
    /// Sets the name of the Server Farm.
    member __.Name(state:ServicePlanConfig, name) = { state with Name = ResourceName name }
    /// Sets the sku of the service plan.
    [<CustomOperation "sku">]
    member __.Sku(state:ServicePlanConfig, sku) = { state with Sku = sku }
    /// Sets the size of the service plan worker.
    [<CustomOperation "worker_size">]
    member __.WorkerSize(state:ServicePlanConfig, workerSize) = { state with WorkerSize = workerSize }
    /// Sets the number of instances on the service plan.
    [<CustomOperation "number_of_workers">]
    member __.NumberOfWorkers(state:ServicePlanConfig, workerCount) = { state with WorkerCount = workerCount }
    [<CustomOperation "operating_system">]
    /// Sets the operating system
    member __.OperatingSystem(state:ServicePlanConfig, os) = { state with OperatingSystem = os }
    [<CustomOperation "serverless">]
    /// Configures this server farm to host serverless functions, not web apps.
    member __.Serverless(state:ServicePlanConfig) = { state with Sku = Functions; WorkerSize = Serverless }

let appInsights = AppInsightsBuilder()
let webApp = WebAppBuilder()
let servicePlan = ServicePlanBuilder()
