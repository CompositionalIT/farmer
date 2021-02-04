[<AutoOpen>]
module Farmer.Builders.DiagnosticSetting

open Farmer
open Farmer.Builders.Storage
open Farmer.Arm.Storage
open Farmer.Arm.EventHub
open Farmer.Arm.LogAnalytics
open Farmer.Arm.DiagnosticSetting

type DiagnosticSettingsConfig =
    { Name : ResourceName
      MetricsSource: ResourceId

      Sinks :
        {| StorageAccount : ResourceId option
           EventHub : {| AuthorizationRuleId : ResourceId; EventHubName : ResourceName option |} option
           LogAnalyticsWorkspace : (ResourceId * DestinationType) option |}

      Metrics : MetricSetting Set
      Logs : LogSetting Set

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
               LogAnalyticsWorkspace = None |}
           Metrics = Set.empty
           Logs = Set.empty
           MetricsSource = ResourceId.create(ResourceType("", ""), ResourceName "")
           Dependencies = Set.empty
           Tags = Map.empty }
    member _.Run(state:DiagnosticSettingsConfig) =
        let (|EmptySet|_|) theSet = if Set.isEmpty theSet then Some EmptySet else None
        match state with
        | { Metrics = EmptySet; Logs = EmptySet } ->
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
    member _.MetricsSource(state:DiagnosticSettingsConfig, metricsSource:ResourceId) =
       { state with
            MetricsSource = metricsSource
            Dependencies = state.Dependencies.Add metricsSource }
    member this.MetricsSource(state:DiagnosticSettingsConfig, builder:IBuilder) =
        this.MetricsSource(state, builder.ResourceId)

    static member AddDestinationPrivate (state:DiagnosticSettingsConfig, resourceId, ?dependency) =
        let dependency = defaultArg dependency resourceId
        { state with
            Sinks =
                match resourceId with
                | HasResourceType storageAccounts -> {| state.Sinks with StorageAccount = Some resourceId |}
                | HasResourceType workspaces -> {| state.Sinks with LogAnalyticsWorkspace = Some (resourceId, AzureDiagnostics) |}
                | HasResourceType Namespaces.authorizationRules -> {| state.Sinks with EventHub = Some {| AuthorizationRuleId = resourceId; EventHubName = None |} |}
                | _ -> failwithf "Unsupported resource type '%O'. Supported types are %O" resourceId.Type [ storageAccounts; workspaces ]
            Dependencies = state.Dependencies.Add dependency }

    /// Adds a destination sink (either a storage account, log analytics workspace or event hub authorization rule)
    [<CustomOperation "add_destination">]
    member _.AddDestination(state:DiagnosticSettingsConfig, resourceId:ResourceId) =
        DiagnosticSettingsBuilder.AddDestinationPrivate (state, resourceId )
    member _.AddDestination(state:DiagnosticSettingsConfig, storageAccount:StorageAccountConfig) =
        DiagnosticSettingsBuilder.AddDestinationPrivate (state, storageAccounts.resourceId storageAccount.Name.ResourceName)
    member _.AddDestination(state, workspace:WorkspaceConfig) =
        DiagnosticSettingsBuilder.AddDestinationPrivate (state, (workspace :> IBuilder).ResourceId)
    member _.AddDestination(state:DiagnosticSettingsConfig, hub:EventHubConfig) : DiagnosticSettingsConfig =
        let ruleId = Namespaces.authorizationRules.resourceId(hub.EventHubNamespaceName, ResourceName "RootManageSharedAccessKey")
        DiagnosticSettingsBuilder.AddDestinationPrivate (state, ruleId, (hub :> IBuilder).ResourceId)

    /// The name of the event hub. If none is specified, the default event hub will be selected.
    [<CustomOperation "event_hub_destination_name">]
    member _.EventHubName(state: DiagnosticSettingsConfig, eventHubName) =
        { state with
            Sinks =
                {| state.Sinks with
                    EventHub =
                        match state.Sinks.EventHub with
                        | Some hub -> Some {| hub with EventHubName = Some eventHubName |}
                        | None -> failwith "You must set the Authorization Rule Id before setting the event hub name" |}
        }
    member this.EventHubName(state, eventHubName:string) =
        this.EventHubName(state, ResourceName eventHubName)

    /// Enable or disable dedicated log analytics category output.
    [<CustomOperation "loganalytics_output_type">]
    member _.DedicatedLogAnalyticsDestination(state: DiagnosticSettingsConfig, outputType) =
        match state.Sinks.LogAnalyticsWorkspace with
        | Some (resourceId, _) ->
            { state with Sinks = {| state.Sinks with LogAnalyticsWorkspace = Some (resourceId, outputType) |} }
        | None ->
            failwith "You must first specify a Log Analytics sink before enabling dedicated outputs."

    /// Add metric settings to the resource.
    [<CustomOperation "capture_metrics">]
    member _.Metrics(state: DiagnosticSettingsConfig, metrics) = { state with Metrics = Set metrics }

    /// Add Log settings to the resource.
    [<CustomOperation "capture_logs">]
    member _.Logs(state:DiagnosticSettingsConfig, logs) = { state with Logs = Set logs }

    interface IDependable<DiagnosticSettingsConfig> with member _.Add state newDeps = { state with Dependencies = state.Dependencies + newDeps }
    interface ITaggable<DiagnosticSettingsConfig> with member _.Add state tags = { state with Tags = state.Tags |> Map.merge tags }

let diagnosticSettings = DiagnosticSettingsBuilder()