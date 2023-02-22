[<AutoOpen>]
module Farmer.Builders.Gallery

open System
open Farmer
open Farmer.Image
open Farmer.GalleryValidation
open Farmer.Arm.Gallery

type GalleryConfig =
    {
        Name: GalleryName
        Description: string option
        SharingProfile: SharingProfile option
        SoftDelete: FeatureFlag option
        Tags: Map<string, string>
        Dependencies: Set<ResourceId>
    }

    interface IBuilder with
        member this.ResourceId = galleries.resourceId this.Name.ResourceName

        member this.BuildResources location =
            [
                {
                    Name = this.Name
                    Location = location
                    Description = this.Description
                    SharingProfile = this.SharingProfile
                    SoftDeletePolicy =
                        this.SoftDelete
                        |> Option.map (fun flag -> { IsSoftDeleteEnabled = flag.AsBoolean })
                    Tags = this.Tags
                    Dependencies = this.Dependencies
                }
            ]

type GalleryBuilder() =
    member _.Yield _ =
        {
            Name = GalleryName.Empty
            Description = None
            SharingProfile = None
            SoftDelete = None
            Tags = Map.empty
            Dependencies = Set.empty
        }

    [<CustomOperation "name">]
    member _.Name(config: GalleryConfig, name: string) =
        { config with
            Name = GalleryName.Create(name).OkValue
        }

    [<CustomOperation "description">]
    member _.Description(config: GalleryConfig, description) =
        { config with
            Description = Some description
        }

    [<CustomOperation "sharing_profile">]
    member _.SharingProfile(config: GalleryConfig, sharingProfile) =
        { config with
            SharingProfile = Some sharingProfile
        }

    [<CustomOperation "soft_delete">]
    member _.SoftDelete(config: GalleryConfig, flag: FeatureFlag) = { config with SoftDelete = Some flag }

    interface ITaggable<GalleryConfig> with
        member _.Add state tags =
            { state with
                Tags = state.Tags |> Map.merge tags
            }

    interface IDependable<GalleryConfig> with
        member _.Add state resources =
            { state with
                Dependencies = state.Dependencies + resources
            }

let gallery = GalleryBuilder()

type GalleryImageConfig =
    {
        Name: ResourceName
        GalleryName: GalleryName
        Architecture: Architecture option
        Description: string option
        Eula: string option
        HyperVGeneration: HyperVGeneration option
        Identifier: GalleryImageIdentifier option
        OsState: OsState option
        OsType: OS option
        PrivacyStatementUri: Uri option
        PurchasePlan: ImagePurchasePlan option
        Recommended: RecommendedMachineConfiguration option
        ReleaseNoteUri: Uri option
        Tags: Map<string, string>
        Dependencies: Set<ResourceId>
    }

    interface IBuilder with
        member this.ResourceId =
            galleryImages.resourceId (this.GalleryName.ResourceName, this.Name)

        member this.BuildResources location =
            [
                {
                    Name = this.Name
                    GalleryName = this.GalleryName
                    Location = location
                    Architecture = this.Architecture
                    Description = this.Description |> Option.toObj
                    Eula = this.Eula
                    HyperVGeneration =
                        this.HyperVGeneration
                        |> Option.defaultWith (fun _ -> raiseFarmer "Gallery image 'hyperv_generation' is required.")
                    Identifier =
                        this.Identifier
                        |> Option.defaultWith (fun _ -> raiseFarmer "Gallery image 'identifier' is required.")
                    OsState = this.OsState |> Option.defaultValue Generalized
                    OsType =
                        this.OsType
                        |> Option.defaultWith (fun _ -> raiseFarmer "Gallery image 'os_type' is required.")
                    PrivacyStatementUri = this.PrivacyStatementUri
                    PurchasePlan = this.PurchasePlan
                    Recommended = this.Recommended |> Option.defaultValue RecommendedMachineConfiguration.Default
                    ReleaseNoteUri = this.ReleaseNoteUri
                    Tags = this.Tags
                    Dependencies = this.Dependencies
                }
            ]

