[<AutoOpen>]
module Farmer.Builders.RoleAssignment

open Farmer
open Farmer.Arm.RoleAssignment

/// Creates a role assignment for a specific scope (a resource or subscription).
let inline scopedRoleAssignment name role (assignee:^t) scope =
    let inline getPrincipalId p = (^t : (member PrincipalId : Farmer.CoreTypes.PrincipalId) p)
    let inline getPrincipalResourceId p = (^t : (member ResourceId : Farmer.CoreTypes.ResourceId) p)
    {
        Name = ResourceName name
        RoleDefinitionId = role
        PrincipalId = getPrincipalId assignee
        PrincipalResourceId = getPrincipalResourceId assignee
        PrincipalType = PrincipalType.ServicePrincipal
        Scope = scope
    }
/// Creates a role assignment for the resource group where the deployment runs.
let inline roleAssignment name role principal = scopedRoleAssignment name role principal ResourceName.Empty
