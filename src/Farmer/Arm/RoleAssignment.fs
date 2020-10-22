[<AutoOpen>]
module Farmer.Arm.RoleAssignment

open Farmer
open Farmer.CoreTypes

let roleAssignments = ResourceType ("Microsoft.Authorization/roleAssignments", "2020-04-01-preview")

type Assignment =
    { /// It's recommended to use a deterministic GUID for the role name.
      Name : ResourceName
      /// The role to assign, such as Roles.Contributor
      RoleDefinitionId : RoleId
      /// The principal ID of the user or service identity that should be granted this role.
      PrincipalId : PrincipalId
      /// Resource this role applies to. If empty, this will apply to the resource group where deployed.
      Scope : ResourceName }
    
    member private this.Dependencies = [
        if this.Scope <> ResourceName.Empty then
            ResourceId.create this.Scope
    ]
    interface IArmResource with
        member this.ResourceName = this.Name
        member this.JsonModel =
            {| roleAssignments.Create(this.Name, dependsOn = this.Dependencies) with
                properties =
                    {| roleDefinitionId = this.RoleDefinitionId.ArmValue.Eval()
                       principalId = this.PrincipalId.ArmExpression.Eval()
                       scope =
                           if this.Scope <> ResourceName.Empty then
                               (ResourceId.create this.Scope).Eval()
                           else null // Scope will be the resource group where this is deployed.
                    |}
            |}:> _
