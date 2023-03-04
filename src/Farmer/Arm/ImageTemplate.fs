[<AutoOpen>]
module Farmer.Arm.ImageTemplate

open System
open Farmer
open Farmer.Arm.Gallery

let imageTemplates =
    ResourceType("Microsoft.VirtualMachineImages/imageTemplates", "2022-02-14")

let images = ResourceType("Microsoft.Compute/images", "2022-08-01")

type PlatformImageSource =
    {
        ImageIdentifier: GalleryImageIdentifier
        PlanInfo: ImagePurchasePlan option
        Version: string
    }

    member this.JsonModel =
        {|
            ``type`` = "PlatformImage"
            offer = this.ImageIdentifier.Offer
            planInfo =
                match this.PlanInfo with
                | Some plan ->
                    {|
                        planName = plan.PlanName
                        planProduct = plan.PlanProduct
                        planPublisher = plan.PlanPublisher
                    |}
                    :> obj
                | None -> null
            publisher = this.ImageIdentifier.Publisher
            sku = this.ImageIdentifier.Sku
            version =
                if String.IsNullOrEmpty this.Version then
                    "latest"
                else
                    this.Version
        |}
        :> obj

type ManagedImageSource =
    {
        ImageId: ResourceId
    }

    member this.JsonModel =
        {|
            ``type`` = "ManagedImage"
            imageId = this.ImageId.Eval()
        |}
        :> obj

type SharedImageVersionSource =
    {
        ImageId: ResourceId
    }

    member this.JsonModel =
        {|
            ``type`` = "SharedImageVersion"
            imageId = this.ImageId.Eval()
        |}
        :> obj

[<RequireQualifiedAccess>]
type ImageBuilderSource =
    | Platform of PlatformImageSource
    | Managed of ManagedImageSource
    | SharedVersion of SharedImageVersionSource

    member this.JsonModel =
        match this with
        | Platform src -> src.JsonModel
        | Managed src -> src.JsonModel
        | SharedVersion src -> src.JsonModel

type FileCustomizer =
    {
        Name: string
        Destination: string
        SourceUri: Uri
        Sha256Checksum: string option
    }

    member this.JsonModel =
        {|
            ``type`` = "File"
            name = this.Name
            destination = this.Destination
            sha256Checksum = this.Sha256Checksum |> Option.toObj
            sourceUri = this.SourceUri.AbsoluteUri
        |}
        :> obj

type ShellScriptCustomizer =
    {
        Name: string
        ScriptUri: Uri
        Sha256Checksum: string option
    }

    member this.JsonModel =
        {|
            ``type`` = "Shell"
            name = this.Name
            scriptUri = this.ScriptUri.AbsoluteUri
            sha256Checksum = this.Sha256Checksum |> Option.toObj
        |}
        :> obj

type ShellCustomizer =
    {
        Name: string
        Inline: string list
    }

    member this.JsonModel =
        {|
            ``type`` = "Shell"
            ``inline`` = this.Inline
            name = this.Name
        |}
        :> obj

type PowerShellScriptCustomizer =
    {
        Name: string
        ScriptUri: Uri
        Sha256Checksum: string option
        RunAsSystem: bool
        RunAsElevated: bool
        ValidExitCodes: int list
    }

    member this.JsonModel =
        {|
            ``type`` = "PowerShell"
            name = this.Name
            scriptUri = this.ScriptUri.AbsoluteUri
            sha256Checksum = this.Sha256Checksum |> Option.toObj
            runAsSystem = this.RunAsSystem
            runAsElevated = this.RunAsElevated
            validExitCodes =
                if this.ValidExitCodes.IsEmpty then
                    null
                else
                    ResizeArray(this.ValidExitCodes)
        |}
        :> obj

type PowerShellCustomizer =
    {
        Name: string
        Inline: string list
        RunAsSystem: bool
        RunAsElevated: bool
        ValidExitCodes: int list
    }

    member this.JsonModel =
        {|
            ``type`` = "PowerShell"
            ``inline`` = this.Inline
            name = this.Name
            runAsSystem = this.RunAsSystem
            runAsElevated = this.RunAsElevated
            validExitCodes =
                if this.ValidExitCodes.IsEmpty then
                    null
                else
                    ResizeArray(this.ValidExitCodes)
        |}
        :> obj

type WindowsRestartCustomizer =
    {
        RestartCheckCommand: string option
        RestartCommand: string option
        RestartTimeout: string option // 5m for 5 minutes, 2h for two hours
    }

    member this.JsonModel =
        {|
            ``type`` = "WindowsRestart"
            restartCheckCommand = this.RestartCheckCommand
            restartCommand = this.RestartCommand
            restartTimeout = this.RestartTimeout
        |}
        :> obj

