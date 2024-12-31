[<AutoOpen>]
module Farmer.Builders.Gallery

open System
open Farmer
open Farmer.Arm
open Farmer.Image
open Farmer.GalleryValidation
open Farmer.Arm.Gallery
open Microsoft.FSharp.Core

type GalleryConfig = {
    Name: GalleryName
    Description: string option
    SharingProfile: SharingProfile option
    SoftDelete: FeatureFlag option
    Tags: Map<string, string>
    Dependencies: Set<ResourceId>
} with

    interface IBuilder with
        member this.ResourceId = galleries.resourceId this.Name.ResourceName

        member this.BuildResources location = [
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
    member _.Yield _ = {
        Name = GalleryName.Empty
        Description = None
        SharingProfile = None
        SoftDelete = None
        Tags = Map.empty
        Dependencies = Set.empty
    }

    [<CustomOperation "name">]
    member _.Name(config: GalleryConfig, name: string) = {
        config with
            Name = GalleryName.Create(name).OkValue
    }

    [<CustomOperation "description">]
    member _.Description(config: GalleryConfig, description) = {
        config with
            Description = Some description
    }

    [<CustomOperation "sharing_profile">]
    member _.SharingProfile(config: GalleryConfig, sharingProfile) = {
        config with
            SharingProfile = Some sharingProfile
    }

    [<CustomOperation "soft_delete">]
    member _.SoftDelete(config: GalleryConfig, flag: FeatureFlag) = { config with SoftDelete = Some flag }

    interface ITaggable<GalleryConfig> with
        member _.Add state tags = {
            state with
                Tags = state.Tags |> Map.merge tags
        }

    interface IDependable<GalleryConfig> with
        member _.Add state resources = {
            state with
                Dependencies = state.Dependencies + resources
        }

let gallery = GalleryBuilder()

type GalleryImageConfig = {
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
} with

    interface IBuilder with
        member this.ResourceId =
            galleryImages.resourceId (this.GalleryName.ResourceName, this.Name)

        member this.BuildResources location = [
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
    member _.Yield _ = {
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
    member _.Name(config: GalleryImageConfig, name) = { config with Name = ResourceName name }

    [<CustomOperation "gallery_name">]
    member _.GalleryName(config: GalleryImageConfig, galleryName) = {
        config with
            GalleryName = GalleryName.Create(galleryName).OkValue
    }

    [<CustomOperation "gallery">]
    member _.Gallery(config: GalleryImageConfig, galleryConfig: GalleryConfig) = {
        config with
            GalleryName = galleryConfig.Name
            Dependencies = config.Dependencies |> Set.add (galleryConfig :> IBuilder).ResourceId
    }

    [<CustomOperation "architecture">]
    member _.Architecture(config: GalleryImageConfig, architecture) = {
        config with
            Architecture = Some architecture
    }

    [<CustomOperation "description">]
    member _.Description(config: GalleryImageConfig, description) = {
        config with
            Description = Some description
    }

    [<CustomOperation "eula">]
    member _.Eula(config: GalleryImageConfig, eula) = { config with Eula = Some eula }

    [<CustomOperation "hyperv_generation">]
    member _.HyperVGeneration(config: GalleryImageConfig, hyperVGeneration) = {
        config with
            HyperVGeneration = Some hyperVGeneration
    }

    [<CustomOperation "gallery_image_identifier">]
    member _.Identifier(config: GalleryImageConfig, identifier) = {
        config with
            Identifier = Some identifier
    }

    [<CustomOperation "os_state">]
    member _.OsState(config: GalleryImageConfig, osState) = { config with OsState = Some osState }

    [<CustomOperation "os_type">]
    member _.OsType(config: GalleryImageConfig, osType) = { config with OsType = Some osType }

    [<CustomOperation "privacy_statement_uri">]
    member _.PrivacyStatementUri(config: GalleryImageConfig, privacyStatementUri: Uri) = {
        config with
            PrivacyStatementUri = Some privacyStatementUri
    }

    member this.PrivacyStatementUri(config, privacyStatementUri: string) =
        this.PrivacyStatementUri(config, Uri privacyStatementUri)

    [<CustomOperation "purchase_plan">]
    member _.PurchasePlan(config: GalleryImageConfig, purchasePlan) = {
        config with
            PurchasePlan = Some purchasePlan
    }

    [<CustomOperation "recommended_configuration">]
    member _.RecommendedConfiguration(config: GalleryImageConfig, recommended) = {
        config with
            Recommended = Some recommended
    }

    [<CustomOperation "recommended_memory">]
    member _.RecommendedMemory(config: GalleryImageConfig, min, max) =
        let existing =
            config.Recommended
            |> Option.defaultValue RecommendedMachineConfiguration.Default

        {
            config with
                Recommended =
                    Some {
                        existing with
                            MemoryMin = min
                            MemoryMax = max
                    }
        }

    [<CustomOperation "recommended_vcpu">]
    member _.RecommendedVCpu(config: GalleryImageConfig, min, max) =
        let existing =
            config.Recommended
            |> Option.defaultValue RecommendedMachineConfiguration.Default

        {
            config with
                Recommended =
                    Some {
                        existing with
                            VCpuMin = min
                            VCpuMax = max
                    }
        }

    [<CustomOperation "release_notes_uri">]
    member _.ReleaseNoteUri(config: GalleryImageConfig, releaseNoteUri) = {
        config with
            ReleaseNoteUri = Some releaseNoteUri
    }

    member this.ReleaseNoteUri(config, releaseNoteUri: string) =
        this.ReleaseNoteUri(config, Uri releaseNoteUri)

    interface ITaggable<GalleryImageConfig> with
        member _.Add state tags = {
            state with
                Tags = state.Tags |> Map.merge tags
        }

    interface IDependable<GalleryImageConfig> with
        member _.Add state resources = {
            state with
                Dependencies = state.Dependencies + resources
        }

let galleryImage = GalleryImageBuilder()

type GalleryApplicationActionParameterBuilder() =
    member _.Yield _ = {
        Description = None
        Name = ""
        Required = None
        ParameterType = ParameterType.String None
    }

    [<CustomOperation "name">]
    member _.Name(config: CustomActionParameter, name) = { config with Name = name }

    [<CustomOperation "description">]
    member _.Description(config: CustomActionParameter, description) = {
        config with
            Description = Some description
    }

    [<CustomOperation "required">]
    member _.Required(config: CustomActionParameter, req) = { config with Required = Some req }

    [<CustomOperation "parameter_type">]
    member _.ParameterType(config: CustomActionParameter, parameterType) = {
        config with
            ParameterType = parameterType
    }

    [<CustomOperation "default_value">]
    member _.DefaultValue(config: CustomActionParameter, defaultVal: string) = {
        config with
            ParameterType = ParameterType.String(Some defaultVal)
    }

let galleryAppActionParam = GalleryApplicationActionParameterBuilder()

type GalleryApplicationCustomActionBuilder() =
    member _.Yield _ = {
        Description = None
        Name = ""
        Parameters = []
        Script = ""
    }

    [<CustomOperation "name">]
    member _.Name(config: CustomAction, name) = { config with Name = name }

    [<CustomOperation "description">]
    member _.Description(config: CustomAction, description) = {
        config with
            Description = Some description
    }

    [<CustomOperation "add_parameters">]
    member _.AddParameters(config: CustomAction, parameters) = {
        config with
            Parameters = config.Parameters @ parameters
    }

    [<CustomOperation "script">]
    member _.Script(config: CustomAction, script) = { config with Script = script }

let galleryAppAction = GalleryApplicationCustomActionBuilder()

type GalleryApplicationConfig = {
    Name: GalleryApplicationName
    GalleryName: GalleryName
    CustomActions: CustomAction list
    Description: string option
    EndOfLifeDate: DateTimeOffset option
    Eula: string option
    OsType: OS option
    PrivacyStatementUri: Uri option
    ReleaseNoteUri: Uri option
    Tags: Map<string, string>
    Dependencies: Set<ResourceId>
} with

    interface IBuilder with
        member this.ResourceId =
            galleryApplications.resourceId (this.GalleryName.ResourceName / this.Name.ResourceName)

        member this.BuildResources location = [
            {
                Name = this.Name
                GalleryName = this.GalleryName
                Location = location
                CustomActions = this.CustomActions
                Description = this.Description
                EndOfLifeDate = this.EndOfLifeDate
                Eula = this.Eula
                OsType =
                    this.OsType
                    |> Option.defaultWith (fun _ -> raiseFarmer "Gallery application 'os_type' is required.")
                PrivacyStatementUri = this.PrivacyStatementUri
                ReleaseNoteUri = this.ReleaseNoteUri
                Tags = this.Tags
                Dependencies = this.Dependencies
            }
        ]

    interface ITaggable<GalleryApplicationConfig> with
        member _.Add state tags = {
            state with
                Tags = state.Tags |> Map.merge tags
        }

    interface IDependable<GalleryApplicationConfig> with
        member _.Add state resources = {
            state with
                Dependencies = state.Dependencies + resources
        }

type GalleryApplicationBuilder() =
    member _.Yield _ = {
        Name = GalleryApplicationName.Empty
        GalleryName = GalleryName.Empty
        CustomActions = []
        Description = None
        EndOfLifeDate = None
        Eula = None
        OsType = None
        PrivacyStatementUri = None
        ReleaseNoteUri = None
        Tags = Map.empty
        Dependencies = Set.empty
    }

    member _.Run(config: GalleryApplicationConfig) =
        if config.Name = GalleryApplicationName.Empty then
            raiseFarmer "Gallery application 'name' is required."

        if config.GalleryName = GalleryName.Empty then
            raiseFarmer "Gallery application 'gallery_name' is required."

        if config.OsType.IsNone then
            raiseFarmer "Gallery application 'os_type' is required."

        config

    [<CustomOperation "name">]
    member _.Name(config: GalleryApplicationConfig, name) = {
        config with
            Name = GalleryApplicationName.Create(name).OkValue
    }

    [<CustomOperation "gallery">]
    member _.Gallery(config: GalleryApplicationConfig, galleryConfig: GalleryConfig) = {
        config with
            GalleryName = galleryConfig.Name
            Dependencies = config.Dependencies |> Set.add (galleryConfig :> IBuilder).ResourceId
    }

    [<CustomOperation "gallery_name">]
    member _.GalleryName(config: GalleryApplicationConfig, name) = {
        config with
            GalleryName = GalleryName.Create(name).OkValue
    }

    [<CustomOperation "description">]
    member _.Description(config: GalleryApplicationConfig, description) = {
        config with
            Description = Some description
    }

    [<CustomOperation "add_custom_actions">]
    member _.AddParameters(config: GalleryApplicationConfig, customActions) = {
        config with
            CustomActions = config.CustomActions @ customActions
    }

    [<CustomOperation "end_of_life">]
    member _.EndOfLife(config: GalleryApplicationConfig, eolDate: DateTimeOffset) = {
        config with
            EndOfLifeDate = Some eolDate
    }

    [<CustomOperation "eula">]
    member _.Eula(config: GalleryApplicationConfig, eula) = { config with Eula = Some eula }

    [<CustomOperation "os_type">]
    member _.OsType(config: GalleryApplicationConfig, os) = { config with OsType = Some os }

    [<CustomOperation "privacy_statement_uri">]
    member _.PrivacyStatementUri(config: GalleryApplicationConfig, privacyStatementUri) = {
        config with
            PrivacyStatementUri = Some privacyStatementUri
    }

    [<CustomOperation "release_note_uri">]
    member _.ReleaseNoteUri(config: GalleryApplicationConfig, releaseNoteUri) = {
        config with
            ReleaseNoteUri = Some releaseNoteUri
    }

let galleryApp = GalleryApplicationBuilder()

type GalleryApplicationTargetVersionBuilder() =
    member _.Yield _ = {
        ExcludeFromLatest = None
        Name = Location.ResourceGroup
        RegionalReplicaCount = None
        StorageAccountType = None
    }

    [<CustomOperation "name">]
    member _.Name(targetRegion: TargetRegion, regionName: Location) = { targetRegion with Name = regionName }

    [<CustomOperation "exclude_from_latest">]
    member _.ExcludeFromLatest(targetRegion: TargetRegion, excludeFromLatest) = {
        targetRegion with
            ExcludeFromLatest = Some excludeFromLatest
    }

    [<CustomOperation "regional_replica_count">]
    member _.RegionalReplicaCount(targetRegion: TargetRegion, regionalReplicaCount: int) = {
        targetRegion with
            RegionalReplicaCount = Some regionalReplicaCount
    }

    [<CustomOperation "storage_account_type">]
    member _.StorageAccountType(targetRegion: TargetRegion, accountType) = {
        targetRegion with
            StorageAccountType = Some accountType
    }

let targetRegion = GalleryApplicationTargetVersionBuilder()

type GalleryApplicationVersionConfig = {
    Name: GalleryApplicationVersionName
    GalleryApplicationName: GalleryApplicationName
    GalleryName: GalleryName
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

    interface IBuilder with
        member this.ResourceId =
            galleryApplicationVersions.resourceId (
                this.GalleryName.ResourceName
                / this.GalleryApplicationName.ResourceName
                / this.Name.ResourceName
            )

        member this.BuildResources location = [
            {
                Name = this.Name
                GalleryApplicationName = this.GalleryApplicationName
                GalleryName = this.GalleryName
                Location = location
                CustomActions = this.CustomActions
                EnableHealthCheck = this.EnableHealthCheck
                EndOfLifeDate = this.EndOfLifeDate
                ExcludeFromLatest = this.ExcludeFromLatest
                ManageActions = this.ManageActions
                ReplicaCount = this.ReplicaCount
                ReplicationMode = this.ReplicationMode
                Settings = this.Settings
                Source = this.Source
                StorageAccountType = this.StorageAccountType
                TargetRegions = this.TargetRegions
                Tags = this.Tags
                Dependencies = this.Dependencies
            }
        ]

    interface ITaggable<GalleryApplicationConfig> with
        member _.Add state tags = {
            state with
                Tags = state.Tags |> Map.merge tags
        }

    interface IDependable<GalleryApplicationConfig> with
        member _.Add state resources = {
            state with
                Dependencies = state.Dependencies + resources
        }

type GalleryApplicationVersionBuilder() =
    member _.Yield _ = {
        Name = GalleryApplicationVersionName.Empty
        GalleryApplicationName = GalleryApplicationName.Empty
        GalleryName = GalleryName.Empty
        CustomActions = []
        EnableHealthCheck = None
        EndOfLifeDate = None
        ExcludeFromLatest = None
        ManageActions = ManageActions.Empty
        ReplicaCount = None
        ReplicationMode = None
        Settings = None
        Source = UserArtifactSource.Empty
        StorageAccountType = None
        TargetRegions = []
        Tags = Map.empty
        Dependencies = Set.empty
    }

    member _.Run(config: GalleryApplicationVersionConfig) =
        if config.Name = GalleryApplicationVersionName.Empty then
            raiseFarmer "Gallery application version 'name' is required."

        if config.GalleryApplicationName = GalleryApplicationName.Empty then
            raiseFarmer "Gallery application version 'application_name' is required."

        if config.GalleryName = GalleryName.Empty then
            raiseFarmer "Gallery application version 'gallery_name' is required."

        if String.IsNullOrEmpty config.ManageActions.Install then
            raiseFarmer "Gallery application version 'install_action' is required."

        if String.IsNullOrEmpty config.ManageActions.Remove then
            raiseFarmer "Gallery application version 'remove_action' is required."

        if config.Source.MediaLink.IsUnc then
            raiseFarmer "Gallery application version 'source_media_link' is required."

        config

    [<CustomOperation "name">]
    member _.Name(config: GalleryApplicationVersionConfig, name) = {
        config with
            Name = GalleryApplicationVersionName.Create(name).OkValue
    }

    [<CustomOperation "gallery_app">]
    member _.GalleryApplicationName(config: GalleryApplicationVersionConfig, name) = {
        config with
            GalleryApplicationName = GalleryApplicationName.Create(name).OkValue
    }

    member _.GalleryApplicationName
        (config: GalleryApplicationVersionConfig, galleryAppConfig: GalleryApplicationConfig)
        =
        {
            config with
                GalleryApplicationName = galleryAppConfig.Name
                Dependencies = config.Dependencies |> Set.add (galleryAppConfig :> IBuilder).ResourceId
        }

    [<CustomOperation "gallery">]
    member _.Gallery(config: GalleryApplicationVersionConfig, galleryConfig: GalleryConfig) = {
        config with
            GalleryName = galleryConfig.Name
            Dependencies = config.Dependencies |> Set.add (galleryConfig :> IBuilder).ResourceId
    }

    member _.Gallery(config: GalleryApplicationVersionConfig, name) = {
        config with
            GalleryName = GalleryName.Create(name).OkValue
    }

    [<CustomOperation "add_custom_actions">]
    member _.AddParameters(config: GalleryApplicationVersionConfig, customActions) = {
        config with
            CustomActions = config.CustomActions @ customActions
    }

    [<CustomOperation "end_of_life">]
    member _.EndOfLife(config: GalleryApplicationVersionConfig, eolDate: DateTimeOffset) = {
        config with
            EndOfLifeDate = Some eolDate
    }

    [<CustomOperation "exclude_from_latest">]
    member _.ExcludeFromLatest(config: GalleryApplicationVersionConfig, exclude) = {
        config with
            ExcludeFromLatest = Some exclude
    }

    [<CustomOperation "install_action">]
    member _.InstallAction(config: GalleryApplicationVersionConfig, install) = {
        config with
            ManageActions = {
                config.ManageActions with
                    Install = install
            }
    }

    [<CustomOperation "remove_action">]
    member _.RemoveAction(config: GalleryApplicationVersionConfig, remove) = {
        config with
            ManageActions = {
                config.ManageActions with
                    Remove = remove
            }
    }

    [<CustomOperation "source_media_link">]
    member _.SourceMediaLink(config: GalleryApplicationVersionConfig, sourceMediaLink) = {
        config with
            Source.MediaLink = Uri sourceMediaLink
    }

    member _.SourceMediaLink(config: GalleryApplicationVersionConfig, sourceMediaLink) = {
        config with
            Source.MediaLink = sourceMediaLink
    }

    [<CustomOperation "default_configuration_link">]
    member _.ConfigurationMediaLink(config: GalleryApplicationVersionConfig, defaultConfigLink) = {
        config with
            Source.DefaultConfigurationLink = Some(Uri defaultConfigLink)
    }

    member _.ConfigurationMediaLink(config: GalleryApplicationVersionConfig, defaultConfigLink) = {
        config with
            Source.DefaultConfigurationLink = Some defaultConfigLink
    }

    [<CustomOperation "replica_count">]
    member _.ReplicaCount(config: GalleryApplicationVersionConfig, replicaCount) = {
        config with
            ReplicaCount = Some replicaCount
    }

    [<CustomOperation "replication_mode">]
    member _.ReplicationMode(config: GalleryApplicationVersionConfig, mode) = {
        config with
            ReplicationMode = Some mode
    }

    [<CustomOperation "config_file_name">]
    member _.ConfigFileName(config: GalleryApplicationVersionConfig, configFileName) =
        let settings =
            match config.Settings with
            | None -> {
                ConfigFileName = Some configFileName
                PackageFileName = None
              }
            | Some settings -> {
                settings with
                    ConfigFileName = Some configFileName
              }

        { config with Settings = Some settings }

    [<CustomOperation "package_file_name">]
    member _.PackageFileName(config: GalleryApplicationVersionConfig, packageFileName) =
        let settings =
            match config.Settings with
            | None -> {
                ConfigFileName = None
                PackageFileName = Some packageFileName
              }
            | Some settings -> {
                settings with
                    PackageFileName = Some packageFileName
              }

        { config with Settings = Some settings }

    [<CustomOperation "storage_account_type">]
    member _.StorageAccountType(config: GalleryApplicationVersionConfig, storageAcctType) = {
        config with
            StorageAccountType = Some storageAcctType
    }

    [<CustomOperation "add_target_regions">]
    member _.AddTargetRegions(config: GalleryApplicationVersionConfig, targetRegions) = {
        config with
            TargetRegions = config.TargetRegions @ targetRegions
    }

let galleryAppVersion = GalleryApplicationVersionBuilder()