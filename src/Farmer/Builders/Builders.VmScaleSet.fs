[<AutoOpen>]
module Farmer.Builders.VirtualMachineScaleSet

open Farmer
open Farmer.Arm
open Farmer.FeatureFlag
open Farmer.Identity
open Farmer.Vm
open System
open Farmer.VmScaleSet

let makeName (vmName: ResourceName) elementType =
    ResourceName $"{vmName.Value}-%s{elementType}"

type IExtensionBuilder =
    abstract member BuildExtension: Location -> IExtension

type ApplicationHealthExtensionConfig = {
    VirtualMachineScaleSet: ResourceName
    OS: OS option
    Protocol: ApplicationHealthExtensionProtocol
    Port: uint16
    Interval: TimeSpan option
    NumberOfProbes: int option
    GracePeriod: TimeSpan option
    Tags: Map<string, string>
    TypeHandlerVersion: string option
    EnableAutomaticUpgrade: bool option
} with

    interface IExtensionBuilder with
        member this.BuildExtension location = {
            Location = location
            VirtualMachineScaleSet = this.VirtualMachineScaleSet
            OS = this.OS |> Option.defaultValue OS.Linux
            Protocol = this.Protocol
            Port = this.Port
            Interval = this.Interval
            NumberOfProbes = this.NumberOfProbes
            GracePeriod = this.GracePeriod
            Tags = this.Tags
            TypeHandlerVersion = this.TypeHandlerVersion
            EnableAutomaticUpgrade = this.EnableAutomaticUpgrade
        }

    interface IBuilder with
        member this.ResourceId =
            virtualMachineScaleSetsExtensions.resourceId (
                this.VirtualMachineScaleSet / ResourceName ApplicationHealthExtension.Name
            )

        member this.BuildResources location = [ (this :> IExtensionBuilder).BuildExtension location :?> IArmResource ]

