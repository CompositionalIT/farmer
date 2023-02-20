[<AutoOpen>]
module Farmer.Arm.Gallery

open System
open Farmer
open Farmer.Arm
open Farmer.GalleryValidation

let galleries = ResourceType("Microsoft.Compute/galleries", "2022-03-03")
let iamges = ResourceType("Microsoft.Compute/galleries/images", "2022-03-03")

type CommunityGalleryInfo =
    {
        Eula: string
        PublicNamePrefix: string
        PublisherContact: string
        PublisherUri: Uri
    }

type SharingProfile =
    | Community of CommunityGalleryInfo
    | Groups
    | Private

type SoftDeletePolicy = { IsSoftDeleteEnabled: bool }

type ImageGallery =
    {
        Name: GalleryName
        Location: Location
        Description: string option
        SharingProfile: SharingProfile option
        SoftDeletePolicy: SoftDeletePolicy option
        Tags: Map<string, string>
        Dependencies: Set<ResourceId>
    }

    interface IArmResource with
        member this.ResourceId = galleries.resourceId this.Name.ResourceName

        member this.JsonModel =
            {| galleries.Create(this.Name.ResourceName, this.Location, dependsOn = this.Dependencies, tags = this.Tags) with
                properties =
                    {|
                        description = this.Description
                        sharingProfile =
                            match this.SharingProfile with
                            | None -> null
                            | Some sharingProfile ->
                                match sharingProfile with
                                | Community info ->
                                    {|
                                        permissions = "Community"
                                        communityGalleryInfo =
                                            {|
                                                eula = info.Eula
                                                publicNamePrefix = info.PublicNamePrefix
                                                publisherContact = info.PublisherContact
                                                publisherUri = info.PublisherUri.AbsoluteUri
                                            |}
                                    |}
                                    :> obj
                                | Groups -> {| permissions = "Groups" |} :> obj
                                | Private -> {| permissions = "Private" |} :> obj
                        softDeletePolicy =
                            match this.SoftDeletePolicy with
                            | Some policy ->
                                {|
                                    isSoftDeleteEnabled = policy.IsSoftDeleteEnabled
                                |}
                                :> obj
                            | None -> null
                    |}
            |}
