[<AutoOpen>]
module Farmer.Arm.Gallery

open System
open Farmer
open Farmer.Image
open Farmer.GalleryValidation

let galleries = ResourceType("Microsoft.Compute/galleries", "2023-07-03")
let galleryImages = ResourceType("Microsoft.Compute/galleries/images", "2023-07-03")

let imageVersions =
    ResourceType("Microsoft.Compute/galleries/images/versions", "2023-07-03")

let galleryApplications =
    ResourceType("Microsoft.Compute/galleries/applications", "2023-07-03")

let galleryApplicationVersions =
    ResourceType("Microsoft.Compute/galleries/applications/versions", "2023-07-03")

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

type Gallery = {
    Name: GalleryName
    Location: Location
    Description: string option
    SharingProfile: SharingProfile option
    SoftDeletePolicy: SoftDeletePolicy option
    Tags: Map<string, string>
    Dependencies: Set<ResourceId>
} with

    interface IArmResource with
        member this.ResourceId = galleries.resourceId this.Name.ResourceName

        member this.JsonModel = {|
            galleries.Create(this.Name.ResourceName, this.Location, dependsOn = this.Dependencies, tags = this.Tags) with
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

type RecommendedMachineConfiguration = {
    MemoryMin: int<Gb>
    MemoryMax: int<Gb>
    VCpuMin: int
    VCpuMax: int
} with

    static member Default = {
        MemoryMin = 1<Gb>
        MemoryMax = 32<Gb>
        VCpuMin = 1
        VCpuMax = 16
    }

type GalleryImage = {
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
} with

    interface IArmResource with
        member this.ResourceId =
            galleryImages.resourceId (this.GalleryName.ResourceName, this.Name)

        member this.JsonModel = {|
            galleryImages.Create(
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

[<RequireQualifiedAccess>]
type ParameterType =
    | ConfigurationDataBlob
    | LogOutputBlob
    | String of DefaultValue: string option

    member internal this.ToArmJson =
        match this with
        | ConfigurationDataBlob -> "ConfigurationDataBlob"
        | LogOutputBlob -> "LogOutputBlob"
        | String _ -> "String"

type CustomActionParameter = {
    Description: string option
    Name: string
    Required: bool option
    ParameterType: ParameterType
} with

    member internal this.ToArmJson = {|
        defaultValue =
            match this.ParameterType with
            | ParameterType.String(Some defaultVal) -> defaultVal
            | _ -> null
        description = this.Description |> Option.toObj
        name = this.Name
        required = this.Required |> Option.toNullable
        ``type`` = this.ParameterType.ToArmJson
    |}

type CustomAction = {
    Description: string option
    Name: string
    Parameters: CustomActionParameter list
    Script: string
} with

    member internal this.ToArmJson = {|
        description = this.Description |> Option.toObj
        name = this.Name
        parameters = this.Parameters |> List.map _.ToArmJson
        script = this.Script
    |}

type GalleryApplication = {
    Name: GalleryApplicationName
    GalleryName: GalleryName
    Location: Location
    CustomActions: CustomAction list
    Description: string option
    EndOfLifeDate: DateTimeOffset option
    Eula: string option
    OsType: OS
    PrivacyStatementUri: Uri option
    ReleaseNoteUri: Uri option
    Tags: Map<string, string>
    Dependencies: Set<ResourceId>
} with

    interface IArmResource with
        member this.ResourceId =
            galleryApplications.resourceId (this.GalleryName.ResourceName, this.Name.ResourceName)

        member this.JsonModel = {|
            galleryApplications.Create(
                this.GalleryName.ResourceName / this.Name.ResourceName,
                this.Location,
                dependsOn = this.Dependencies,
                tags = this.Tags
            ) with
                properties = {|
                    customActions = this.CustomActions |> List.map _.ToArmJson
                    description = this.Description |> Option.toObj
                    endOfLifeDate = this.EndOfLifeDate |> Option.map (_.ToString("yyyy-MM-dd")) |> Option.toObj
                    eula = this.Eula |> Option.toObj
                    supportedOSType = string<OS> this.OsType
                    privacyStatementUri =
                        this.PrivacyStatementUri
                        |> Option.map (fun uri -> uri.AbsoluteUri)
                        |> Option.toObj
                    releaseNoteUri = this.ReleaseNoteUri |> Option.map (fun uri -> uri.AbsoluteUri) |> Option.toObj
                |}
        |}

type ManageActions = {
    Install: string
    Remove: string
    Update: string option
} with

    static member Empty = {
        Install = ""
        Remove = ""
        Update = None
    }

    member internal this.ToArmJson = {|
        install = this.Install
        remove = this.Remove
        update = this.Update |> Option.toObj
    |}

type ReplicationMode =
    | Full
    | Shallow

    member this.ArmValue =
        match this with
        | Full -> "Full"
        | Shallow -> "Shallow"

type UserArtifactSettings = {
    ConfigFileName: string option
    PackageFileName: string option
} with

    member internal this.ToArmJson = {|
        configFileName = this.ConfigFileName |> Option.toObj
        packageFileName = this.PackageFileName |> Option.toObj
    |}

type UserArtifactSource = {
    DefaultConfigurationLink: Uri option
    MediaLink: string
} with

    static member Empty = {
        DefaultConfigurationLink = None
        MediaLink = null
    }

    member internal this.ToArmJson = {|
        defaultConfigurationLink = this.DefaultConfigurationLink |> Option.toObj
        mediaLink = this.MediaLink
    |}

[<RequireQualifiedAccess>]
type StorageAccountType =
    | Premium_LRS
    | Standard_LRS
    | Standard_ZRS

    member internal this.ArmValue =
        match this with
        | Premium_LRS -> "Premium_LRS"
        | Standard_LRS -> "Standard_LRS"
        | Standard_ZRS -> "Standard_ZRS"

type TargetRegion = {
    ExcludeFromLatest: bool option
    Name: Location
    RegionalReplicaCount: int option
    StorageAccountType: StorageAccountType option
} with

    member internal this.ToArmJson = {|
        excludeFromLatest = this.ExcludeFromLatest |> Option.toNullable
        name = this.Name.ArmValue
        regionalReplicaCount = this.RegionalReplicaCount |> Option.toNullable
        storageAccountType = this.StorageAccountType |> Option.map _.ArmValue |> Option.toObj
    |}

type GalleryApplicationVersion = {
    Name: GalleryApplicationVersionName
    GalleryApplicationName: GalleryApplicationName
    GalleryName: GalleryName
    Location: Location
    CustomActions: CustomAction list
    EnableHealthCheck: bool option
    EndOfLifeDate: DateTimeOffset option
    ExcludeFromLatest: bool option
    ManageActions: ManageActions
    ReplicaCount: int option
    ReplicationMode: ReplicationMode option
    Settings: UserArtifactSettings option
    Source: UserArtifactSource
    StorageAccountType: StorageAccountType option
    TargetRegions: TargetRegion list
    Tags: Map<string, string>
    Dependencies: Set<ResourceId>
} with

    interface IArmResource with
        member this.ResourceId =
            galleryApplicationVersions.resourceId (
                this.GalleryName.ResourceName
                / this.GalleryApplicationName.ResourceName
                / this.Name.ResourceName
            )

        member this.JsonModel = {|
            galleryApplicationVersions.Create(
                this.GalleryName.ResourceName
                / this.GalleryApplicationName.ResourceName
                / this.Name.ResourceName,
                this.Location,
                dependsOn = this.Dependencies,
                tags = this.Tags
            ) with
                properties = {|
                    publishingProfile = {|
                        customActions = this.CustomActions |> List.map _.ToArmJson
                        enableHealthCheck = this.EnableHealthCheck |> Option.toNullable
                        endOfLifeDate = this.EndOfLifeDate |> Option.map (_.ToString("yyyy-MM-dd")) |> Option.toObj
                        excludeFromLatest = this.ExcludeFromLatest |> Option.toNullable
                        manageActions = this.ManageActions.ToArmJson
                        replicaCount = this.ReplicaCount |> Option.toNullable
                        replicationMode = this.ReplicationMode |> Option.map _.ArmValue |> Option.toObj
                        settings =
                            this.Settings
                            |> Option.map _.ToArmJson
                            |> Option.defaultValue Unchecked.defaultof<_>
                        source = this.Source.ToArmJson
                        storageAccountType = this.StorageAccountType |> Option.map _.ArmValue |> Option.toObj
                        targetRegions = this.TargetRegions |> List.map _.ToArmJson
                    |}
                |}
        |}