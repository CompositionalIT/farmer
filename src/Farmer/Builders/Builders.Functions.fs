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
      Dependencies : ResourceId list
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
    member this.StorageAccountKey = StorageAccount.getConnectionString this.StorageAccountName
    /// Gets the ARM expression path to the app insights key of this functions app, if it exists.
    member this.AppInsightsKey = this.AppInsightsName |> Option.map AppInsights.getInstrumentationKey
    /// Gets the default key for the functions site
    member this.DefaultKey =
        sprintf "listkeys(concat(resourceId('Microsoft.Web/sites', '%s'), '/host/default/'),'2016-08-01').functionKeys.default" this.Name.Value
        |> ArmExpression.create
    /// Gets the master key for the functions site
    member this.MasterKey =
        sprintf "listkeys(concat(resourceId('Microsoft.Web/sites', '%s'), '/host/default/'),'2016-08-01').masterKey" this.Name.Value
        |> ArmExpression.create
    /// Gets the Service Plan name for this functions app.
    member this.ServicePlanName = this.ServicePlan.CreateResourceId(this).Name
    /// Gets the App Insights name for this functions app, if it exists.
    member this.AppInsightsName : ResourceName option = this.AppInsights |> Option.map (fun ai -> ai.CreateResourceId(this).Name)
    /// Gets the Storage Account name for this functions app.
    member this.StorageAccountName : Storage.StorageAccountName = this.StorageAccount.CreateResourceId(this).Name |> Storage.StorageAccountName.Create |> Result.get
    interface IBuilder with
        member this.DependencyName = this.ServicePlanName
        member this.BuildResources location = [
            { Name = this.Name
              ServicePlan = this.ServicePlanName
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

                match this.AppInsightsKey with
                | Some key -> "APPINSIGHTS_INSTRUMENTATIONKEY", key |> ArmExpression.Eval
                | None -> ()

                if this.OperatingSystem = Windows then
                    "WEBSITE_CONTENTAZUREFILECONNECTIONSTRING", StorageAccount.getConnectionString this.StorageAccountName |> ArmExpression.Eval
                    "WEBSITE_CONTENTSHARE", this.Name.Value.ToLower()
              ]
              |> List.map Setting.AsLiteral
              |> List.append (this.Settings |> Map.toList)
              |> Map

              Identity = this.Identity
              Kind =
                match this.OperatingSystem with
                | Windows -> "functionapp"
                | Linux -> "functionapp,linux"
              Dependencies = [
                yield! this.Dependencies
                match this.AppInsights with
                | Some (DependableResource this resourceName) -> ResourceId.create resourceName
                | _ -> ()
                for setting in this.Settings do
                    match setting.Value with
                    | ExpressionSetting e ->
                        match e.Owner with
                        | Some owner -> owner
                        | None -> ()
                    | ParameterSetting _ | LiteralSetting _ ->
                        ()
                match this.ServicePlan with
                | DependableResource this resourceName -> ResourceId.create resourceName
                | _ -> ()

                ResourceId.create this.StorageAccountName.ResourceName
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
                { Name = Storage.StorageAccountName.Create(resourceName).OkValue
                  Location = location
                  Sku = Storage.Standard_LRS
                  StaticWebsite = None
                  EnableHierarchicalNamespace = None
                  Tags = this.Tags }
            | _ ->
                ()
            match this.AppInsights with
            | Some (DeployableResource this resourceName) ->
                { Name = resourceName
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
    /// Do not create an automatic service plan; instead, link to a service plan that is created outside of this Functions instance.
    [<CustomOperation "link_to_service_plan">]
    member __.LinkToServicePlan(state:FunctionsConfig, name) = { state with ServicePlan = External (Managed name) }
    member this.LinkToServicePlan(state:FunctionsConfig, name:string) = this.LinkToServicePlan (state, ResourceName name)
    member this.LinkToServicePlan(state:FunctionsConfig, config:ServicePlanConfig) = this.LinkToServicePlan (state, config.Name)
    /// Do not create an automatic storage account; instead, link to a storage account that is created outside of this Functions instance.
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
    member _.AddSetting(state:FunctionsConfig, key, value:ArmExpression) =
        { state with Settings = state.Settings.Add(key, ExpressionSetting value) }
    /// Sets a list of app setting of the web app in the form "key" "value".
    [<CustomOperation "settings">]
    member __.AddSettings(state:FunctionsConfig, settings: (string * string) list) =
        settings
        |> List.fold (fun state (key,value: string) -> __.AddSetting(state, key, value)) state
    /// Sets a dependency for the functions app.
    /// Creates an app setting of the web app whose value will be supplied as a secret parameter.
    [<CustomOperation "secret_setting">]
    member __.AddSecret(state:FunctionsConfig, key) =
        { state with Settings = state.Settings.Add(key, ParameterSetting (SecureParameter key)) }

    member private _.AddDependency (state:FunctionsConfig, resourceName:ResourceName) = { state with Dependencies = ResourceId.create resourceName :: state.Dependencies }
    member private _.AddDependencies (state:FunctionsConfig, resourceNames:ResourceName list) = { state with Dependencies = (resourceNames |> List.map ResourceId.create) @ state.Dependencies }
    /// Sets a dependency for the web app.
    [<CustomOperation "depends_on">]
    member this.DependsOn(state:FunctionsConfig, resourceName) = this.AddDependency(state, resourceName)
    member this.DependsOn(state:FunctionsConfig, resources) = this.AddDependencies(state, resources)
    member this.DependsOn(state:FunctionsConfig, builder:IBuilder) = this.AddDependency(state, builder.DependencyName)
    member this.DependsOn(state:FunctionsConfig, builders:IBuilder list) = this.AddDependencies(state, builders |> List.map (fun x -> x.DependencyName))
    member this.DependsOn(state:FunctionsConfig, resource:IArmResource) = this.AddDependency(state, resource.ResourceName)
    member this.DependsOn(state:FunctionsConfig, resources:IArmResource list) = this.AddDependencies(state, resources |> List.map (fun x -> x.ResourceName))

    /// sets the list of origins that should be allowed to make cross-origin calls. Use AllOrigins to allow all.
    [<CustomOperation "enable_cors">]
    member _.EnableCors (state:FunctionsConfig, origins) = { state with Cors = Some (SpecificOrigins (List.map Uri origins, None)) }
    member _.EnableCors (state:FunctionsConfig, origins) = { state with Cors = Some origins }
    /// Allows CORS requests with credentials.
    [<CustomOperation "enable_cors_credentials">]
    member _.EnableCorsCredentials (state:FunctionsConfig) =
        { state with
            Cors =
                state.Cors
                |> Option.map (function
                | SpecificOrigins (origins, _) -> SpecificOrigins (origins, Some true)
                | AllOrigins -> failwith "You cannot enable CORS Credentials if you have already set CORS to AllOrigins.")
        }
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
