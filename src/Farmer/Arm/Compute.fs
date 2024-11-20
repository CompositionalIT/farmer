[<AutoOpen>]
module Farmer.Arm.Compute

open Farmer
open Farmer.DedicatedHosts
open Farmer.Identity
open Farmer.Vm
open System
open System.Text
open Farmer.VmScaleSet

let virtualMachines =
    ResourceType("Microsoft.Compute/virtualMachines", "2023-03-01")

let virtualMachineScaleSets =
    ResourceType("Microsoft.Compute/virtualMachineScaleSets", "2023-03-01")

let extensions =
    ResourceType("Microsoft.Compute/virtualMachines/extensions", "2019-12-01")

let virtualMachineScaleSetsExtensions =
    ResourceType("Microsoft.Compute/virtualMachineScaleSets/extensions", "2023-03-01")

let hostGroups = ResourceType("Microsoft.Compute/hostGroups", "2021-03-01")
let hosts = ResourceType("Microsoft.Compute/hostGroups/hosts", "2021-03-01")

/// Interface to get the properties of a VM extension so it can be added to the
/// extension profile of a VM Scale Set in addition to adding after deployment.
type IExtension =
    abstract member JsonProperties: obj
    abstract member Name: string

type CustomScriptExtension = {
    Name: ResourceName
    Location: Location
    VirtualMachine: ResourceName
    FileUris: Uri list
    ScriptContents: string
    OS: OS
    Tags: Map<string, string>
} with

    interface IExtension with
        member this.Name = this.Name.Value

        member this.JsonProperties =
            match this.OS with
            | Windows ->
                {|
                    publisher = "Microsoft.Compute"
                    ``type`` = "CustomScriptExtension"
                    typeHandlerVersion = "1.10"
                    autoUpgradeMinorVersion = true
                    settings = {|
                        fileUris = this.FileUris |> List.map string
                    |}
                    protectedSettings = {|
                        commandToExecute = this.ScriptContents
                    |}
                |}
                |> box
            | Linux -> {|
                publisher = "Microsoft.Azure.Extensions"
                ``type`` = "CustomScript"
                typeHandlerVersion = "2.1"
                autoUpgradeMinorVersion = true
                protectedSettings = {|
                    fileUris = this.FileUris |> List.map string
                    script = this.ScriptContents |> Encoding.UTF8.GetBytes |> Convert.ToBase64String
                |}
              |}

    interface IArmResource with
        member this.ResourceId = extensions.resourceId (this.VirtualMachine / this.Name)

        member this.JsonModel = {|
            extensions.Create(
                this.VirtualMachine / this.Name,
                this.Location,
                [ virtualMachines.resourceId this.VirtualMachine ],
                this.Tags
            ) with
                properties = (this :> IExtension).JsonProperties
        |}

type AadSshLoginExtension = {
    Location: Location
    VirtualMachine: ResourceName
    Tags: Map<string, string>
} with

    member this.Name = "AADSSHLoginForLinux"

    interface IExtension with
        member this.Name = this.Name

        member this.JsonProperties = {|
            publisher = "Microsoft.Azure.ActiveDirectory"
            ``type`` = "AADSSHLoginForLinux"
            typeHandlerVersion = "1.0"
            autoUpgradeMinorVersion = true
        |}

    interface IArmResource with
        member this.ResourceId = extensions.resourceId (this.VirtualMachine / this.Name)

        member this.JsonModel = {|
            extensions.Create(
                this.VirtualMachine / this.Name,
                this.Location,
                [ virtualMachines.resourceId this.VirtualMachine ],
                this.Tags
            ) with
                properties = (this :> IExtension).JsonProperties
        |}

