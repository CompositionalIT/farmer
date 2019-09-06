namespace Farmer

open Farmer.Internal

module Helpers =
    module WebApp =
        module Skus =
            let F1 = Literal "F1"
            let B1 = Literal "B1"
            let B2 = Literal "B2"
            let B3 = Literal "B3"
            let S1 = Literal "S1"
            let S2 = Literal "S2"
            let S3 = Literal "S3"
            let P1 = Literal "P1"
            let P2 = Literal "P2"
            let P3 = Literal "P3"
            let P1V2 = Literal "P1V2"
            let P2V2 = Literal "P2V2"
            let P3V2 = Literal "P3V2"
            let I1 = Literal "I1"
            let I2 = Literal "I2"
            let I3 = Literal "I3"

        let PublishingPassword (webSite:Value) =
            sprintf "[list(resourceId('Microsoft.Web/sites/config', %s, 'publishingcredentials'), '2014-06-01').properties.publishingPassword]" webSite.QuotedValue
            |> Literal

        module AppSettings =
            let WebsiteNodeDefaultVersion version = "WEBSITE_NODE_DEFAULT_VERSION", version
            let RunFromPackage = "WEBSITE_RUN_FROM_PACKAGE", Literal "1"
    module Storage =
        let accountKey (accountName:Value) =
            sprintf
                "[concat('DefaultEndpointsProtocol=https;AccountName=', %s, ';AccountKey=', listKeys(resourceId('Microsoft.Storage/storageAccounts/', %s), '2017-10-01').keys[0].value)]"
                accountName.QuotedValue
                accountName.QuotedValue

        let StandardLRS = Literal "Standard_LRS"
        let StandardGRS = Literal "Standard_GRS"
        let StandardRAGRS = Literal "Standard_RAGRS"
        let StandardZRS = Literal "Standard_ZRS"
        let StandardGZRS = Literal "Standard_GZRS"
        let StandardRAGZRS = Literal "Standard_RAGZRS"
        let PremiumLRS = Literal "Premium_LRS"
        let PremiumZRS = Literal "Premium_ZRS"
    module AppInsights =
        let instrumentationKey (accountName:Value) =
            sprintf "[reference(concat('Microsoft.Insights/components/', %s)).InstrumentationKey]" accountName.QuotedValue
    module Locations =
        let ``East Asia`` = Literal "eastasia"
        let ``Southeast Asia`` = Literal "southeastasia"
        let ``Central US`` = Literal "centralus"
        let ``East US`` = Literal "eastus"
        let ``East US 2`` = Literal "eastus2"
        let ``West US`` = Literal "westus"
        let ``North Central US`` = Literal "northcentralus"
        let ``South Central US`` = Literal "southcentralus"
        let ``North Europe`` = Literal "northeurope"
        let ``West Europe`` = Literal "westeurope"
        let ``Japan West`` = Literal "japanwest"
        let ``Japan East`` = Literal "japaneast"
        let ``Brazil South`` = Literal "brazilsouth"
        let ``Australia East`` = Literal "australiaeast"
        let ``Australia Southeast`` = Literal "australiasoutheast"
        let ``South India`` = Literal "southindia"
        let ``Central India`` = Literal "centralindia"
        let ``West India`` = Literal "westindia"

[<NoComparison>]
type ArmConfig =
    { Parameters : string Set
      Variables : (string * string) list
      Outputs : (string * Value) list
      Location : Value
      Resources : obj list }

type WebAppConfig =
    { Name : Value
      ServicePlanName : Value
      Sku : Value
      AppInsights : Value option
      RunFromPackage : bool
      WebsiteNodeDefaultVersion : Value option
      Settings : Map<string, Value>
      Dependencies : (Dependency * Value) list }
    member this.PublishingPassword = Helpers.WebApp.PublishingPassword this.Name

type StorageAccount =
    { /// The name of the storage account.
      Name : Value
      /// The sku of the storage account.
      Sku : Value }
    member this.Key = Literal (Helpers.Storage.accountKey this.Name)
    member this.DependencyPath = StorageDependency, this.Name

type WebAppBuilder() =
    member __.Yield _ =
        { Name = Literal ""
          ServicePlanName = Literal ""
          Sku = Helpers.WebApp.Skus.F1
          AppInsights = None
          RunFromPackage = false
          WebsiteNodeDefaultVersion = None
          Settings = Map.empty
          Dependencies = [] }
    member __.Run (state:WebAppConfig) =
        { state with
            Dependencies = (Dependency.ServerFarmDependency, state.ServicePlanName) :: state.Dependencies }
    /// Sets the name of the web app; use the `name` keyword.
    [<CustomOperation "name">]
    member __.Name(state:WebAppConfig, name:Value) = { state with Name = name }
    /// Sets the name of service plan of the web app; use the `service_plan_name` keyword.
    [<CustomOperation "service_plan_name">]
    member __.ServicePlanName(state:WebAppConfig, name:Value) = { state with ServicePlanName = name }
    /// Sets the sku of the web app; use the `sku` keyword.
    [<CustomOperation "sku">]
    member __.Sku(state:WebAppConfig, sku:Value) = { state with Sku = sku }
    /// Creates a fully-configured application insights resource linked to this web app; use the `use_app_insights` keyword.
    [<CustomOperation "use_app_insights">]
    member __.UseAppInsights(state:WebAppConfig, name) = { state with AppInsights = Some name }
    /// Sets the web app to use run from package mode; use the `run_from_package` keyword.
    [<CustomOperation "run_from_package">]
    member __.RunFromPackage(state:WebAppConfig) = { state with RunFromPackage = true }
    /// Sets the node version of the web app; use the `website_node_default_version` keyword.
    [<CustomOperation "website_node_default_version">]
    member __.NodeVersion(state:WebAppConfig, version) = { state with WebsiteNodeDefaultVersion = Some version }
    /// Sets an app setting of the web app; use the `setting` keyword.
    [<CustomOperation "setting">]
    member __.AddSetting(state:WebAppConfig, key, value) = { state with Settings = state.Settings.Add(key, value) }
    /// Sets a dependency for the web app; use the `depends_on` keyword.
    [<CustomOperation "depends_on">]
    member __.DependsOn(state:WebAppConfig, (dependencyType, resourceName)) =
        { state with Dependencies = (dependencyType, resourceName) :: state.Dependencies }

