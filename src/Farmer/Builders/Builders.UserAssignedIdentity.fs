[<AutoOpen>]
module Farmer.Builders.UserAssignedIdentity

open Farmer
open Farmer.Identity
open Farmer.Arm.ManagedIdentity

[<Literal>]
let EntraIdAudience = "api://AzureADTokenExchange"

type FederatedIdentityCredentialConfig = {
    Name: ResourceName
    UserAssignedIdentity: LinkedResource option
    Audiences: string list
    Issuer: string option
    Subject: string option
} with

    interface IBuilder with
        member this.ResourceId =
            match this.UserAssignedIdentity with
            | Some identity -> {
                federatedIdentityCredentials.resourceId identity.Name with
                    Segments = [ this.Name ]
              }
            | None -> raiseFarmer "A federated identity credential must be assigned to a user assigned identity."

        member this.BuildResources _ =
            match this.UserAssignedIdentity with
            | None -> raiseFarmer "A federated identity credential must be assigned to a user assigned identity."
            | Some identity -> [
                {
                    FederatedIdentityCredential.Name = this.Name
                    UserAssignedIdentity = identity
                    Audiences = this.Audiences
                    Issuer =
                        this.Issuer
                        |> Option.defaultValue (raiseFarmer "Issuer must be set on a federated identity credential.")
                    Subject =
                        this.Subject
                        |> Option.defaultValue (raiseFarmer "Subject must be set on a federated identity credential.")
                }
              ]

type FederatedIdentityCredentialBuilder() =
    member _.Yield _ = {
        Name = ResourceName.Empty
        UserAssignedIdentity = None
        Audiences = []
        Issuer = None
        Subject = None
    }

    /// Sets the name of the federated identity credential.
    [<CustomOperation "name">]
    member _.Name(state: FederatedIdentityCredentialConfig, name) = { state with Name = ResourceName name }

    /// Sets the user assigned identity for the federated identity credential.
    [<CustomOperation "user_assigned_identity">]
    member _.UserAssignedIdentity(state: FederatedIdentityCredentialConfig, identity: LinkedResource) = {
        state with
            UserAssignedIdentity = Some identity
    }

    member _.UserAssignedIdentity(state: FederatedIdentityCredentialConfig, identity: ResourceId) = {
        state with
            UserAssignedIdentity = Some(Managed identity)
    }

    member _.UserAssignedIdentity(state: FederatedIdentityCredentialConfig, identity: ResourceName) = {
        state with
            UserAssignedIdentity = Some(Managed(userAssignedIdentities.resourceId identity))
    }

    [<CustomOperation "link_to_user_assigned_identity">]
    member _.LinkToUserAssignedIdentity(state: FederatedIdentityCredentialConfig, identity: LinkedResource) = {
        state with
            UserAssignedIdentity = Some identity
    }

    member _.LinkToUserAssignedIdentity(state: FederatedIdentityCredentialConfig, identity: ResourceId) = {
        state with
            UserAssignedIdentity = Some(Unmanaged identity)
    }

    member _.LinkToUserAssignedIdentity(state: FederatedIdentityCredentialConfig, identity: ResourceName) = {
        state with
            UserAssignedIdentity = Some(Unmanaged(userAssignedIdentities.resourceId identity))
    }

    /// Sets the Audiences of the federated identity credential.
    [<CustomOperation "audiences">]
    member _.Audiences(state: FederatedIdentityCredentialConfig, audiences) = { state with Audiences = audiences }

    [<CustomOperation "audience">]
    member _.Audiences(state: FederatedIdentityCredentialConfig, audience) = { state with Audiences = [ audience ] }

    /// Sets the issuer of the federated identity credential.
    [<CustomOperation "issuer">]
    member _.Issuer(state: FederatedIdentityCredentialConfig, issuer) = { state with Issuer = Some issuer }

    /// Sets the issuer of the federated identity credential.
    [<CustomOperation "subject">]
    member _.Subject(state: FederatedIdentityCredentialConfig, subject) = { state with Subject = Some subject }

