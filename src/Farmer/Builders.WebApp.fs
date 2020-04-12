[<AutoOpen>]
module Farmer.Resources.WebApp

open Farmer.Helpers
open Farmer.Resources.Storage
open Farmer

type WorkerSize = Small | Medium | Large | Serverless
type WebAppSku = Shared | Free | Basic of string | Standard of string | Premium of string | PremiumV2 of string | Isolated of string | Functions
type FunctionsRuntime = DotNet | Node | Java | Python
type FunctionsExtensionVersion = V1 | V2 | V3
type OS = Windows | Linux
type DotNetCoreRuntime = DotNetCore21 | DotNetCore31 | DotNetCoreLts | DotNetCoreLatest
type AspNetRuntime = | AspNet47 | AspNet35
type JavaHost = JavaSE | WildFly14 | Tomcat90 | Tomcat85
type JavaRuntime = Java8 of JavaHost | Java11 of JavaHost
type PhpRuntime = Php73 | Php72 | Php71 | Php70 | Php56
type PythonRuntime = Python37 | Python36 | Python27
type RubyRuntime = Ruby26 | Ruby25 | Ruby24 | Ruby23
type NodeRuntime = Node6 | Node8 | Node10 | Node12 | NodeLts
type WebAppRuntime =
    | DotNetCore of DotNetCoreRuntime
    | AspNet of AspNetRuntime
    | Java of JavaRuntime
    | Node of NodeRuntime
    | Php of PhpRuntime
    | Python of PythonRuntime
    | Ruby of RubyRuntime
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
    open Farmer.Models

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

open Farmer.Models

type WebAppConfig =
    { Name : ResourceName
      ServicePlanName : ResourceRef
      Sku : WebAppSku
      WorkerSize : WorkerSize
      WorkerCount : int
      AppInsightsName : ResourceRef option
      RunFromPackage : bool
      WebsiteNodeDefaultVersion : string option
      AlwaysOn : bool
      HTTPSOnly : bool
      Settings : Map<string, string>
      Dependencies : ResourceName list
      Runtime : WebAppRuntime
      OperatingSystem : OS
      ZipDeployPath : string option
      DockerImage : (string * string) option }
    /// Gets the ARM expression path to the publishing password of this web app.
    member this.PublishingPassword = publishingPassword this.Name
    /// Gets the Service Plan name for this web app.
    member this.ServicePlan = this.ServicePlanName.ResourceName
    /// Gets the App Insights name for this web app, if it exists.
    member this.AppInsights = this.AppInsightsName |> Option.map (fun ai -> ai.ResourceName)