type WindowsUpdateCustomizer =
    {
        Filters: string list
        SearchCriteria: string option // defaults to "BrowseOnly=0 and IsInstalled=0" (Recommended)
        UpdateLimit: int option // defaults to limit of 1000 updates
    }

    member this.JsonModel =
        {|
            ``type`` = "WindowsUpdate"
            filters =
                if this.Filters.IsEmpty then
                    null
                else
                    ResizeArray(this.Filters)
            searchCriteria = this.SearchCriteria |> Option.toObj
            updateLimit = this.UpdateLimit |> Option.map box |> Option.toObj
        |}
        :> obj

[<RequireQualifiedAccess>]
type Customizer =
    | File of FileCustomizer
    | PowerShell of PowerShellCustomizer
    | PowerShellScript of PowerShellScriptCustomizer
    | Shell of ShellCustomizer
    | ShellScript of ShellScriptCustomizer
    | WindowsRestart of WindowsRestartCustomizer
    | WindowsUpdate of WindowsUpdateCustomizer

    member this.JsonModel =
        match this with
        | File customizer -> customizer.JsonModel
        | PowerShell customizer -> customizer.JsonModel
        | PowerShellScript customizer -> customizer.JsonModel
        | Shell customizer -> customizer.JsonModel
        | ShellScript customizer -> customizer.JsonModel
        | WindowsRestart customizer -> customizer.JsonModel
        | WindowsUpdate customizer -> customizer.JsonModel

type ManagedImageDistributor = {
    ImageId: ResourceId
    RunOutputName: string
    Location: string
    ArtifactTags: Map<string, string>
}

type SharedImageDistributor = {
    GalleryImageId: ResourceId
    RunOutputName: string
    ReplicationRegions: Location list
    ExcludeFromLatest: bool option
    ArtifactTags: Map<string, string>
}

type VhdDistributor = {
    RunOutputName: string
    ArtifactTags: Map<string, string>
}

[<RequireQualifiedAccess>]
type Distibutor =
    | ManagedImage of ManagedImageDistributor
    | SharedImage of SharedImageDistributor
    | VHD of VhdDistributor

    member this.JsonModel =
        match this with
        | ManagedImage distributor ->
            {|
                ``type`` = "ManagedImage"
                imageId = distributor.ImageId.Eval()
                location = distributor.Location
                artifactTags =
                    if distributor.ArtifactTags.IsEmpty then
                        null
                    else
                        distributor.ArtifactTags :> obj
            |}
            :> obj
        | SharedImage distributor -> {|
            ``type`` = "SharedImage"
            galleryImageId = distributor.GalleryImageId.Eval()
            replicationRegions = distributor.ReplicationRegions |> List.map (fun location -> location.ArmValue)
            runOutputName = distributor.RunOutputName
            excludeFromLatest = distributor.ExcludeFromLatest |> Option.map box |> Option.toObj
            artifactTags =
                if distributor.ArtifactTags.IsEmpty then
                    null
                else
                    distributor.ArtifactTags :> obj
          |}
        | VHD distributor ->
            {|
                ``type`` = "VHD"
                runOutputName = distributor.RunOutputName
                artifactTags =
                    if distributor.ArtifactTags.IsEmpty then
                        null
                    else
                        distributor.ArtifactTags :> obj
            |}
            :> obj

type ImageBuilder =
    {
        Name: ResourceName
        Location: Location
        Identity: Identity.ManagedIdentity
        BuildTimeoutInMinutes: int option
        Source: ImageBuilderSource
        Customize: Customizer list
        Distribute: Distibutor list
        Tags: Map<string, string>
        Dependencies: ResourceId Set
    }

    interface IArmResource with
        member this.ResourceId = imageTemplates.resourceId this.Name

        member this.JsonModel =
            let dependencies =
                seq {
                    yield! this.Identity.Dependencies
                    yield! this.Dependencies
                }
                |> Set.ofSeq

            {| imageTemplates.Create(this.Name, this.Location, dependsOn = dependencies, tags = this.Tags) with
                identity = this.Identity.ToArmJson
                properties = {|
                    buildTimeoutInMinutes = this.BuildTimeoutInMinutes |> Option.map box |> Option.toObj
                    source = this.Source.JsonModel
                    customize = this.Customize |> List.map (fun customizer -> customizer.JsonModel)
                    distribute = this.Distribute |> List.map (fun distributor -> distributor.JsonModel)
                |}
            |}