type VmScaleSetConfig = {
    Name: ResourceName
    Dependencies: ResourceId Set
    Vm: VmConfig option
    AutomaticRepairsPolicy: ScaleSetAutomaticRepairsPolicy option
    Autoscale: AutoscaleSettings option
    AvailabilityZones: string list
    Capacity: int option
    Overprovision: bool option
    RunExtensionsOnOverprovisionedVMs: bool option
    Extensions: IExtensionBuilder list
    HealthProbeId: ResourceId option
    LoadBalancerBackendAddressPools: LinkedResource list
    ScaleInPolicy: ScaleSetScaleInPolicy option
    UpgradePolicy: ScaleSetUpgradePolicy option
    ZoneBalance: bool option

    Tags: Map<string, string>
} with

    member internal this.DeriveResourceName (vm: VmConfig) (resourceType: ResourceType) elementName =
        resourceType.resourceId (makeName vm.Name elementName)

    member private this.NicName(vm: VmConfig) =
        this.DeriveResourceName (vm: VmConfig) networkInterfaces "nic"

    member this.PasswordParameterArm =
        this.Vm
        |> Option.bind (fun vm -> vm.PasswordParameter)
        |> Option.defaultValue $"password-for-{this.Name.Value}"

    member this.SystemIdentity = SystemIdentity this.ResourceId
    member this.ResourceId = virtualMachineScaleSets.resourceId this.Name

    /// Builds NICs for this VM, one for each subnet.
    member private this.buildNetworkInterfaceConfigurations(vnetId, nsgId) : NetworkInterfaceConfiguration list =
        // NIC for each distinct subnet
        match this.Vm with
        | Some vm ->
            let ipConfigsBySubnet =
                vm.buildIpConfigs ()
                // Remove Public IP when building scale set IP configs.
                |> List.map (fun ipconfig -> { ipconfig with PublicIpAddress = None })
                |> List.groupBy (fun ipconfig -> ipconfig.SubnetName)

            ipConfigsBySubnet
            |> List.mapi (fun index (subnetName, subnetIpConfigs) -> {
                Name = ResourceName $"{subnetName.Value}-nic"
                EnableAcceleratedNetworking = vm.AcceleratedNetworking |> Option.map toBool
                EnableIpForwarding = None // Not supporting for VMSS yet.
                IpConfigs = subnetIpConfigs
                Primary =
                    if index = 0 then // First NIC is the primary...for now.
                        true
                    else
                        false
                VirtualNetwork = vnetId
                NetworkSecurityGroup = nsgId
            })
        | None -> []

    member private this.CustomScriptExtension =
        this.Vm
        |> Option.bind (fun vm ->
            if vm.CustomScript.IsSome || not (vm.CustomScriptFiles.IsEmpty) then
                Some
                    { new IExtensionBuilder with
                        member _.BuildExtension location = {
                            Name = this.Name.Map(sprintf "%s-custom-script")
                            Location = location
                            VirtualMachine = this.Name
                            FileUris = vm.CustomScriptFiles
                            ScriptContents = vm.CustomScript.Value
                            OS =
                                match vm.OsDisk with
                                | FromImage(ImageDefinition image, _) -> image.OS
                                | FromImage(GalleryImageRef(os, _), _) -> os
                                | _ ->
                                    raiseFarmer
                                        "Unable to determine OS for custom script when attaching an existing disk"
                            Tags = this.Tags
                        }
                    }
            else
                None)

    member private this.AadSshLoginExtension =
        this.Vm
        |> Option.bind (fun vm ->
            match vm.AadSshLogin, vm.OsDisk with
            | FeatureFlag.Enabled, FromImage(ImageDefinition image, _) when
                image.OS = Linux && vm.Identity.SystemAssigned = Disabled
                ->
                raiseFarmer "AAD SSH login requires that system assigned identity be enabled on the virtual machine."
            | FeatureFlag.Enabled, FromImage(ImageDefinition image, _) when image.OS = Windows ->
                raiseFarmer "AAD SSH login is only supported for Linux Virtual Machines"
            | FeatureFlag.Enabled, FromImage(GalleryImageRef(Windows, _), _) ->
                raiseFarmer "AAD SSH login is only supported for Linux Virtual Machines"
            // Assuming a user that attaches a disk knows to only using this extension for Linux images.
            | FeatureFlag.Enabled, _ ->
                Some
                    { new IExtensionBuilder with
                        member _.BuildExtension location = {
                            AadSshLoginExtension.Location = location
                            VirtualMachine = this.Name
                            Tags = this.Tags
                        }
                    }
            | FeatureFlag.Disabled, _ -> None)

    member private this.extensions =
        seq {
            this.CustomScriptExtension |> Option.toList
            this.AadSshLoginExtension |> Option.toList
            this.Extensions
        }
        |> List.concat

    interface IBuilder with
        member this.ResourceId = this.ResourceId

        member this.BuildResources location =
            match this.Vm with
            | None -> raiseFarmer "The 'vm_profile' must be set for the VM scale set."
            | Some vm ->
                let nsgId =
                    vm.NetworkSecurityGroup |> Option.map (fun lr -> (Managed lr.ResourceId))

                [
                    // The VM Scale Set
                    yield {
                        Name = this.Name
                        Location = location
                        Dependencies = this.Dependencies
                        AutomaticRepairsPolicy = this.AutomaticRepairsPolicy
                        AvailabilityZones = this.AvailabilityZones
                        Capacity = this.Capacity |> Option.defaultValue 1
                        Overprovision = this.Overprovision
                        RunExtensionsOnOverprovisionedVMs = this.RunExtensionsOnOverprovisionedVMs
                        Credentials =
                            match vm.Username with
                            | Some username -> {|
                                Username = username
                                Password = SecureParameter this.PasswordParameterArm
                              |}
                            | None -> raiseFarmer $"You must specify a username for virtual machine {this.Name.Value}"
                        CustomData = vm.CustomData
                        DataDisks = vm.DataDisks |> Option.defaultValue []
                        DiagnosticsEnabled = vm.DiagnosticsEnabled
                        DisablePasswordAuthentication = vm.DisablePasswordAuthentication
                        Extensions = this.extensions |> List.map (fun ext -> ext.BuildExtension location)
                        GalleryApplications = vm.GalleryApplications
                        HealthProbeId = this.HealthProbeId
                        Identity = vm.Identity
                        NetworkInterfaceConfigs =
                            let linkedVnet = vm.VNet.toLinkedResource (vm)
                            this.buildNetworkInterfaceConfigurations (linkedVnet, nsgId)
                        OsDisk = vm.OsDisk
                        Priority = vm.Priority
                        PublicKeys =
                            if
                                vm.DisablePasswordAuthentication.IsSome
                                && vm.DisablePasswordAuthentication.Value
                                && vm.SshPathAndPublicKeys.IsNone
                            then
                                raiseFarmer
                                    $"You must include at least one ssh key when Password Authentication is disabled"
                            else
                                (vm.SshPathAndPublicKeys)
                        ScaleInPolicy =
                            this.ScaleInPolicy
                            |> Option.defaultValue {
                                ForceDeletion = false
                                Rules = [ ScaleInPolicyRule.Default ]
                            }
                        SecurityProfile = vm.SecurityProfile
                        Size = vm.Size
                        UpgradePolicy =
                            this.UpgradePolicy
                            |> Option.defaultValue {
                                ScaleSetUpgradePolicy.Default with
                                    Mode = UpgradeMode.Automatic
                            }
                        ZoneBalance = this.ZoneBalance
                        Tags = this.Tags
                    }
                    yield! vm.BuildVNet(location, nsgId) |> Option.toList
                    match this.Autoscale with
                    | Some autoscaleSettings ->
                        yield {
                            autoscaleSettings with
                                Location = location
                                Properties = {
                                    autoscaleSettings.Properties with
                                        TargetResourceUri = Managed this.ResourceId
                                        Profiles =
                                            seq {
                                                for profile in autoscaleSettings.Properties.Profiles do
                                                    {
                                                        profile with
                                                            Rules =
                                                                seq {
                                                                    for rule in profile.Rules do
                                                                        if
                                                                            rule.MetricTrigger.MetricResourceUri = ResourceId.Empty
                                                                        then
                                                                            {
                                                                                rule with
                                                                                    MetricTrigger = {
                                                                                        rule.MetricTrigger with
                                                                                            MetricResourceUri =
                                                                                                this.ResourceId
                                                                                    }
                                                                            }
                                                                        else
                                                                            rule
                                                                }
                                                                |> List.ofSeq
                                                    }
                                            }
                                            |> List.ofSeq
                                }
                        }
                    | None -> ()
                ]

