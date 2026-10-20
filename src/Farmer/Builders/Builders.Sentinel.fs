[<AutoOpen>]
module Farmer.Builders.Sentinel

open Farmer
open Farmer.Arm.SecurityInsights
open Farmer.Arm.LogAnalytics

type SentinelConfig = {
    WorkspaceName: ResourceName
    WorkspaceId: ResourceId option
    Dependencies: ResourceId Set
} with

    interface IBuilder with
        member this.ResourceId =
            onboardingStates.resourceId (this.WorkspaceName, ResourceName "default")

        member this.BuildResources _ =
            let dependencies =
                match this.WorkspaceId with
                | Some wsId -> this.Dependencies + Set [ wsId ]
                | None -> this.Dependencies

            [
                {
                    SentinelOnboarding.WorkspaceName = this.WorkspaceName
                    Dependencies = dependencies
                }
            ]

type SentinelBuilder() =
    member _.Yield _ = {
        WorkspaceName = ResourceName.Empty
        WorkspaceId = None
        Dependencies = Set.empty
    }

    /// Links to a Log Analytics Workspace to enable Sentinel on.
    [<CustomOperation "link_to_workspace">]
    member _.LinkToWorkspace(state: SentinelConfig, workspace: IBuilder) = {
        state with
            WorkspaceName = workspace.ResourceId.Name
            WorkspaceId = Some workspace.ResourceId
    }

    /// Sets the workspace name directly (for existing workspaces).
    [<CustomOperation "workspace_name">]
    member _.WorkspaceName(state: SentinelConfig, workspaceName: string) = {
        state with
            WorkspaceName = ResourceName workspaceName
    }

    /// Adds a dependency to Sentinel onboarding.
    [<CustomOperation "add_dependency">]
    member _.AddDependency(state: SentinelConfig, dependency: ResourceId) = {
        state with
            Dependencies = state.Dependencies.Add dependency
    }

/// Enables Azure Sentinel (SIEM) on a Log Analytics Workspace.
let sentinel = SentinelBuilder()
