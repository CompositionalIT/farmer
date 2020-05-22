[<AutoOpen>]
module Farmer.Builders.Functions

open Farmer
open Farmer.CoreTypes
open Farmer.Helpers
open Farmer.Web
open Farmer.Arm.Web
open Farmer.Arm.Insights
open Farmer.Arm.Storage

type FunctionsRuntime = DotNet | Node | Java | Python
type FunctionsExtensionVersion = V1 | V2 | V3
type FunctionsConfig =
    { Name : ResourceName
      ServicePlanName : ResourceRef
      HTTPSOnly : bool
      AppInsightsName : ResourceRef option
      OperatingSystem : OS
      Settings : Map<string, string>
      Dependencies : ResourceName list

      StorageAccountName : ResourceRef
      Runtime : FunctionsRuntime
      ExtensionVersion : FunctionsExtensionVersion }
    /// Gets the ARM expression path to the publishing password of this functions app.
    member this.PublishingPassword = publishingPassword this.Name
    /// Gets the ARM expression path to the storage account key of this functions app.
    member this.StorageAccountKey =
        Storage.buildKey this.StorageAccountName.ResourceName
    /// Gets the ARM expression path to the app insights key of this functions app, if it exists.
    member this.AppInsightsKey =
        this.AppInsightsName
        |> Option.bind (fun r -> r.ResourceNameOpt)
        |> Option.map instrumentationKey
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
    member this.StorageAccount = this.StorageAccountName.ResourceName
    interface IBuilder with
        member this.DependencyName = this.ServicePlanName.ResourceName
        member this.BuildResources location _ = [
            { Name = this.Name
              ServicePlan = this.ServicePlanName.ResourceName
              Location = location
              AppSettings = [
                yield! this.Settings |> Map.toList
                "FUNCTIONS_WORKER_RUNTIME", (string this.Runtime).ToLower()
                "WEBSITE_NODE_DEFAULT_VERSION", "10.14.1"
                "FUNCTIONS_EXTENSION_VERSION", match this.ExtensionVersion with V1 -> "~1" | V2 -> "~2" | V3 -> "~3"
                "AzureWebJobsStorage", Storage.buildKey this.StorageAccountName.ResourceName |> ArmExpression.Eval
                "AzureWebJobsDashboard", Storage.buildKey this.StorageAccountName.ResourceName |> ArmExpression.Eval

                match this.AppInsightsName with
                | Some (External resourceName)
                | Some (AutomaticallyCreated resourceName) ->
                    "APPINSIGHTS_INSTRUMENTATIONKEY", instrumentationKey resourceName |> ArmExpression.Eval
                | Some AutomaticPlaceholder
                | None -> ()

                if this.OperatingSystem = Windows then
                    "WEBSITE_CONTENTAZUREFILECONNECTIONSTRING", Storage.buildKey this.StorageAccountName.ResourceName |> ArmExpression.Eval
                    "WEBSITE_CONTENTSHARE", this.Name.Value.ToLower()
              ]

              Kind =
                match this.OperatingSystem with
                | Windows -> "functionapp"
                | Linux -> "functionapp,linux"
              Dependencies = [
                yield! this.Dependencies
                match this.AppInsightsName with
                | Some (AutomaticallyCreated appInsightsName)
                | Some (External appInsightsName) ->
                    appInsightsName
                | Some AutomaticPlaceholder
                | None ->
                    ()
                match this.ServicePlanName.ResourceNameOpt with Some resourceName -> resourceName | None -> ()
                this.StorageAccountName.ResourceName
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
              Parameters = []
            }
            match this.ServicePlanName with
            | External _
            | AutomaticPlaceholder ->
                ()
            | AutomaticallyCreated resourceName ->
                { Name = resourceName
                  Location = location
                  Sku = Sku.Y1
                  WorkerSize = Serverless
                  WorkerCount = 0
                  OperatingSystem = this.OperatingSystem }
            match this.StorageAccountName with
            | AutomaticallyCreated resourceName ->
                { StorageAccount.Name = resourceName
                  Location = location
                  Sku = Storage.Standard_LRS
                  Containers = [] }
            | AutomaticPlaceholder | External _ ->
                ()
            match this.AppInsightsName with
            | Some (AutomaticallyCreated resourceName) ->
                { Name = resourceName
                  Location = location
                  LinkedWebsite =
                    match this.OperatingSystem with
                    | Windows -> Some this.Name
                    | Linux -> None }
            | Some (External _)
            | Some AutomaticPlaceholder
            | None ->
                ()
        ]

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
                | AutomaticPlaceholder -> AutomaticallyCreated(ResourceName(sprintf "%s-farm" state.Name.Value))
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
                tryCreateAppInsightsName state.AppInsightsName state.Name.Value
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
    /// Sets a list of app setting of the web app in the form "key" "value".
    [<CustomOperation "settings">]
    member __.AddSettings(state:FunctionsConfig, settings: (string*string) list) =
        settings
        |> List.fold (fun state (key,value: string) -> __.AddSetting(state, key, value)) state
    /// Sets a dependency for the web app.
    [<CustomOperation "depends_on">]
    member __.DependsOn(state:FunctionsConfig, resourceName) =
        { state with Dependencies = resourceName :: state.Dependencies }

[<AutoOpen>]
module Extensions =
    open Farmer.Builders
    type FunctionsBuilder with
        member this.DependsOn(state:FunctionsConfig, storageAccountConfig:StorageAccountConfig) =
            this.DependsOn(state, storageAccountConfig.Name)
        member this.DependsOn(state:FunctionsConfig, webAppConfig:WebAppConfig) =
            this.DependsOn(state, webAppConfig.Name)
        member this.DependsOn(state:FunctionsConfig, appInsightsConfig:AppInsightsConfig) =
            this.DependsOn(state, appInsightsConfig.Name)

let functions = FunctionsBuilder()
