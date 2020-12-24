[<AutoOpen>]
module Farmer.Arm.RoleAssignment

open Farmer

let roleAssignments = ResourceType ("Microsoft.Authorization/roleAssignments", "2020-04-01-preview")

[<RequireQualifiedAccess>]
type PrincipalType =
    | User
    | Group
    | ServicePrincipal
    | Unknown
    | DirectoryRoleTemplate
    | ForeignGroup
    | Application
    | MSI
    | DirectoryObjectOrGroup
    | Everyone
    member this.ArmValue =
        match this with
        | User -> "User"
        | Group -> "Group"
        | ServicePrincipal -> "ServicePrincipal"
        | Unknown -> "Unknown"
        | DirectoryRoleTemplate -> "DirectoryRoleTemplate"
        | ForeignGroup -> "ForeignGroup"
        | Application -> "Application"
        | MSI -> "MSI"
        | DirectoryObjectOrGroup -> "DirectoryObjectOrGroup"
        | Everyone -> "Everyone"

/// The scope that an assignment applies to - either a specific resource, or the entire resource group.
type AssignmentScope = ResourceGroup | SpecificResource of ResourceId

type RoleAssignment =
    { /// It's recommended to use a deterministic GUID for the role name.
      Name : ResourceName
      /// The role to assign, such as Roles.Contributor
      RoleDefinitionId : RoleId
      /// The principal ID of the user or service identity that should be granted this role.
      PrincipalId : PrincipalId
      /// The type of principal being assigned - should be set to ServicePrincipal for managed identities to avoid
      /// the role assignment being created before Active Directory can replicate the principal.
      PrincipalType : PrincipalType
      /// Resource this role applies to. If this is set to a specific resource, it will automatically be set as a dependency for you.
      Scope : AssignmentScope
      Dependencies : ResourceId Set }
    interface IArmResource with
        member this.ResourceId = roleAssignments.resourceId this.Name
        member this.JsonModel =
            let dependencies = this.Dependencies + Set [
                match this.Scope with
                | SpecificResource resourceId -> resourceId
                | ResourceGroup -> ()
            ]

            {| roleAssignments.Create(this.Name, dependsOn = dependencies) with
                properties =
                    {| roleDefinitionId = this.RoleDefinitionId.ArmValue.Eval()
                       principalId = this.PrincipalId.ArmExpression.Eval()
                       scope =
                        match this with
                        | { Scope = ResourceGroup } -> null
                        | { Scope = SpecificResource resourceId } -> resourceId.Eval()
                       principalType = this.PrincipalType.ArmValue |}
            |}:> _