type FunctionsConfig =
    { Name : ResourceName
      ServicePlanName : ResourceRef
      StorageAccountName : ResourceRef
      AppInsightsName : ResourceRef option
      HTTPSOnly : bool
      Runtime : FunctionsRuntime
      ExtensionVersion : FunctionsExtensionVersion
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
        |> Option.map Ai.instrumentationKey
    /// Gets the default key for the functions site
    member this.DefaultKey =
        sprintf "listkeys(concat(resourceId('Microsoft.Web/sites', '%s'), '/host/default/'),'2016-08-01').functionKeys.default" this.Name.Value
        |> ArmExpression
    /// Gets the master key for the functions site
    member this.MasterKey =
        sprintf "listkeys(concat(resourceId('Microsoft.Web/sites', '%s'), '/host/default/'),'2016-08-01').masterKey" this.Name.Value
        |> ArmExpression
    /// Gets the Service Plan name for this functions app.
    member this.ServicePlan = this.ServicePlanName.ResourceName
    /// Gets the App Insights name for this functions app, if it exists.
    member this.AppInsights = this.AppInsightsName |> Option.map (fun ai -> ai.ResourceName)
    /// Gets the Storage Account name for this functions app.
    member this.StorageAccount =
        this.StorageAccountName.ResourceName
type AppInsightsConfig =
    { Name : ResourceName }
    /// Gets the ARM expression path to the instrumentation key of this App Insights instance.
    member this.InstrumentationKey = Ai.instrumentationKey this.Name
type ServicePlanConfig =
    { Name : ResourceName
      Sku : WebAppSku
      WorkerSize : WorkerSize
      WorkerCount : int
      OperatingSystem : OS }

module Converters =
    let serverFarm location (sfc:ServicePlanConfig) =
        { Location = location
          Name = sfc.Name
          Sku =
            match sfc.Sku with
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
            match sfc.WorkerSize with
            | Small -> "0"
            | Medium -> "1"
            | Large -> "2"
            | Serverless -> "Y1"
          IsDynamic =
            match sfc.Sku, sfc.WorkerSize with
            | Functions, Serverless -> true
            | _ -> false
          Kind =
            match sfc.OperatingSystem with
            | Linux -> Some "linux"
            | _ -> None
          Tier =
            match sfc.Sku with
            | Free -> "Free"
            | Shared -> "Shared"
            | Basic _ -> "Basic"
            | Standard _ -> "Standard"
            | Premium _ -> "Premium"
            | PremiumV2 _ -> "PremiumV2"
            | Isolated _ -> "Isolated"
            | Functions -> "Dynamic"
          IsLinux =
            match sfc.OperatingSystem with
            | Linux -> true
            | Windows -> false
          WorkerCount =
            sfc.WorkerCount }

    let webApp location (wac:WebAppConfig) =
        let webApp =
            { Name = wac.Name
              Location = location
              ServicePlan = wac.ServicePlanName.ResourceName
              HTTPSOnly = wac.HTTPSOnly
              AppSettings = [
                yield! wac.Settings |> Map.toList
                if wac.RunFromPackage then AppSettings.RunFromPackage

                match wac.WebsiteNodeDefaultVersion with
                | Some v -> AppSettings.WebsiteNodeDefaultVersion v
                | None -> ()

                match wac.OperatingSystem, wac.AppInsightsName with
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
              ]
              Kind = [
                "app"
                match wac.OperatingSystem with Linux -> "linux" | Windows -> ()
                match wac.DockerImage with Some _ -> "container" | _ -> ()
              ] |> String.concat ","
              Dependencies = [
                wac.ServicePlanName.ResourceName
                yield! wac.Dependencies
                match wac.AppInsightsName with
                | Some (AutomaticallyCreated appInsightsName)
                | Some (External appInsightsName) ->
                    appInsightsName
                | Some AutomaticPlaceholder
                | None ->
                    ()
              ]
              AlwaysOn = wac.AlwaysOn
              LinuxFxVersion =
                match wac.DockerImage, wac.Runtime, wac.OperatingSystem with
                | Some (image, _), _, Linux -> Some ("DOCKER|" + image)
                | _, DotNetCore DotNetCore21, Linux -> Some "DOTNETCORE|2.1"
                | _, DotNetCore DotNetCore31, Linux -> Some "DOTNETCORE|3.1"
                | _, DotNetCore DotNetCoreLts, Linux -> Some "DOTNETCORE|LTS"
                | _, DotNetCore DotNetCoreLatest, Linux -> Some "DOTNETCORE|Latest"
                | _, Java (Java11 JavaSE), _ -> Some "JAVA|11-java11"
                | _, Java (Java11 Tomcat90), Linux -> Some "TOMCAT|9.0-java11"
                | _, Java (Java11 Tomcat85), Linux -> Some "TOMCAT|8.5-java11"
                | _, Java (Java8 JavaSE), _ -> Some "JAVA|8-jre8"
                | _, Java (Java8 WildFly14), _ -> Some "WILDFLY|14-jre8"
                | _, Java (Java8 Tomcat90), Linux -> Some "TOMCAT|9.0-jre8"
                | _, Java (Java8 Tomcat85), Linux -> Some "TOMCAT|8.5-jre8"
                | _, Node Node6, _ -> Some "NODE|6-lts"
                | _, Node Node8, _ -> Some "NODE|8-lts"
                | _, Node Node10, _ -> Some "NODE|10-lts"
                | _, Node Node12, _ -> Some "NODE|12-lts"
                | _, Node NodeLts, _ -> Some "NODE|lts"
                | _, Php Php73, Linux -> Some "PHP|7.3"
                | _, Php Php72, Linux -> Some "PHP|7.2"
                | _, Php Php70, Linux -> Some "PHP|7.0"
                | _, Php Php56, Linux -> Some "PHP|5.6"
                | _, Python Python37, _ -> Some "PYTHON|3.7"
                | _, Python Python36, Linux -> Some "PYTHON|3.6"
                | _, Python Python27, Linux -> Some "PYTHON|2.7"
                | _, Ruby Ruby26, _ -> Some "RUBY|2.6"
                | _, Ruby Ruby25, _ -> Some "RUBY|2.5"
                | _, Ruby Ruby24, _ -> Some "RUBY|2.4"
                | _, Ruby Ruby23, _ -> Some "RUBY|2.3"
                | _ -> None
              NetFrameworkVersion =
                match wac.Runtime with
                | AspNet AspNet47 -> Some "v4.0"
                | AspNet AspNet35 -> Some "v2.0"
                | _ -> None
              JavaVersion =
                match wac.Runtime, wac.OperatingSystem with
                | Java (Java11 Tomcat90), Windows
                | Java (Java11 Tomcat85), Windows ->
                    Some "11"
                | Java (Java8 Tomcat90), Windows
                | Java (Java8 Tomcat85), Windows ->
                    Some "1.8"
                | _ ->
                    None
              JavaContainer =
                match wac.Runtime, wac.OperatingSystem with
                | Java (Java11 Tomcat90), Windows
                | Java (Java11 Tomcat85), Windows
                | Java (Java8 Tomcat90), Windows
                | Java (Java8 Tomcat85), Windows ->
                    Some "Tomcat"
                | _ ->
                    None
              JavaContainerVersion =
                match wac.Runtime, wac.OperatingSystem with
                | Java (Java11 Tomcat90), Windows
                | Java (Java8 Tomcat90), Windows ->
                    Some "9.0"
                | Java (Java11 Tomcat85), Windows
                | Java (Java8 Tomcat85), Windows ->
                    Some "8.5"
                | _ ->
                    None
              PhpVersion =
                match wac.Runtime, wac.OperatingSystem with
                | Php Php73, Windows -> Some "7.3"
                | Php Php72, Windows -> Some "7.2"
                | Php Php70, Windows -> Some "7.0"
                | Php Php71, _ -> Some "7.1"
                | Php Php56, Windows -> Some "5.6"
                | _ -> None
              PythonVersion =
                match wac.Runtime, wac.OperatingSystem with
                | Python Python36, Windows -> Some "3.4" // not typo, really version 3.4
                | Python Python27, Windows -> Some "2.7"
                | _ -> None
              Metadata =
                match wac.Runtime, wac.OperatingSystem with
                | Java (Java11 Tomcat90), Windows
                | Java (Java11 Tomcat85), Windows
                | Java (Java8 Tomcat90), Windows
                | Java (Java8 Tomcat85), Windows ->
                    Some "java"
                | Php _, _ ->
                    Some "php"
                | Python Python36, Windows
                | Python Python27, Windows ->
                    Some "python"
                | DotNetCore _, Windows ->
                    Some "dotnetcore"
                | AspNet _, _ ->
                    Some "dotnet"
                | _ ->
                    None
                |> Option.map(fun stack -> "CURRENT_STACK", stack)
                |> Option.toList
              AppCommandLine = wac.DockerImage |> Option.map snd
              ZipDeployPath = wac.ZipDeployPath
            }

        let ai =
            match wac.AppInsightsName with
            | Some (AutomaticallyCreated resourceName) ->
                { Name = resourceName
                  Location = location
                  LinkedWebsite =
                    match wac.OperatingSystem with
                    | Windows -> Some wac.Name
                    | Linux -> None }
                |> Some
            | Some AutomaticPlaceholder
            | Some (External _)
            | None ->
                None

        let serverFarm =
            match wac.ServicePlanName with
            | External _
            | AutomaticPlaceholder ->
                None
            | AutomaticallyCreated name ->
                { Name = name
                  Sku = wac.Sku
                  WorkerSize = wac.WorkerSize
                  WorkerCount = wac.WorkerCount
                  OperatingSystem = wac.OperatingSystem }
                |> serverFarm location
                |> Some

        {| Ai = ai
           ServerFarm = serverFarm
           WebApp = webApp |}
    let functions location (fns:FunctionsConfig) =
        let webApp =
            { Name = fns.Name
              ServicePlan = fns.ServicePlanName.ResourceName
              Location = location
              AppSettings = [
                yield! fns.Settings |> Map.toList
                "FUNCTIONS_WORKER_RUNTIME", (string fns.Runtime).ToLower()
                "WEBSITE_NODE_DEFAULT_VERSION", "10.14.1"
                "FUNCTIONS_EXTENSION_VERSION", match fns.ExtensionVersion with V1 -> "~1" | V2 -> "~2" | V3 -> "~3"
                "AzureWebJobsStorage", Storage.buildKey fns.StorageAccountName.ResourceName |> ArmExpression.Eval
                "AzureWebJobsDashboard", Storage.buildKey fns.StorageAccountName.ResourceName |> ArmExpression.Eval

                match fns.AppInsightsName with
                | Some (External resourceName)
                | Some (AutomaticallyCreated resourceName) ->
                    "APPINSIGHTS_INSTRUMENTATIONKEY", Ai.instrumentationKey resourceName |> ArmExpression.Eval
                | Some AutomaticPlaceholder
                | None -> ()

                if fns.OperatingSystem = Windows then
                    "WEBSITE_CONTENTAZUREFILECONNECTIONSTRING", Storage.buildKey fns.StorageAccountName.ResourceName |> ArmExpression.Eval
                    "WEBSITE_CONTENTSHARE", fns.Name.Value.ToLower()
              ]

              Kind =
                match fns.OperatingSystem with
                | Windows -> "functionapp"
                | Linux -> "functionapp,linux"
              Dependencies = [
                yield! fns.Dependencies
                match fns.AppInsightsName with
                | Some (AutomaticallyCreated appInsightsName)
                | Some (External appInsightsName) ->
                    appInsightsName
                | Some AutomaticPlaceholder
                | None ->
                    ()
                match fns.ServicePlanName.ResourceNameOpt with Some resourceName -> resourceName | None -> ()
                fns.StorageAccountName.ResourceName
              ]
              AlwaysOn = false
              HTTPSOnly = false
              LinuxFxVersion = None
              NetFrameworkVersion = None
              JavaVersion = None
              JavaContainer = None
              JavaContainerVersion = None
              PhpVersion = None
              PythonVersion = None
              Metadata = []
              ZipDeployPath = None
              AppCommandLine = None
            }

        let serverFarm =
            match fns.ServicePlanName with
            | External _
            | AutomaticPlaceholder ->
                None
            | AutomaticallyCreated resourceName ->
                { Name = resourceName
                  Sku = Sku.Y1
                  WorkerSize = Serverless
                  WorkerCount = 0
                  OperatingSystem = fns.OperatingSystem }
                |> serverFarm location
                |> Some
        let storage =
            match fns.StorageAccountName with
            | AutomaticallyCreated resourceName ->
                { StorageAccount.Name = resourceName
                  Location = location
                  Sku = Storage.Sku.StandardLRS
                  Containers = [] }
                |> Some
            | AutomaticPlaceholder | External _ ->
                None

        let ai =
            match fns.AppInsightsName with
            | Some (AutomaticallyCreated resourceName) ->
                Some
                    { Name = resourceName
                      Location = location
                      LinkedWebsite =
                        match fns.OperatingSystem with
                        | Windows -> Some fns.Name
                        | Linux -> None }
            | Some (External _)
            | Some AutomaticPlaceholder
            | None ->
                None
        {| Ai = ai
           WebApp = webApp
           ServerFarm = serverFarm
           Storage = storage |}
    let appInsights location (ai:AppInsightsConfig) =
        { Name = ai.Name
          Location = location
          LinkedWebsite = None }

    module Outputters =
        let appInsights (resource:AppInsights) = {|
            ``type`` = "Microsoft.Insights/components"
            kind = "web"
            name = resource.Name.Value
            location = resource.Location.Value
            apiVersion = "2014-04-01"
            tags =
                [ match resource.LinkedWebsite with
                  | Some linkedWebsite -> sprintf "[concat('hidden-link:', resourceGroup().id, '/providers/Microsoft.Web/sites/', '%s')]" linkedWebsite.Value, "Resource"
                  | None -> ()
                  "displayName", "AppInsightsComponent" ]
                |> Map.ofList
            properties =
             match resource.LinkedWebsite with
             | Some linkedWebsite ->
                box {| name = resource.Name.Value
                       Application_Type = "web"
                       ApplicationId = linkedWebsite.Value |}
             | None ->
                box {| name = resource.Name.Value
                       Application_Type = "web" |}
        |}
        let serverFarm (farm:ServerFarm) =
            {|  ``type`` = "Microsoft.Web/serverfarms"
                sku =
                    {| name = farm.Sku
                       tier = farm.Tier
                       size = farm.WorkerSize
                       family = if farm.IsDynamic then "Y" else null
                       capacity = if farm.IsDynamic then 0 else farm.WorkerCount |}
                name = farm.Name.Value
                apiVersion = "2018-02-01"
                location = farm.Location.Value
                properties =
                    if farm.IsDynamic then
                        box {| name = farm.Name.Value
                               computeMode = "Dynamic"
                               reserved = farm.IsLinux |}
                    else
                        box {| name = farm.Name.Value
                               perSiteScaling = false
                               reserved = farm.IsLinux |}
                kind = farm.Kind |> Option.toObj
            |}
        let webApp (webApp:WebApp) = {|
            ``type`` = "Microsoft.Web/sites"
            name = webApp.Name.Value
            apiVersion = "2016-08-01"
            location = webApp.Location.Value
            dependsOn = webApp.Dependencies |> List.map(fun p -> p.Value)
            kind = webApp.Kind
            properties =
                {| serverFarmId = webApp.ServicePlan.Value
                   httpsOnly = webApp.HTTPSOnly
                   siteConfig =
                        [ "alwaysOn", box webApp.AlwaysOn
                          "appSettings", webApp.AppSettings |> List.map(fun (k,v) -> {| name = k; value = v |}) |> box
                          match webApp.LinuxFxVersion with Some v -> "linuxFxVersion", box v | None -> ()
                          match webApp.AppCommandLine with Some v -> "appCommandLine", box v | None -> ()
                          match webApp.NetFrameworkVersion with Some v -> "netFrameworkVersion", box v | None -> ()
                          match webApp.JavaVersion with Some v -> "javaVersion", box v | None -> ()
                          match webApp.JavaContainer with Some v -> "javaContainer", box v | None -> ()
                          match webApp.JavaContainerVersion with Some v -> "javaContainerVersion", box v | None -> ()
                          match webApp.PhpVersion with Some v -> "phpVersion", box v | None -> ()
                          match webApp.PythonVersion with Some v -> "pythonVersion", box v | None -> ()
                          "metadata", webApp.Metadata |> List.map(fun (k,v) -> {| name = k; value = v |}) |> box ]
                        |> Map.ofList
                 |}
        |}


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
          Runtime = DotNetCore DotNetCoreLts
          OperatingSystem = Windows
          ZipDeployPath = None
          DockerImage = None }
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
    /// Sets the dotnetcore runtime stack
    member __.RuntimeStack(state:WebAppConfig, runtime) = { state with Runtime = DotNetCore runtime }
    /// Sets the ASP NET runtime stack
    member __.RuntimeStack(state:WebAppConfig, runtime) = { state with Runtime = AspNet runtime }
    /// Sets the Java runtime stack
    member __.RuntimeStack(state:WebAppConfig, runtime) = { state with Runtime = Java runtime }
    /// Sets the PHP runtime stack
    member __.RuntimeStack(state:WebAppConfig, runtime) = { state with Runtime = Php runtime }
    /// Sets the Python runtime stack
    member __.RuntimeStack(state:WebAppConfig, runtime) = { state with Runtime = Python runtime }
    /// Sets the Ruby runtime stack
    member __.RuntimeStack(state:WebAppConfig, runtime) = { state with Runtime = Ruby runtime }
    [<CustomOperation "operating_system">]
    /// Sets the operating system
    member __.OperatingSystem(state:WebAppConfig, os) = { state with OperatingSystem = os }
    [<CustomOperation "zip_deploy">]
    /// Specifies a folder path or a zip file containing the web application to install as a post-deployment task.
    member __.ZipDeploy(state:WebAppConfig, path) = { state with ZipDeployPath = Some path }
    [<CustomOperation "docker_image">]
    /// Specifies a docker image to use from the registry (linux only), and the startup command to execute.
    member __.DockerImage(state:WebAppConfig, registryPath, startupFile) = { state with DockerImage = Some (registryPath, startupFile) }


