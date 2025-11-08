[<AutoOpen>]
module Farmer.Builders.Policy

open Farmer
open Farmer.Arm.Policy

type PolicyDefinitionConfig = {
    Name: ResourceName
    DisplayName: string option
    Description: string option
    Mode: PolicyMode
    PolicyRule: string
    Parameters: Map<string, obj> option
    Metadata: Map<string, string> option
} with

    interface IBuilder with
        member this.ResourceId = policyDefinitions.resourceId this.Name

        member this.BuildResources _ = [
            {
                PolicyDefinition.Name = this.Name
                DisplayName = this.DisplayName
                Description = this.Description
                Mode = this.Mode
                PolicyRule = this.PolicyRule
                Parameters = this.Parameters
                Metadata = this.Metadata
            }
        ]

type PolicyDefinitionBuilder() =
    member _.Yield _ = {
        Name = ResourceName.Empty
        DisplayName = None
        Description = None
        Mode = PolicyMode.All
        PolicyRule = ""
        Parameters = None
        Metadata = None
    }

    /// Sets the name of the policy definition.
    [<CustomOperation "name">]
    member _.Name(state: PolicyDefinitionConfig, name: string) = { state with Name = ResourceName name }

    /// Sets the display name of the policy definition.
    [<CustomOperation "display_name">]
    member _.DisplayName(state: PolicyDefinitionConfig, displayName: string) = {
        state with
            DisplayName = Some displayName
    }

    /// Sets the description of the policy definition.
    [<CustomOperation "description">]
    member _.Description(state: PolicyDefinitionConfig, description: string) = {
        state with
            Description = Some description
    }

    /// Sets the mode of the policy definition (All or Indexed). Default is All.
    [<CustomOperation "mode">]
    member _.Mode(state: PolicyDefinitionConfig, mode: PolicyMode) = { state with Mode = mode }

    /// Sets the policy rule as a JSON string.
    [<CustomOperation "policy_rule">]
    member _.PolicyRule(state: PolicyDefinitionConfig, rule: string) = { state with PolicyRule = rule }

    /// Sets the parameters for the policy definition.
    [<CustomOperation "parameters">]
    member _.Parameters(state: PolicyDefinitionConfig, parameters: Map<string, obj>) = {
        state with
            Parameters = Some parameters
    }

    /// Sets metadata for the policy definition (category, version, etc.).
    [<CustomOperation "add_metadata">]
    member _.AddMetadata(state: PolicyDefinitionConfig, metadata: Map<string, string>) = {
        state with
            Metadata = Some metadata
    }

    /// Adds a single metadata field to the policy definition.
    [<CustomOperation "add_metadata_field">]
    member _.AddMetadataField(state: PolicyDefinitionConfig, key: string, value: string) = {
        state with
            Metadata =
                match state.Metadata with
                | Some existing -> Some(existing.Add(key, value))
                | None -> Some(Map.ofList [ key, value ])
    }

type PolicyAssignmentConfig = {
    Name: ResourceName
    DisplayName: string option
    Description: string option
    PolicyDefinition: PolicyDefinitionConfig option
    PolicyDefinitionId: ResourceId option
    EnforcementMode: EnforcementMode
    Parameters: Map<string, obj> option
    Scope: ResourceId option
    NotScopes: string list
    Location: Location option
    Identity: Identity.ManagedIdentity option
    Dependencies: ResourceId Set
} with

    interface IBuilder with
        member this.ResourceId = policyAssignments.resourceId this.Name

        member this.BuildResources location =
            let policyDefId =
                match this.PolicyDefinition, this.PolicyDefinitionId with
                | Some def, _ -> (def :> IBuilder).ResourceId
                | None, Some id -> id
                | None, None -> raiseFarmer "Policy assignment must reference a policy definition"

            [
                {
                    PolicyAssignment.Name = this.Name
                    DisplayName = this.DisplayName
                    Description = this.Description
                    PolicyDefinitionId = policyDefId
                    EnforcementMode = this.EnforcementMode
                    Parameters = this.Parameters
                    Scope = this.Scope
                    NotScopes = this.NotScopes
                    Location = this.Location |> Option.orElse (Some location)
                    Identity = this.Identity
                    Dependencies = this.Dependencies
                }
            ]

type PolicyAssignmentBuilder() =
    member _.Yield _ = {
        Name = ResourceName.Empty
        DisplayName = None
        Description = None
        PolicyDefinition = None
        PolicyDefinitionId = None
        EnforcementMode = EnforcementMode.Default
        Parameters = None
        Scope = None
        NotScopes = []
        Location = None
        Identity = None
        Dependencies = Set.empty
    }

    /// Sets the name of the policy assignment.
    [<CustomOperation "name">]
    member _.Name(state: PolicyAssignmentConfig, name: string) = { state with Name = ResourceName name }

    /// Sets the display name of the policy assignment.
    [<CustomOperation "display_name">]
    member _.DisplayName(state: PolicyAssignmentConfig, displayName: string) = {
        state with
            DisplayName = Some displayName
    }

    /// Sets the description of the policy assignment.
    [<CustomOperation "description">]
    member _.Description(state: PolicyAssignmentConfig, description: string) = {
        state with
            Description = Some description
    }

    /// Links to a policy definition config built in this deployment.
    [<CustomOperation "link_to_policy">]
    member _.LinkToPolicy(state: PolicyAssignmentConfig, policy: PolicyDefinitionConfig) = {
        state with
            PolicyDefinition = Some policy
    }

    /// Links to an existing policy definition by resource ID.
    [<CustomOperation "link_to_policy_id">]
    member _.LinkToPolicyId(state: PolicyAssignmentConfig, policyId: ResourceId) = {
        state with
            PolicyDefinitionId = Some policyId
    }

    /// Sets the enforcement mode (Default or DoNotEnforce).
    [<CustomOperation "enforcement_mode">]
    member _.EnforcementMode(state: PolicyAssignmentConfig, mode: EnforcementMode) = {
        state with
            EnforcementMode = mode
    }

    /// Sets the parameters for the policy assignment.
    [<CustomOperation "parameters">]
    member _.Parameters(state: PolicyAssignmentConfig, parameters: Map<string, obj>) = {
        state with
            Parameters = Some parameters
    }

    /// Sets the scope for the policy assignment.
    [<CustomOperation "scope">]
    member _.Scope(state: PolicyAssignmentConfig, scope: ResourceId) = { state with Scope = Some scope }

    /// Adds resource scopes to exclude from this policy assignment.
    [<CustomOperation "not_scopes">]
    member _.NotScopes(state: PolicyAssignmentConfig, notScopes: string list) = {
        state with
            NotScopes = notScopes
    }

    /// Sets the location for the policy assignment (required for policies with managed identity).
    [<CustomOperation "location">]
    member _.Location(state: PolicyAssignmentConfig, location: Location) = {
        state with
            Location = Some location
    }

    /// Assigns a system-assigned managed identity to the policy assignment (required for DeployIfNotExists and Modify effects).
    [<CustomOperation "system_identity">]
    member _.SystemIdentity(state: PolicyAssignmentConfig) = {
        state with
            Identity = Some { SystemAssigned = Enabled; UserAssigned = [] }
    }

    /// Adds a dependency to this policy assignment.
    [<CustomOperation "add_dependency">]
    member _.AddDependency(state: PolicyAssignmentConfig, dependency: ResourceId) = {
        state with
            Dependencies = state.Dependencies.Add dependency
    }

/// Builds a policy definition resource.
let policyDefinition = PolicyDefinitionBuilder()

/// Builds a policy assignment resource.
let policyAssignment = PolicyAssignmentBuilder()
