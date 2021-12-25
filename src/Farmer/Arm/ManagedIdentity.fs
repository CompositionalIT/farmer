[<AutoOpen>]
module Farmer.Arm.ManagedIdentity

open Farmer
open Farmer.Identity

let userAssignedIdentities = ResourceType ("Microsoft.ManagedIdentity/userAssignedIdentities", "2018-11-30")

type UserAssignedIdentity =
    { Name : ResourceName
      Location : Location
      Tags: Map<string,string>  }

    interface IArmResource with
        member this.ResourceId = userAssignedIdentities.resourceId this.Name
        member this.JsonModel = userAssignedIdentities.Create(this.Name, this.Location, [], this.Tags)

/// Builds the JSON ARM value for a resource's identity.
let toArmJson = function
    | { SystemAssigned = Disabled; UserAssigned = [] } ->
        {| ``type`` = "None"; userAssignedIdentities = null |}
    | { SystemAssigned = Enabled; UserAssigned = [] } ->
        {| ``type`` = "SystemAssigned"; userAssignedIdentities = null |}
    | { SystemAssigned = Disabled; UserAssigned = identities } ->
        {| ``type`` = "UserAssigned"
           userAssignedIdentities = identities |> List.map(fun identity -> identity.ResourceId.Eval(), obj()) |> dict |}
    | { SystemAssigned = Enabled; UserAssigned = identities } ->
        {| ``type`` = "SystemAssigned, UserAssigned"
           userAssignedIdentities = identities |> List.map(fun identity -> identity.ResourceId.Eval(), obj()) |> dict |}

type ManagedIdentity with
    /// Builds the JSON ARM value for a resource's identity.
    member this.ToArmJson = toArmJson this