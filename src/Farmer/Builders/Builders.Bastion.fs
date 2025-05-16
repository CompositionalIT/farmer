[<AutoOpen>]
module Farmer.Builders.Bastion

open Farmer
open Farmer.Arm
open Farmer.Network
open Farmer.Arm.Bastion
open Farmer.Arm.Network
open Farmer.PublicIpAddress

type BastionConfig = {
    Name: ResourceName
    VirtualNetwork: LinkedResource option
    Sku: BastionSku
    Tags: Map<string, string>
} with

    interface IBuilder with
        member this.ResourceId = bastionHosts.resourceId this.Name

        member this.BuildResources location =
            match this.VirtualNetwork with
            | None -> raiseFarmer "Bastion requires 'vnet' to be set."
            | Some vnetRes ->
                let publicIpName = ResourceName $"{this.Name.Value}-ip"

                [
                    // IP Address
                    {
                        Name = publicIpName
                        AvailabilityZones = NoZone
                        Location = location
                        AllocationMethod = AllocationMethod.Static
                        AddressVersion = Network.AddressVersion.IPv4
                        Sku = PublicIpAddress.Sku.Standard
                        DomainNameLabel = None
                        Tags = this.Tags
                    }
                    // Bastion
                    {
                        BastionHost.Name = this.Name
                        Location = location
                        VirtualNetwork = vnetRes
                        IpConfigs = [ {| PublicIpName = publicIpName |} ]
                        Sku = this.Sku
                        Tags = this.Tags
                    }
                ]

type BastionStandardSkuOptions with

    /// Default set of options for the standard SKU.
    static member Default = {
        DisableCopyPaste = None
        DnsName = None
        EnableFileCopy = None
        EnableIpConnect = None
        EnableKerberos = None
        EnableShareableLink = None
        EnableTunneling = None
        ScaleUnits = Some 2
    }

    /// Takes the 'Some' value from either option, with preference given to the one being merged in if they
    /// both already have 'Some' value.
    member internal this.MergeOptions(opts: BastionStandardSkuOptions) =
        let mergeTwoOptions (existingOpt, newOpt) =
            match existingOpt, newOpt with
            | None, None -> None
            | Some _, None -> existingOpt
            | _, Some _ -> newOpt

        {
            this with
                DisableCopyPaste = mergeTwoOptions (this.DisableCopyPaste, opts.DisableCopyPaste)
                DnsName = mergeTwoOptions (this.DnsName, opts.DnsName)
                EnableFileCopy = mergeTwoOptions (this.EnableFileCopy, opts.EnableFileCopy)
                EnableIpConnect = mergeTwoOptions (this.EnableIpConnect, opts.EnableIpConnect)
                EnableKerberos = mergeTwoOptions (this.EnableKerberos, opts.EnableKerberos)
                EnableShareableLink = mergeTwoOptions (this.EnableShareableLink, opts.EnableShareableLink)
                EnableTunneling = mergeTwoOptions (this.EnableTunneling, opts.EnableTunneling)
                ScaleUnits = mergeTwoOptions (this.ScaleUnits, opts.ScaleUnits)
        }

