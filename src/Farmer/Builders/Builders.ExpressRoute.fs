[<AutoOpen>]
module Farmer.Builders.ExpressRoute

open Farmer
open Farmer.CoreTypes
open Farmer.ExpressRoute
open System.Net
open Farmer.Arm.Network

/// An IP address block in CIDR notation, such as 10.100.0.0/16.
type ExpressRouteCircuitPeering =
    { PeeringType : PeeringType
      AzureASN : int
      PeerASN : int64
      /// A /30 IP address block to use for the primary link
      PrimaryPeerAddressPrefix : IPAddressCidr
      /// A /30 IP address block to use for the secondary link
      SecondaryPeerAddressPrefix : IPAddressCidr
      SharedKey : string option
      VlanId : int }

type ExpressRouteCircuitPeeringConfig =
    { /// The peering type
      PeeringType : PeeringType
      /// Azure-side BGP Autonomous System Number (ASN)
      AzureASN : int
      /// Peer-side BGP Autonomous System Number (ASN)
      PeerASN : int64
      /// A /30 IP address block to use for the primary link
      PrimaryPeerAddressPrefix : IPAddressCidr
      /// A /30 IP address block to use for the secondary link
      SecondaryPeerAddressPrefix : IPAddressCidr
      /// An optional shared key that can be used when creating the peering
      SharedKey : string option
      /// The VLAN tag.
      VlanId : int }

type ExpressRouteCircuitPeeringBuilder() =
    member __.Yield _ =
      { PeeringType = AzurePrivatePeering
        AzureASN = 0
        PeerASN = 0L
        PrimaryPeerAddressPrefix = { Address = IPAddress.None; Prefix = 0 }
        SecondaryPeerAddressPrefix = { Address = IPAddress.None; Prefix = 0 }
        SharedKey = None
        VlanId = 0 }
    /// Sets the peering type.
    [<CustomOperation "peering_type">]
    member __.PeeringType (state:ExpressRouteCircuitPeeringConfig, peeringType) = { state with PeeringType = peeringType }
    [<CustomOperation "azure_asn">]
    member __.AzureASN (state:ExpressRouteCircuitPeeringConfig, azureAsn) = { state with AzureASN = azureAsn }
    [<CustomOperation "peer_asn">]
    member __.PeerASN (state:ExpressRouteCircuitPeeringConfig, peerAsn) = { state with PeerASN = peerAsn }
    [<CustomOperation "primary_prefix">]
    member __.PrimaryPeerAddressPrefix (state:ExpressRouteCircuitPeeringConfig, primaryPrefix) = { state with PrimaryPeerAddressPrefix = primaryPrefix }
    [<CustomOperation "secondary_prefix">]
    member __.SecondaryPeerAddressPrefix (state:ExpressRouteCircuitPeeringConfig, secondaryPrefix) = { state with SecondaryPeerAddressPrefix = secondaryPrefix }
    [<CustomOperation "shared_key">]
    member __.SharedKey (state:ExpressRouteCircuitPeeringConfig, sharedKey) = { state with SharedKey = Some sharedKey }
    [<CustomOperation "vlan">]
    member __.VlanId (state:ExpressRouteCircuitPeeringConfig, vlan) = { state with VlanId = vlan }
let peering = ExpressRouteCircuitPeeringBuilder()

type ExpressRouteConfig =
  { /// The name of the express route circuit
    Name : ResourceName
    /// Tier of the circuit (standard or premium)
    Tier : Tier
    /// Unlimited or metered data
    Family : Family
    /// The service provider name for the circuit
    ServiceProviderName : string
    /// A valid peering location
    PeeringLocation : string
    /// Bandwidth in Mbps
    Bandwidth : int<Mbps>
    /// Indicates if global reach is enabled on this circuit
    GlobalReachEnabled : bool
    /// Peerings on this circuit
    Peerings : ExpressRouteCircuitPeeringConfig list
    Tags: Map<string,string>  }

    interface IBuilder with
        member this.DependencyName = this.Name
        member this.BuildResources location = [
            { Name = this.Name
              Location = location
              Tier = this.Tier
              Family = this.Family
              ServiceProviderName = this.ServiceProviderName
              PeeringLocation = this.PeeringLocation
              Bandwidth = this.Bandwidth
              GlobalReachEnabled = this.GlobalReachEnabled
              Peerings = [
                  for peering in this.Peerings do
                      {| PeeringType = peering.PeeringType
                         AzureASN = peering.AzureASN
                         PeerASN = peering.PeerASN
                         PrimaryPeerAddressPrefix = peering.PrimaryPeerAddressPrefix
                         SecondaryPeerAddressPrefix = peering.SecondaryPeerAddressPrefix
                         SharedKey = peering.SharedKey
                         VlanId = peering.VlanId |}
              ]
              Tags = this.Tags
            }
        ]

type ExpressRouteBuilder() =
    member __.Yield _ =
      { Name = ResourceName.Empty
        Tier = Standard
        Family = MeteredData
        ServiceProviderName = ""
        PeeringLocation = ""
        Bandwidth = 50<Mbps>
        GlobalReachEnabled = false
        Peerings = [] 
        Tags = Map.empty}
    /// Sets the name of the circuit
    [<CustomOperation "name">]
    member __.Name(state:ExpressRouteConfig, name) = { state with Name = ResourceName name }
    /// Sets the tier of the circuit (standard or premium - default standard).
    [<CustomOperation "tier">]
    member __.Tier(state:ExpressRouteConfig, tier) = { state with Tier = tier }
    /// Sets the family of the circuit (metered or unlimited - default:metered).
    [<CustomOperation "family">]
    member __.Family(state:ExpressRouteConfig, family) = { state with Family = family }
    /// Sets the service provider for of the circuit.
    [<CustomOperation "service_provider">]
    member __.ServiceProviderName(state:ExpressRouteConfig, provider) = { state with ServiceProviderName = provider }
    /// Sets the peering location for this circuit.
    [<CustomOperation "peering_location">]
    member __.PeeringLocation(state:ExpressRouteConfig, location) = { state with PeeringLocation = location }
    /// Sets the tier of the circuit (standard or premium).
    [<CustomOperation "bandwidth">]
    member __.Bandwidth(state:ExpressRouteConfig, bandwidth) = { state with Bandwidth = bandwidth }
    /// Sets the tier of the circuit (standard or premium).
    [<CustomOperation "add_peering">]
    member __.AddPeering(state:ExpressRouteConfig, peering) = { state with Peerings = peering :: state.Peerings }
    /// Enables Global Reach on the circuit
    [<CustomOperation "enable_global_reach">]
    member __.EnableGlobalReach(state:ExpressRouteConfig) = { state with GlobalReachEnabled = true }
    [<CustomOperation "add_tags">]
    member _.Tags(state:ExpressRouteConfig, pairs) = 
        { state with 
            Tags = pairs |> List.fold (fun map (key,value) -> Map.add key value map) state.Tags }
    [<CustomOperation "add_tag">]
    member this.Tag(state:ExpressRouteConfig, key, value) = this.Tags(state, [ (key,value) ])
let expressRoute = ExpressRouteBuilder()