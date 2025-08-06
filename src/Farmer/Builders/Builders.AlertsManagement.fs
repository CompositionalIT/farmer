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
    Severity: AlertSeverity option
    Actions: (Action list) option
    ResolveConfiguration: ResolveConfiguration option
}

type PrometheusRuleBuilder() =
    member _.Run(config: PrometheusRuleConfig) =
        if System.String.IsNullOrWhiteSpace(config.Expression) then
            raiseFarmer "Missing Expression on Premethus Rule - please specify 'expression'"

        config

    member _.Yield _ = {
        Record = None
        Expression = System.String.Empty
        Labels = None
        Enabled = None
        Alert = None
        Severity = None
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

    [<CustomOperation "disable_rule">]
    member _.DisableRule(state: PrometheusRuleConfig) = { state with Enabled = Some Disabled }

    [<CustomOperation "alert">]
    member _.Alert(state: PrometheusRuleConfig, alert) = { state with Alert = alert }

    [<CustomOperation "severity">]
    member _.Severity(state: PrometheusRuleConfig, severity) = { state with Severity = severity }

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
    ClusterName: ResourceName option
    Tags: Map<string, string>
    Enabled: FeatureFlag option
    Interval: IsoDateTime option
    Rules: PrometheusRuleConfig list
    Scopes: ResourceId Set
    MonitorWorkspaceId: ResourceId
} with

    interface IBuilder with
        member this.ResourceId = prometheusRuleGroups.resourceId this.Name

        member this.BuildResources location = [
            {
                Name = this.Name
                Location = location
                Description = this.Description
                Tags = this.Tags
                Enabled = this.Enabled
                Interval = this.Interval
                Rules =
                    this.Rules
                    |> List.map (fun rule -> {
                        PrometheusRule.Record = rule.Record
                        Expression = rule.Expression
                        Labels = rule.Labels
                        Enabled = rule.Enabled
                        Alert = rule.Alert
                        Severity = rule.Severity
                        Actions = rule.Actions
                        ResolveConfiguration = rule.ResolveConfiguration
                    })
                Scopes = this.Scopes
                ClusterName = this.ClusterName
                MonitorWorkspaceId = this.MonitorWorkspaceId
            }
        ]

type PrometheusRuleGroupBuilder() =
    member _.Yield _ = {
        Name = ResourceName.Empty
        ClusterName = None
        Interval = None
        Rules = []
        Tags = Map.empty
        Scopes = Set.empty
        Description = None
        Enabled = None
        MonitorWorkspaceId = ResourceId.Empty
    }

    member _.Run(config: PrometheusRuleGroupConfig) =
        if config.Rules.IsEmpty then
            raiseFarmer "Missing rules on prometheus rule group - please specify at least one rule"

        if config.MonitorWorkspaceId = ResourceId.Empty then
            raiseFarmer
                "Missing Azure Monitor Workspace Id on prometheus rule group - please specify a valid Monitor Workspace Id"

        config

    [<CustomOperation "name">]
    member _.Name(state: PrometheusRuleGroupConfig, name) = { state with Name = ResourceName name }

    [<CustomOperation "description">]
    member _.Description(state: PrometheusRuleGroupConfig, desc) = { state with Description = desc }

    [<CustomOperation "cluster_name">]
    member _.ClusterName(state: PrometheusRuleGroupConfig, cluster) = { state with ClusterName = cluster }

    [<CustomOperation "interval">]
    member _.Interval(state: PrometheusRuleGroupConfig, interval) = { state with Interval = Some interval }

    [<CustomOperation "add_rules">]
    member _.AddRules(state: PrometheusRuleGroupConfig, rules) = {
        state with
            Rules = state.Rules @ rules
    }

    [<CustomOperation "enable_rule_group">]
    member _.EnableRuleGroup(state: PrometheusRuleGroupConfig) = { state with Enabled = Some Enabled }

    [<CustomOperation "disable_rule_group">]
    member _.DisableRuleGroup(state: PrometheusRuleGroupConfig) = { state with Enabled = Some Disabled }

    [<CustomOperation "azure_monitor_workspace_id">]
    member _.AzureMonitorWorkspaceId(state: PrometheusRuleGroupConfig, workspaceId) = {
        state with
            MonitorWorkspaceId = workspaceId
    }

    interface ITaggable<PrometheusRuleGroupConfig> with
        member _.Add state tags = {
            state with
                Tags = state.Tags |> Map.merge tags
        }

let prometheusRuleGroup = PrometheusRuleGroupBuilder()