[<AutoOpen>]
module Farmer.Builders.Insights

open Farmer
open System
open Farmer.Arm.Insights

type ScheduledQueryRuleConfig = {
    Name: ResourceName
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
    Dependencies: ResourceId Set
    Tags: Map<string, string>
} with

    interface IBuilder with
        member this.ResourceId = scheduledQueryRules.resourceId this.Name

        member this.BuildResources location = [
            {
                Name = this.Name
                Location = location
                Description = this.Description
                Severity = this.Severity
                Enabled = this.Enabled
                Scopes = this.Scopes
                EvaluationFrequency = this.EvaluationFrequency
                WindowSize = this.WindowSize
                MuteActionsDuration = this.MuteActionsDuration
                Criteria = this.Criteria
                AutoMitigate = this.AutoMitigate
                CheckWorkspaceAlertsStorageConfigured = this.CheckWorkspaceAlertsStorageConfigured
                Actions = this.Actions
                Tags = this.Tags
                Dependencies = this.Dependencies
            }
        ]

type ScheduledQueryRuleBuilder() =
    member _.Yield _ : ScheduledQueryRuleConfig =
        {
            Name = ResourceName.Empty
            Description = ""
            Severity = None
            Enabled = true
            Scopes = []
            EvaluationFrequency = None
            WindowSize = None
            MuteActionsDuration = None
            Criteria = []
            AutoMitigate = None
            CheckWorkspaceAlertsStorageConfigured = None
            Actions = { ActionGroups = [] }
            Dependencies = Set.empty
            Tags = Map.empty
        }

    [<CustomOperation "name">]
    member _.Name(state: ScheduledQueryRuleConfig, name) = { state with Name = ResourceName name }

    [<CustomOperation "description">]
    member _.Description(state: ScheduledQueryRuleConfig, description) = { state with Description = description }

    [<CustomOperation "severity">]
    member _.Severity(state: ScheduledQueryRuleConfig, severity) = { state with Severity = Some severity }

    [<CustomOperation "enabled">]
    member _.Enabled(state: ScheduledQueryRuleConfig, enabled) = { state with Enabled = enabled }

    [<CustomOperation "scopes">]
    member _.Scopes(state: ScheduledQueryRuleConfig, scopes) = { state with Scopes = scopes }

    [<CustomOperation "evaluation_frequency">]
    member _.EvaluationFrequency(state: ScheduledQueryRuleConfig, frequency) =
        { state with EvaluationFrequency = Some frequency }

    member _.EvaluationFrequency(state: ScheduledQueryRuleConfig, frequency : string) =
        { state with EvaluationFrequency = Some (Xml.XmlConvert.ToTimeSpan frequency) }

    [<CustomOperation "window_size">]
    member _.WindowSize(state: ScheduledQueryRuleConfig, size) =
        { state with WindowSize = Some size }

    member _.WindowSize(state: ScheduledQueryRuleConfig, size : string) =
        { state with WindowSize = Some (Xml.XmlConvert.ToTimeSpan size) }

    [<CustomOperation "mute_actions_duration">]
    member _.MuteActionsDuration(state: ScheduledQueryRuleConfig, duration) =
        { state with MuteActionsDuration = Some duration }

    member _.MuteActionsDuration(state: ScheduledQueryRuleConfig, duration : string) =
        { state with MuteActionsDuration = Some (Xml.XmlConvert.ToTimeSpan duration) }

    [<CustomOperation "criteria">]
    member _.Criteria(state: ScheduledQueryRuleConfig, criteria: Condition list) =
        { state with Criteria = criteria }

    [<CustomOperation "auto_mitigate">]
    member _.AutoMitigate(state: ScheduledQueryRuleConfig, autoMitigate) =
        { state with AutoMitigate = Some autoMitigate }

    [<CustomOperation "check_workspace_alerts_storage_configured">]
    member _.CheckWorkspaceAlertsStorageConfigured(state: ScheduledQueryRuleConfig, check) =
        { state with CheckWorkspaceAlertsStorageConfigured = Some check }

    [<CustomOperation "actions">]
    member _.Actions(state: ScheduledQueryRuleConfig, actions) =
        { state with Actions = actions }

    interface ITaggable<ScheduledQueryRuleConfig> with
        member _.Add state tags = {
            state with
                Tags = state.Tags |> Map.merge tags
        }

    interface IDependable<ScheduledQueryRuleConfig> with
        member _.Add state newDeps = {
            state with
                Dependencies = state.Dependencies + newDeps
        }

let scheduledQueryRule = ScheduledQueryRuleBuilder()