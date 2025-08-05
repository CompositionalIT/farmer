[<AutoOpen>]
module Farmer.Builders.AlertsManagement

open Farmer
open Farmer.Arm

type PrometheusRuleConfig = {
    Record: string option
    Expression: string
    Labels: Map<string, string> option
    Enabled: FeatureFlag option
    Alert: string option
    Severity: int option
    Annotation: Map<string, string> option
    Actions: (Action list) option
    ResolveConfiguration: ResolveConfiguration option
}

type PrometheusRuleBuilder() =
    member _.Yield _ = {
        Record = None
        Expression = ""
        Labels = None
        Enabled = None
        Alert = None
        Severity = None
        Annotation = None
        Actions = None
        ResolveConfiguration = None
    }

    [<CustomOperation "record">]
    member _.Record(state: PrometheusRuleConfig, record) = { state with Record = record }

    [<CustomOperation "expression">]
    member _.Expression(state: PrometheusRuleConfig, expression) = { state with Expression = expression }

    [<CustomOperation "labels">]
    member _.Labels(state: PrometheusRuleConfig, labels) = { state with Labels = labels }

    [<CustomOperation "enable_rule">]
    member _.EnableRule(state: PrometheusRuleConfig) = { state with Enabled = Some Enabled }

    [<CustomOperation "alert">]
    member _.Alert(state: PrometheusRuleConfig, alert) = { state with Alert = alert }

    [<CustomOperation "severity">]
    member _.Severity(state: PrometheusRuleConfig, severity) = { state with Severity = severity }

    [<CustomOperation "annotation">]
    member _.Annotation(state: PrometheusRuleConfig, annotation) = { state with Annotation = annotation }

    [<CustomOperation "actions">]
    member _.Actions(state: PrometheusRuleConfig, actions) = { state with Actions = actions }

    [<CustomOperation "resolve_configuration">]
    member _.ResolveConfiguration(state: PrometheusRuleConfig, resolveConfiguration) = {
        state with
            ResolveConfiguration = resolveConfiguration
    }

let prometheusRule = PrometheusRuleBuilder()

type PrometheusRuleGroupConfig = {
    Name: ResourceName
    Description: string option
    ClusterName: ResourceName
    Tags: Map<string, string>
    Enabled: FeatureFlag option
    Interval: string option
    Rules: PrometheusRule list
    Scopes: ResourceId Set
} with

    interface IBuilder with
        member this.ResourceId = prometheusRuleGroups.resourceId this.Name

        member this.BuildResources location = [
            {
                Name = this.Name
                Location = location
                Description = this.Description
                ClusterName = this.ClusterName
                Tags = this.Tags
                Enabled = this.Enabled
                Interval = this.Interval
                Rules = this.Rules
                Scopes = this.Scopes
            }
        ]

type PrometheusRuleGroupBuilder() =
    member _.Yield _ = {
        Name = ResourceName.Empty
        ClusterName = ResourceName.Empty
        Interval = None
        Rules = []
        Tags = Map.empty
        Scopes = Set.empty
        Description = None
        Enabled = None
    }

    [<CustomOperation "name">]
    member _.Name(state: PrometheusRuleGroupConfig, name) = { state with Name = ResourceName name }

    [<CustomOperation "description">]
    member _.Description(state: PrometheusRuleGroupConfig, desc) = { state with Description = Some desc }

    [<CustomOperation "cluster_name">]
    member _.ClusterName(state: PrometheusRuleGroupConfig, cluster) = { state with ClusterName = cluster }

    [<CustomOperation "interval">]
    member _.Interval(state: PrometheusRuleGroupConfig, interval) = { state with Interval = interval }

    [<CustomOperation "add_rules">]
    member _.AddRules(state: PrometheusRuleGroupConfig, rules) = {
        state with
            Rules = state.Rules @ rules
    }

    [<CustomOperation "enable_rule_group">]
    member _.EnableRuleGroup(state: PrometheusRuleGroupConfig) = { state with Enabled = Some Enabled }

    interface ITaggable<PrometheusRuleGroupConfig> with
        member _.Add state tags = {
            state with
                Tags = state.Tags |> Map.merge tags
        }

let prometheusRuleGroup = PrometheusRuleGroupBuilder()