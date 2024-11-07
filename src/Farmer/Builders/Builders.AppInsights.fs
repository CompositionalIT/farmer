[<AutoOpen>]
module Farmer.Builders.AppInsights

open Farmer
open Farmer.Arm.Insights
open Farmer.Arm.LogAnalytics

type AppInsights =
    static member getInstrumentationKey(resourceId: ResourceId) =
        ArmExpression
            .reference(resourceId)
            .Map(sprintf "%s.InstrumentationKey")
            .WithOwner(resourceId)

    static member getInstrumentationKey(name: ResourceName, ?resourceGroup) =
        AppInsights.getInstrumentationKey (ResourceId.create (components, name, ?group = resourceGroup))

    static member getConnectionString(resourceId: ResourceId) =
        ArmExpression
            .reference(resourceId)
            .Map(sprintf "%s.ConnectionString")
            .WithOwner(resourceId)

    static member getConnectionString(name: ResourceName, ?resourceGroup) =
        AppInsights.getConnectionString (ResourceId.create (components, name, ?group = resourceGroup))

type AppInsightsConfig = {
    Name: ResourceName
    DisableIpMasking: bool
    SamplingPercentage: int
    InstanceKind: InstanceKind
    Dependencies: ResourceId Set
    Tags: Map<string, string>
} with

    /// Gets the ARM expression path to the instrumentation key of this App Insights instance.
    member this.InstrumentationKey = AppInsights.getInstrumentationKey this.Name
    member this.ConnectionString = AppInsights.getConnectionString this.Name

    interface IBuilder with
        member this.ResourceId = components.resourceId this.Name

        member this.BuildResources location = [
            {
                Name = this.Name
                Location = location
                LinkedWebsite = None
                DisableIpMasking = this.DisableIpMasking
                SamplingPercentage = this.SamplingPercentage
                Dependencies = this.Dependencies
                InstanceKind = this.InstanceKind
                Tags = this.Tags
            }
        ]

type AppInsightsBuilder() =
    member _.Yield _ = {
        Name = ResourceName.Empty
        DisableIpMasking = false
        SamplingPercentage = 100
        Tags = Map.empty
        Dependencies = Set.empty
        InstanceKind = Classic
    }

    [<CustomOperation "name">]
    /// Sets the name of the App Insights instance.
    member _.Name(state: AppInsightsConfig, name) = { state with Name = ResourceName name }

    [<CustomOperation "disable_ip_masking">]
    /// Sets the name of the App Insights instance.
    member _.DisableIpMasking(state: AppInsightsConfig) = { state with DisableIpMasking = true }

    [<CustomOperation "sampling_percentage">]
    /// Sets the name of the App Insights instance.
    member _.SamplingPercentage(state: AppInsightsConfig, samplingPercentage) = {
        state with
            SamplingPercentage = samplingPercentage
    }

    /// Links this AI instance to a Log Analytics workspace, using the newer 2020-02-02-preview App Insights version.
    [<CustomOperation "log_analytics_workspace">]
    member _.Workspace(state: AppInsightsConfig, workspace: ResourceId) = {
        state with
            InstanceKind = Workspace workspace
            Dependencies = state.Dependencies.Add workspace
    }

    member this.Workspace(state: AppInsightsConfig, workspace: WorkspaceConfig) =
        this.Workspace(state, workspaces.resourceId workspace.Name)

    member _.Run(state: AppInsightsConfig) =
        if state.SamplingPercentage > 100 then
            raiseFarmer "Sampling Percentage cannot be higher than 100%"
        elif state.SamplingPercentage <= 0 then
            raiseFarmer "Sampling Percentage cannot be lower than or equal to 0%"

        state

    interface ITaggable<AppInsightsConfig> with
        member _.Add state tags = {
            state with
                Tags = state.Tags |> Map.merge tags
        }

    interface IDependable<AppInsightsConfig> with
        member _.Add state resources = {
            state with
                Dependencies = state.Dependencies + resources
        }

let appInsights = AppInsightsBuilder()