type FunctionsBuilder() =
    member __.Yield _ =
        { Name = ResourceName.Empty
          ServicePlanName = AutomaticPlaceholder
          AppInsightsName = Some AutomaticPlaceholder
          StorageAccountName = AutomaticPlaceholder
          Runtime = DotNet
          ExtensionVersion = V2
          HTTPSOnly = false
          OperatingSystem = Windows
          Settings = Map.empty
          Dependencies = [] }
    member __.Run (state:FunctionsConfig) =
        { state with
            ServicePlanName =
                match state.ServicePlanName with
                | External e -> External e
                | AutomaticPlaceholder -> AutomaticallyCreated(ResourceName(sprintf "%s-plan" state.Name.Value))
                | AutomaticallyCreated a -> AutomaticallyCreated a
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
                Ai.tryCreateAppInsightsName state.AppInsightsName state.Name.Value
        }
    /// Sets the name of the functions instance.
    [<CustomOperation "name">]
    member __.Name(state:FunctionsConfig, name) = { state with Name = ResourceName name }
    /// Sets the name of the service plan hosting the function instance.
    [<CustomOperation "service_plan_name">]
    member __.ServicePlanName(state:FunctionsConfig, name) = { state with ServicePlanName = AutomaticallyCreated(ResourceName name) }
    /// Do not create an automatic storage account; instead, link to a storage account that is created outside of this Functions instance.
    [<CustomOperation "link_to_service_plan">]
    member __.LinkToServicePlan(state:FunctionsConfig, name) = { state with ServicePlanName = External name }
    [<CustomOperation "link_to_storage_account">]
    member __.StorageAccountName(state:FunctionsConfig, name) = { state with StorageAccountName = External (ResourceName name) }
    member __.StorageAccountName(state:FunctionsConfig, name) = { state with StorageAccountName = External name }
    /// Sets the name of the automatically-created app insights instance.
    [<CustomOperation "app_insights_auto_name">]
    member __.UseAppInsights(state:FunctionsConfig, name) = { state with AppInsightsName = Some (AutomaticallyCreated name) }
    member this.UseAppInsights(state:FunctionsConfig, name:string) = this.UseAppInsights(state, ResourceName name)
    /// Removes any automatic app insights creation, configuration and settings for this webapp.
    [<CustomOperation "app_insights_off">]
    member __.DeactivateAppInsights(state:FunctionsConfig) = { state with AppInsightsName = None }
    /// Disables http for this webapp so that only https is used.
    [<CustomOperation "https_only">]
    member __.HttpsOnly(state:FunctionsConfig) = { state with HTTPSOnly = true }
    /// Instead of creating a new AI instance, configure this webapp to point to another AI instance that you are managing
    /// yourself.
    [<CustomOperation "link_to_app_insights">]
    member __.LinkAppInsights(state:FunctionsConfig, name) = { state with AppInsightsName = Some(External name) }
    member __.LinkAppInsights(state:FunctionsConfig, name) = { state with AppInsightsName = name |> Option.map External }
    /// Sets the runtime of the Functions host.
    [<CustomOperation "use_runtime">]
    member __.Runtime(state:FunctionsConfig, runtime) = { state with Runtime = runtime }
    [<CustomOperation "use_extension_version">]
    member __.ExtensionVersion(state:FunctionsConfig, version) = { state with ExtensionVersion = version }
    /// Sets the operating system of the Functions host.
    [<CustomOperation "operating_system">]
    member __.OperatingSystem(state:FunctionsConfig, os) = { state with OperatingSystem = os }
    /// Sets an app setting of the web app in the form "key" "value".
    [<CustomOperation "setting">]
    member __.AddSetting(state:FunctionsConfig, key, value) = { state with Settings = state.Settings.Add(key, value) }
    member __.AddSetting(state:FunctionsConfig, key, value:ArmExpression) = { state with Settings = state.Settings.Add(key, value.Eval()) }
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

