[<AutoOpen>]
module Farmer.Arm.InsightsAlerts

open Farmer
open Farmer.Insights

let metricAlert = Farmer.ResourceType("microsoft.insights/metricAlerts", "2018-03-01")

type AlertSeverity =
/// 0
| Alert_Critical
/// 1
| Alert_Error
/// 2
| Alert_Warning
/// 3
| Alert_Informational
/// 4
| Alert_Verbose

type MetricComparison =
| GreaterThan
| LessThan

type MetricAggregation =
| Average
| Count
| Maximum
| Minimum
| Total

/// See the MetricNames and their Aggregations:
/// https://docs.microsoft.com/en-us/azure/azure-monitor/essentials/metrics-supported
type ResourceCriteria = {
    /// Resource type name
    MetricNamespace : ResourceType
    /// Name of the metric
    MetricName : MetricsName
    /// Threshold to exceed to hit the alert
    Threshold : int
    /// GreaterThan or LessThan
    Comparison : MetricComparison
    /// Average, Count, Total, Maximum, Minimum
    Aggregation : MetricAggregation
}

/// Metric criterias
/// https://docs.microsoft.com/en-us/azure/templates/microsoft.insights/metricalerts?tabs=json#metricalertcriteria
type MetricAlertCriteria =
| MultipleResourceMultipleMetricCriteria of MultiCriterias : obj list
/// If avg of metric x is going over threshold for selected windowSize time. E.g. if average of VM CPU is going over 80% for 15 minutes -> alert
| SingleResourceMultipleMetricCriteria of Criterias : ResourceCriteria list
/// If webtest is failing at the same time from x different locations
| WebtestLocationAvailabilityCriteria of AiComponentId:Farmer.ResourceId * WebTestId:Farmer.ResourceId * FailedLocationCount:int 

type AlertAction = {
    actionGroupId : string
    webHookProperties : obj
}

let createCriteria (criteria:MetricAlertCriteria) =
    match criteria with
    | MultipleResourceMultipleMetricCriteria (multicriteria:obj list) ->
        {| allOf = multicriteria 
           ``odata.type`` = "Microsoft.Azure.Monitor.MultipleResourceMultipleMetricCriteria" |} :> obj
    | SingleResourceMultipleMetricCriteria criterias ->
        {| allOf = criterias |> List.map(fun resourcecriteria ->
            {|  threshold = resourcecriteria.Threshold
                name = "Metric1"
                metricNamespace = resourcecriteria.MetricNamespace.Type
                metricName = resourcecriteria.MetricName |> (function | MetricsName n -> n)
                operator = match resourcecriteria.Comparison with
                           | GreaterThan -> "GreaterThan"
                           | LessThan -> "LessThan"
                timeAggregation =
                    match resourcecriteria.Aggregation with
                    | Average -> "Average"
                    | Count -> "Count"
                    | Maximum -> "Maximum"
                    | Minimum -> "Minimum"
                    | Total -> "Total"
                criterionType = "StaticThresholdCriterion"
            |})
           ``odata.type`` = "Microsoft.Azure.Monitor.SingleResourceMultipleMetricCriteria" |} :> obj
    | WebtestLocationAvailabilityCriteria (componentId, webTestId, failedLocationCount) ->
        {| webTestId = webTestId.Eval()
           componentId = componentId.Eval()
           failedLocationCount = failedLocationCount
           ``odata.type`` = "Microsoft.Azure.Monitor.WebtestLocationAvailabilityCriteria" |} :> obj

type AlertData =
    {
        Name : Farmer.ResourceName
        Description : string
        Severity : AlertSeverity
        Frequency : DurationInterval
        Window : DurationInterval
        Actions : List<AlertAction>
        LinkedResources : ResourceId list
        Criteria : MetricAlertCriteria }
    interface Farmer.IArmResource with
        member this.ResourceId = metricAlert.resourceId this.Name
        member this.JsonModel =
            let tags =
                this.LinkedResources
                |> List.map(fun r -> $"[concat('hidden-link:', " + r.ArmExpression.Value + ")]", "Resource")
                |> Map.ofList
            let scopes =
                this.LinkedResources
                |> List.rev
                |> List.map(fun r -> r.Eval())
            {| metricAlert.Create(this.Name) with
                   tags = tags
                   location = "global"
                   dependsOn = this.LinkedResources |> List.map(fun r -> r.Eval())
                   properties =
                    {| description = this.Description
                       severity =
                           match this.Severity with
                           | Alert_Critical -> 0
                           | Alert_Error -> 1
                           | Alert_Warning -> 2
                           | Alert_Informational -> 3
                           | Alert_Verbose -> 4
                       enabled = true
                       scopes = scopes
                       evaluationFrequency = this.Frequency |> (function | ISO8601DurationFormat x -> x)
                       windowSize = this.Window|> (function | ISO8601DurationFormat x -> x) 
                       criteria = this.Criteria |> createCriteria
                       autoMitigate = true
                       targetResourceType =
                           match this.LinkedResources with
                           | [r] -> r.Type.Type
                           | _ -> null
                       actions = this.Actions
                   |}
            |} :> _