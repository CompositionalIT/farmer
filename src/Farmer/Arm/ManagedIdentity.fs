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
      // set of groups to add this identity to in a post-deploy action
      ActiveDirectoryGroups: string Set }

    interface IArmResource with
        member this.ResourceId = userAssignedIdentities.resourceId this.Name
        member this.JsonModel = userAssignedIdentities.Create(this.Name, this.Location, [], this.Tags)
    interface IPostDeploy with
        member this.Run rg = 
            if this.ActiveDirectoryGroups.IsEmpty 
            then None // There are no groups to add this identity to so we can skip the post-deploy action
            else 
                result {
                    let (ResourceName idName) = this.Name
                    // get the object id of the user assigned identity
                    let! identityId =
                        printfn $"Getting object id for identity '{idName}'"
                        result {
                            let! identityResponse = az $"identity show -g {rg} --name {idName} --query \"principalId\""
                            return Serialization.ofJson<string> identityResponse
                        }

                    let! results =
                        [for group in this.ActiveDirectoryGroups do
                            // There is no add-if-not-contains method for AAD groups and the add method will fail if the identity is already contained in the group
                            // Because of this, we need to check if the group already contains the id before we add it.
                            result {
                                // Print the start of the line - we add a result to this line later on
                                printf $"Adding identity '{idName}' ({identityId}) to group '{group}'..."
                                let! inGroup =
                                    az $"ad group member check -g {group} --member-id {identityId} --query \"value\""
                                    |> Result.map Serialization.ofJson<bool>

                                let addedToGroup = 
                                    if not inGroup then 
                                        az $"ad group member add -g {group} --member-id {identityId}"
                                        |> Result.map (fun _ -> $"{idName} added to group '{group}'")
                                    else 
                                        // We consider the member already being in the group as a successful result even though we haven't done anything as this represents 
                                        // the resultant state of azure being as requested
                                        Result.Ok $"{idName} already in group '{group}'"

                                do 
                                    match addedToGroup with
                                    | Ok s -> printfn $"OK: {s}"
                                    | Error s -> printfn $"Error: {s}"

                                return! addedToGroup
                            }
                        ] |> Result.sequence 
                    return String.concat "; " results
                } |> Some
            

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