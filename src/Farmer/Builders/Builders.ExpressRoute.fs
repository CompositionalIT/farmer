[<AutoOpen>]
module Farmer.Builders.ExpressRoute

open Farmer
open System
open System.Net
open Farmer.Arm.Network

[<RequireQualifiedAccess>]
type ExpressRouteTier =
    | Standard
    | Premium
type ExpressRouteFamily =
    | UnlimitedData
    | MeteredData
type ExpressRouteCircuitPeeringType =
    | AzurePrivatePeering
    | MicrosoftPeering
module ExpressRouteCircuitPeeringType =
    let format = function
        | AzurePrivatePeering -> "AzurePrivatePeering"
        | MicrosoftPeering -> "MicrosoftPeering"
/// An IP address block in CIDR notation, such as 10.100.0.0/16.
type IPAddressCidr =
    { Address : IPAddress
      Prefix : int }

module IPAddressCidr =
    let parse (s:string) : IPAddressCidr =
        match s.Split([|'/'|], StringSplitOptions.RemoveEmptyEntries) with
        [| ip; prefix |] ->
            { Address = IPAddress.Parse (ip.Trim ())
              Prefix = int prefix }
        | _ -> raise (ArgumentOutOfRangeException "Malformed CIDR, expecting and IP and prefix separated by '/'")
    let safeParse (s:string) : Result<IPAddressCidr, Exception> =
        try parse s |> Ok
        with ex -> Error ex
    let format cidr =
        sprintf "%O/%d" cidr.Address cidr.Prefix
type ExpressRouteCircuitPeering =
    { PeeringType : ExpressRouteCircuitPeeringType
      AzureASN : int
      PeerASN : int64
      /// A /30 IP address block to use for the primary link
      PrimaryPeerAddressPrefix : IPAddressCidr
      /// A /30 IP address block to use for the secondary link
      SecondaryPeerAddressPrefix : IPAddressCidr
      SharedKey : string option
      VlanId : int }

type [<Measure>] Mbps

type ExpressRouteCircuitPeeringConfig =
    { /// The peering type
      PeeringType : ExpressRouteCircuitPeeringType
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
        PrimaryPeerAddressPrefix = { Address = IPAddress.None; Prefix = 0}
        SecondaryPeerAddressPrefix = { Address = IPAddress.None; Prefix = 0}
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
    Tier : ExpressRouteTier
    /// Unlimited or metered data
    Family : ExpressRouteFamily
    /// The service provider name for the circuit
    ServiceProviderName : string
    /// A valid peering location
    PeeringLocation : string
    /// Bandwidth in Mbps
    Bandwidth : int<Mbps>
    /// Indicates if global reach is enabled on this circuit
    GlobalReachEnabled : bool
    /// Peerings on this circuit
    Peerings : ExpressRouteCircuitPeeringConfig list }
    interface IBuilder with
        member exr.BuildResources location _ = [
            { Name = exr.Name
              Location = location
              Tier = string exr.Tier
              Family = string exr.Family
              ServiceProviderName = exr.ServiceProviderName
              PeeringLocation = exr.PeeringLocation
              Bandwidth = int exr.Bandwidth
              GlobalReachEnabled = exr.GlobalReachEnabled
              Peerings = [
                  for peering in exr.Peerings do
                      {| PeeringType = peering.PeeringType |> ExpressRouteCircuitPeeringType.format
                         AzureASN = peering.AzureASN
                         PeerASN = peering.PeerASN
                         PrimaryPeerAddressPrefix = peering.PrimaryPeerAddressPrefix |> IPAddressCidr.format
                         SecondaryPeerAddressPrefix = peering.SecondaryPeerAddressPrefix |> IPAddressCidr.format
                         SharedKey = peering.SharedKey
                         VlanId = peering.VlanId |}
              ] }
        ]

type ExpressRouteBuilder() =
    member __.Yield _ =
      { Name = ResourceName.Empty
        Tier = ExpressRouteTier.Standard
        Family = MeteredData
        ServiceProviderName = ""
        PeeringLocation = ""
        Bandwidth = 50<Mbps>
        GlobalReachEnabled = false
        Peerings = [] }
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
let expressRoute = ExpressRouteBuilder()