[<AutoOpen>]
module Extensions =
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
    type ArmBuilder.ArmBuilder with
        member __.AddResource(state:ArmConfig, config:WebAppConfig) =
            let outputs = Converters.webApp state.Location config
            let resources = [
                WebApp outputs.WebApp
                match outputs.ServerFarm with Some farm -> ServerFarm farm | None -> ()
                match outputs.Ai with Some ai -> AppInsights ai | None -> ()
            ]
            { state with Resources = state.Resources @ resources }
        member __.AddResource(state:ArmConfig, config:FunctionsConfig) =
            let outputs = config |> Converters.functions state.Location
            let resources = [
                WebApp outputs.WebApp
                match outputs.ServerFarm with Some farm -> ServerFarm farm | None -> ()
                match outputs.Ai with Some ai -> AppInsights ai | None -> ()
                match outputs.Storage with Some storage -> StorageAccount storage | None -> ()
            ]
            { state with Resources = state.Resources @ resources }
        member __.AddResource(state:ArmConfig, config:ServicePlanConfig) =
            let output = config |> Converters.serverFarm state.Location
            { state with Resources = state.Resources @ [ ServerFarm output ]}
        member this.AddResource(state:ArmConfig, config:AppInsightsConfig) =
            { state with Resources = AppInsights (Converters.appInsights state.Location config) :: state.Resources }
        member this.AddResources (state, configs) = addResources<FunctionsConfig> this.AddResource state configs
        member this.AddResources (state, configs) = addResources<AppInsightsConfig> this.AddResource state configs
        member this.AddResources (state, configs) = addResources<WebAppConfig> this.AddResource state configs

let appInsights = AppInsightsBuilder()
let webApp = WebAppBuilder()
let functions = FunctionsBuilder()
let servicePlan = ServicePlanBuilder()