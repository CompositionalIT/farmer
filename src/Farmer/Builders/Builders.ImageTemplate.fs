[<AutoOpen>]
module Farmer.Builders.ImageTemplate

open Farmer
open Farmer.Arm.ImageTemplate
open Farmer.Arm.Gallery
open Farmer.Identity

type ImageTemplateConfig = {
    Name: ResourceName
    Identity: Identity.ManagedIdentity
    BuildTimeoutInMinutes: int option
    Source: ImageBuilderSource option
    Customize: Customizer list
    Distribute: Distibutor list
    Dependencies: Set<ResourceId>
    Tags: Map<string, string>
} with

    interface IBuilder with
        member this.ResourceId = imageTemplates.resourceId this.Name

        member this.BuildResources location = [
            {
                Name = this.Name
                Location = location
                Identity = this.Identity
                BuildTimeoutInMinutes = this.BuildTimeoutInMinutes
                Source =
                    match this.Source with
                    | Some source -> source
                    | None -> raiseFarmer "Image template requires a 'source'"
                Customize = this.Customize
                Distribute = this.Distribute
                Dependencies = this.Dependencies
                Tags = this.Tags
            }
        ]

type ImageTemplateBuilder() =

    member _.Yield _ = {
        Name = ResourceName.Empty
        Identity = ManagedIdentity.Empty
        BuildTimeoutInMinutes = None
        Source = None
        Customize = []
        Distribute = []
        Dependencies = Set.empty
        Tags = Map.empty
    }

    [<CustomOperation "name">]
    member _.Name(config: ImageTemplateConfig, name: string) = { config with Name = ResourceName name }

    [<CustomOperation "build_timeout">]
    member _.BuildTimeout(config: ImageTemplateConfig, timeoutMinutes: int<Minutes>) = {
        config with
            BuildTimeoutInMinutes = Some(timeoutMinutes / 1<Minutes>)
    }

    member this.BuildTimeout(config, timeout: System.TimeSpan) =
        this.BuildTimeout(config, int timeout.TotalMinutes * 1<Minutes>)

    [<CustomOperation "source_platform_image">]
    member _.PlatformImageSource(config: ImageTemplateConfig, imageSource: PlatformImageSource) = {
        config with
            Source = Some(ImageBuilderSource.Platform imageSource)
    }

    member this.PlatformImageSource(config: ImageTemplateConfig, image: Vm.ImageDefinition) =
        let imageSource = {
            ImageIdentifier = {
                Publisher = image.Publisher.ArmValue
                Offer = image.Offer.ArmValue
                Sku = image.Sku.ArmValue
            }
            PlanInfo = None
            Version = "latest"
        }

        this.PlatformImageSource(config, imageSource)

    [<CustomOperation "source_managed_image">]
    member _.ManagedImageSource(config: ImageTemplateConfig, imageId: ResourceId) = {
        config with
            Source = Some(ImageBuilderSource.Managed { ImageId = imageId })
    }

    [<CustomOperation "source_shared_image_version">]
    member _.SharedImageVersionSource(config: ImageTemplateConfig, imageId: ResourceId) = {
        config with
            Source = Some(ImageBuilderSource.SharedVersion { ImageId = imageId })
    }

    [<CustomOperation "add_customizers">]
    member _.AddCustomizer(config: ImageTemplateConfig, customizers) = {
        config with
            Customize = config.Customize @ customizers
    }

    [<CustomOperation "add_distributors">]
    member _.AddCustomizer(config: ImageTemplateConfig, distributors) = {
        config with
            Distribute = config.Distribute @ distributors
    }

    interface IIdentity<ImageTemplateConfig> with
        member _.Add state updater = {
            state with
                Identity = updater state.Identity
        }

    interface ITaggable<ImageTemplateConfig> with
        member _.Add state tags = {
            state with
                Tags = state.Tags |> Map.merge tags
        }

    interface IDependable<ImageTemplateConfig> with
        member _.Add state resources = {
            state with
                Dependencies = state.Dependencies + resources
        }

let imageTemplate = ImageTemplateBuilder()

