[<AutoOpen>]
module Farmer.Resources.ExpressRoute

open Farmer
open Farmer.Models

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
    VlanId : int
  }

type ExpressRouteCircuitPeeringBuilder() =
    member __.Yield _ =
      { PeeringType = AzurePrivatePeering
        AzureASN = 0
        PeerASN = 0L
        PrimaryPeerAddressPrefix = { Address = System.Net.IPAddress.None; Prefix = 0}
        SecondaryPeerAddressPrefix = { Address = System.Net.IPAddress.None; Prefix = 0}
        SharedKey = None
        VlanId = 0
      }
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
    Peerings : ExpressRouteCircuitPeeringConfig list
  }

type ExpressRouteBuilder() =
    member __.Yield _ =
      { Name = ResourceName.Empty
        Tier = ExpressRouteTier.Standard
        Family = ExpressRouteFamily.MeteredData
        ServiceProviderName = ""
        PeeringLocation = ""
        Bandwidth = 50<Mbps>
        GlobalReachEnabled = false
        Peerings = []
      }
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

module Converters =
    let private peering (p:ExpressRouteCircuitPeeringConfig) : ExpressRouteCircuitPeering =
        {
            PeeringType = p.PeeringType
            AzureASN = p.AzureASN
            PeerASN = p.PeerASN
            PrimaryPeerAddressPrefix = p.PrimaryPeerAddressPrefix
            SecondaryPeerAddressPrefix = p.SecondaryPeerAddressPrefix
            SharedKey = p.SharedKey
            VlanId = p.VlanId
        }
        
    let expressRoute location (exr:ExpressRouteConfig) : ExpressRouteCircuit =
        {
            Location = location
            Name = exr.Name
            Tier = exr.Tier
            Family = exr.Family
            ServiceProviderName = exr.ServiceProviderName
            PeeringLocation = exr.PeeringLocation
            Bandwidth = exr.Bandwidth
            GlobalReachEnabled = exr.GlobalReachEnabled
            Peerings =
                exr.Peerings |> List.map peering
        }
    
    module Outputters =
        let private peering (p:ExpressRouteCircuitPeering) = {|
            name = p.PeeringType |> ExpressRouteCircuitPeeringType.format
            properties =
                {| peeringType = p.PeeringType |> ExpressRouteCircuitPeeringType.format
                   azureASN = p.AzureASN
                   peerASN = p.PeerASN
                   primaryPeerAddressPrefix = p.PrimaryPeerAddressPrefix |> IPAddressCidr.format
                   secondaryPeerAddressPrefix = p.SecondaryPeerAddressPrefix |> IPAddressCidr.format
                   vlanId = p.VlanId
                   sharedKey = p.SharedKey
                |}
        |}
        
        let expressRoute (exr:ExpressRouteCircuit) = {|
            ``type`` = "Microsoft.Network/expressRouteCircuits"
            apiVersion = "2019-02-01"
            name = exr.Name.Value
            location = exr.Location.ArmValue
            sku = {| name = System.String.Format("{0}_{1}", exr.Tier, exr.Family); tier = string exr.Tier; family = string exr.Family |}
            properties =
                {|
                    peerings = exr.Peerings |> List.map peering
                    serviceProviderProperties =
                        {|
                            serviceProviderName = exr.ServiceProviderName
                            peeringLocation = exr.PeeringLocation
                            bandwidthInMbps = exr.Bandwidth
                        |}
                    globalReachEnabled = exr.GlobalReachEnabled
                |}
        |}

type ArmBuilder.ArmBuilder with
    member __.AddResource(state:ArmConfig, config:ExpressRouteConfig) =
        { state with
            Resources = ExpressRoute (Converters.expressRoute state.Location config) :: state.Resources
        }
    member this.AddResources (state, configs) =
        addResources<ExpressRouteConfig> this.AddResource state configs