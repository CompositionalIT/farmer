[<AutoOpen>]
module Farmer.Arm.ManagedIdentity

open Farmer
open Farmer.Identity
open Deploy.Az

let userAssignedIdentities = ResourceType ("Microsoft.ManagedIdentity/userAssignedIdentities", "2018-11-30")

type UserAssignedIdentity =
    { Name : ResourceName
      Location : Location
      Tags: Map<string,string>
      ActiveDirectoryGroups: string Set }

    interface IArmResource with
        member this.ResourceId = userAssignedIdentities.resourceId this.Name
        member this.JsonModel = userAssignedIdentities.Create(this.Name, this.Location, [], this.Tags) :> _
    interface IPostDeploy with
        member this.Run rg = 
            if this.ActiveDirectoryGroups.IsEmpty 
            then None
            else 
                let identityId =
                    result {
                        let! identityResponse = az $"identity show -g {rg} --name {this.Name.Value} --query \"principalId\""
                        return Serialization.ofJson<string> identityResponse
                    }
                let results : Result<string,string> seq =
                    [for group in this.ActiveDirectoryGroups do
                        result {
                            let! inGroupResponse = az $"ad group member check -g {group} --member-id {identityId} --query \"value\""
                            let inGroup = Serialization.ofJson<bool> inGroupResponse
                            let! addResponse = 
                                if not inGroup then 
                                     az $"ad group member add -g {group} --member-id {identityId}"
                                     |> Result.map (fun _ -> $"Successfully added to group '{group}'")
                                else 
                                    Result.Ok $"Already in group '{group}'"
                            return addResponse
                        }
                    ]
                Result.sequence results
                |> Result.map (String.concat "; ")
                |> Some
            

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