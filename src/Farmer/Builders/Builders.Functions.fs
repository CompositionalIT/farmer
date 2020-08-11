[<AutoOpen>]
module Farmer.Builders.Functions

open Farmer
open Farmer.CoreTypes
open Farmer.Helpers
open Farmer.WebApp
open Farmer.Arm.Web
open Farmer.Arm.Insights
open Farmer.Arm.Storage
open System

type FunctionsRuntime = DotNet | Node | Java | Python
type FunctionsExtensionVersion = V1 | V2 | V3
type FunctionsConfig =
    { Name : ResourceName
      ServicePlan : ResourceRef<FunctionsConfig>
      HTTPSOnly : bool
      AppInsights : ResourceRef<FunctionsConfig> option
      OperatingSystem : OS
      Settings : Map<string, Setting>
      Tags : Map<string, string>
      Dependencies : ResourceName list
      Cors : Cors option
      StorageAccount : ResourceRef<FunctionsConfig>
      Runtime : FunctionsRuntime
      ExtensionVersion : FunctionsExtensionVersion
      Identity : FeatureFlag option
      ZipDeployPath : string option }

    /// Gets the system-created managed principal for the functions instance. It must have been enabled using enable_managed_identity.
    member this.SystemIdentity =
        sprintf "reference(resourceId('Microsoft.Web/sites', '%s'), '2019-08-01', 'full').identity.principalId" this.Name.Value
        |> ArmExpression.create
        |> PrincipalId
    /// Gets the ARM expression path to the publishing password of this functions app.
    member this.PublishingPassword = publishingPassword this.Name
    /// Gets the ARM expression path to the storage account key of this functions app.
    member this.StorageAccountKey = Storage.buildKey this.StorageAccountName
    /// Gets the ARM expression path to the app insights key of this functions app, if it exists.
    member this.AppInsightsKey = this.AppInsightsName |> Option.map instrumentationKey
    /// Gets the default key for the functions site
    member this.DefaultKey =
        sprintf "listkeys(concat(resourceId('Microsoft.Web/sites', '%s'), '/host/default/'),'2016-08-01').functionKeys.default" this.Name.Value
        |> ArmExpression.create
    /// Gets the master key for the functions site
    member this.MasterKey =
        sprintf "listkeys(concat(resourceId('Microsoft.Web/sites', '%s'), '/host/default/'),'2016-08-01').masterKey" this.Name.Value
        |> ArmExpression.create
    /// Gets the Service Plan name for this functions app.
    member this.ServicePlanName = this.ServicePlan.CreateResourceName this
    /// Gets the App Insights name for this functions app, if it exists.
    member this.AppInsightsName = this.AppInsights |> Option.map (fun ai -> ai.CreateResourceName this)
    /// Gets the Storage Account name for this functions app.
    member this.StorageAccountName = this.StorageAccount.CreateResourceName this
    interface IBuilder with
        member this.DependencyName = this.ServicePlanName
        member this.BuildResources location = [
            { Name = this.Name
              ServicePlan = this.ServicePlanName
              Location = location
              Cors = this.Cors
              Tags = this.Tags
              AppSettings = [
                "FUNCTIONS_WORKER_RUNTIME", (string this.Runtime).ToLower()
                "WEBSITE_NODE_DEFAULT_VERSION", "10.14.1"
                "FUNCTIONS_EXTENSION_VERSION", match this.ExtensionVersion with V1 -> "~1" | V2 -> "~2" | V3 -> "~3"
                "AzureWebJobsStorage", Storage.buildKey this.StorageAccountName |> ArmExpression.Eval
                "AzureWebJobsDashboard", Storage.buildKey this.StorageAccountName |> ArmExpression.Eval

                match this.AppInsightsKey with
                | Some key -> "APPINSIGHTS_INSTRUMENTATIONKEY", key |> ArmExpression.Eval
                | None -> ()

                if this.OperatingSystem = Windows then
                    "WEBSITE_CONTENTAZUREFILECONNECTIONSTRING", Storage.buildKey this.StorageAccountName |> ArmExpression.Eval
                    "WEBSITE_CONTENTSHARE", this.Name.Value.ToLower()
              ]
              |> List.map Setting.AsLiteral
              |> List.append (this.Settings |> Map.toList)

              Identity = this.Identity
              Kind =
                match this.OperatingSystem with
                | Windows -> "functionapp"
                | Linux -> "functionapp,linux"
              Dependencies = [
                yield! this.Dependencies
                match this.AppInsights with
                | Some (DependableResource this resourceName) -> resourceName
                | _ -> ()
                match this.ServicePlan with
                | DependableResource this resourceName -> resourceName
                | _ -> ()

                this.StorageAccountName
              ]
              AlwaysOn = false
              HTTPSOnly = this.HTTPSOnly
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
            }
            match this.ServicePlan with
            | DeployableResource this resourceName ->
                { Name = resourceName
                  Location = location
                  Sku = Sku.Y1
                  WorkerSize = Serverless
                  WorkerCount = 0
                  OperatingSystem = this.OperatingSystem
                  Tags = this.Tags }
            | _ ->
                ()
            match this.StorageAccount with
            | DeployableResource this resourceName ->
                { StorageAccount.Name = resourceName
                  Location = location
                  Sku = Storage.Standard_LRS
                  StaticWebsite = None
                  EnableHierarchicalNamespace = false
                  Tags = this.Tags }
            | _ ->
                ()
            match this.AppInsights with
            | Some (DeployableResource this resourceName) ->
                { Name = resourceName
                  Location = location
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
    member __.Yield _ =
        { Name = ResourceName.Empty
          ServicePlan = derived (fun config -> config.Name.Map(sprintf "%s-farm"))
          AppInsights = Some (derived (fun config -> config.Name.Map(sprintf "%s-ai")))
          StorageAccount = derived (fun config ->
            config.Name.Map (sprintf "%sstorage")
            |> sanitiseStorage
            |> ResourceName)
          Runtime = DotNet
          ExtensionVersion = V3
          Cors = None
          HTTPSOnly = false
          OperatingSystem = Windows
          Settings = Map.empty
          Dependencies = []
          Identity = None
          Tags = Map.empty
          ZipDeployPath = None }
    /// Sets the name of the functions instance.
    [<CustomOperation "name">]
    member __.Name(state:FunctionsConfig, name) = { state with Name = ResourceName name }
    /// Sets the name of the service plan hosting the function instance.
    [<CustomOperation "service_plan_name">]
    member __.ServicePlanName(state:FunctionsConfig, name) = { state with ServicePlan = AutoCreate(Named(ResourceName name)) }
    /// Do not create an automatic storage account; instead, link to a storage account that is created outside of this Functions instance.
    [<CustomOperation "link_to_service_plan">]
    member __.LinkToServicePlan(state:FunctionsConfig, name) = { state with ServicePlan = External name }
    [<CustomOperation "link_to_storage_account">]
    member __.LinkToStorageAccount(state:FunctionsConfig, name) = { state with StorageAccount = External (Managed name) }
    member this.LinkToStorageAccount(state:FunctionsConfig, name) = this.LinkToStorageAccount(state, ResourceName name)
    /// Sets the name of the automatically-created app insights instance.
    [<CustomOperation "app_insights_name">]
    member __.AppInsightsName(state:FunctionsConfig, name) = { state with AppInsights = Some (AutoCreate (Named name)) }
    member this.AppInsightsName(state:FunctionsConfig, name:string) = this.AppInsightsName(state, ResourceName name)
    /// Removes any automatic app insights creation, configuration and settings for this webapp.
    [<CustomOperation "app_insights_off">]
    member __.DeactivateAppInsights(state:FunctionsConfig) = { state with AppInsights = None }
    /// Disables http for this webapp so that only https is used.
    [<CustomOperation "https_only">]
    member __.HttpsOnly(state:FunctionsConfig) = { state with HTTPSOnly = true }
    /// Instead of creating a new AI instance, configure this webapp to point to another AI instance that you are managing
    /// yourself.
    [<CustomOperation "link_to_app_insights">]
    member __.LinkToAppInsights(state:FunctionsConfig, name) = { state with AppInsights = Some(External (Managed name)) }
    member __.LinkToAppInsights(state:FunctionsConfig, name) = { state with AppInsights = name |> Option.map (Managed >> External)  }
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
    member __.AddSetting(state:FunctionsConfig, key, value) =
        { state with Settings = state.Settings.Add(key, LiteralSetting value) }
    member this.AddSetting(state:FunctionsConfig, key, value:ArmExpression) =
        this.AddSetting(state, key, value.Eval())
    /// Sets a list of app setting of the web app in the form "key" "value".
    [<CustomOperation "settings">]
    member __.AddSettings(state:FunctionsConfig, settings: (string*string) list) =
        settings
        |> List.fold (fun state (key,value: string) -> __.AddSetting(state, key, value)) state
    /// Sets a dependency for the functions app.
    /// Creates an app setting of the web app whose value will be supplied as a secret parameter.
    [<CustomOperation "secret_setting">]
    member __.AddSecret(state:FunctionsConfig, key) =
        { state with Settings = state.Settings.Add(key, ParameterSetting (SecureParameter key)) }
    [<CustomOperation "depends_on">]
    member __.DependsOn(state:FunctionsConfig, resourceName) = { state with Dependencies = resourceName :: state.Dependencies }
    member __.DependsOn(state:FunctionsConfig, resource:IBuilder) = { state with Dependencies = resource.DependencyName :: state.Dependencies }
    member __.DependsOn(state:FunctionsConfig, resource:IArmResource) = { state with Dependencies = resource.ResourceName :: state.Dependencies }
    [<CustomOperation "enable_cors">]
    member _.EnableCors (state:FunctionsConfig, origins) = { state with Cors = Some (SpecificOrigins (List.map Uri origins)) }
    member _.EnableCors (state:FunctionsConfig, cors) = { state with Cors = Some cors }
    [<CustomOperation "enable_managed_identity">]
    member _.EnableManagedIdentity(state:FunctionsConfig) =
        { state with Identity = Some Enabled }
    [<CustomOperation "disable_managed_identity">]
    member _.DisableManagedIdentity(state:FunctionsConfig) =
        { state with Identity = Some Disabled }
    [<CustomOperation "add_tags">]
    member _.Tags(state:FunctionsConfig, pairs) = 
        { state with 
            Tags = pairs |> List.fold (fun map (key,value) -> Map.add key value map) state.Tags }
    [<CustomOperation "add_tag">]
    member this.Tag(state:FunctionsConfig, key, value) = this.Tags(state, [ (key,value) ])
    [<CustomOperation "zip_deploy">]
    /// Specifies a folder path or a zip file containing the function app to install as a post-deployment task.
    member __.ZipDeploy(state:FunctionsConfig, path) = { state with ZipDeployPath = Some path }


let functions = FunctionsBuilder()