type ApplicationHealthExtension = {
    Location: Location
    VirtualMachineScaleSet: ResourceName
    OS: OS
    Protocol: ApplicationHealthExtensionProtocol
    Port: uint16
    Interval: TimeSpan option
    NumberOfProbes: int option
    GracePeriod: TimeSpan option
    Tags: Map<string, string>
} with

    static member Name = "HealthExtension"

    interface IExtension with

        member _.Name = ApplicationHealthExtension.Name

        member this.JsonProperties = {|
            publisher = "Microsoft.ManagedServices"
            ``type`` =
                match this.OS with
                | Linux -> "ApplicationHealthLinux"
                | Windows -> "ApplicationHealthWindows"
            typeHandlerVersion = "1.0"
            autoUpgradeMinorVersion = true
            settings = {|
                protocol = this.Protocol.ArmValue
                port = this.Port
                requestPath = this.Protocol.RequestPath |> Option.defaultValue null
                intervalInSeconds =
                    this.Interval
                    |> Option.map (fun i -> box i.TotalSeconds)
                    |> Option.defaultValue null
                numberOfProbes = this.NumberOfProbes |> Option.map box |> Option.defaultValue null
                gracePeriod =
                    this.GracePeriod
                    |> Option.map (fun p -> box p.TotalSeconds)
                    |> Option.defaultValue null
            |}
        |}

    interface IArmResource with
        member this.ResourceId =
            extensions.resourceId (this.VirtualMachineScaleSet / ApplicationHealthExtension.Name)

        member this.JsonModel = {|
            virtualMachineScaleSetsExtensions.Create(
                this.VirtualMachineScaleSet / ApplicationHealthExtension.Name,
                this.Location,
                [ virtualMachineScaleSets.resourceId this.VirtualMachineScaleSet ],
                this.Tags
            ) with
                properties = (this :> IExtension).JsonProperties
        |}

type NetworkInterfaceConfiguration = {
    Name: ResourceName
    EnableAcceleratedNetworking: bool option
    EnableIpForwarding: bool option
    IpConfigs: IpConfiguration list
    VirtualNetwork: LinkedResource
    NetworkSecurityGroup: LinkedResource option
    Primary: bool
} with

    member this.ToArmJson = {|
        name = this.Name.Value
        properties = {|
            enableAcceleratedNetworking = this.EnableAcceleratedNetworking |> Option.map box |> Option.defaultValue null
            enableIPForwarding = this.EnableIpForwarding |> Option.map box |> Option.defaultValue null
            networkSecurityGroup =
                this.NetworkSecurityGroup
                |> Option.map _.ResourceId
                |> Option.map ResourceId.AsIdObject
                |> Option.map box
                |> Option.defaultValue null
            ipConfigurations =
                this.IpConfigs
                |> List.mapi (fun index ipconfig -> ipconfig.ToArmJson(index, this.VirtualNetwork.ResourceId, false))
            primary = this.Primary
        |}
    |}