type GalleryImageBuilder() =
    member _.Yield _ =
        {
            Name = ResourceName.Empty
            GalleryName = GalleryName.Empty
            Architecture = None
            Description = None
            Eula = None
            HyperVGeneration = None
            Identifier = None
            OsState = None
            OsType = None
            PrivacyStatementUri = None
            PurchasePlan = None
            Recommended = None
            ReleaseNoteUri = None
            Tags = Map.empty
            Dependencies = Set.empty
        }

    member _.Run(config: GalleryImageConfig) =
        if config.Name = ResourceName.Empty then
            raiseFarmer "Gallery image 'name' is required."

        if config.GalleryName = GalleryName.Empty then
            raiseFarmer "Gallery image 'gallery_name' is required."

        if config.HyperVGeneration.IsNone then
            raiseFarmer "Gallery image 'hyperv_generation' is required."

        if config.Identifier.IsNone then
            raiseFarmer "Gallery image 'gallery_image_identifier' is required."

        if config.OsType.IsNone then
            raiseFarmer "Gallery image 'os_type' is required."

        config

    [<CustomOperation "name">]
    member _.Name(config: GalleryImageConfig, name) =
        { config with Name = ResourceName name }

    [<CustomOperation "gallery_name">]
    member _.GalleryName(config: GalleryImageConfig, galleryName) =
        { config with
            GalleryName = GalleryName.Create(galleryName).OkValue
        }

    [<CustomOperation "gallery">]
    member _.Gallery(config: GalleryImageConfig, galleryConfig: GalleryConfig) =
        { config with
            GalleryName = galleryConfig.Name
            Dependencies = config.Dependencies |> Set.add (galleryConfig :> IBuilder).ResourceId
        }

    [<CustomOperation "architecture">]
    member _.Architecture(config: GalleryImageConfig, architecture) =
        { config with
            Architecture = Some architecture
        }

    [<CustomOperation "description">]
    member _.Description(config: GalleryImageConfig, description) =
        { config with
            Description = Some description
        }

    [<CustomOperation "eula">]
    member _.Eula(config: GalleryImageConfig, eula) = { config with Eula = Some eula }

    [<CustomOperation "hyperv_generation">]
    member _.HyperVGeneration(config: GalleryImageConfig, hyperVGeneration) =
        { config with
            HyperVGeneration = Some hyperVGeneration
        }

    [<CustomOperation "gallery_image_identifier">]
    member _.Identifier(config: GalleryImageConfig, identifier) =
        { config with
            Identifier = Some identifier
        }

    [<CustomOperation "os_state">]
    member _.OsState(config: GalleryImageConfig, osState) = { config with OsState = Some osState }

    [<CustomOperation "os_type">]
    member _.OsType(config: GalleryImageConfig, osType) = { config with OsType = Some osType }

    [<CustomOperation "privacy_statement_uri">]
    member _.PrivacyStatementUri(config: GalleryImageConfig, privacyStatementUri: Uri) =
        { config with
            PrivacyStatementUri = Some privacyStatementUri
        }

    member this.PrivacyStatementUri(config, privacyStatementUri: string) =
        this.PrivacyStatementUri(config, Uri privacyStatementUri)

    [<CustomOperation "purchase_plan">]
    member _.PurchasePlan(config: GalleryImageConfig, purchasePlan) =
        { config with
            PurchasePlan = Some purchasePlan
        }

    [<CustomOperation "recommended_configuration">]
    member _.RecommendedConfiguration(config: GalleryImageConfig, recommended) =
        { config with
            Recommended = Some recommended
        }

    [<CustomOperation "recommended_memory">]
    member _.RecommendedMemory(config: GalleryImageConfig, min, max) =
        let existing =
            config.Recommended
            |> Option.defaultValue RecommendedMachineConfiguration.Default

        { config with
            Recommended =
                Some
                    { existing with
                        MemoryMin = min
                        MemoryMax = max
                    }
        }

    [<CustomOperation "recommended_vcpu">]
    member _.RecommendedVCpu(config: GalleryImageConfig, min, max) =
        let existing =
            config.Recommended
            |> Option.defaultValue RecommendedMachineConfiguration.Default

        { config with
            Recommended =
                Some
                    { existing with
                        VCpuMin = min
                        VCpuMax = max
                    }
        }

    [<CustomOperation "release_notes_uri">]
    member _.ReleaseNoteUri(config: GalleryImageConfig, releaseNoteUri) =
        { config with
            ReleaseNoteUri = Some releaseNoteUri
        }

    member this.ReleaseNoteUri(config, releaseNoteUri: string) =
        this.ReleaseNoteUri(config, Uri releaseNoteUri)

    interface ITaggable<GalleryImageConfig> with
        member _.Add state tags =
            { state with
                Tags = state.Tags |> Map.merge tags
            }

    interface IDependable<GalleryImageConfig> with
        member _.Add state resources =
            { state with
                Dependencies = state.Dependencies + resources
            }

let galleryImage = GalleryImageBuilder()
