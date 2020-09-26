module VirtualNetworkGateway


open Expecto
open Farmer
open Farmer.Sql
open Farmer.Builders
open System
open Microsoft.Rest
open Farmer.Arm.Network


let tests = testList "VirtualNetworkGateway" [
    test "Can create a basic VirtualNetworkGateway" {
        let b = gateway { name "gateway" } :> IBuilder
        let resources = b.BuildResources Location.WestEurope
        let gw = resources |> List.pick (function :? VirtualNetworkGateway as v -> Some v | _ -> None)
        Expect.equal gw.Name.Value "gateway" "Incorrect Resource Name"
        Expect.equal gw.Location Location.WestEurope "Incorrect location"
        Expect.isNone gw.VpnClientConfiguration "Incorrect VpnClientConfiguration"
    }

    test "Can create a VirtualNetworkGateway attached to a vnet" {
        let b = gateway { name "gateway"; vnet "vnet"} :> IBuilder
        let resources = b.BuildResources Location.WestEurope
        let gw = resources |> List.pick (function :? VirtualNetworkGateway as v -> Some v | _ -> None)
        Expect.equal gw.VirtualNetwork.Value "vnet" "Incorrect Virtual network"
    }

    test "Can create a VirtualNetworkGateway with VpnClientConfiguration" {
        let b =
            gateway {
                name "gateway"
                vpn_client (vpnclient
                    { add_address_pool "10.31.0.0/16"
                      add_address_pool "10.32.1.0/24" })
            } :> IBuilder
        let resources = b.BuildResources Location.WestEurope
        let gw = resources |> List.pick (function :? VirtualNetworkGateway as v -> Some v | _ -> None)
        Expect.isSome gw.VpnClientConfiguration  "Incorrect VpnClientConfiguration"
        Expect.equal gw.VpnClientConfiguration.Value.ClientAddressPools
                    [ { Address = Net.IPAddress.Parse "10.31.0.0"; Prefix = 16 }
                      { Address = Net.IPAddress.Parse "10.32.1.0"; Prefix = 24 }] "Incorect client pools"
    }
    test "Can create a VirtualNetworkGateway with root cert" {
        let b =
            gateway {
                name "gateway"
                vpn_client (
                    vpnclient {
                      add_root_certificate "root" "certdata"
                      add_root_certificate "root2" """
-----BEGIN CERTIFICATE-----
IQfNUTod7Jl7ZOacFlV3fvJTANBgkqh
TER7A0qo591ewpAPMpugHh9eQ3ucR5o
-----END CERTIFICATE-----"""
                    })
            } :> IBuilder
        let resources = b.BuildResources Location.WestEurope
        let gw = resources |> List.pick (function :? VirtualNetworkGateway as v -> Some v | _ -> None)
        Expect.isSome gw.VpnClientConfiguration  "Incorrect VpnClientConfiguration"
        Expect.equal
            gw.VpnClientConfiguration.Value.ClientRootCertificates
            [ {| Name = "root"; PublicCertData = "certdata" |}
              {| Name = "root2"; PublicCertData = "IQfNUTod7Jl7ZOacFlV3fvJTANBgkqhTER7A0qo591ewpAPMpugHh9eQ3ucR5o" |} ]
              "Incorect Root Certificates"
    }

    test "Can create a VirtualNetworkGateway with revoked client certs" {
        let b =
            gateway {
                name "gateway"
                vpn_client (vpnclient
                    {   add_revoked_certificate "revoked" "certdata"
                        add_revoked_certificate "revoked2" "certdata2"
                    })
            } :> IBuilder
        let resources = b.BuildResources Location.WestEurope
        let gw = resources |> List.pick (function :? VirtualNetworkGateway as v -> Some v | _ -> None)
        Expect.isSome gw.VpnClientConfiguration  "Incorrect VpnClientConfiguration"
        Expect.equal
            gw.VpnClientConfiguration.Value.ClientRevokedCertificates
            [ {| Name = "revoked"; Thumbprint = "certdata" |}
              {| Name = "revoked2"; Thumbprint = "certdata2" |} ]
            "Incorect Revoked Certificates"
    }

    test "Can create a VirtualNetworkGateway with protocols" {
        let b =
            gateway {
                name "gateway"
                vpn_client (vpnclient
                    {   protocols [ OpenVPN; IkeV2 ]
                    })
            } :> IBuilder
        let resources = b.BuildResources Location.WestEurope
        let gw = resources |> List.pick (function :? VirtualNetworkGateway as v -> Some v | _ -> None)
        Expect.isSome gw.VpnClientConfiguration  "Incorrect VpnClientConfiguration"
        Expect.equal gw.VpnClientConfiguration.Value.ClientProtocols [ OpenVPN; IkeV2 ] "Incorect Protocols"
    }

    test "Can create a VirtualNetworkGateway with default protocol" {
        let b =
            gateway {
                name "gateway"
                vpn_client (vpnclient
                    {   add_address_pool "10.31.0.0/16" })
            } :> IBuilder
        let resources = b.BuildResources Location.WestEurope
        let gw = resources |> List.pick (function :? VirtualNetworkGateway as v -> Some v | _ -> None)
        Expect.isSome gw.VpnClientConfiguration  "Incorrect VpnClientConfiguration"
        Expect.equal gw.VpnClientConfiguration.Value.ClientProtocols [ SSTP ] "Incorect Protocols"
    }
]
