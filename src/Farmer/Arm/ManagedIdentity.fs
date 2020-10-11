module Farmer.Arm.ManagedIdentity

open Farmer
open Farmer.ManagedIdentity
open Farmer.CoreTypes

let userAssignedIdentities = ResourceType ("Microsoft.ManagedIdentity/userAssignedIdentities", "2018-11-30")

type ManagedIdentity.UserAssignedIdentity with
    /// Builds a user assigned identity resource ID.
    member this.ResourceId =
        let (UserAssignedIdentity (name, resourceGroup)) = this
        ResourceId.create (userAssignedIdentities, ResourceName name, ?group = resourceGroup)

/// Creates a user assigned identity ARM resource
type UserAssignedIdentity =
    { Name : ResourceName
      Location : Location
      Tags: Map<string,string>  }

    interface IArmResource with
        member this.ResourceName = this.Name
        member this.JsonModel = userAssignedIdentities.Create(this.Name, this.Location, [], this.Tags) :> _
