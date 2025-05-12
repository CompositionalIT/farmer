[<AutoOpen>]
module Farmer.Arm.Bastion

open Farmer
open Farmer.Arm.Network
open Farmer.Network

let bastionHosts = ResourceType("Microsoft.Network/bastionHosts", "2023-04-01")

type BastionHost = {
    Name: ResourceName
    Location: Location
    VirtualNetwork: LinkedResource
    IpConfigs: {| PublicIpName: ResourceName |} list
    Sku: BastionSku
    Tags: Map<string, string>
} with

    interface IArmResource with
        member this.ResourceId = bastionHosts.resourceId this.Name

        member this.JsonModel =
            let dependsOn = [
                match this.VirtualNetwork with
                | Managed id -> id
                | Unmanaged _ -> ()
                for config in this.IpConfigs do
                    publicIPAddresses.resourceId config.PublicIpName
            ]

            let commonProperties = {|
                ipConfigurations =
                    this.IpConfigs
                    |> List.mapi (fun index ipConfig -> {|
                        name = $"ipconfig{index + 1}"
                        properties = {|
                            publicIPAddress = {|
                                id = publicIPAddresses.resourceId(ipConfig.PublicIpName).Eval()
                            |}
                            subnet = {|
                                id =
                                    {
                                        this.VirtualNetwork.ResourceId with
                                            Segments = [ ResourceName "AzureBastionSubnet" ]
                                            Type = subnets
                                    }
                                        .Eval()
                            |}
                        |}
                    |})
            |}

            let standardProperties (opts: BastionStandardSkuOptions) = {|
                commonProperties with
                    disableCopyPaste = opts.DisableCopyPaste |> Option.map box |> Option.toObj
                    dnsName = opts.DnsName |> Option.toObj
                    enableFileCopy = opts.EnableFileCopy |> Option.map box |> Option.toObj
                    enableIpConnect = opts.EnableIpConnect |> Option.map box |> Option.toObj
                    enableKerberos = opts.EnableKerberos |> Option.map box |> Option.toObj
                    enableShareableLink = opts.EnableShareableLink |> Option.map box |> Option.toObj
                    enableTunneling = opts.EnableTunneling |> Option.map box |> Option.toObj
                    scaleUnits = opts.ScaleUnits |> Option.defaultValue 2 // If not passed, default for Standard is 2.
            |}

            {|
                bastionHosts.Create(this.Name, this.Location, dependsOn, this.Tags) with
                    sku = {| name = this.Sku.ArmValue |}
                    properties =
                        match this.Sku with
                        | Standard standardOptions -> box (standardProperties standardOptions)
                        | _ -> box commonProperties
            |}