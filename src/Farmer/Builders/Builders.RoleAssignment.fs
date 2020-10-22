[<AutoOpen>]
module Farmer.Builders.RoleAssignment

open Farmer
open Farmer.Arm.RoleAssignment

/// Creates a role assignment for a specific scope (a resource or subscription).
let scoped_role_assignment name role principal scope =
    {
        Name = ResourceName name
        RoleDefinitionId = role
        PrincipalId = principal
        Scope = scope
    }
/// Creates a role assignment for the resource group where the deployment runs.
let role_assignment name role principal = scoped_role_assignment name role principal ResourceName.Empty
