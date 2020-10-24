[<AutoOpen>]
module Farmer.Builders.VirtualNetworkGateway

open Farmer
open Farmer.PublicIpAddress
open Farmer.VirtualNetworkGateway
open Farmer.Arm.Network

type VpnClientConfig =
    { ClientAddressPools: IPAddressCidr list
      ClientRootCertificates:
          {| Name: string
             PublicCertData: string |} list
      ClientRevokedCertificates:
          {| Name: string
             Thumbprint: string |} list
      ClientProtocols: VPNClientProtocol list
    }

type VNetGatewayConfig =
    { /// The name of the gateway
      Name : ResourceName
      /// Private IP allocation method for the gateway's primary interface
      GatewayPrivateIpAllocationMethod : PrivateIpAllocationMethod
      /// Public IP for the gateway's interface
      GatewayPublicIpName: ResourceName
      /// Private IP allocation method for the gateway's secondary interface if Active-Active
      ActiveActivePrivateIpAllocationMethod : PrivateIpAllocationMethod
      /// Public IP for the gateway's secondary interface if Active-Active
      ActiveActivePublicIpName: ResourceName option
      /// Virtual network where the gateway will be attached
      VirtualNetwork : ResourceName
      /// Gateway type - ExpressRoute or VPN
      GatewayType : GatewayType
      /// VPN type - RouteBased or PolicyBased
      VpnType : VpnType
      /// VPN client configuration for Point to Site connexion
      VpnClientConfiguration: VpnClientConfig option
      /// Enable Border Gateway Protocol on this gateway
      EnableBgp : bool
      Tags: Map<string,string>  }
    interface IBuilder with
        member this.DependencyName = this.Name
        member this.BuildResources location = [
            if this.GatewayPublicIpName = ResourceName.Empty then
                { // No public IP set, so generate one named after the gateway
                    Name = ResourceName (sprintf "%s-ip" this.Name.Value)
                    AllocationMethod = AllocationMethod.Dynamic
                    Location = location
                    Sku = PublicIpAddress.Sku.Basic
                    DomainNameLabel = None
                    Tags = this.Tags }
            { Name = this.Name
              Location = location
              IpConfigs = [
                {| Name = ResourceName "default"
                   PrivateIpAllocationMethod = this.GatewayPrivateIpAllocationMethod
                   PublicIpName = this.GatewayPublicIpName.IfEmpty (sprintf "%s-ip" this.Name.Value) |}
                if this.ActiveActivePublicIpName.IsSome then
                    {| Name = ResourceName "redundant"; PrivateIpAllocationMethod = this.ActiveActivePrivateIpAllocationMethod; PublicIpName = this.ActiveActivePublicIpName.Value |}
              ]
              VirtualNetwork = this.VirtualNetwork
              GatewayType = this.GatewayType
              VpnType = this.VpnType
              EnableBgp = this.EnableBgp
              VpnClientConfiguration =
                  this.VpnClientConfiguration
                  |> Option.map (fun config ->
                      { VpnClientConfiguration.ClientAddressPools = config.ClientAddressPools
                        ClientRootCertificates = config.ClientRootCertificates
                        ClientRevokedCertificates = config.ClientRevokedCertificates
                        ClientProtocols = config.ClientProtocols } )
              Tags = this.Tags }
        ]

