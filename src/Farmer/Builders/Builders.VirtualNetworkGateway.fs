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
      GatewayPublicIpName: ResourceName option
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
      EnableBgp : bool }
    interface IBuilder with
        member this.DependencyName = this.Name
        member this.BuildResources location = [
            let defaultIpSetName = ResourceName (sprintf "%s-ip" this.Name.Value)

            // If no public IP is set, generate one named after the gateway
            if this.GatewayPublicIpName.IsNone then
                { Name = defaultIpSetName
                  Location = location
                  DomainNameLabel = None }

            { Name = this.Name
              Location = location
              IpConfigs = [
                  {| Name = ResourceName "default"
                     PrivateIpAllocationMethod = this.GatewayPrivateIpAllocationMethod
                     PublicIpName = this.GatewayPublicIpName |> Option.defaultValue defaultIpSetName |}
                  match this.ActiveActivePublicIpName with
                  | Some activeActivePublicIpName ->
                    {| Name = ResourceName "redundant"
                       PrivateIpAllocationMethod = this.ActiveActivePrivateIpAllocationMethod
                       PublicIpName = activeActivePublicIpName |}
                  | None ->
                    ()
              ]
              VirtualNetwork = this.VirtualNetwork
              GatewayType = this.GatewayType
              VpnType = this.VpnType
              EnableBgp = this.EnableBgp }
        ]

type VnetGatewayBuilder() =
    member __.Yield _ =
      { Name = ResourceName.Empty
        GatewayPrivateIpAllocationMethod = DynamicPrivateIp
        GatewayPublicIpName = None
        ActiveActivePrivateIpAllocationMethod = DynamicPrivateIp
        ActiveActivePublicIpName = None
        VirtualNetwork = ResourceName.Empty
        GatewayType = GatewayType.Vpn VpnGatewaySku.VpnGw1
        VpnType = VpnType.RouteBased
        EnableBgp = true }
    /// Sets the name of the gateway
    [<CustomOperation "name">]
    member _.Name(state:VNetGatewayConfig, name) = { state with Name = name }
    member this.Name(state:VNetGatewayConfig, name) = this.Name(state, ResourceName name)
    /// Sets the virtual network to which this gateway is attached.
    [<CustomOperation "vnet">]
    member _.VNet(state:VNetGatewayConfig, vnet) = { state with VirtualNetwork = vnet }
    member this.VNet(state:VNetGatewayConfig, vnet) = this.VNet(state, ResourceName vnet)
    member this.VNet(state:VNetGatewayConfig, vnet:VirtualNetworkConfig) = this.VNet(state, vnet.Name)
    /// Sets the ExpressRoute gateway type with an ExpressRoute SKU.
    [<CustomOperation "sku">]
    member _.Sku(state:VNetGatewayConfig, sku) = { state with GatewayType = GatewayType.ExpressRoute sku }
    member _.Sku(state:VNetGatewayConfig, sku) = { state with GatewayType = GatewayType.Vpn sku }
    /// Sets the VPN type with - RouteBased or PolicyBased.
    [<CustomOperation "vpn_type">]
    member _.VpnType(state:VNetGatewayConfig, vpnType) = { state with VpnType = vpnType }
    /// Sets the default gateway IP config.
    [<CustomOperation "gateway_ip_config">]
    member _.GatewayIpConfig(state:VNetGatewayConfig, allocationMethod, publicIp) =
        { state with GatewayPrivateIpAllocationMethod = allocationMethod; GatewayPublicIpName = Some publicIp }
    member this.GatewayIpConfig(state:VNetGatewayConfig, allocationMethod, publicIp:PublicIpAddress) =
        this.GatewayIpConfig(state, allocationMethod, publicIp.Name)
    /// Sets the default gateway IP config and enables active-active if not already.
    [<CustomOperation "active_active_ip_config">]
    member _.ActiveActiveIpConfig(state:VNetGatewayConfig, allocationMethod, publicIpName) =
        match state.GatewayType with
        | GatewayType.ExpressRoute _ ->
            // No active-active config on ER gateways
            state
        | GatewayType.Vpn _ ->
            { state with
                ActiveActivePrivateIpAllocationMethod = allocationMethod
                ActiveActivePublicIpName = Some (ResourceName publicIpName) }
    /// Disable BGP (enabled by default).
    [<CustomOperation "disable_bgp">]
    member _.DisableBgp(state:VNetGatewayConfig) = { state with EnableBgp = false }

let gateway = VnetGatewayBuilder()

type ConnectionConfig =
    { Name : ResourceName
      ConnectionType : ConnectionType
      VirtualNetworkGateway1 : ResourceName
      VirtualNetworkGateway2 : ResourceName option
      LocalNetworkGateway : ResourceName option
      PeerId : string option
      AuthorizationKey : string option }
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
        AuthorizationKey = None }
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

let connection = ConnectionBuilder()
