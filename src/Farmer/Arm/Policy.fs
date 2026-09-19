[<AutoOpen>]
module Farmer.Arm.Policy

open Farmer
open System.Text.Json

let policyDefinitions =
    ResourceType("Microsoft.Authorization/policyDefinitions", "2021-06-01")

let policyAssignments =
    ResourceType("Microsoft.Authorization/policyAssignments", "2024-04-01")

[<RequireQualifiedAccess>]
type PolicyMode =
    | All
    | Indexed

    member this.ArmValue =
        match this with
        | All -> "All"
        | Indexed -> "Indexed"

[<RequireQualifiedAccess>]
type PolicyEffect =
    | Audit
    | AuditIfNotExists
    | Deny
    | DenyAction
    | Disabled
    | Modify
    | Append
    | DeployIfNotExists

    member this.ArmValue =
        match this with
        | Audit -> "Audit"
        | AuditIfNotExists -> "AuditIfNotExists"
        | Deny -> "Deny"
        | DenyAction -> "DenyAction"
        | Disabled -> "Disabled"
        | Modify -> "Modify"
        | Append -> "Append"
        | DeployIfNotExists -> "DeployIfNotExists"

[<RequireQualifiedAccess>]
type EnforcementMode =
    | Default
    | DoNotEnforce

    member this.ArmValue =
        match this with
        | Default -> "Default"
        | DoNotEnforce -> "DoNotEnforce"

type PolicyDefinition = {
    Name: ResourceName
    DisplayName: string option
    Description: string option
    Mode: PolicyMode
    PolicyRule: string
    Parameters: Map<string, obj> option
    Metadata: Map<string, string> option
} with

    interface IArmResource with
        member this.ResourceId = policyDefinitions.resourceId this.Name

        member this.JsonModel =
            {|
                policyDefinitions.Create(this.Name) with
                    properties =
                        {|
                            displayName = this.DisplayName |> Option.toObj
                            description = this.Description |> Option.toObj
                            mode = this.Mode.ArmValue
                            policyRule = JsonDocument.Parse(this.PolicyRule).RootElement
                            parameters =
                                match this.Parameters with
                                | Some p -> box p
                                | None -> null
                            metadata =
                                match this.Metadata with
                                | Some m -> box m
                                | None -> null
                        |}
                        :> obj
            |}

type PolicyAssignment = {
    Name: ResourceName
    DisplayName: string option
    Description: string option
    PolicyDefinitionId: ResourceId
    EnforcementMode: EnforcementMode
    Parameters: Map<string, obj> option
    Scope: ResourceId option
    NotScopes: string list
    Location: Location option
    Identity: Identity.ManagedIdentity option
    Dependencies: ResourceId Set
} with

    interface IArmResource with
        member this.ResourceId = policyAssignments.resourceId this.Name

        member this.JsonModel =
            let dependencies =
                this.Dependencies
                + Set [ this.PolicyDefinitionId ]
                + (match this.Identity with
                   | Some identity ->
                       identity.Dependencies
                       |> Set.ofList
                       |> Set.map (fun (rid: ResourceId) -> rid)
                   | None -> Set.empty)

            {|
                policyAssignments.Create(this.Name, dependsOn = dependencies) with
                    location =
                        match this.Location with
                        | Some loc -> loc.ArmValue
                        | None -> null
                    identity =
                        match this.Identity with
                        | Some identity -> identity.ToArmJson :> obj
                        | None -> null
                    properties =
                        {|
                            displayName = this.DisplayName |> Option.toObj
                            description = this.Description |> Option.toObj
                            policyDefinitionId = this.PolicyDefinitionId.Eval()
                            enforcementMode = this.EnforcementMode.ArmValue
                            parameters =
                                match this.Parameters with
                                | Some p -> box p
                                | None -> null
                            notScopes =
                                match this.NotScopes with
                                | [] -> null
                                | scopes -> box scopes
                        |}
                        :> obj
                    scope = this.Scope |> Option.map (fun s -> s.Eval()) |> Option.toObj
            |}
