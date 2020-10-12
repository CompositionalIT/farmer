module Farmer.Arm.ManagedIdentity

open Farmer
open Farmer.ManagedIdentity
open Farmer.CoreTypes

let userAssignedIdentities = ResourceType ("Microsoft.ManagedIdentity/userAssignedIdentities", "2018-11-30")

type private ManagedIdentity.UserAssignedIdentity with
    /// Builds a user assigned identity resource ID.
    member this.resourceId =
        let (UserAssignedIdentity (name, resourceGroup)) = this
        ResourceId.create (userAssignedIdentities, ResourceName name, ?group = resourceGroup)

/// List of resource ID's for the managed identities when a resource is using user assigned identities.
let Dependencies = function
    | Some (UserAssigned identities) ->
        identities |> List.map (fun identity -> identity.resourceId)
    | _ -> []

/// Builds the JSON ARM value for a resource's identity.
let ArmValue = function
    | None ->
        {| ``type`` = "None"; userAssignedIdentities = null |}
    | Some (SystemAssigned) ->
        {| ``type`` = "SystemAssigned"; userAssignedIdentities = null |}
    | Some (UserAssigned identities) ->
        // Identities are assigned as a dictionary with the user identity resource ID as the key
        // and an empty object as the value.
        {| ``type`` = "UserAssigned"
           userAssignedIdentities =
            identities
            |> List.map (fun identity -> identity.resourceId.Eval(), obj)
            |> dict |}

/// Creates a user assigned identity ARM resource
type UserAssignedIdentity =
    { Name : ResourceName
      Location : Location
      Tags: Map<string,string>  }

    interface IArmResource with
        member this.ResourceName = this.Name
        member this.JsonModel = userAssignedIdentities.Create(this.Name, this.Location, [], this.Tags) :> _