let federatedIdentityCredential = FederatedIdentityCredentialBuilder()

type UserAssignedIdentityConfig = {
    Name: ResourceName
    FederatedIdentityCredentials: FederatedIdentityCredentialConfig list
    Tags: Map<string, string>
} with

    interface IBuilder with
        member this.ResourceId = this.ResourceId

        member this.BuildResources location = [
            {
                UserAssignedIdentity.Name = this.Name
                Location = location
                Tags = this.Tags
            }
            for cred in this.FederatedIdentityCredentials do
                {
                    FederatedIdentityCredential.Name = cred.Name
                    UserAssignedIdentity = LinkedResource.Managed this.ResourceId
                    Audiences = cred.Audiences
                    Issuer =
                        match cred.Issuer with
                        | Some issuer -> issuer
                        | None -> raiseFarmer "Issuer must be set on a federated identity credential."
                    Subject =
                        match cred.Subject with
                        | Some subject -> subject
                        | None -> raiseFarmer "Subject must be set on a federated identity credential."
                }

        ]

    member this.ResourceId = userAssignedIdentities.resourceId this.Name

    member this.UserAssignedIdentity = UserAssignedIdentity this.ResourceId

    member this.ClientId = this.UserAssignedIdentity.ClientId
    member this.PrincipalId = this.UserAssignedIdentity.PrincipalId

type UserAssignedIdentityBuilder() =
    member _.Yield _ = {
        Name = ResourceName.Empty
        FederatedIdentityCredentials = []
        Tags = Map.empty
    }

    /// Sets the name of the user assigned identity.
    [<CustomOperation "name">]
    member _.Name(state: UserAssignedIdentityConfig, name) = { state with Name = ResourceName name }

    /// Adds federated identity credentials to this identity
    [<CustomOperation "add_federated_identity_credentials">]
    member _.AddFederatedIdentityCredentials
        (state: UserAssignedIdentityConfig, creds: FederatedIdentityCredentialConfig list)
        =
        {
            state with
                FederatedIdentityCredentials = state.FederatedIdentityCredentials @ creds
        }
    /// Adds tags to the user assigned identity.
    interface ITaggable<UserAssignedIdentityConfig> with
        member _.Add state tags = {
            state with
                Tags = state.Tags |> Map.merge tags
        }

/// Builds a user assigned identity.
let userAssignedIdentity = UserAssignedIdentityBuilder()

/// Quickly creates a user-assigned managed identity.
let createUserAssignedIdentity userName = userAssignedIdentity { name userName }

type IIdentity<'TConfig> =
    abstract member Add: 'TConfig -> (ManagedIdentity -> ManagedIdentity) -> 'TConfig

[<AutoOpen>]
module Extensions =
    type IIdentity<'TConfig> with

        [<CustomOperation "add_identity">]
        member this.AddIdentity(state: 'TConfig, identity: Identity.UserAssignedIdentity) =
            this.Add state (fun current -> current + identity)

        member this.AddIdentity(state, identity: UserAssignedIdentityConfig) =
            this.AddIdentity(state, identity.UserAssignedIdentity)

        member this.AddIdentity(state: 'TConfig, resourceId: ResourceId) =
            let userAssignedIdentity = UserAssignedIdentity resourceId
            this.Add state (fun current -> current + userAssignedIdentity)

        [<CustomOperation "link_to_identity">]
        member this.LinkToIdentity(state: 'TConfig, resourceId: ResourceId) =
            let userAssignedIdentity = LinkedUserAssignedIdentity resourceId
            this.Add state (fun current -> current + userAssignedIdentity)

        member this.LinkToIdentity(state, identity: UserAssignedIdentityConfig) =
            this.LinkToIdentity(state, identity.ResourceId)

        [<CustomOperation "system_identity">]
        member this.SystemIdentity(state: 'TConfig) =
            this.Add state (fun current -> {
                current with
                    SystemAssigned = Enabled
            })