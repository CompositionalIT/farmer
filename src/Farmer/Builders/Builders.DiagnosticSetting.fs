[<AutoOpen>]
module  Farmer.Builders.DiagnosticSetting
open Farmer
open Farmer.Arm

let private (|InBounds|OutOfBounds|) days =
    if days > 365<Days> then OutOfBounds days
    elif days < 1<Days> then OutOfBounds days
    else InBounds days

type diagnosticSettingsConfig=
    { Name : ResourceName
      StorageAccountId : ResourceId option 
      ServiceBusRuleId : ResourceId option 
      ParentResourceType: string
      EventHubAuthorizationRuleId : ResourceId option
      EventHubName : string option
      Dependencies : ResourceId list
      Metrics : List<MetricSettings> 
      Logs : List<LogSettings>
      WorkspaceId : ResourceId option
      DedicatedLogAnalyticsDestination : string option
      Tags : Map<string, string>
    } 
    interface IBuilder with
        member this.ResourceId =  diagnosticSettingsType(this.ParentResourceType).resourceId this.Name
        member this.BuildResources location = [
                { Name = this.Name
                  Location = location
                  ParentResourceType = this.ParentResourceType
                  StorageAccountId = this.StorageAccountId
                  ServiceBusRuleId = this.ServiceBusRuleId
                  EventHubAuthorizationRuleId = this.EventHubAuthorizationRuleId
                  EventHubName = this.EventHubName
                  Logs = [|
                      for i in this.Logs do
                          { Category = i.Category
                            Enabled = i.Enabled
                            RetentionPolicy = 
                              i.RetentionPolicy 
                              |> Option.map( fun x ->  
                                { Enabled = x.Enabled 
                                  Retention_period = x.Retention_period  }) 
                          }
                    |]
                  Metrics = [|
                      for i in this.Metrics do
                          { Category = i.Category
                            Enabled = i.Enabled
                            TimeGrain = i.TimeGrain
                            RetentionPolicy = 
                              i.RetentionPolicy 
                              |> Option.map( fun x ->  
                                { Enabled = x.Enabled 
                                  Retention_period = x.Retention_period  }) 
                          }
                    |]
                  WorkspaceId = this.WorkspaceId
                  Dependencies = this.Dependencies
                  DedicatedLogAnalyticsDestination = this.DedicatedLogAnalyticsDestination
                  Tags = this.Tags }
        ]
type RenentionPolicyBuilder () =
    member _.Yield _ =
        { Enabled = false
          Retention_period = 0<Days>
        }
    member _.Run(state:RetentionPolicy)=
            match state.Retention_period with
            | OutOfBounds days -> 
                failwithf "The retention period must be between 1 and 365 days. It is currently %d" days
            | InBounds _ -> 
                {state with Enabled = true}

    [<CustomOperation "retention_period">]
    member _.Days(state:RetentionPolicy, days) = { state with Retention_period = days }

let retentionPolicy = RenentionPolicyBuilder()

type MetricBuilder () = 
    member _.Yield _ =
        { Category = ""
          Enabled = true
          TimeGrain = None
          RetentionPolicy = 
             Some
                { Enabled = false
                  Retention_period = 0<Days>  
                } 
         }

    /// Sets the Diagnostic Metric category for a resource type.
    [<CustomOperation "category">]
    member _.Category(state:MetricSettings, category) = { state with Category=category }
    
    /// Sets the timegrain of the metric in ISO8601 format.
    [<CustomOperation "time_grain">]
    member _.TimeGrain(state:MetricSettings, timeGrain) = { state with TimeGrain = Some timeGrain }

    ///  The retention in days metric setting. Must be between 1 and 365 days. 0 is selected by default.                         
    [<CustomOperation "retention_period">]
    member _.Retention_period(state: MetricSettings, retentionPeriod: int<Days>) = 
        { state with 
            RetentionPolicy = Some (retentionPolicy { retention_period retentionPeriod }) }

type LogBuilder () = 
    member _.Yield _ =
        { Category = ""
          Enabled = true
          RetentionPolicy =
            Some
                { Enabled = false
                  Retention_period = 0<Days>  
                } 
         }

    /// Sets the Diagnostic Log category for a resource type 
    [<CustomOperation "category">]
    member _.Category(state: LogSettings, category) = { state with Category = category }

     ///  The retention in days for log settings. Must be between 1 and 365 days. 0 is selected by default.                      
    [<CustomOperation "retention_period">]
    member _.Retention_period(state: LogSettings, retentionPeriod: int<Days>) = 
        {state with 
            RetentionPolicy = Some (retentionPolicy { retention_period retentionPeriod }) }