type VpnClientConfigurationBuilder() =
    member _.Yield _ =
        { ClientAddressPools = []
          ClientRootCertificates = []
          ClientRevokedCertificates = []
          ClientProtocols = [] }

    member _.Run(state: VpnClientConfig) =
        match state.ClientProtocols with
        | [] ->
            { state with ClientProtocols = [ SSTP ]}
        | _ ->
            state


    /// Adds an address pool which represents Address space for P2S VpnClient
    [<CustomOperation "add_address_pool">]
    member _.AddAddressPool(state: VpnClientConfig, prefix: IPAddressCidr) =
        { state  with ClientAddressPools = state.ClientAddressPools @ [ prefix ] }
    member this.AddAddressPool(state: VpnClientConfig, prefix: string) =
        this.AddAddressPool(state, IPAddressCidr.parse prefix )

    /// Adds the public root certificate to authenticate client VPN connexions
    [<CustomOperation "add_root_certificate">]
    member _.AddRootCertificate(state: VpnClientConfig, name: string, publicCertificate: string) =
        let certData =
            if System.Text.RegularExpressions.Regex.IsMatch(publicCertificate,"\s*-----BEGIN CERTIFICATE-----", System.Text.RegularExpressions.RegexOptions.Multiline) then
                publicCertificate.Split('\r','\n')
                |> Seq.filter(fun l -> not (l.StartsWith "-----" && l.EndsWith "-----"))
                |> String.concat ""
            else
                publicCertificate

        { state  with ClientRootCertificates = state.ClientRootCertificates @ [ {| Name = name; PublicCertData = certData |} ] }

    /// Adds the thumbprint of a revoked client certificate.
    [<CustomOperation "add_revoked_certificate">]
    member _.AddRevokedCertificate(state: VpnClientConfig, name: string, thumbprint: string) =
        { state  with ClientRevokedCertificates = state.ClientRevokedCertificates @ [ {| Name = name; Thumbprint = thumbprint |} ] }

    /// Sets the protocols for the client VPN connexion. Default is SSTP
    [<CustomOperation "protocols">]
    member _.SetProtocols(state: VpnClientConfig, protocols: VPNClientProtocol list) =
        { state  with ClientProtocols = protocols  }

let vpnclient = VpnClientConfigurationBuilder()

type VnetGatewayBuilder() =
    member _.Yield _ =
      { Name = ResourceName.Empty
        GatewayPrivateIpAllocationMethod = DynamicPrivateIp
        GatewayPublicIpName = ResourceName.Empty
        ActiveActivePrivateIpAllocationMethod = DynamicPrivateIp
        ActiveActivePublicIpName = None
        VirtualNetwork = ResourceName.Empty
        GatewayType = GatewayType.Vpn VpnGatewaySku.VpnGw1
        VpnType = VpnType.RouteBased
        EnableBgp = true
        VpnClientConfiguration = None
        Tags = Map.empty }
    /// Sets the name of the gateway
    [<CustomOperation "name">]
    member _.Name(state:VNetGatewayConfig, name) = { state with Name = ResourceName name }
    /// Sets the virtual network where this gateway is attached.
    [<CustomOperation "vnet">]
    member _.VNet(state:VNetGatewayConfig, vnet) = { state with VirtualNetwork = ResourceName vnet }
    /// Sets the ExpressRoute gateway type with an ExpressRoute SKU.
    [<CustomOperation "er_gateway_sku">]
    member _.ErGatewaySku(state:VNetGatewayConfig, sku ) = { state with GatewayType = GatewayType.ExpressRoute sku }
    /// Sets the VPN gateway type with a VPN SKU.
    [<CustomOperation "vpn_gateway_sku">]
    member _.VpnType(state:VNetGatewayConfig, sku) = { state with GatewayType = GatewayType.Vpn sku }
    /// Sets the VPN type with - RouteBased or PolicyBased.
    [<CustomOperation "vpn_type">]
    member _.VpnGatewaySku(state:VNetGatewayConfig, vpnType) = { state with VpnType = vpnType }
    /// Sets the default gateway IP config.
    [<CustomOperation "gateway_ip_config">]
    member _.GatewayIpConfig(state:VNetGatewayConfig, allocationMethod, publicIp:PublicIpAddress) =
        { state with GatewayPrivateIpAllocationMethod = allocationMethod; GatewayPublicIpName = publicIp.Name  }
    /// Sets the default gateway IP config and enables active-active if not already.
    [<CustomOperation "active_active_ip_config">]
    member _.ActiveActiveIpConfig(state:VNetGatewayConfig, allocationMethod, publicIpName) =
        match state.GatewayType with
        | GatewayType.ExpressRoute _ -> state // No active-active config on ER gateways
        | GatewayType.Vpn _ ->
            { state with
                ActiveActivePrivateIpAllocationMethod = allocationMethod
                ActiveActivePublicIpName = Some (ResourceName publicIpName) }
    /// Disable BGP (enabled by default).
    [<CustomOperation "disable_bgp">]
    member _.DisableBgp(state:VNetGatewayConfig) = { state with EnableBgp = false }
    [<CustomOperation "vpn_client">]
    /// Sets the VPN Client configuration.
    member _.SetVpnClient(state:VNetGatewayConfig, vpnClientConfig) =
        { state with VpnClientConfiguration = Some vpnClientConfig }
    [<CustomOperation "add_tags">]
    member _.Tags(state:VNetGatewayConfig, pairs) =
        { state with
            Tags = pairs |> List.fold (fun map (key,value) -> Map.add key value map) state.Tags }
    [<CustomOperation "add_tag">]
    member this.Tag(state:VNetGatewayConfig, key, value) = this.Tags(state, [ (key,value) ])

