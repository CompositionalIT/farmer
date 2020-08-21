[<AutoOpen>]
module Farmer.Builders.VirtualNetworkGateway

open Farmer
open Farmer.VirtualNetworkGateway
open Farmer.Arm.Network

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
      /// Enable Border Gateway Protocol on this gateway
      EnableBgp : bool
      Tags: Map<string,string>  }
    interface IBuilder with
        member this.DependencyName = this.Name
        member this.BuildResources location = [
            if this.GatewayPublicIpName = ResourceName.Empty then
                yield { // No public IP set, so generate one named after the gateway
                    Name = ResourceName (sprintf "%s-ip" this.Name.Value)
                    Location = location
                    DomainNameLabel = None
                    Tags = this.Tags
                }
            yield {
                Name = this.Name
                Location = location
                IpConfigs = [
                    yield {| Name = ResourceName "default"
                             PrivateIpAllocationMethod = this.GatewayPrivateIpAllocationMethod
                             PublicIpName = this.GatewayPublicIpName.IfEmpty (sprintf "%s-ip" this.Name.Value) |}
                    if this.ActiveActivePublicIpName.IsSome then
                        yield {| Name = ResourceName "redundant"; PrivateIpAllocationMethod = this.ActiveActivePrivateIpAllocationMethod; PublicIpName = this.ActiveActivePublicIpName.Value |}
                ]
                VirtualNetwork = this.VirtualNetwork
                GatewayType = this.GatewayType
                VpnType = this.VpnType
                EnableBgp = this.EnableBgp
                Tags = this.Tags
            }
        ]

type VnetGatewayBuilder() =
    member __.Yield _ =
      { Name = ResourceName.Empty
        GatewayPrivateIpAllocationMethod = DynamicPrivateIp
        GatewayPublicIpName = ResourceName.Empty
        ActiveActivePrivateIpAllocationMethod = DynamicPrivateIp
        ActiveActivePublicIpName = None
        VirtualNetwork = ResourceName.Empty
        GatewayType = GatewayType.Vpn VpnGatewaySku.VpnGw1
        VpnType = VpnType.RouteBased
        EnableBgp = true
        Tags = Map.empty }
    /// Sets the name of the gateway
    [<CustomOperation "name">]
    member __.Name(state:VNetGatewayConfig, name) = { state with Name = ResourceName name }
    /// Sets the virtual network where this gateway is attached.
    [<CustomOperation "vnet">]
    member __.VNet(state:VNetGatewayConfig, vnet) = { state with VirtualNetwork = ResourceName vnet }
    /// Sets the ExpressRoute gateway type with an ExpressRoute SKU.
    [<CustomOperation "er_gateway_sku">]
    member __.ErGatewaySku(state:VNetGatewayConfig, sku ) = { state with GatewayType = GatewayType.ExpressRoute sku }
    /// Sets the VPN gateway type with a VPN SKU.
    [<CustomOperation "vpn_gateway_sku">]
    member __.VpnType(state:VNetGatewayConfig, sku) = { state with GatewayType = GatewayType.Vpn sku }
    /// Sets the VPN type with - RouteBased or PolicyBased.
    [<CustomOperation "vpn_type">]
    member __.VpnGatewaySku(state:VNetGatewayConfig, vpnType) = { state with VpnType = vpnType }
    /// Sets the default gateway IP config.
    [<CustomOperation "gateway_ip_config">]
    member __.GatewayIpConfig(state:VNetGatewayConfig, allocationMethod, publicIp:PublicIpAddress) =
        { state with GatewayPrivateIpAllocationMethod = allocationMethod; GatewayPublicIpName = publicIp.Name  }
    /// Sets the default gateway IP config and enables active-active if not already.
    [<CustomOperation "active_active_ip_config">]
    member __.ActiveActiveIpConfig(state:VNetGatewayConfig, allocationMethod, publicIpName) =
        match state.GatewayType with
        | GatewayType.ExpressRoute _ -> state // No active-active config on ER gateways
        | GatewayType.Vpn _ ->
            { state with
                ActiveActivePrivateIpAllocationMethod = allocationMethod
                ActiveActivePublicIpName = Some (ResourceName publicIpName)
            }
    /// Disable BGP (enabled by default).
    [<CustomOperation "disable_bgp">]
    member __.DisableBgp(state:VNetGatewayConfig) = { state with EnableBgp = false }
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
              Tags = this.Tags
            }
        ]

type ConnectionBuilder() =
    member __.Yield _ =
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
    member __.Name(state:ConnectionConfig, name) = { state with Name = ResourceName name }
    /// Sets the first vnet gateway
    [<CustomOperation "vnet_gateway1">]
    member __.VNetGateway1(state:ConnectionConfig, vng1) = { state with VirtualNetworkGateway1 = vng1 }
    /// Sets the first vnet gateway
    [<CustomOperation "vnet_gateway2">]
    member __.VNetGateway2(state:ConnectionConfig, vng2) = { state with VirtualNetworkGateway2 = vng2 }
    /// Sets the first vnet gateway
    [<CustomOperation "local_gateway">]
    member __.LocalGateway(state:ConnectionConfig, lng) = { state with LocalNetworkGateway = lng }
    /// Sets the first vnet gateway
    [<CustomOperation "peer_id">]
    member __.PeerId(state:ConnectionConfig, peer) = { state with PeerId = Some peer }
    /// Sets the first vnet gateway
    [<CustomOperation "auth_key">]
    member __.Authorization(state:ConnectionConfig, auth) = { state with AuthorizationKey = Some auth }
    [<CustomOperation "add_tags">]
    member _.Tags(state:ConnectionConfig, pairs) = 
        { state with 
            Tags = pairs |> List.fold (fun map (key,value) -> Map.add key value map) state.Tags }
    [<CustomOperation "add_tag">]
    member this.Tag(state:ConnectionConfig, key, value) = this.Tags(state, [ (key,value) ])

let connection = ConnectionBuilder()
