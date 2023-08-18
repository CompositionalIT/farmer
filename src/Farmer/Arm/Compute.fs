[<AutoOpen>]
module Farmer.Arm.Compute

open System.ComponentModel
open Farmer
open Farmer.DedicatedHosts
open Farmer.Identity
open Farmer.Vm
open System
open System.Text

let virtualMachines =
    ResourceType("Microsoft.Compute/virtualMachines", "2020-06-01")

let extensions =
    ResourceType("Microsoft.Compute/virtualMachines/extensions", "2019-12-01")

let hostGroups = ResourceType("Microsoft.Compute/hostGroups", "2021-03-01")
let hosts = ResourceType("Microsoft.Compute/hostGroups/hosts", "2021-03-01")

type CustomScriptExtension = {
    Name: ResourceName
    Location: Location
    VirtualMachine: ResourceName
    FileUris: Uri list
    ScriptContents: string
    OS: OS
    Tags: Map<string, string>
} with

    interface IArmResource with
        member this.ResourceId = extensions.resourceId (this.VirtualMachine / this.Name)

        member this.JsonModel = {|
            extensions.Create(
                this.VirtualMachine / this.Name,
                this.Location,
                [ virtualMachines.resourceId this.VirtualMachine ],
                this.Tags
            ) with
                properties =
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
        |}

type AadSshLoginExtension = {
    Location: Location
    VirtualMachine: ResourceName
    Tags: Map<string, string>
} with

    member this.Name = "AADSSHLoginForLinux"

    interface IArmResource with
        member this.ResourceId = extensions.resourceId (this.VirtualMachine / this.Name)

        member this.JsonModel = {|
            extensions.Create(
                this.VirtualMachine / this.Name,
                this.Location,
                [ virtualMachines.resourceId this.VirtualMachine ],
                this.Tags
            ) with
                properties = {|
                    publisher = "Microsoft.Azure.ActiveDirectory"
                    ``type`` = "AADSSHLoginForLinux"
                    typeHandlerVersion = "1.0"
                    autoUpgradeMinorVersion = true
                |}
        |}

type VirtualMachine = {
    Name: ResourceName
    Location: Location
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
                additionalCapabilities = // If data disks use UltraSSD then enable that support
                    if this.DataDisks |> List.exists (fun disk -> disk.IsUltraDisk) then
                        {| ultraSSDEnabled = true |} :> obj
                    else
                        null
                priority =
                    match this.Priority with
                    | Some priority -> priority.ArmValue
                    | _ -> Unchecked.defaultof<_>
                hardwareProfile = {| vmSize = this.Size.ArmValue |}
                osProfile =
                    match this.OsDisk with
                    | AttachOsDisk _ -> null
                    | _ ->
                        {|
                            computerName = this.Name.Value
                            adminUsername = this.Credentials.Username
                            adminPassword =
                                if
                                    this.DisablePasswordAuthentication.IsSome
                                    && this.DisablePasswordAuthentication.Value
                                then //If the disablePasswordAuthentication is set and the value is true then we don't need a password
                                    null
                                else
                                    this.Credentials.Password.ArmExpression.Eval()
                            customData =
                                this.CustomData
                                |> Option.map (System.Text.Encoding.UTF8.GetBytes >> Convert.ToBase64String)
                                |> Option.toObj
                            linuxConfiguration =
                                if this.DisablePasswordAuthentication.IsSome || this.PublicKeys.IsSome then
                                    {|
                                        disablePasswordAuthentication =
                                            this.DisablePasswordAuthentication |> Option.map box |> Option.toObj
                                        ssh =
                                            match this.PublicKeys with
                                            | Some publicKeys -> {|
                                                publicKeys =
                                                    publicKeys
                                                    |> List.map (fun k -> {| path = fst k; keyData = snd k |})
                                              |}
                                            | None -> Unchecked.defaultof<_>
                                    |}
                                else
                                    Unchecked.defaultof<_>
                        |}
                        :> obj
                storageProfile =
                    let vmNameLowerCase = this.Name.Value.ToLower()

                    {|
                        imageReference =
                            match this.OsDisk with
                            | FromImage(imageDefintion, _) ->
                                {|
                                    publisher = imageDefintion.Publisher.ArmValue
                                    offer = imageDefintion.Offer.ArmValue
                                    sku = imageDefintion.Sku.ArmValue
                                    version = "latest"
                                |}
                                :> obj
                            | _ -> null
                        osDisk =
                            match this.OsDisk with
                            | FromImage(_, diskInfo) ->
                                {|
                                    createOption = "FromImage"
                                    name = $"{vmNameLowerCase}-osdisk"
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
                            this.DataDisks
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
                                        name = $"{vmNameLowerCase}-datadisk-{lun}"
                                        diskSizeGB = diskInfo.Size
                                        lun = lun
                                        managedDisk = {|
                                            storageAccountType = diskInfo.DiskType.ArmValue
                                        |}
                                    |}
                                    :> obj)
                    |}
                networkProfile = {|
                    networkInterfaces =
                        this.NetworkInterfaceIds
                        |> List.mapi (fun idx id -> {|
                            id = id.Eval()
                            properties =
                                if this.NetworkInterfaceIds.Length > 1 then
                                    box {| primary = idx = 0 |} // First NIC is primary
                                else
                                    null // Don't emit primary if there aren't multiple NICs
                        |})
                |}
                diagnosticsProfile =
                    match this.DiagnosticsEnabled with
                    | None
                    | Some false ->
                        box {|
                            bootDiagnostics = {| enabled = false |}
                        |}
                    | Some true ->
                        match this.StorageAccount with
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