type DiagnosticSettingsBuilder() =
    member _.Yield _ =
         { Name = ResourceName.Empty
           StorageAccountId = None
           ServiceBusRuleId = None
           EventHubAuthorizationRuleId = None
           EventHubName = None
           Metrics = []
           Logs = []
           Dependencies=[]
           ParentResourceType = ""
           WorkspaceId = None
           DedicatedLogAnalyticsDestination = None
           Tags = Map.empty }
    member _.Run(state:diagnosticSettingsConfig) =
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
    member _.Name(state: diagnosticSettingsConfig,resourceName:string) =
        { state with Name = ResourceName ("/Microsoft.Insights/" + resourceName) }

    ///Sets the namespace type of the parent resource.
    [<CustomOperation "parent_resource">]
    member this.ParentResourceType(state: diagnosticSettingsConfig, parentId:ResourceId) = 
       { state with ParentResourceType =  parentId.Type.Type ; Name = parentId.Name / state.Name }

    /// Sets the storage Account Id.
    [<CustomOperation "storage_account_id">]
    member _.StorageAccountId(state: diagnosticSettingsConfig, storageAccountId) =
        { state with StorageAccountId = Some storageAccountId }
   
    /// Sets The service bus rule Id of the diagnostic setting.
    [<CustomOperation "service_bus_rule_id">]
    member _.ServiceBusRuleId(state: diagnosticSettingsConfig, serviceBusRuleId) = 
        { state with ServiceBusRuleId = serviceBusRuleId}

    /// Sets The authorization rule Id for the event hub.
    [<CustomOperation "event_hub_authorization_rule_id">]
    member _.EventHubAuthorizationRuleId(state: diagnosticSettingsConfig, eventHubAuthorizationRuleId) =
        { state with EventHubAuthorizationRuleId = Some eventHubAuthorizationRuleId }

    /// The name of the event hub. If none is specified, the default event hub will be selected.
    [<CustomOperation "event_hub_name">]
    member _.PublicNetworkAccessForIngestion(state: diagnosticSettingsConfig, eventHubName) =
        { state with EventHubName = Some eventHubName }

    /// Sets the log analytics workspace id.
    [<CustomOperation "work_space_id">]
    member _.WorkspaceId(state: diagnosticSettingsConfig,workspaceId) =
             { state with WorkspaceId = Some workspaceId}
 
    /// Enable dedicated log analytics."
    [<CustomOperation "enable_dedicated_loganalytics">]
    member _.DedicatedLogAnalyticsDestination(state: diagnosticSettingsConfig) =
              { state with DedicatedLogAnalyticsDestination = Some "Dedicated" }

    /// Add metric settings to the resource.
    [<CustomOperation "metrics">]
    member _.Metrics(state: diagnosticSettingsConfig, metrics) =
                { state with Metrics = List.append metrics state.Metrics }

    /// Add Log settings to the resource.
    [<CustomOperation "logs">]
    member _.logs(state:diagnosticSettingsConfig, logs) = 
        { state with Logs = List.append logs state.Logs }

    
    /// Add  a dependency for the resource.
    [<CustomOperation "depends_on">]
    member this.DependsOn(state:diagnosticSettingsConfig, builder:IBuilder) = this.DependsOn (state, builder.ResourceId)
    member this.DependsOn(state:diagnosticSettingsConfig, builders:IBuilder list) = this.DependsOn (state, builders |> List.map (fun x -> x.ResourceId))
    member this.DependsOn(state:diagnosticSettingsConfig, resource:IArmResource) = this.DependsOn (state, resource.ResourceId)
    member this.DependsOn(state:diagnosticSettingsConfig, resources:IArmResource list) = this.DependsOn (state, resources |> List.map (fun x -> x.ResourceId))
    member this.DependsOn(state:diagnosticSettingsConfig, resourceId:ResourceId) = { state with Dependencies = resourceId :: state.Dependencies }
    member this.DependsOn(state:diagnosticSettingsConfig, resourceIds:ResourceId list) = { state with Dependencies = resourceIds @ state.Dependencies }

    /// Adds a set of tags to the resource
    [<CustomOperation "add_tags">]
        member _.Tags(state:diagnosticSettingsConfig, pairs) =
            { state with
                Tags = pairs |> List.fold (fun map (key, value) -> Map.add key value map) state.Tags }

    /// Adds a tag to the resource
    [<CustomOperation "add_tag">]
        member this.Tag(state:diagnosticSettingsConfig, key, value) = this.Tags(state, [ key, value ])

let diagnosticSettings = DiagnosticSettingsBuilder()
let metric = MetricBuilder()
let log = LogBuilder()