type ApplicationHealthExtensionBuilder() =
    member _.Yield _ = {
        VirtualMachineScaleSet = ResourceName.Empty
        OS = None
        Protocol = ApplicationHealthExtensionProtocol.HTTP "/"
        Port = 80us
        Interval = None
        NumberOfProbes = None
        GracePeriod = None
        Tags = Map.empty
        TypeHandlerVersion = None
        EnableAutomaticUpgrade = None
    }

    [<CustomOperation "enable_automatic_upgrade">]
    member _.EnableAutomaticUpgrade(state: ApplicationHealthExtensionConfig, enable) = {
        state with
            EnableAutomaticUpgrade = Some enable
    }

    [<CustomOperation "type_handler_version">]
    member _.TypeHandlerVersion(state: ApplicationHealthExtensionConfig, version) = {
        state with
            TypeHandlerVersion = Some version
    }

    /// Sets the VMSS where this health extension should be installed.
    [<CustomOperation "vmss">]
    member _.VmScaleSet(state: ApplicationHealthExtensionConfig, vmss) = {
        state with
            VirtualMachineScaleSet = vmss
    }

    member _.VmScaleSet(state: ApplicationHealthExtensionConfig, vmss) = {
        state with
            VirtualMachineScaleSet = ResourceName vmss
    }

    member _.VmScaleSet(state: ApplicationHealthExtensionConfig, vmss: VmScaleSetConfig) = {
        state with
            VirtualMachineScaleSet = vmss.ResourceId.Name
    }

    /// Sets the VMSS where this health extension should be installed.
    [<CustomOperation "os">]
    member _.Os(state: ApplicationHealthExtensionConfig, os) = { state with OS = Some os }

    /// Sets the protocol for connections to probe.
    [<CustomOperation "protocol">]
    member _.Protocol(state: ApplicationHealthExtensionConfig, protocol) = { state with Protocol = protocol }

    /// Sets the port for connections to probe.
    [<CustomOperation "port">]
    member _.Port(state: ApplicationHealthExtensionConfig, port: uint16) = { state with Port = port }

    member _.Port(state: ApplicationHealthExtensionConfig, port: int) = { state with Port = uint16 port }

    /// Sets the interval in seconds between probes.
    [<CustomOperation "interval">]
    member _.Interval(state: ApplicationHealthExtensionConfig, intervalInSeconds: int) = {
        state with
            Interval = Some(TimeSpan.FromSeconds intervalInSeconds)
    }

    member _.Interval(state: ApplicationHealthExtensionConfig, interval: TimeSpan) = {
        state with
            Interval = Some interval
    }

    /// Sets the number of probes to consider this instance as failed.
    [<CustomOperation "number_of_probes">]
    member _.NumberOfProbes(state: ApplicationHealthExtensionConfig, numberOfProbes) = {
        state with
            NumberOfProbes = numberOfProbes
    }

let applicationHealthExtension = ApplicationHealthExtensionBuilder()

