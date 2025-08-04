[<AutoOpen>]
module Farmer.Arm.AlertsManagement

open Farmer

let prometheusRuleGroups =
    ResourceType("Microsoft.AlertsManagement/prometheusRuleGroups", "2023-04-01")

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
    TimeToResolve: string
} with

    member internal this.ToArmJson = {|
        autoResolved = this.AutoResolved
        timeToResolve = this.TimeToResolve
    |}

type PrometheusRule = {
    Record: string option
    Expression: string
    Labels: Map<string, string> option
    Enabled: FeatureFlag option
    Alert: string option
    Severity: int option
    Annotation: Map<string, string> option
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
        Annotation = None
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
        enabled =
            rule.Enabled
            |> Option.map (fun enabled -> enabled.AsBoolean)
            |> Option.defaultValue Unchecked.defaultof<_>
        severity = rule.Severity |> Option.defaultValue Unchecked.defaultof<_>
        resolveConfiguration =
            rule.ResolveConfiguration
            |> Option.map (fun config -> config.ToArmJson)
            |> Option.defaultValue Unchecked.defaultof<_>
    |}

type PrometheusRuleGroup = {
    Name: ResourceName
    Location: Location
    Description: string option
    ClusterName: ResourceName
    Tags: Map<string, string>
    Enabled: FeatureFlag option
    Interval: string option
    Rules: PrometheusRule list
    Scopes: ResourceId Set
} with

    interface IArmResource with
        member this.ResourceId = prometheusRuleGroups.resourceId this.Name

        member this.JsonModel = {|
            prometheusRuleGroups.Create(this.Name, this.Location, tags = this.Tags) with
                properties = {|
                    clusterName = this.ClusterName.Value
                    description = this.Description |> Option.defaultValue Unchecked.defaultof<_>
                    enabled =
                        this.Enabled
                        |> Option.map (fun enabled -> enabled.AsBoolean)
                        |> Option.defaultValue Unchecked.defaultof<_>
                    interval = this.Interval |> Option.defaultValue Unchecked.defaultof<_>
                    scopes = this.Scopes |> Set.map (fun s -> s.Eval())
                    rules = this.Rules |> List.map (fun r -> PrometheusRule.ToArmJson)
                |}
        |}