type FileCustomizerBuilder() =
    member _.Yield _ = {
        Name = null
        SourceUri = null
        Destination = null
        Sha256Checksum = None
    }

    member _.Run(customizer: FileCustomizer) =
        if isNull customizer.SourceUri then
            raiseFarmer "fileCustomizer must have 'source_uri'"

        if isNull customizer.Destination then
            raiseFarmer "fileCustomizer must have 'destination'"

        Customizer.File customizer

    [<CustomOperation "name">]
    member _.Name(customizer: FileCustomizer, name) = { customizer with Name = name }

    [<CustomOperation "source_uri">]
    member _.SourceUri(customizer: FileCustomizer, uri) = { customizer with SourceUri = uri }

    member this.SourceUri(customizer: FileCustomizer, uri: string) =
        this.SourceUri(customizer, System.Uri uri)

    [<CustomOperation "destination">]
    member _.Destination(customizer: FileCustomizer, destination) = {
        customizer with
            Destination = destination
    }

    [<CustomOperation "checksum">]
    member _.Checksum(customizer: FileCustomizer, checksum) = {
        customizer with
            Sha256Checksum = Some checksum
    }

/// File customizer for downloading small files to the image, < 20MB. For larger files, use a shell or inline command
/// in a shell or powershell customizer.
let fileCustomizer = FileCustomizerBuilder()

type ShellCustomizerBuilder() =
    member _.Yield _ = { Name = null; Inline = [] }

    member _.Run(customizer: ShellCustomizer) = Customizer.Shell customizer

    [<CustomOperation "name">]
    member _.Name(customizer: ShellCustomizer, name) = { customizer with Name = name }

    [<CustomOperation "inline_statements">]
    member _.InlineStatements(customizer: ShellCustomizer, inlineStatements) = {
        customizer with
            Inline = inlineStatements
    }

/// Shell customizer for running shell commands defined as inline strings.
let shellCustomizer = ShellCustomizerBuilder()

type ShellScriptCustomizerBuilder() =
    member _.Yield _ = {
        Name = null
        ScriptUri = null
        Sha256Checksum = None
    }

    member _.Run(customizer: ShellScriptCustomizer) =
        if isNull customizer.ScriptUri then
            raiseFarmer "shellScriptCustomizer must have 'script_uri'"

        Customizer.ShellScript customizer

    [<CustomOperation "name">]
    member _.InlineShell(customizer: ShellScriptCustomizer, name) = { customizer with Name = name }

    [<CustomOperation "script_uri">]
    member _.ScriptUri(customizer: ShellScriptCustomizer, uri) = { customizer with ScriptUri = uri }

    member this.ScriptUri(customizer: ShellScriptCustomizer, uri: string) =
        this.ScriptUri(customizer, System.Uri uri)

    [<CustomOperation "checksum">]
    member _.Checksum(customizer: ShellScriptCustomizer, checksum) = {
        customizer with
            Sha256Checksum = Some checksum
    }

/// Shell script customizer to download a shell script to execute.
let shellScriptCustomizer = ShellScriptCustomizerBuilder()

type PowerShellCustomizerBuilder() =
    member _.Yield _ = {
        Name = null
        Inline = []
        RunAsElevated = false
        RunAsSystem = false
        ValidExitCodes = []
    }

    member _.Run(customizer: PowerShellCustomizer) = Customizer.PowerShell customizer

    [<CustomOperation "name">]
    member _.Name(customizer: PowerShellCustomizer, name) = { customizer with Name = name }

    [<CustomOperation "inline_statements">]
    member _.InlineShell(customizer: PowerShellCustomizer, inlineStatements) = {
        customizer with
            Inline = inlineStatements
    }

    [<CustomOperation "run_as_elevated">]
    member _.RunAsElevated(customizer: PowerShellCustomizer, runAsElevated) = {
        customizer with
            RunAsElevated = runAsElevated
    }

    [<CustomOperation "run_as_system">]
    member _.RunAsSystem(customizer: PowerShellCustomizer, runAsSystem) = {
        customizer with
            RunAsSystem = runAsSystem
    }

    [<CustomOperation "valid_exit_codes">]
    member _.ValidExitCodes(customizer: PowerShellCustomizer, validExitCodes) = {
        customizer with
            ValidExitCodes = customizer.ValidExitCodes @ validExitCodes
    }

/// PowerShell customizer for running PowerShell commands on Windows images defined as inline strings.
let powerShellCustomizer: PowerShellCustomizerBuilder =
    PowerShellCustomizerBuilder()