let gateway = VnetGatewayBuilder()

type ConnectionConfig =
    { Name : ResourceName
      ConnectionType : ConnectionType
      VirtualNetworkGateway1 : ResourceName
      VirtualNetworkGateway2 : ResourceName option
      LocalNetworkGateway : ResourceName option
      PeerId : string option
      AuthorizationKey : string option
      Tags: Map<string,string> }
    interface IBuilder with
        member this.DependencyName = this.Name
        member this.BuildResources location = [
            { Name = this.Name
              Location = location
              ConnectionType = this.ConnectionType
              VirtualNetworkGateway1 = this.VirtualNetworkGateway1
              VirtualNetworkGateway2 = this.VirtualNetworkGateway2
              LocalNetworkGateway = this.LocalNetworkGateway
              PeerId = this.PeerId
              AuthorizationKey = this.AuthorizationKey
              Tags = this.Tags }
        ]

type ConnectionBuilder() =
    member _.Yield _ =
      { Name = ResourceName.Empty
        ConnectionType = ConnectionType.ExpressRoute
        VirtualNetworkGateway1 = ResourceName.Empty
        VirtualNetworkGateway2 = None
        LocalNetworkGateway = None
        PeerId = None
        AuthorizationKey = None
        Tags = Map.empty }
    /// Sets the name of the connection
    [<CustomOperation "name">]
    member _.Name(state:ConnectionConfig, name) = { state with Name = ResourceName name }
    /// Sets the first vnet gateway
    [<CustomOperation "vnet_gateway1">]
    member _.VNetGateway1(state:ConnectionConfig, vng1) = { state with VirtualNetworkGateway1 = vng1 }
    /// Sets the first vnet gateway
    [<CustomOperation "vnet_gateway2">]
    member _.VNetGateway2(state:ConnectionConfig, vng2) = { state with VirtualNetworkGateway2 = vng2 }
    /// Sets the first vnet gateway
    [<CustomOperation "local_gateway">]
    member _.LocalGateway(state:ConnectionConfig, lng) = { state with LocalNetworkGateway = lng }
    /// Sets the first vnet gateway
    [<CustomOperation "peer_id">]
    member _.PeerId(state:ConnectionConfig, peer) = { state with PeerId = Some peer }
    /// Sets the first vnet gateway
    [<CustomOperation "auth_key">]
    member _.Authorization(state:ConnectionConfig, auth) = { state with AuthorizationKey = Some auth }
    [<CustomOperation "add_tags">]
    member _.Tags(state:ConnectionConfig, pairs) =
        { state with
            Tags = pairs |> List.fold (fun map (key,value) -> Map.add key value map) state.Tags }
    [<CustomOperation "add_tag">]
    member this.Tag(state:ConnectionConfig, key, value) = this.Tags(state, [ (key,value) ])

let connection = ConnectionBuilder()
