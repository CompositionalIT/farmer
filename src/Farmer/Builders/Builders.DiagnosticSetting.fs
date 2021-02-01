[<AutoOpen>]
module Farmer.Builders.DiagnosticSetting

open Farmer
open Farmer.Builders.Storage
open Farmer.Arm.Storage
open Farmer.Arm.LogAnalytics
open Farmer.Arm.DiagnosticSetting

type DiagnosticSettingsConfig =
    { Name : ResourceName
      MetricsSource: ResourceId

      Sinks :
        {| StorageAccount : ResourceId option
           EventHub : {| AuthorizationRuleId : ResourceId; EventHubName : ResourceName option |} option
           LogAnalyticsWorkspace : ResourceId option
           //TODO: Find out which "way" Dedicated goes (see https://docs.microsoft.com/en-us/azure/templates/microsoft.insights/diagnosticsettings#subscriptiondiagnosticsettings-object)
           DedicatedLogAnalyticsDestination : FeatureFlag option |}

      Metrics : MetricSetting list
      Logs : LogSetting list

      Dependencies : ResourceId Set
      Tags : Map<string, string> }
    interface IBuilder with
        member this.ResourceId = diagnosticSettingsType(this.MetricsSource.Type).resourceId this.Name
        member this.BuildResources location = [
            { Name = this.Name
              Location = location
              MetricsSource = this.MetricsSource
              Sinks = this.Sinks
              Logs = this.Logs
              Metrics = this.Metrics
              Dependencies = this.Dependencies
              Tags = this.Tags }
        ]

type DiagnosticSettingsBuilder() =
    member _.Yield _ =
         { Name = ResourceName.Empty
           Sinks =
            {| StorageAccount = None
               EventHub = None
               LogAnalyticsWorkspace = None
               DedicatedLogAnalyticsDestination = None |}
           Metrics = []
           Logs = []
           MetricsSource = ResourceId.create(ResourceType("", ""), ResourceName "")
           Dependencies = Set.empty
           Tags = Map.empty }
    member _.Run(state:DiagnosticSettingsConfig) =
        match state with
        | { Metrics = []; Logs = [] } ->
            failwith "You must specify at least one metric or log setting."
        | _ when state.Sinks.EventHub = None && state.Sinks.StorageAccount = None && state.Sinks.LogAnalyticsWorkspace = None ->
            failwith "You must specify at least one data sink."
        | _ ->
            state

    /// Sets the name of the diagnostic settings.
    [<CustomOperation "name">]
    member _.Name(state: DiagnosticSettingsConfig, resourceName:string) =
        { state with Name = ResourceName resourceName }

    /// The source resource of diagnostic metrics.
    [<CustomOperation "metrics_source">]
    member _.ParentResource(state:DiagnosticSettingsConfig, metricsSource:ResourceId) =
       { state with MetricsSource = metricsSource }

    /// Sets the storage Account.
    [<CustomOperation "storage_account">]
    member _.StorageAccount (state:DiagnosticSettingsConfig, storageAccount) =
        { state with Sinks = {| state.Sinks with StorageAccount = Some storageAccount |} }
    member this.StorageAccount (state, storageAccount:StorageAccountConfig) =
        this.StorageAccount (state, storageAccounts.resourceId storageAccount.Name.ResourceName)

    /// Sets the authorization rule Id for the event hub.
    [<CustomOperation "event_hub_authorization_rule_id">]
    member _.EventHubAuthorizationRuleId(state: DiagnosticSettingsConfig, eventHubAuthorizationRuleId) =
        { state with Sinks = {| state.Sinks with EventHub = Some {| AuthorizationRuleId = eventHubAuthorizationRuleId; EventHubName = None |} |} }

    /// The name of the event hub. If none is specified, the default event hub will be selected.
    [<CustomOperation "event_hub_name">]
    member _.EventHubName(state: DiagnosticSettingsConfig, eventHubName) =
        { state with
            Sinks =
                {| state.Sinks with
                    EventHub =
                        match state.Sinks.EventHub with
                        | Some hub -> Some {| hub with EventHubName = Some eventHubName |}
                        | None -> failwith "You must set the Authorization Rule Id before setting the event hub name" |}
        }
    member this.EventHubName(state, eventHubName) = this.EventHubName(state, ResourceName eventHubName)

    /// Sets the log analytics workspace id.
    [<CustomOperation "log_analytics_workspace">]
    member _.WorkspaceId(state: DiagnosticSettingsConfig, workspaceId) =
        { state with Sinks = {| state.Sinks with LogAnalyticsWorkspace = Some workspaceId |} }
    member this.WorkspaceId(state, workspace:WorkspaceConfig) =
        this.WorkspaceId(state, workspaces.resourceId workspace.Name)

    /// Enable dedicated log analytics."
    [<CustomOperation "enable_dedicated_loganalytics">]
    member _.DedicatedLogAnalyticsDestination(state: DiagnosticSettingsConfig) =
        { state with Sinks = {| state.Sinks with DedicatedLogAnalyticsDestination = Some Enabled |} }

    /// Add metric settings to the resource.
    [<CustomOperation "metrics">]
    member _.Metrics(state: DiagnosticSettingsConfig, metrics) =
        { state with Metrics = List.append metrics state.Metrics }

    /// Add Log settings to the resource.
    [<CustomOperation "logs">]
    member _.Logs(state:DiagnosticSettingsConfig, logs) =
        { state with Logs = List.append logs state.Logs }

    interface IDependable<DiagnosticSettingsConfig> with member _.Add state newDeps = { state with Dependencies = state.Dependencies + newDeps }
    interface ITaggable<DiagnosticSettingsConfig> with member _.Add state tags = { state with Tags = state.Tags |> Map.merge tags }

let diagnosticSettings = DiagnosticSettingsBuilder()