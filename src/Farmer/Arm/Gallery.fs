[<AutoOpen>]
module Farmer.Arm.Gallery

open System
open Farmer
open Farmer.Image
open Farmer.GalleryValidation

let galleries = ResourceType("Microsoft.Compute/galleries", "2022-03-03")
let galleryImages = ResourceType("Microsoft.Compute/galleries/images", "2022-03-03")

let imageVersions =
    ResourceType("Microsoft.Compute/galleries/images/versions", "2022-03-03")

type CommunityGalleryInfo = {
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

type Gallery =
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
                properties = {|
                    description = this.Description
                    sharingProfile =
                        match this.SharingProfile with
                        | None -> null
                        | Some sharingProfile ->
                            match sharingProfile with
                            | Community info ->
                                {|
                                    permissions = "Community"
                                    communityGalleryInfo = {|
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

type GalleryImageIdentifier = {
    Offer: string
    Publisher: string
    Sku: string
}

type ImagePurchasePlan = {
    PlanName: string
    PlanProduct: string
    PlanPublisher: string
}

type RecommendedMachineConfiguration =
    {
        MemoryMin: int<Gb>
        MemoryMax: int<Gb>
        VCpuMin: int
        VCpuMax: int
    }

    static member Default = {
        MemoryMin = 1<Gb>
        MemoryMax = 32<Gb>
        VCpuMin = 1
        VCpuMax = 16
    }

type GalleryImage =
    {
        Name: ResourceName
        GalleryName: GalleryName
        Location: Location
        Architecture: Architecture option
        Description: string
        Eula: string option
        HyperVGeneration: HyperVGeneration
        Identifier: GalleryImageIdentifier
        OsState: OsState
        OsType: OS
        PrivacyStatementUri: Uri option
        PurchasePlan: ImagePurchasePlan option
        Recommended: RecommendedMachineConfiguration
        ReleaseNoteUri: Uri option
        Tags: Map<string, string>
        Dependencies: Set<ResourceId>
    }

    interface IArmResource with
        member this.ResourceId =
            galleryImages.resourceId (this.GalleryName.ResourceName, this.Name)

        member this.JsonModel =
            {| galleryImages.Create(
                   this.GalleryName.ResourceName / this.Name,
                   this.Location,
                   dependsOn = this.Dependencies,
                   tags = this.Tags
               ) with
                properties = {|
                    architecture = this.Architecture |> Option.map (fun arch -> arch.ArmValue) |> Option.toObj
                    description = this.Description
                    eula = this.Eula |> Option.toObj
                    hyperVGeneration = this.HyperVGeneration.ArmValue
                    identifier = {|
                        offer = this.Identifier.Offer
                        publisher = this.Identifier.Publisher
                        sku = this.Identifier.Sku
                    |}
                    osState = this.OsState.ArmValue
                    osType = string<OS> this.OsType
                    privacyStatementUri =
                        this.PrivacyStatementUri
                        |> Option.map (fun uri -> uri.AbsoluteUri)
                        |> Option.toObj
                    purchasePlan =
                        match this.PurchasePlan with
                        | None -> null
                        | Some plan ->
                            {|
                                name = plan.PlanName
                                product = plan.PlanProduct
                                publisher = plan.PlanPublisher
                            |}
                            :> obj
                    recommended = {|
                        memory = {|
                            min = this.Recommended.MemoryMin
                            max = this.Recommended.MemoryMax
                        |}
                        vCPUs = {|
                            min = this.Recommended.VCpuMin
                            max = this.Recommended.VCpuMax
                        |}
                    |}
                    releaseNoteUri = this.ReleaseNoteUri |> Option.map (fun uri -> uri.AbsoluteUri) |> Option.toObj
                |}
            |}
