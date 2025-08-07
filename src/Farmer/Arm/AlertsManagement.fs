[<AutoOpen>]
module Farmer.Arm.AlertsManagement

open Farmer

let prometheusRuleGroups =
    ResourceType("Microsoft.AlertsManagement/prometheusRuleGroups", "2023-03-01")

type Action = {
    ActionGroupId: ResourceId
    ActionProperties: Map<string, string> option
} with

    member internal this.ToArmJson = {|
        actionGroupId = this.ActionGroupId.Eval()
        actionProperties = this.ActionProperties |> Option.defaultValue Unchecked.defaultof<_>
    |}


type ResolveConfiguration = {
    AutoResolved: bool
    TimeToResolve: IsoDateTime
} with

    member internal this.ToArmJson = {|
        autoResolved = this.AutoResolved
        timeToResolve =
            this.TimeToResolve
            |> (function
            | IsoDateTime x -> x)
    |}

type PrometheusRule = {
    Record: string option
    Expression: string
    Labels: Map<string, string> option
    Enabled: FeatureFlag option
    Alert: string option
    Severity: AlertSeverity option
    Actions: (Action list) option
    ResolveConfiguration: ResolveConfiguration option
} with

    static member Default = {
        Record = None
        Expression = ""
        Labels = None
        Enabled = None
        Alert = None
        Severity = None
        Actions = None
        ResolveConfiguration = None
    }

    static member ToArmJson(rule: PrometheusRule) = {|
        actions =
            rule.Actions
            |> Option.map (fun actions -> actions |> List.map (fun action -> action.ToArmJson))
            |> Option.defaultValue Unchecked.defaultof<_>
        record = rule.Record |> Option.defaultValue Unchecked.defaultof<_>
        expression = rule.Expression
        labels =
            rule.Labels
            |> Option.map (Map.toList >> dict)
            |> Option.defaultValue Unchecked.defaultof<_>
        alert = rule.Alert |> Option.defaultValue Unchecked.defaultof<_>
        enabled = rule.Enabled |> Option.map (fun e -> e.AsBoolean)
        severity =
            rule.Severity
            |> Option.map (fun severity ->
                match severity with
                | AlertSeverity.Critical -> 0
                | AlertSeverity.Error -> 1
                | AlertSeverity.Warning -> 2
                | AlertSeverity.Informational -> 3
                | AlertSeverity.Verbose -> 4)
        resolveConfiguration =
            rule.ResolveConfiguration
            |> Option.map (fun config -> config.ToArmJson)
            |> Option.defaultValue Unchecked.defaultof<_>
    |}

type PrometheusRuleGroup = {
    Name: ResourceName
    Location: Location
    Description: string option
    ClusterName: ResourceName option
    Tags: Map<string, string>
    Enabled: FeatureFlag option
    Interval: IsoDateTime option
    MonitorWorkspaceId: ResourceId
    Rules: PrometheusRule list
    /// This api version currently limits creation to one scope in addition to the Monitor Workspace.
    Scopes: ResourceId Set
} with

    interface IArmResource with
        member this.ResourceId = prometheusRuleGroups.resourceId this.Name

        member this.JsonModel =
            let scopes = (Set [ this.MonitorWorkspaceId ]) |> Set.union this.Scopes

            {|
                prometheusRuleGroups.Create(this.Name, this.Location, tags = this.Tags) with
                    properties = {|
                        clusterName =
                            this.ClusterName
                            |> Option.map (fun name -> name.Value)
                            |> Option.defaultValue Unchecked.defaultof<_>
                        description = this.Description
                        enabled = this.Enabled |> Option.map (fun e -> e.AsBoolean)
                        interval =
                            this.Interval
                            |> Option.map (fun interval ->
                                match interval with
                                | IsoDateTime x -> x)
                        scopes = scopes |> Set.map (fun scope -> scope.Eval())
                        rules = this.Rules |> List.map (fun r -> r |> PrometheusRule.ToArmJson)
                    |}
            |}