type BastionBuilder() =
    member _.Yield _ = {
        Name = ResourceName.Empty
        VirtualNetwork = None
        Sku = BastionSku.Basic
        Tags = Map.empty
    }

    member _.Run bastionConfig =
        if bastionConfig.VirtualNetwork = None then
            raiseFarmer "Bastion requires 'vnet' to be set."

        bastionConfig

    /// Sets the name of the bastion host.
    [<CustomOperation "name">]
    member _.Name(state: BastionConfig, name) = { state with Name = ResourceName name }

    /// Sets the virtual network where this bastion host is attached.
    [<CustomOperation "vnet">]
    member _.VNet(state: BastionConfig, vnet: string) = {
        state with
            VirtualNetwork = virtualNetworks.resourceId vnet |> Managed |> Some
    }

    member _.VNet(state: BastionConfig, vnet: ResourceId) = {
        state with
            VirtualNetwork = vnet |> Managed |> Some
    }

    member _.VNet(state: BastionConfig, vnet: IBuilder) = {
        state with
            VirtualNetwork = vnet.ResourceId |> Managed |> Some
    }

    [<CustomOperation "link_to_vnet">]
    member _.LinkToVNet(state: BastionConfig, vnet: string) = {
        state with
            VirtualNetwork = virtualNetworks.resourceId vnet |> Unmanaged |> Some
    }

    member _.LinkToVNet(state: BastionConfig, vnet: ResourceId) = {
        state with
            VirtualNetwork = vnet |> Unmanaged |> Some
    }

    member _.LinkToVNet(state: BastionConfig, vnet: IBuilder) = {
        state with
            VirtualNetwork = vnet.ResourceId |> Unmanaged |> Some
    }

    /// Upgrades the SKU to Standard when using any of those options.
    static member private upgradeSku(sku, standardSkuOptions) : BastionSku =
        match sku with
        | BastionSku.Basic
        | BastionSku.Developer -> BastionSku.Standard standardSkuOptions
        | BastionSku.Standard opts -> BastionSku.Standard(opts.MergeOptions standardSkuOptions)

    [<CustomOperation "scale_units">]
    member _.Sku(state: BastionConfig, scaleUnits) =
        if scaleUnits > 50 then
            raiseFarmer "Bastion standard sku supports a maximum of 50 scale units."

        let newOpt = {
            BastionStandardSkuOptions.Default with
                ScaleUnits = Some scaleUnits
        }

        {
            state with
                Sku = BastionBuilder.upgradeSku (state.Sku, newOpt)
        }

    [<CustomOperation "disable_copy_paste">]
    member _.DisableCopyPaste(state: BastionConfig, disableCopyPaste) =
        let newOpt = {
            BastionStandardSkuOptions.Default with
                DisableCopyPaste = Some disableCopyPaste
        }

        {
            state with
                Sku = BastionBuilder.upgradeSku (state.Sku, newOpt)
        }

    [<CustomOperation "dns_name">]
    member _.DnsName(state: BastionConfig, dnsName) =
        let newOpt = {
            BastionStandardSkuOptions.Default with
                DnsName = Some dnsName
        }

        {
            state with
                Sku = BastionBuilder.upgradeSku (state.Sku, newOpt)
        }

    [<CustomOperation "enable_file_copy">]
    member _.EnableFileCopy(state: BastionConfig, enable) =
        let newOpt = {
            BastionStandardSkuOptions.Default with
                EnableFileCopy = Some enable
        }

        {
            state with
                Sku = BastionBuilder.upgradeSku (state.Sku, newOpt)
        }

    [<CustomOperation "enable_ip_connect">]
    member _.EnableIpConnect(state: BastionConfig, enable) =
        let newOpt = {
            BastionStandardSkuOptions.Default with
                EnableIpConnect = Some enable
        }

        {
            state with
                Sku = BastionBuilder.upgradeSku (state.Sku, newOpt)
        }

    [<CustomOperation "enable_kerberos">]
    member _.EnableKerberos(state: BastionConfig, enable) =
        let newOpt = {
            BastionStandardSkuOptions.Default with
                EnableKerberos = Some enable
        }

        {
            state with
                Sku = BastionBuilder.upgradeSku (state.Sku, newOpt)
        }

    [<CustomOperation "enable_shareable_link">]
    member _.EnableShareableLink(state: BastionConfig, enable) =
        let newOpt = {
            BastionStandardSkuOptions.Default with
                EnableShareableLink = Some enable
        }

        {
            state with
                Sku = BastionBuilder.upgradeSku (state.Sku, newOpt)
        }

    [<CustomOperation "enable_tunneling">]
    member _.EnableTunneling(state: BastionConfig, enable) =
        let newOpt = {
            BastionStandardSkuOptions.Default with
                EnableTunneling = Some enable
        }

        {
            state with
                Sku = BastionBuilder.upgradeSku (state.Sku, newOpt)
        }

let bastion = BastionBuilder()