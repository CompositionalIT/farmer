[<AutoOpen>]
module Farmer.Builders.DiagnosticSetting

open Farmer
open Farmer.Arm.DiagnosticSetting

type DiagnosticSettingsConfig =
    { Name : ResourceName
      StorageAccountId : ResourceId option
      ServiceBusRuleId : ResourceId option
      ParentResource: ResourceId
      EventHubAuthorizationRuleId : ResourceId option
      EventHubName : string option
      Metrics : MetricSetting list
      Logs : LogSetting list
      WorkspaceId : ResourceId option
      DedicatedLogAnalyticsDestination : string option
      Dependencies : ResourceId Set
      Tags : Map<string, string> }
    interface IBuilder with
        member this.ResourceId = diagnosticSettingsType(this.ParentResource.Type).resourceId this.Name
        member this.BuildResources location = [
            { Name = this.Name
              Location = location
              ParentResource = this.ParentResource
              StorageAccountId = this.StorageAccountId
              ServiceBusRuleId = this.ServiceBusRuleId
              EventHubAuthorizationRuleId = this.EventHubAuthorizationRuleId
              EventHubName = this.EventHubName
              Logs = this.Logs
              Metrics = this.Metrics
              WorkspaceId = this.WorkspaceId
              Dependencies = this.Dependencies
              DedicatedLogAnalyticsDestination = this.DedicatedLogAnalyticsDestination
              Tags = this.Tags }
        ]

type DiagnosticSettingsBuilder() =
    member _.Yield _ =
         { Name = ResourceName.Empty
           StorageAccountId = None
           ServiceBusRuleId = None
           EventHubAuthorizationRuleId = None
           EventHubName = None
           Metrics = []
           Logs = []
           ParentResource = ResourceId.create(ResourceType("", ""), ResourceName "")
           WorkspaceId = None
           DedicatedLogAnalyticsDestination = None
           Dependencies = Set.empty
           Tags = Map.empty }
    member _.Run(state:DiagnosticSettingsConfig) =
        match state with
        | { EventHubName = Some _; EventHubAuthorizationRuleId = None } ->
            failwith "EventHubAuthorizationRuleId is not specified."
        | { Metrics = []; Logs = [] } ->
            failwith "Specify at least one category details."
        | { EventHubAuthorizationRuleId = None; StorageAccountId = None; WorkspaceId = None } ->
            failwith "Specify at least one data sink."
        | _ -> state

    /// Sets the name of the diagnostic settings.
    [<CustomOperation "name">]
    member _.Name(state: DiagnosticSettingsConfig, resourceName:string) =
        { state with Name = ResourceName resourceName }

    ///Sets the namespace type of the parent resource.
    [<CustomOperation "parent_resource">]
    member _.ParentResourceType(state: DiagnosticSettingsConfig, parentId:ResourceId) =
       { state with ParentResource = parentId }

    /// Sets the storage Account Id.
    [<CustomOperation "storage_account_id">]
    member _.StorageAccountId(state: DiagnosticSettingsConfig, storageAccountId) =
        { state with StorageAccountId = Some storageAccountId }

    /// Sets The service bus rule Id of the diagnostic setting.
    [<CustomOperation "service_bus_rule_id">]
    member _.ServiceBusRuleId(state: DiagnosticSettingsConfig, serviceBusRuleId) =
        { state with ServiceBusRuleId = serviceBusRuleId}

    /// Sets The authorization rule Id for the event hub.
    [<CustomOperation "event_hub_authorization_rule_id">]
    member _.EventHubAuthorizationRuleId(state: DiagnosticSettingsConfig, eventHubAuthorizationRuleId) =
        { state with EventHubAuthorizationRuleId = Some eventHubAuthorizationRuleId }

    /// The name of the event hub. If none is specified, the default event hub will be selected.
    [<CustomOperation "event_hub_name">]
    member _.PublicNetworkAccessForIngestion(state: DiagnosticSettingsConfig, eventHubName) =
        { state with EventHubName = Some eventHubName }

    /// Sets the log analytics workspace id.
    [<CustomOperation "work_space_id">]
    member _.WorkspaceId(state: DiagnosticSettingsConfig,workspaceId) =
             { state with WorkspaceId = Some workspaceId}

    /// Enable dedicated log analytics."
    [<CustomOperation "enable_dedicated_loganalytics">]
    member _.DedicatedLogAnalyticsDestination(state: DiagnosticSettingsConfig) =
              { state with DedicatedLogAnalyticsDestination = Some "Dedicated" }

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