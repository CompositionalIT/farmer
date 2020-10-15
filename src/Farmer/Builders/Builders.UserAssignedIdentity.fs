[<AutoOpen>]
module Farmer.Builders.UserAssignedIdentity

open Farmer
open Farmer.ManagedIdentity
open Farmer.Arm.ManagedIdentity

type UserAssignedIdentityConfig =
    { Name : ResourceName
      Tags : Map<string, string> }
    member this.Identity = UserAssigned [ UserAssignedIdentity(this.Name.Value, None) ]
    interface IBuilder with
        member this.DependencyName = this.Name
        member this.BuildResources location = [
            { UserAssignedIdentity.Name = this.Name
              Location = location
              Tags = this.Tags }
        ]

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