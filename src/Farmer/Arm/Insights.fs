[<AutoOpen>]
module Farmer.Arm.Insights

open Farmer
open System

let private createComponents version =
    ResourceType("Microsoft.Insights/components", version)

let scheduledQueryRules =
    ResourceType("Microsoft.Insights/scheduledQueryRules", "2021-08-01")

/// Classic AI instance
let components = createComponents "2014-04-01"
/// Workspace-enabled AI instance
let componentsWorkspace = createComponents "2020-02-02"

/// The type of AI instance to create.
type InstanceKind =
    | Classic
    | Workspace of workspace: ResourceId

    member this.ResourceType =
        match this with
        | Classic -> components
        | Workspace _ -> componentsWorkspace

type Components = {
    Name: ResourceName
    Location: Location
    LinkedWebsite: ResourceName option
    DisableIpMasking: bool
    SamplingPercentage: int
    InstanceKind: InstanceKind
    Tags: Map<string, string>
    Dependencies: ResourceId Set
} with

    interface IArmResource with
        member this.ResourceId = components.resourceId this.Name

        member this.JsonModel =
            let tags =
                match this.LinkedWebsite with
                | Some linkedWebsite ->
                    this.Tags.Add(
                        $"[concat('hidden-link:', resourceGroup().id, '/providers/Microsoft.Web/sites/', '{linkedWebsite.Value}')]",
                        "Resource"
                    )
                | None -> this.Tags

            {|
                this.InstanceKind.ResourceType.Create(this.Name, this.Location, this.Dependencies, tags) with
                    kind = "web"
                    properties = {|
                        name = this.Name.Value
                        Application_Type = "web"
                        ApplicationId =
                            match this.LinkedWebsite with
                            | Some linkedWebsite -> linkedWebsite.Value
                            | None -> null
                        DisableIpMasking = this.DisableIpMasking
                        SamplingPercentage = this.SamplingPercentage
                        IngestionMode =
                            match this.InstanceKind with
                            | Workspace _ -> "LogAnalytics"
                            | Classic -> null
                        WorkspaceResourceId =
                            match this.InstanceKind with
                            | Workspace resourceId -> resourceId.Eval()
                            | Classic -> null
                    |}
            |}

type SeverityLevel =
    | Zero
    | One
    | Two
    | Three
    | Four

type DimensionOperator =
    | Include
    | Exclude

type Dimension = {
    Name: string
    Operator: DimensionOperator
    Values: string list option
} with

    member this.JsonModel = {|
        name = this.Name
        operator =
            match this.Operator with
            | Include -> "Include"
            | Exclude -> "Exclude"
        values = this.Values |> Option.defaultValue List.empty
    |}

type ConditionFailingPeriods = {
    MinFailingPeriodsToAlert: int
    NumberOfEvaluationPeriods: int
} with

    member this.JsonModel =
        if this.MinFailingPeriodsToAlert > this.NumberOfEvaluationPeriods then
            failwith "MinFailingPeriodsToAlert cannot be greater than NumberOfEvaluationPeriods."

        {|
            minFailingPeriodsToAlert = this.MinFailingPeriodsToAlert
            numberOfEvaluationPeriods = this.NumberOfEvaluationPeriods
        |}

type ConditionOperator =
    | Equals
    | GreaterThan
    | GreaterThanOrEqual
    | LessThan
    | LessThanOrEqual

type TimeAggregation =
    | Average
    | Count
    | Maximum
    | Minimum
    | Total

type Condition = {
    Query: string
    MetricMeasureColumn: string option
    ResourceIdColumn: string option
    Dimensions: Dimension list option
    Operator: ConditionOperator option
    Threshold: int option
    TimeAggregation: TimeAggregation option
    FailingPeriods: ConditionFailingPeriods option
} with

    member this.JsonModel = {|
        query = this.Query
        metricMeasureColumn = this.MetricMeasureColumn |> Option.toObj
        resourceIdColumn = this.ResourceIdColumn |> Option.toObj
        dimensions =
            this.Dimensions
            |> Option.map (List.map (fun d -> d.JsonModel))
            |> Option.defaultValue List.empty
        operator =
            this.Operator
            |> Option.map (function
                | Equals -> "Equals"
                | GreaterThan -> "GreaterThan"
                | GreaterThanOrEqual -> "GreaterThanOrEqual"
                | LessThan -> "LessThan"
                | LessThanOrEqual -> "LessThanOrEqual")
            |> Option.toObj
        threshold = this.Threshold |> Option.toNullable
        timeAggregation =
            this.TimeAggregation
            |> Option.map (function
                | Average -> "Average"
                | Count -> "Count"
                | Maximum -> "Maximum"
                | Minimum -> "Minimum"
                | Total -> "Total")
            |> Option.toObj
        failingPeriods =
            this.FailingPeriods
            |> Option.map (fun fp -> fp.JsonModel)
            |> Option.defaultValue Unchecked.defaultof<_>
    |}

type Actions = {
    ActionGroups: string list // ActionGroupConfig.ActionGroupId is a string instead of a ResourceId for some reason
}

type ScheduledQueryRule = {
    Name: ResourceName
    Location: Location
    Description: string
    Severity: SeverityLevel option
    Enabled: bool
    Scopes: ResourceId list
    EvaluationFrequency: TimeSpan option
    WindowSize: TimeSpan option
    MuteActionsDuration: TimeSpan option
    Criteria: Condition list
    AutoMitigate: bool option
    CheckWorkspaceAlertsStorageConfigured: bool option
    Actions: Actions
    Tags: Map<string, string>
    Dependencies: ResourceId Set
} with

    interface IArmResource with
        member this.ResourceId = scheduledQueryRules.resourceId this.Name

        member this.JsonModel = {|
            scheduledQueryRules.Create(this.Name, this.Location, this.Dependencies, tags = this.Tags) with
                properties = {|
                    description = this.Description
                    severity =
                        this.Severity
                        |> Option.map (function
                            | Zero -> 0
                            | One -> 1
                            | Two -> 2
                            | Three -> 3
                            | Four -> 4)
                        |> Option.toNullable
                    enabled = this.Enabled
                    scopes = this.Scopes |> List.map (fun r -> r.Eval())
                    evaluationFrequency = this.EvaluationFrequency |> Option.map Xml.XmlConvert.ToString |> Option.toObj
                    windowSize = this.WindowSize |> Option.map Xml.XmlConvert.ToString |> Option.toObj
                    muteActionsDuration = this.MuteActionsDuration |> Option.map Xml.XmlConvert.ToString |> Option.toObj
                    criteria = {|
                        allOf = this.Criteria |> List.map (fun c -> c.JsonModel)
                    |}
                    autoMitigate = this.AutoMitigate |> Option.toNullable
                    checkWorkspaceAlertsStorageConfigured =
                        this.CheckWorkspaceAlertsStorageConfigured |> Option.toNullable
                    actions = {|
                        actionGroups = this.Actions.ActionGroups
                        customProperties = {| |}
                    |}
                |}
        |}