module VirtualMachine =
    let additionalCapabilities (dataDisks: DataDiskCreateOption list) =
        // If data disks use UltraSSD then enable that support
        if dataDisks |> List.exists (fun disk -> disk.IsUltraDisk) then
            {| ultraSSDEnabled = true |} :> obj
        else
            null

    let priority (priority: Priority option) =
        match priority with
        | Some priority -> priority.ArmValue
        | _ -> Unchecked.defaultof<_>

    let applicationProfile (galleryApplications: VmGalleryApplication list) =
        match galleryApplications with
        | [] -> null
        | galleryApps ->
            {|
                galleryApplications =
                    galleryApps
                    |> List.map (fun galleryApp -> {|
                        configurationReference = galleryApp.ConfigurationReference |> Option.toObj
                        enableAutomaticUpgrade = galleryApp.EnableAutomaticUpgrade |> (Option.map box >> Option.toObj)
                        order = galleryApp.Order |> (Option.map box >> Option.toObj)
                        packageReferenceId = galleryApp.PackageReferenceId.Eval()
                        tags = galleryApp.Tags |> Option.toObj
                        treatFailureAsDeploymentFailure =
                            galleryApp.TreatFailureAsDeploymentFailure |> (Option.map box >> Option.toObj)
                    |})
            |}
            |> box

    let osProfile
        (
            name: ResourceName,
            isScaleSet: bool,
            osDisk: OsDiskCreateOption,
            credentials:
                {|
                    Password: SecureParameter
                    Username: string
                |},
            disablePasswordAuthentication: bool option,
            customData: string option,
            publicKeys: (string * string) list option
        ) =
        match osDisk with
        | AttachOsDisk _ -> null
        | _ ->
            {|
                computerName = if isScaleSet then null else name.Value
                computerNamePrefix = if isScaleSet then name.Value else null
                adminUsername = credentials.Username
                adminPassword =
                    if disablePasswordAuthentication.IsSome && disablePasswordAuthentication.Value then //If the disablePasswordAuthentication is set and the value is true then we don't need a password
                        null
                    else
                        credentials.Password.ArmExpression.Eval()
                customData =
                    customData
                    |> Option.map (System.Text.Encoding.UTF8.GetBytes >> Convert.ToBase64String)
                    |> Option.toObj
                linuxConfiguration =
                    if disablePasswordAuthentication.IsSome || publicKeys.IsSome then
                        {|
                            disablePasswordAuthentication =
                                disablePasswordAuthentication |> Option.map box |> Option.toObj
                            ssh =
                                match publicKeys with
                                | Some publicKeys -> {|
                                    publicKeys = publicKeys |> List.map (fun k -> {| path = fst k; keyData = snd k |})
                                  |}
                                | None -> Unchecked.defaultof<_>
                        |}
                    else
                        Unchecked.defaultof<_>
            |}
            :> obj

    let storageProfile
        (name: ResourceName, osDisk: OsDiskCreateOption, dataDisks: DataDiskCreateOption list, isScaleSet: bool)
        =
        let vmNameLowerCase = name.Value.ToLower()

        {|
            imageReference =
                match osDisk with
                | FromImage(GalleryImageRef (_,SharedGalleryImageId imageId), _) ->
                    {|
                        sharedGalleryImageId = imageId
                    |} :> obj
                | FromImage(GalleryImageRef (_,CommunityGalleryImageId imageId), _) ->
                    {|
                        communityGalleryImageId = imageId
                    |} :> obj
                | FromImage(ImageDefinition imageDefintion, _) ->
                    {|
                        publisher = imageDefintion.Publisher.ArmValue
                        offer = imageDefintion.Offer.ArmValue
                        sku = imageDefintion.Sku.ArmValue
                        version = "latest"
                    |}
                    :> obj
                | _ -> null
            osDisk =
                match osDisk with
                | FromImage(_, diskInfo) ->
                    {|
                        createOption = "FromImage"
                        name = if isScaleSet then null else $"{vmNameLowerCase}-osdisk"
                        diskSizeGB = diskInfo.Size
                        managedDisk = {|
                            storageAccountType = diskInfo.DiskType.ArmValue
                        |}
                    |}
                    :> obj
                | AttachOsDisk(os, managedDiskId) -> {|
                    createOption = "Attach"
                    managedDisk = {|
                        id = managedDiskId.ResourceId.Eval()
                    |}
                    name = managedDiskId.Name.Value
                    osType = string<OS> os
                  |}
            dataDisks =
                dataDisks
                |> List.mapi (fun lun dataDisk ->
                    match dataDisk with
                    | AttachDataDisk(managedDiskId)
                    | AttachUltra(managedDiskId) ->
                        {|
                            createOption = "Attach"
                            name = managedDiskId.Name.Value
                            lun = lun
                            managedDisk = {|
                                id = managedDiskId.ResourceId.Eval()
                            |}
                        |}
                        :> obj
                    | Empty diskInfo ->
                        {|
                            createOption = "Empty"
                            name =
                                if not isScaleSet then
                                    $"{vmNameLowerCase}-datadisk-{lun}"
                                else
                                    null
                            diskSizeGB = diskInfo.Size
                            lun = lun
                            managedDisk = {|
                                storageAccountType = diskInfo.DiskType.ArmValue
                            |}
                        |}
                        :> obj)
        |}

    let networkProfile (networkInterfaceIds: ResourceId list, nicConfig: NetworkInterfaceConfiguration list) = {|
        networkInterfaces =
            networkInterfaceIds
            |> List.mapi (fun idx id -> {|
                id = id.Eval()
                properties =
                    if networkInterfaceIds.Length > 1 then
                        box {| primary = idx = 0 |} // First NIC is primary
                    else
                        null // Don't emit primary if there aren't multiple NICs
            |})
    |}

    let diagnosticsProfile (diagnosticsEnabled: bool option, storageAccount: LinkedResource option) =
        match diagnosticsEnabled with
        | None
        | Some false ->
            box {|
                bootDiagnostics = {| enabled = false |}
            |}
        | Some true ->
            match storageAccount with
            | Some storageAccount ->
                let resourceId = storageAccount.ResourceId

                let storageUriExpr =
                    ArmExpression
                        .reference(storageAccounts, resourceId)
                        .Map(fun r -> r + ".primaryEndpoints.blob")
                        .WithOwner(resourceId)
                        .Eval()

                box {|
                    bootDiagnostics = {|
                        enabled = true
                        storageUri = storageUriExpr
                    |}
                |}
            | None ->
                box {|
                    bootDiagnostics = {| enabled = true |}
                |}

