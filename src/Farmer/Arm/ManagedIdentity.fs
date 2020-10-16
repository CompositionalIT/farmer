module Farmer.Arm.ManagedIdentity

open Farmer
open Farmer.ManagedIdentity
open Farmer.CoreTypes

let userAssignedIdentities = ResourceType ("Microsoft.ManagedIdentity/userAssignedIdentities", "2018-11-30")

//TODO: Remove optionality?
/// List of resource ID's for the managed identities when a resource is using user assigned identities.
let Dependencies = function
    | Some (UserAssignedIdentity resourceId)
    | Some (SystemIdentity (Some resourceId)) ->
        Some resourceId
    | Some (SystemIdentity None)
    | None ->
        None

/// Builds the JSON ARM value for a resource's identity.
let ArmValue = function
    | None ->
        {| ``type`` = "None"; userAssignedIdentities = null |}
    | Some (SystemIdentity _) ->
        {| ``type`` = "SystemAssigned"; userAssignedIdentities = null |}
    | Some (UserAssignedIdentity resourceId) ->
        // Identities are assigned as a dictionary with the user identity resource ID as the key
        // and an empty object as the value.
        {| ``type`` = "UserAssigned"
           userAssignedIdentities =
            [ resourceId.Eval(), obj ]
            |> dict
        |}

/// Creates a user assigned identity ARM resource
type UserAssignedIdentity =
    { Name : ResourceName
      Location : Location
      Tags: Map<string,string>  }

    interface IArmResource with
        member this.ResourceName = this.Name
        member this.JsonModel = userAssignedIdentities.Create(this.Name, this.Location, [], this.Tags) :> _
