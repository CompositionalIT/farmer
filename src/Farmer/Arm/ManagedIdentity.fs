[<AutoOpen>]
module Farmer.Arm.ManagedIdentity

open Farmer
open Farmer.Identity

let userAssignedIdentities =
    ResourceType("Microsoft.ManagedIdentity/userAssignedIdentities", "2023-01-31")

let federatedIdentityCredentials =
    ResourceType("Microsoft.ManagedIdentity/userAssignedIdentities/federatedIdentityCredentials", "2023-01-31")

type UserAssignedIdentity =
    {
        Name: ResourceName
        Location: Location
        Tags: Map<string, string>
    }

    interface IArmResource with
        member this.ResourceId = userAssignedIdentities.resourceId this.Name

        member this.JsonModel =
            userAssignedIdentities.Create(this.Name, this.Location, [], this.Tags)

/// A federated identity credential from an OpenId Connect issuer.
type FederatedIdentityCredential =
    {
        Name: ResourceName
        UserAssignedIdentity: LinkedResource
        Audiences: string list
        Issuer: string
        Subject: string
    }

    interface IArmResource with
        member this.ResourceId =
            { federatedIdentityCredentials.resourceId this.UserAssignedIdentity.Name with
                Segments = [ this.Name ]
            }

        member this.JsonModel =
            let dependencies =
                Set.empty |> LinkedResource.addToSetIfManaged this.UserAssignedIdentity

            {| federatedIdentityCredentials.Create(this.UserAssignedIdentity.Name / this.Name, dependsOn = dependencies) with
                properties =
                    {|
                        audiences = this.Audiences
                        issuer = this.Issuer
                        subject = this.Subject
                    |}
            |}

/// Builds the JSON ARM value for a resource's identity.
let toArmJson =
    function
    | {
          SystemAssigned = Disabled
          UserAssigned = []
      } ->
        {|
            ``type`` = "None"
            userAssignedIdentities = null
        |}
    | {
          SystemAssigned = Enabled
          UserAssigned = []
      } ->
        {|
            ``type`` = "SystemAssigned"
            userAssignedIdentities = null
        |}
    | {
          SystemAssigned = Disabled
          UserAssigned = identities
      } ->
        {|
            ``type`` = "UserAssigned"
            userAssignedIdentities =
                identities
                |> List.map (fun identity -> identity.ResourceId.Eval(), obj ())
                |> dict
        |}
    | {
          SystemAssigned = Enabled
          UserAssigned = identities
      } ->
        {|
            ``type`` = "SystemAssigned, UserAssigned"
            userAssignedIdentities =
                identities
                |> List.map (fun identity -> identity.ResourceId.Eval(), obj ())
                |> dict
        |}

type ManagedIdentity with

    /// Builds the JSON ARM value for a resource's identity.
    member this.ToArmJson = toArmJson this