type ArmBuilder() =
    member __.Yield _ =
        { Parameters = Set.empty
          Variables = List.empty
          Outputs = List.empty
          Resources = List.empty
          Location = Helpers.Locations.``West Europe`` }

    member __.Run (state:ArmConfig) = {
        Parameters = state.Parameters |> Set.toList
        Outputs = state.Outputs
        Variables = state.Variables
        Resources =
            state.Resources
            |> List.collect(function
            | :? StorageAccount as s ->
                [ StorageAccount { Location = state.Location; Name = s.Name; Sku = s.Sku } ]
            | :? WebAppConfig as c -> [
                let webApp =
                    { Name = c.Name
                      AppSettings = [
                        yield! Map.toList c.Settings
                        if c.RunFromPackage then yield Helpers.WebApp.AppSettings.RunFromPackage

                        match c.WebsiteNodeDefaultVersion with
                        | Some v ->
                            yield Helpers.WebApp.AppSettings.WebsiteNodeDefaultVersion v
                        | None ->
                            ()

                        match c.AppInsights with
                        | Some v ->
                            yield "APPINSIGHTS_INSTRUMENTATIONKEY", Literal (Helpers.AppInsights.instrumentationKey v)
                            yield "APPINSIGHTS_PROFILERFEATURE_VERSION", Literal ("1.0.0")
                            yield "APPINSIGHTS_SNAPSHOTFEATURE_VERSION", Literal ("1.0.0")
                            yield "ApplicationInsightsAgent_EXTENSION_VERSION", Literal ("~2")
                            yield "DiagnosticServices_EXTENSION_VERSION", Literal ("~3")
                            yield "InstrumentationEngine_EXTENSION_VERSION", Literal ("~1")
                            yield "SnapshotDebugger_EXTENSION_VERSION", Literal ("~1")
                            yield "XDT_MicrosoftApplicationInsights_BaseExtensions", Literal ("~1")
                            yield "XDT_MicrosoftApplicationInsights_Mode", Literal ("recommended")
                        | None ->
                            ()
                      ]
                      Extensions =
                        match c.AppInsights with
                        | Some _ ->
                            Set [ AppInsightsExtension ]
                        | None ->
                            Set.empty
                      Dependencies = [
                        yield! c.Dependencies
                        match c.AppInsights with
                        | Some v ->
                            yield (AppInsightsDependency, v)
                        | None ->
                            ()
                      ]
                    }

                let serverFarm =
                    ServerFarm
                        { Location = state.Location
                          Name = c.ServicePlanName
                          Sku = c.Sku
                          WebApps = [ webApp ] }

                yield serverFarm
                match c.AppInsights with | Some ai -> yield AppInsights { AppInsights.Name = ai; Location = state.Location; LinkedWebsite = c.Name } | None -> () ]
            | _ ->
                failwith "Sorry, I don't know how to handle this resource.")
    }

    /// Creates a variable; use the `variable` keyword.
    [<CustomOperation "variable">]
    member __.AddVariable (state, key, value) : ArmConfig =
        { state with
            Variables = (key, value) :: state.Variables }

    /// Creates a parameter; use the `parameter` keyword.
    [<CustomOperation "parameter">]
    member __.AddParameter (state, parameter) : ArmConfig =
        { state with
            Parameters = state.Parameters.Add parameter }

    /// Creates a list of parameters; use the `parameters` keyword.
    [<CustomOperation "parameters">]
    member __.AddParameters (state, parameters) : ArmConfig =
        { state with
            Parameters = state.Parameters + (Set.ofList parameters) }

    /// Creates an output; use the `output` keyword.
    [<CustomOperation "output">]
    member __.Output (state, outputName, outputValue) : ArmConfig =
        { state with
            Outputs = (outputName, outputValue) :: state.Outputs }

    /// Sets the default location of all resources; use the `location` keyword.
    [<CustomOperation "location">]
    member __.Location (state, location) : ArmConfig = { state with Location = location }

    /// Adds a resource to the template; use the `resource` keyword.
    [<CustomOperation "resource">]
    member __.AddResource(state, resource) : ArmConfig =
        { state with Resources = box resource :: state.Resources }


[<AutoOpen>]
module Builders =
    let webApp = WebAppBuilder()
    let arm = ArmBuilder()