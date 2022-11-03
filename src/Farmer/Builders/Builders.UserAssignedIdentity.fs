[<AutoOpen>]
module Farmer.Builders.UserAssignedIdentity

open Farmer
open Farmer.Identity
open Farmer.Arm.ManagedIdentity

type UserAssignedIdentityConfig =
    {
        Name: ResourceName
        Tags: Map<string, string>
        ActiveDirectoryGroups: string Set
    }

    interface IBuilder with
        member this.ResourceId = this.ResourceId

        member this.BuildResources location =
            [
                {
                    UserAssignedIdentity.Name = this.Name
                    Location = location
                    Tags = this.Tags
                    ActiveDirectoryGroups = this.ActiveDirectoryGroups
                }
            ]

    member this.ResourceId = userAssignedIdentities.resourceId this.Name
    member this.UserAssignedIdentity = UserAssignedIdentity this.ResourceId
    member this.ClientId = this.UserAssignedIdentity.ClientId
    member this.PrincipalId = this.UserAssignedIdentity.PrincipalId

type UserAssignedIdentityBuilder() =
    member _.Yield _ =
        {
            Name = ResourceName.Empty
            Tags = Map.empty
            ActiveDirectoryGroups = Set.empty
        }

    /// Sets the name of the user assigned identity.
    [<CustomOperation "name">]
    member _.Name(state: UserAssignedIdentityConfig, name) = { state with Name = ResourceName name }

    /// Adds the user assigned identity to the specified active directory groups. This happens as an Az script after the ARM deployment has completed.
    [<CustomOperation "add_to_ad_groups">]
    member _.AddToGroups(state: UserAssignedIdentityConfig, groupNames) =
        { state with
            ActiveDirectoryGroups = Set.union (Set.ofSeq groupNames) state.ActiveDirectoryGroups
        }

    /// Adds the user assigned identity to the specified active directory group. This happens as an Az script after the ARM deployment has completed.
    [<CustomOperation "add_to_ad_group">]
    member this.AddToGroup(state: UserAssignedIdentityConfig, groupName) = this.AddToGroups(state, [ groupName ])
    /// Adds tags to the user assigned identity.
    interface ITaggable<UserAssignedIdentityConfig> with
        member _.Add state tags =
            { state with
                Tags = state.Tags |> Map.merge tags
            }

/// Builds a user assigned identity.
let userAssignedIdentity = UserAssignedIdentityBuilder()

/// Quickly creates a user-assigned managed identity.
let createUserAssignedIdentity userName = userAssignedIdentity { name userName }

type IIdentity<'TConfig> =
    abstract member Add: 'TConfig -> (ManagedIdentity -> ManagedIdentity) -> 'TConfig

[<AutoOpen>]
module Extensions =
    type IIdentity<'TConfig> with

        [<CustomOperation "add_identity">]
        member this.AddIdentity(state: 'TConfig, identity: Identity.UserAssignedIdentity) =
            this.Add state (fun current -> current + identity)

        member this.AddIdentity(state, identity: UserAssignedIdentityConfig) =
            this.AddIdentity(state, identity.UserAssignedIdentity)

        [<CustomOperation "system_identity">]
        member this.SystemIdentity(state: 'TConfig) =
            this.Add state (fun current ->
                { current with
                    SystemAssigned = Enabled
                })