type VirtualMachine = {
    Name: ResourceName
    Location: Location
    Dependencies: ResourceId Set
    AvailabilityZone: string option
    DiagnosticsEnabled: bool option
    StorageAccount: LinkedResource option
    Size: VMSize
    Priority: Priority option
    Credentials: {|
        Username: string
        Password: SecureParameter
    |}
    CustomData: string option
    DisablePasswordAuthentication: bool option
    GalleryApplications: VmGalleryApplication list
    PublicKeys: (string * string) list option
    OsDisk: OsDiskCreateOption
    DataDisks: DataDiskCreateOption list
    NetworkInterfaceIds: ResourceId list
    Identity: Identity.ManagedIdentity
    Tags: Map<string, string>
} with

    interface IParameters with
        member this.SecureParameters =
            match this.DisablePasswordAuthentication, this.OsDisk with
            | Some(true), _
            | _, AttachOsDisk _ -> [] // What attaching an OS disk, the osConfig cannot be set, so cannot set password
            | _ -> [ this.Credentials.Password ]

    interface IArmResource with
        member this.ResourceId = virtualMachines.resourceId this.Name

        member this.JsonModel =
            let dependsOn = [
                yield! this.Dependencies
                yield! this.NetworkInterfaceIds
                match this.StorageAccount with
                | Some(Managed rid) -> rid
                | Some(Unmanaged _)
                | None -> ()
                match this.OsDisk with
                | AttachOsDisk(_, Managed(resourceId)) -> resourceId
                | _ -> ()
                for disk in this.DataDisks do
                    match disk with
                    | AttachDataDisk(Managed(resourceId))
                    | AttachUltra(Managed(resourceId)) -> resourceId
                    | _ -> ()
            ]

            let properties = {|
                additionalCapabilities = VirtualMachine.additionalCapabilities this.DataDisks
                applicationProfile = VirtualMachine.applicationProfile this.GalleryApplications
                priority = VirtualMachine.priority this.Priority
                hardwareProfile = {| vmSize = this.Size.ArmValue |}
                osProfile =
                    VirtualMachine.osProfile (
                        this.Name,
                        false, // not a VmScaleSet
                        this.OsDisk,
                        this.Credentials,
                        this.DisablePasswordAuthentication,
                        this.CustomData,
                        this.PublicKeys
                    )
                storageProfile = VirtualMachine.storageProfile (this.Name, this.OsDisk, this.DataDisks, false)
                networkProfile = VirtualMachine.networkProfile (this.NetworkInterfaceIds, [])
                diagnosticsProfile = VirtualMachine.diagnosticsProfile (this.DiagnosticsEnabled, this.StorageAccount)
            |}

            {|
                virtualMachines.Create(this.Name, this.Location, dependsOn, this.Tags) with
                    identity =
                        if this.Identity = ManagedIdentity.Empty then
                            Unchecked.defaultof<_>
                        else
                            this.Identity.ToArmJson
                    properties =
                        match this.Priority with
                        | None
                        | Some Low
                        | Some Regular -> box properties
                        | Some(Spot(evictionPolicy, maxPrice)) -> {|
                            properties with
                                evictionPolicy = evictionPolicy.ArmValue
                                billingProfile = {| maxPrice = maxPrice |}
                          |}
                    zones = this.AvailabilityZone |> Option.map ResizeArray |> Option.toObj
            |}

