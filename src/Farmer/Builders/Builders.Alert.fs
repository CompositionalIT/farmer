[<AutoOpen>]
module Farmer.Builders.Alert

open Farmer
open Farmer.Arm.InsightsAlerts
open System

type AlertConfig =
    {
        Name: Farmer.ResourceName
        Description: string
        Severity: AlertSeverity
        Frequency: IsoDateTime
        Window: IsoDateTime
        Actions: List<AlertAction>
        LinkedResources: LinkedResource list
        Criteria: MetricAlertCriteria
    }

    interface IBuilder with
        member this.ResourceId = metricAlert.resourceId this.Name

        member this.BuildResources _ =
            let a: AlertData =
                {
                    Name = this.Name
                    Description = this.Description
                    Severity = this.Severity
                    Frequency = this.Frequency
                    Window = this.Window
                    Actions = this.Actions
                    LinkedResources = this.LinkedResources |> List.map (fun r -> r.ResourceId)
                    Criteria = this.Criteria
                }

            [ a ]

type AlertBuilder() =
    member __.Yield _ =
        {
            Name = ResourceName.Empty
            Description = String.Empty
            Severity = AlertSeverity.Error
            Frequency = System.TimeSpan(0, 5, 0) |> IsoDateTime.OfTimeSpan
            Window = System.TimeSpan(0, 15, 0) |> IsoDateTime.OfTimeSpan
            Actions = List.empty
            LinkedResources = List.empty
            Criteria = SingleResourceMultipleMetricCriteria List.empty
        }

    /// Sets the name of the alert.
    [<CustomOperation "name">]
    member __.Name(state: AlertConfig, name) = { state with Name = ResourceName name }

    /// Sets the description of the alert.
    [<CustomOperation "description">]
    member __.Description(state: AlertConfig, description) =
        { state with Description = description }

    /// How often the metric alert is evaluated
    [<CustomOperation "frequency">]
    member __.Frequency(state: AlertConfig, frequency) = { state with Frequency = frequency }

    /// The period of time that is used to monitor alert activity based on the threshold.
    [<CustomOperation "window">]
    member __.Window(state: AlertConfig, window) = { state with Window = window }

    /// Alert severity
    [<CustomOperation "severity">]
    member __.Severity(state: AlertConfig, severity) = { state with Severity = severity }

    /// Add the target resources on which the alert is created/updated.
    [<CustomOperation "add_linked_resources">]
    member __.LinkedResources(state: AlertConfig, linked_resources) =
        { state with
            LinkedResources = linked_resources @ state.LinkedResources
        }

    member this.LinkedResources(state: AlertConfig, resourceIds: ResourceId list) =
        this.LinkedResources(state, resourceIds |> List.map Managed)

    member this.LinkedResources(state: AlertConfig, builders: IBuilder list) =
        this.LinkedResources(state, builders |> List.map (fun builder -> builder.ResourceId |> Managed))

    /// Add target resource on which the alert is created/updated.
    [<CustomOperation "add_linked_resource">]
    member this.LinkedResource(state: AlertConfig, linked_resource) =
        { state with
            LinkedResources = linked_resource :: state.LinkedResources
        }

    member this.LinkedResource(state: AlertConfig, resourceId: ResourceId) =
        this.LinkedResource(state, resourceId |> Managed)

    member this.LinkedResource(state: AlertConfig, builder: IBuilder) =
        this.LinkedResource(state, builder.ResourceId |> Managed)

    /// The rule criteria that defines the conditions of the alert rule.
    [<CustomOperation "single_resource_multiple_metric_criteria">]
    member __.SingleCriteria(state: AlertConfig, criteria) =
        { state with
            Criteria = SingleResourceMultipleMetricCriteria criteria
        }

    /// The rule criteria that defines the conditions of the alert rule based on Application Insights custom metric.
    [<CustomOperation "single_resource_multiple_custom_metric_criteria">]
    member __.SingleCustomMetricCriteria(state: AlertConfig, criteria) =
        { state with
            Criteria = SingleResourceMultipleCustomMetricCriteria criteria
        }

    /// The rule criterias that defines the conditions of the alert rule.
    [<CustomOperation "multiple_resource_multiple_metric_criteria">]
    member __.MultiCriteria(state: AlertConfig, criteria) =
        { state with
            Criteria = MultipleResourceMultipleMetricCriteria criteria
        }

    /// The rule criteria that defines the conditions of the alert rule.
    /// AppInsightsId * WebTestId * FailedLocationCount
    /// If webtest is failing at the same time from x different locations
    [<CustomOperation "webtest_location_availability_criteria">]
    member __.WebCriteria(state: AlertConfig, criteria) =
        { state with
            Criteria = WebtestLocationAvailabilityCriteria criteria
        }

    /// Add an action that are performed when the alert rule becomes active.
    [<CustomOperation "add_action">]
    member this.Actions(state: AlertConfig, actionGroupId, ?webHookProperties: obj) =
        { state with
            Actions =
                {
                    actionGroupId = actionGroupId
                    webHookProperties = webHookProperties
                }
                :: state.Actions
        }

    member this.Actions(state: AlertConfig, ag: ActionGroupConfig, ?webHookProperties: obj) =
        this.Actions(state, ag.ActionGroupId, webHookProperties)


let alert = AlertBuilder()