type VirtualMachineScaleSetBuilder() =
    member _.Yield _ : VmScaleSetConfig = {
        Name = ResourceName.Empty
        Dependencies = Set.empty
        Vm = None
        Capacity = None
        ScaleInPolicy = None
        UpgradePolicy = None
        AutomaticRepairsPolicy = None
        Autoscale = None
        Overprovision = None
        RunExtensionsOnOverprovisionedVMs = None
        AvailabilityZones = []
        HealthProbeId = None
        LoadBalancerBackendAddressPools = []
        Extensions = []
        ZoneBalance = None
        Tags = Map.empty
    }

    member _.Run(state: VmScaleSetConfig) =
        match state.Vm with
        | Some vm ->
            match vm.AcceleratedNetworking with
            | Some Enabled ->
                match vm.Size with
                | NetworkInterface.AcceleratedNetworkingUnsupported ->
                    raiseFarmer $"Accelerated networking unsupported for specified VM size '{vm.Size.ArmValue}'."
                | NetworkInterface.AcceleratedNetworkingSupported -> ()
            | _ -> ()
        | None -> ()

        // Using automatic repair policy requires the health extension.
        match state.AutomaticRepairsPolicy with
        | Some { Enabled = true; GracePeriod = _ } ->
            if
                state.Extensions
                |> List.exists (function
                    | :? ApplicationHealthExtensionConfig -> true
                    | _ -> false)
                |> not
            then
                raiseFarmer "Enabling automatic repairs requires adding the application health extension."
        | _ -> ()

        // Name the VM for the scale set so generated resources are named properly.
        {
            state with
                Vm =
                    state.Vm
                    |> Option.map (fun vm -> {
                        vm with
                            Name = vm.Name.IfEmpty state.Name.Value
                    })
        }

    /// Sets the name of the VM.
    [<CustomOperation "name">]
    member _.Name(state: VmScaleSetConfig, name) = { state with Name = name }

    member this.Name(state: VmScaleSetConfig, name) = this.Name(state, ResourceName name)

    interface ITaggable<VmScaleSetConfig> with
        member _.Add state tags = {
            state with
                Tags = state.Tags |> Map.merge tags
        }

    interface IDependable<VmScaleSetConfig> with
        member _.Add state newDeps = {
            state with
                Dependencies = state.Dependencies + newDeps
        }

    /// Defines the VM settings to use when provisioning VMs for this scale set.
    [<CustomOperation "vm_profile">]
    member _.VmProfile(state: VmScaleSetConfig, vm: VmConfig) =
        let vmSize = // Fix up the VM size since the current default is not supported.
            match vm.Size with
            | Basic_A0
            | Basic_A1 -> Standard_A1_v2
            | Basic_A2 -> Standard_A2_v2
            | Basic_A3 -> Standard_A3
            | Basic_A4 -> Standard_A4
            | _ -> vm.Size

        {
            state with
                Vm = Some { vm with Size = vmSize }
        }

    [<CustomOperation "add_availability_zones">]
    member _.AddAvailabilityZone(state: VmScaleSetConfig, zones: string list) = {
        state with
            AvailabilityZones = state.AvailabilityZones @ zones
    }

    /// Add extensions.
    [<CustomOperation "add_extensions">]
    member _.AddExtensions(state: VmScaleSetConfig, extensions: IExtensionBuilder list) = {
        state with
            Extensions = state.Extensions @ extensions
    }

    /// Enable automatic repairs.
    [<CustomOperation "automatic_repair_policy">]
    member _.AutomaticRepairPolicy(state: VmScaleSetConfig, policy: ScaleSetAutomaticRepairsPolicy) = {
        state with
            AutomaticRepairsPolicy = Some(policy)
    }

    [<CustomOperation "automatic_repair_enabled_after">]
    member _.AutomaticRepairPolicy(state: VmScaleSetConfig, timespan: TimeSpan) =
        let policy = {
            ScaleSetAutomaticRepairsPolicy.Enabled = true
            GracePeriod = timespan
        }

        {
            state with
                AutomaticRepairsPolicy = Some(policy)
        }

    [<CustomOperation "autoscale">]
    member _.Autoscale(state: VmScaleSetConfig, autoscaleSettings: AutoscaleSettings) = {
        state with
            Autoscale = Some autoscaleSettings
    }

    [<CustomOperation "capacity">]
    member _.Capacity(state: VmScaleSetConfig, capacity: int) = { state with Capacity = Some capacity }

    [<CustomOperation "overprovision">]
    member _.Overprovision(state: VmScaleSetConfig, enable) = {
        state with
            Overprovision = Some enable
    }

    [<CustomOperation "run_extensions_on_overprovisioned_vms">]
    member _.RunExtensionsOnOverprovisionedVMs(state: VmScaleSetConfig, enable) = {
        state with
            RunExtensionsOnOverprovisionedVMs = Some enable
    }

    [<CustomOperation "health_probe">]
    member _.HealthProbeId(state: VmScaleSetConfig, healthProbeId: ResourceId) = {
        state with
            HealthProbeId = Some healthProbeId
    }

    member this.HealthProbeId(state: VmScaleSetConfig, healthProbeId: string) =
        this.HealthProbeId(state, LoadBalancer.loadBalancerProbes.resourceId healthProbeId)

    [<CustomOperation "scale_in_policy">]
    member _.ScaleInPolicy(state: VmScaleSetConfig, scaleInPolicy: ScaleInPolicyRule) =
        let policy =
            match state.ScaleInPolicy with
            | None -> {
                ForceDeletion = false
                Rules = [ scaleInPolicy ]
              }
            | Some existing -> {
                existing with
                    Rules = [ scaleInPolicy ]
              }

        {
            state with
                ScaleInPolicy = Some policy
        }

    [<CustomOperation "scale_in_force_deletion">]
    member _.ScaleInForceDeletion(state: VmScaleSetConfig, forceDeletion: FeatureFlag) =
        let policy =
            match state.ScaleInPolicy with
            | None -> {
                ForceDeletion = forceDeletion.AsBoolean
                Rules = [ Default ]
              }
            | Some existing -> {
                existing with
                    ForceDeletion = forceDeletion.AsBoolean
              }

        {
            state with
                ScaleInPolicy = Some policy
        }

    [<CustomOperation "upgrade_mode">]
    member _.UpgradeMode(state: VmScaleSetConfig, mode: UpgradeMode) = {
        state with
            UpgradePolicy =
                state.UpgradePolicy
                |> Option.defaultValue ScaleSetUpgradePolicy.Default
                |> (fun x -> { x with Mode = mode })
                |> Some
    }

    [<CustomOperation "osupgrade_automatic">]
    member _.OSUpgrade(state: VmScaleSetConfig, enabled) = {
        state with
            UpgradePolicy =
                state.UpgradePolicy
                |> Option.defaultValue ScaleSetUpgradePolicy.Default
                |> (fun x -> {
                    x with
                        AutomaticOSUpgradePolicy =
                            x.AutomaticOSUpgradePolicy
                            |> Option.defaultValue VmssAutomaticOSUpgradePolicy.Default
                            |> (fun x -> {
                                x with
                                    EnableAutomaticOSUpgrade = Some enabled
                            })
                            |> Some
                })
                |> Some
    }

    [<CustomOperation "osupgrade_automatic_rollback">]
    member _.OSUpgradeRollback(state: VmScaleSetConfig, enabled) = {
        state with
            UpgradePolicy =
                state.UpgradePolicy
                |> Option.defaultValue ScaleSetUpgradePolicy.Default
                |> (fun x -> {
                    x with
                        AutomaticOSUpgradePolicy =
                            x.AutomaticOSUpgradePolicy
                            |> Option.defaultValue VmssAutomaticOSUpgradePolicy.Default
                            |> (fun x -> {
                                x with
                                    DisableAutomaticRollback = Some(not enabled)
                            })
                            |> Some
                })
                |> Some
    }

    [<CustomOperation "osupgrade_rolling_upgrade">]
    member _.OSUpgradeRollingUpgrade(state: VmScaleSetConfig, enabled) = {
        state with
            UpgradePolicy =
                state.UpgradePolicy
                |> Option.defaultValue ScaleSetUpgradePolicy.Default
                |> (fun x -> {
                    x with
                        AutomaticOSUpgradePolicy =
                            x.AutomaticOSUpgradePolicy
                            |> Option.defaultValue VmssAutomaticOSUpgradePolicy.Default
                            |> (fun x -> {
                                x with
                                    UseRollingUpgradePolicy = Some enabled
                            })
                            |> Some
                })
                |> Some
    }

    [<CustomOperation "osupgrade_rolling_upgrade_deferral">]
    member _.OSUpgradeRollingUpgradeDeferral(state: VmScaleSetConfig, enabled) = {
        state with
            UpgradePolicy =
                state.UpgradePolicy
                |> Option.defaultValue ScaleSetUpgradePolicy.Default
                |> (fun x -> {
                    x with
                        AutomaticOSUpgradePolicy =
                            x.AutomaticOSUpgradePolicy
                            |> Option.defaultValue VmssAutomaticOSUpgradePolicy.Default
                            |> (fun x -> {
                                x with
                                    OsRollingUpgradeDeferral = Some enabled
                            })
                            |> Some
                })
                |> Some
    }

let vmss = VirtualMachineScaleSetBuilder()