type ScaleSetUpgradePolicy = {
    Mode: VmScaleSet.UpgradeMode
} with

    member this.ArmJson = {| mode = this.Mode.ArmValue |}

type ScaleSetScaleInPolicy = {
    // Set false when reusing disks or MAC addresses
    ForceDeletion: bool
    Rules: ScaleInPolicyRule list
} with

    member this.ArmJson =
        // https://learn.microsoft.com/azure/templates/microsoft.compute/virtualmachinescalesets?pivots=deployment-language-arm-template#scaleinpolicy-1
        {|
            forceDeletion = this.ForceDeletion
            rules = this.Rules |> List.map (fun rule -> rule.ArmValue)
        |}

type ScaleSetAutomaticRepairsPolicy = {
    Enabled: bool
    // Amount of time to wait after a change before starting repairs.
    // Minimum of 10 minutes, maximum 90 minutes.
    GracePeriod: TimeSpan
} with

    member this.ArmJson = {|
        enabled = this.Enabled
        gracePeriod = (IsoDateTime.OfTimeSpan this.GracePeriod).Value
    |}

type VirtualMachineScaleSet = {
    Name: ResourceName
    Location: Location
    Dependencies: ResourceId Set
    AvailabilityZones: string list
    ZoneBalance: bool option
    DiagnosticsEnabled: bool option
    Size: VMSize
    Capacity: int
    ScaleInPolicy: ScaleSetScaleInPolicy
    UpgradePolicy: ScaleSetUpgradePolicy
    AutomaticRepairsPolicy: ScaleSetAutomaticRepairsPolicy option
    Priority: Priority option
    Credentials: {|
        Username: string
        Password: SecureParameter
    |}
    CustomData: string option
    DisablePasswordAuthentication: bool option
    Extensions: IExtension list
    GalleryApplications: VmGalleryApplication list
    PublicKeys: (string * string) list option
    OsDisk: OsDiskCreateOption
    DataDisks: DataDiskCreateOption list
    HealthProbeId: ResourceId option
    NetworkInterfaceConfigs: NetworkInterfaceConfiguration list
    Identity: Identity.ManagedIdentity
    Tags: Map<string, string>
} with

    interface IParameters with
        member this.SecureParameters =
            match this.DisablePasswordAuthentication, this.OsDisk with
            | Some(true), _
            | _, AttachOsDisk _ -> []
            | _ -> [ this.Credentials.Password ]

    interface IArmResource with
        member this.ResourceId = virtualMachineScaleSets.resourceId this.Name

        member this.JsonModel =
            let dependsOn = [
                yield! this.Dependencies
                for nic in this.NetworkInterfaceConfigs do
                    match nic.VirtualNetwork with
                    | Managed rid -> rid
                    | _ -> ()
                match this.OsDisk with
                | AttachOsDisk(_, Managed(resourceId)) -> resourceId
                | _ -> ()
                for disk in this.DataDisks do
                    match disk with
                    | AttachDataDisk(Managed(resourceId))
                    | AttachUltra(Managed(resourceId)) -> resourceId
                    | _ -> ()
            ]

            {|
                virtualMachineScaleSets.Create(this.Name, this.Location, dependsOn, this.Tags) with
                    identity =
                        if this.Identity = ManagedIdentity.Empty then
                            Unchecked.defaultof<_>
                        else
                            this.Identity.ToArmJson
                    sku = {|
                        capacity = this.Capacity
                        name = this.Size.ArmValue
                        tier = this.Size.Tier
                    |}
                    properties = {|
                        additionalCapabilities = VirtualMachine.additionalCapabilities this.DataDisks
                        virtualMachineProfile = {|
                            applicationProfile = VirtualMachine.applicationProfile this.GalleryApplications
                            diagnosticsProfile = VirtualMachine.diagnosticsProfile (this.DiagnosticsEnabled, None)
                            extensionProfile =
                                if this.Extensions = [] then
                                    null
                                else
                                    {|
                                        extensions =
                                            this.Extensions
                                            |> List.map (fun ext -> {|
                                                name = ext.Name
                                                properties = ext.JsonProperties
                                            |})
                                    |}
                                    |> box
                            osProfile =
                                VirtualMachine.osProfile (
                                    this.Name,
                                    true, // is a VmScaleSet
                                    this.OsDisk,
                                    this.Credentials,
                                    this.DisablePasswordAuthentication,
                                    this.CustomData,
                                    this.PublicKeys
                                )
                            storageProfile =
                                VirtualMachine.storageProfile (this.Name, this.OsDisk, this.DataDisks, true)
                            networkProfile = {|
                                healthProbe =
                                    this.HealthProbeId
                                    |> Option.map (ResourceId.AsIdObject >> box)
                                    |> Option.defaultValue null
                                networkInterfaceConfigurations =
                                    this.NetworkInterfaceConfigs |> List.map (fun c -> c.ToArmJson)
                            |}
                        |}
                        billingProfile =
                            match this.Priority with
                            | Some(Spot(_, maxPrice)) -> box {| maxPrice = maxPrice |}
                            | _ -> null
                        evictionPolicy =
                            match this.Priority with
                            | Some(Spot(evictionPolicy, _)) -> evictionPolicy.ArmValue
                            | _ -> null
                        priority = VirtualMachine.priority this.Priority
                        automaticRepairsPolicy =
                            match this.AutomaticRepairsPolicy with
                            | None -> null
                            | Some repairsPolicy -> box repairsPolicy.ArmJson
                        scaleInPolicy = this.ScaleInPolicy.ArmJson
                        upgradePolicy = this.UpgradePolicy.ArmJson
                    |}
                    zones =
                        if this.AvailabilityZones.Length > 0 then
                            Seq.ofList this.AvailabilityZones
                        else
                            null
            |}

