[<AutoOpen>]
module Farmer.Builders.UserAssignedIdentity

open Farmer
open Farmer.Identity
open Farmer.Arm.ManagedIdentity

type UserAssignedIdentityConfig =
    { Name : ResourceName
      Tags : Map<string, string> }
    interface IBuilder with
        member this.ResourceId = this.ResourceId
        member this.BuildResources location = [
            { UserAssignedIdentity.Name = this.Name
              Location = location
              Tags = this.Tags }
        ]
    member this.ResourceId = userAssignedIdentities.resourceId this.Name
    member this.UserAssignedIdentity = UserAssignedIdentity this.ResourceId
    member this.ClientId = this.UserAssignedIdentity.ClientId
    member this.PrincipalId = this.UserAssignedIdentity.PrincipalId

type UserAssignedIdentityBuilder() =
    member _.Yield _ =
        { Name = ResourceName.Empty
          Tags = Map.empty }
    /// Sets the name of the user assigned identity.
    [<CustomOperation "name">]
    member __.Name(state:UserAssignedIdentityConfig, name) = { state with Name = ResourceName name }
    /// Adds tags to the user assigned identity.
    [<CustomOperation "add_tags">]
    member _.Tags(state:UserAssignedIdentityConfig, pairs) =
        { state with
            Tags = pairs |> List.fold (fun map (key, value) -> Map.add key value map) state.Tags }
    /// Adds a tag to the user assigned identity.
    [<CustomOperation "add_tag">]
    member this.Tag(state:UserAssignedIdentityConfig, key, value) = this.Tags(state, [ (key,value) ])

/// Builds a user assigned identity.
let userAssignedIdentity = UserAssignedIdentityBuilder()

/// Quickly creates a user-assigned managed identity.
let createUserAssignedIdentity userName = userAssignedIdentity { name userName }