type PowerShellScriptCustomizerBuilder() =
    member _.Yield _ = {
        Name = null
        ScriptUri = null
        Sha256Checksum = None
        RunAsElevated = false
        RunAsSystem = false
        ValidExitCodes = []
    }

    member _.Run(customizer: PowerShellScriptCustomizer) = Customizer.PowerShellScript customizer

    [<CustomOperation "name">]
    member _.Name(customizer: PowerShellScriptCustomizer, name) = { customizer with Name = name }

    [<CustomOperation "run_as_elevated">]
    member _.RunAsElevated(customizer: PowerShellScriptCustomizer, runAsElevated) = {
        customizer with
            RunAsElevated = runAsElevated
    }

    [<CustomOperation "run_as_system">]
    member _.RunAsSystem(customizer: PowerShellScriptCustomizer, runAsSystem) = {
        customizer with
            RunAsSystem = runAsSystem
    }

    [<CustomOperation "valid_exit_codes">]
    member _.ValidExitCodes(customizer: PowerShellScriptCustomizer, validExitCodes) = {
        customizer with
            ValidExitCodes = customizer.ValidExitCodes @ validExitCodes
    }

    [<CustomOperation "script_uri">]
    member _.ScriptUri(customizer: PowerShellScriptCustomizer, uri) = { customizer with ScriptUri = uri }

    member this.ScriptUri(customizer: PowerShellScriptCustomizer, uri: string) =
        this.ScriptUri(customizer, System.Uri uri)

    [<CustomOperation "checksum">]
    member _.Checksum(customizer: PowerShellScriptCustomizer, checksum) = {
        customizer with
            Sha256Checksum = Some checksum
    }

/// PowerShell script customizer for downloading a PowerShell script to run on Windows images.
let powerShellScriptCustomizer = PowerShellScriptCustomizerBuilder()

type WindowsRestartCustomizerBuilder() =
    member _.Yield _ = {
        RestartCommand = None
        RestartCheckCommand = None
        RestartTimeout = None
    }

    member _.Run(customizer: WindowsRestartCustomizer) = Customizer.WindowsRestart customizer

    [<CustomOperation "restart_command">]
    member _.RestartCommand(customizer: WindowsRestartCustomizer, restartCommand) = {
        customizer with
            RestartCommand = Some restartCommand
    }

    [<CustomOperation "restart_check_command">]
    member _.RestartCheckCommand(customizer: WindowsRestartCustomizer, restartCheckCommand) = {
        customizer with
            RestartCheckCommand = Some restartCheckCommand
    }

    [<CustomOperation "restart_timeout">]
    member _.RestartTimeout(customizer: WindowsRestartCustomizer, restartTimeout) = {
        customizer with
            RestartTimeout = Some restartTimeout
    }

/// Windows Restart Customizer to restart Windows while building the image.
let windowsRestartCustomizer = WindowsRestartCustomizerBuilder()

type WindowsUpdateCustomizerBuilder() =
    member _.Yield _ = {
        SearchCriteria = None
        Filters = []
        UpdateLimit = None
    }

    member _.Run(customizer: WindowsUpdateCustomizer) = Customizer.WindowsUpdate customizer

    [<CustomOperation "search_criteria">]
    member _.SearchCriteria(customizer: WindowsUpdateCustomizer, searchCriteria) = {
        customizer with
            SearchCriteria = Some searchCriteria
    }

    [<CustomOperation "filters">]
    member _.Filters(customizer: WindowsUpdateCustomizer, filters) = {
        customizer with
            Filters = customizer.Filters @ filters
    }

    [<CustomOperation "update_limit">]
    member _.Destination(customizer: WindowsUpdateCustomizer, updateLimit) = {
        customizer with
            UpdateLimit = Some updateLimit
    }

/// Windows Update Customizer to install Windows updates while building the image.
let windowsUpdateCustomizer = WindowsUpdateCustomizerBuilder()