type Host = {
    Name: ResourceName
    Location: Location
    Sku: HostSku
    ParentHostGroupName: ResourceName
    AutoReplaceOnFailure: FeatureFlag
    LicenseType: HostLicenseType
    PlatformFaultDomain: PlatformFaultDomainCount
    Tags: Map<string, string>
    DependsOn: Set<ResourceId>
} with

    member internal this.JsonModelProperties = {|
        autoReplaceOnFailure = this.AutoReplaceOnFailure.AsBoolean
        licenseType = HostLicenseType.Print this.LicenseType
        platformFaultDomain = PlatformFaultDomainCount.ToArmValue this.PlatformFaultDomain
    |}

    interface IArmResource with
        member this.ResourceId = hosts.resourceId this.Name

        member this.JsonModel =
            let dependsOn =
                [ hostGroups.resourceId this.ParentHostGroupName ] @ (List.ofSeq this.DependsOn)

            let hostResourceName =
                ResourceName($"{this.ParentHostGroupName.Value}/{this.Name.Value}")

            {|
                hosts.Create(hostResourceName, this.Location, dependsOn, tags = this.Tags) with
                    sku = this.Sku.JsonProperties
                    properties = this.JsonModelProperties
            |}

type HostGroup = {
    Name: ResourceName
    Location: Location
    AvailabilityZone: string list
    SupportAutomaticPlacement: FeatureFlag
    PlatformFaultDomainCount: PlatformFaultDomainCount
    Tags: Map<string, string>
    DependsOn: Set<ResourceId>
} with

    member internal this.JsonModelProperties = {|
        supportAutomaticPlacement = this.SupportAutomaticPlacement.AsBoolean
        platformFaultDomainCount = PlatformFaultDomainCount.ToArmValue this.PlatformFaultDomainCount
    |}

    interface IArmResource with
        member this.ResourceId = hostGroups.resourceId this.Name

        member this.JsonModel = {|
            hostGroups.Create(this.Name, this.Location, tags = this.Tags, dependsOn = this.DependsOn) with
                zones = this.AvailabilityZone
                properties = this.JsonModelProperties
        |}