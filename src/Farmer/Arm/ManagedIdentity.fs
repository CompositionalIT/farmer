module Farmer.Arm.ManagedIdentity

open Farmer
open Farmer.Identity
open Farmer.CoreTypes

let userAssignedIdentities = ResourceType ("Microsoft.ManagedIdentity/userAssignedIdentities", "2018-11-30")

/// Creates a user assigned identity ARM resource
type UserAssignedIdentity =
    { Name : ResourceName
      Location : Location
      Tags: Map<string,string>  }

    interface IArmResource with
        member this.ResourceName = this.Name
        member this.JsonModel = userAssignedIdentities.Create(this.Name, this.Location, [], this.Tags) :> _


//TODO: Remove optionality?
/// List of resource ID's for the managed identities when a resource is using user assigned identities.
let Dependencies (identity:ManagedIdentity option) = identity |> Option.bind (fun i -> i.ResourceId) 

/// Builds the JSON ARM value for a resource's identity.
let toArmJson = function
    | None ->
        {| ``type`` = "None"; userAssignedIdentities = null |}
    | Some (SystemAssigned _) ->
        {| ``type`` = "SystemAssigned"; userAssignedIdentities = null |}
    | Some (UserAssigned identity) ->
        // Identities are assigned as a dictionary with the user identity resource ID as the key
        // and an empty object as the value.
        {| ``type`` = "UserAssigned"
           userAssignedIdentities =
            [ identity.ResourceId.Eval(), obj ]
            |> dict
        |}