type ManagedImageDistributorBuilder() =
    member _.Yield _ = {
        ImageId = images.resourceId ResourceName.Empty
        RunOutputName = "managed-image-run"
        Location = null
        ArtifactTags = Map.empty
    }

    member _.Run(distributor: ManagedImageDistributor) =
        if distributor.ImageId = images.resourceId ResourceName.Empty then
            raiseFarmer "Must set 'image_id' for managedImageDistributor"

        if System.String.IsNullOrEmpty distributor.Location then
            raiseFarmer "Must set 'location' for managedImageDistributor"

        Distibutor.ManagedImage distributor

    [<CustomOperation "image_id">]
    member _.ImageId(distibutor: ManagedImageDistributor, imageId) = { distibutor with ImageId = imageId }

    [<CustomOperation "image_name">]
    member _.Image(distibutor: ManagedImageDistributor, imageName: ResourceName) = {
        distibutor with
            ImageId = images.resourceId imageName
    }

    [<CustomOperation "location">]
    member _.Location(distibutor: ManagedImageDistributor, location: Location) = {
        distibutor with
            Location = location.ArmValue
    }

    [<CustomOperation "run_output_name">]
    member _.RunOutputName(distibutor: ManagedImageDistributor, runOutputName) = {
        distibutor with
            RunOutputName = runOutputName
    }

    [<CustomOperation "add_tags">]
    member _.AddTags(distibutor: ManagedImageDistributor, tags) = {
        distibutor with
            ArtifactTags = distibutor.ArtifactTags |> Map.merge tags
    }

/// Managed Image Distributor to copy the image that is built to a managed image.
let managedImageDistributor = ManagedImageDistributorBuilder()

type SharedImageDistributorBuilder() =
    let emptyGalleryImageId =
        ResourceType("Microsoft.Compute/galleries/images", "2020-09-30")
            .resourceId (ResourceName.Empty, ResourceName.Empty)

    member _.Yield _ = {
        GalleryImageId = emptyGalleryImageId
        RunOutputName = "shared-image-run"
        ReplicationRegions = List.empty
        ExcludeFromLatest = None
        ArtifactTags = Map.empty
    }

    member _.Run(distributor: SharedImageDistributor) =
        if distributor.GalleryImageId = emptyGalleryImageId then
            raiseFarmer "Must set 'gallery_image_id' for sharedImageDistributor"

        if distributor.ReplicationRegions.IsEmpty then
            raiseFarmer "Must 'add_replication_regions' for sharedImageDistributor"

        Distibutor.SharedImage distributor

    [<CustomOperation "gallery_image_id">]
    member _.Image(distibutor: SharedImageDistributor, galleryImageId: ResourceId) = {
        distibutor with
            GalleryImageId = galleryImageId
    }

    [<CustomOperation "add_replication_regions">]
    member _.Image(distibutor: SharedImageDistributor, locations: Location list) = {
        distibutor with
            ReplicationRegions = distibutor.ReplicationRegions @ locations
    }

    [<CustomOperation "exclude_from_latest">]
    member _.Image(distibutor: SharedImageDistributor, excludeFromLatest) = {
        distibutor with
            ExcludeFromLatest = Some excludeFromLatest
    }

    [<CustomOperation "run_output_name">]
    member _.RunOutputName(distibutor: SharedImageDistributor, runOutputName) = {
        distibutor with
            RunOutputName = runOutputName
    }

    [<CustomOperation "add_tags">]
    member _.AddTags(distibutor: SharedImageDistributor, tags) = {
        distibutor with
            ArtifactTags = distibutor.ArtifactTags |> Map.merge tags
    }

/// Shared Image Distributor to copy the image that is built to a gallery of shared images.
let sharedImageDistributor = SharedImageDistributorBuilder()

type VhdDistributorBuilder() =
    member _.Yield _ = {
        RunOutputName = "vhd-run"
        ArtifactTags = Map.empty
    }

    member _.Run(distributor: VhdDistributor) = Distibutor.VHD distributor

    [<CustomOperation "run_output_name">]
    member _.RunOutputName(distibutor: VhdDistributor, runOutputName) = {
        distibutor with
            RunOutputName = runOutputName
    }

    [<CustomOperation "add_tags">]
    member _.AddTags(distibutor: VhdDistributor, tags) = {
        distibutor with
            ArtifactTags = distibutor.ArtifactTags |> Map.merge tags
    }

/// VHD Distributor to simply leave the virtual hard disk that is built in a generated storage account.
let vhdDistributor = VhdDistributorBuilder()
