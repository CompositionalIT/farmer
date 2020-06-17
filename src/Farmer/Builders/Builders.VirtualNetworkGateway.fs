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
      /// Enable Border Gateway Protocol on this gateway
      EnableBgp : bool }
    interface IBuilder with
        member this.DependencyName = this.Name
        member this.BuildResources location _ = [
            { Name = this.Name
              Location = location
              IpConfigs = [
                    yield {| Name = ResourceName "default"; PrivateIpAllocationMethod = this.GatewayPrivateIpAllocationMethod; PublicIpName = this.GatewayPublicIpName |}
                    if this.ActiveActivePublicIpName.IsSome then
                        yield {| Name = ResourceName "redundant"; PrivateIpAllocationMethod = this.ActiveActivePrivateIpAllocationMethod; PublicIpName = this.ActiveActivePublicIpName.Value |}
              ]
              VirtualNetwork = this.VirtualNetwork
              GatewayType = this.GatewayType
              EnableBgp = this.EnableBgp
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
        GatewayType = GatewayType.Vpn (VpnGatewaySku.VpnGw1, VpnType.RouteBased, false)
        EnableBgp = true }
    /// Sets the name of the gateway
    [<CustomOperation "name">]
    member __.Name(state:VNetGatewayConfig, name) = { state with Name = ResourceName name }
    /// Sets the virtual network where this gateway is attached.
    [<CustomOperation "vnet">]
    member __.VNet(state:VNetGatewayConfig, vnet) = { state with VirtualNetwork = ResourceName vnet }
    /// Sets the gateway type with a SKU.
    [<CustomOperation "gateway_type">]
    member __.GatewayType(state:VNetGatewayConfig, gwType) = { state with GatewayType = gwType }
    /// Sets the default gateway IP config.
    [<CustomOperation "gateway_ip_config">]
    member __.GatewayIpConfig(state:VNetGatewayConfig, allocationMethod, publicIpName) =
        { state with GatewayPrivateIpAllocationMethod = allocationMethod; GatewayPublicIpName = ResourceName publicIpName  }
    /// Sets the default gateway IP config and enables active-active if not already.
    [<CustomOperation "active_active_ip_config">]
    member __.ActiveActiveIpConfig(state:VNetGatewayConfig, allocationMethod, publicIpName) =
        match state.GatewayType with
        | GatewayType.ExpressRoute _ -> state // No active-active config on ER gateways
        | GatewayType.Vpn (sku, vpnType, _) ->
            { state with
                ActiveActivePrivateIpAllocationMethod = allocationMethod
                ActiveActivePublicIpName = Some (ResourceName publicIpName)
                GatewayType = GatewayType.Vpn (sku, vpnType, true)
            }
    /// Disable BGP (enabled by default).
    [<CustomOperation "disable_bgp">]
    member __.DisableBgp(state:VNetGatewayConfig) = { state with EnableBgp = false }

let gateway = VnetGatewayBuilder()
