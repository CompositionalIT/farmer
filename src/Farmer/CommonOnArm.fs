namespace Farmer

open Farmer.CoreTypes
open ManagedIdentity
open Farmer.Arm.ManagedIdentity

[<AutoOpen>]
module ManagedIdentityExtensions =
    type ResourceIdentity with
        /// Creates a single User-Assigned ResourceIdentity from a ResourceId
        static member create (resourceId:ResourceId) =
            resourceId.WithType(userAssignedIdentities)
            |> UserAssignedIdentity
            |> List.singleton
            |> UserAssigned
        /// Creates a resource identity from a resource name
        static member create (name:ResourceName) = CoreTypes.ResourceId.create (userAssignedIdentities, name) |> ResourceIdentity.create

