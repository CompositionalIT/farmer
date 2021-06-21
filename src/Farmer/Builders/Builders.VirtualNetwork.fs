[<AutoOpen>]
module Farmer.Builders.VirtualNetwork

open Farmer
open Farmer.Network
open Farmer.Arm.Network

type PeeringMode = 
    | TwoWay
    | OneWayToPeer
    | OneWayFromPeer

type SubnetConfig =
    { Name: ResourceName
      Prefix: IPAddressCidr
      Delegations:  SubnetDelegationService list
      ServiceEndpoints: (EndpointServiceType * Location list) list
      AssociatedServiceEndpointPolicies : ResourceId list }

type SubnetBuilder() =
    member _.Yield _ =
        { Name = ResourceName.Empty
          Prefix = { Address = System.Net.IPAddress.Parse("10.100.0.0"); Prefix = 16 }
          Delegations = []
          ServiceEndpoints = []
          AssociatedServiceEndpointPolicies = [] }
    /// Sets the name of the subnet
    [<CustomOperation "name">]
    member _.Name(state:SubnetConfig, name) = { state with Name = ResourceName name }
    /// Sets the network prefix in CIDR notation
    [<CustomOperation "prefix">]
    member _.Prefix(state:SubnetConfig, prefix) = { state with Prefix = IPAddressCidr.parse prefix }
    /// Sets the network prefix in CIDR notation
    [<CustomOperation "add_delegations">]
    member _.AddDelegations(state:SubnetConfig, delegations) = { state with Delegations = state.Delegations @ delegations }
    /// Add service endpoint types to this subnet
    [<CustomOperation "add_service_endpoints">]
    member _.AddServiceEndpoints(state:SubnetConfig, serviceEndpoints) = { state with ServiceEndpoints = state.ServiceEndpoints @ serviceEndpoints }
    /// Associates service endpoint policies with this subnet
    [<CustomOperation "associate_service_endpoint_policies">]
    member _.AssociateServiceEndpointPolicies(state:SubnetConfig, servicePolicyIds) = { state with AssociatedServiceEndpointPolicies = state.AssociatedServiceEndpointPolicies @ servicePolicyIds }

let subnet = SubnetBuilder ()
/// Specification for a subnet to build from an address space.
type SubnetBuildSpec =
    { Name: string
      Size: int
      Delegations: SubnetDelegationService list
      ServiceEndpoints: (EndpointServiceType * Location list) list
      AssociatedServiceEndpointPolicies : ResourceId list }
/// Builds a subnet of a certain CIDR block size.
let buildSubnet name size =
    { Name = name; Size = size; Delegations = []; ServiceEndpoints = []; AssociatedServiceEndpointPolicies = [] }
/// Builds a subnet of a certain CIDR block size with service delegations.
let buildSubnetDelegations name size delegations =
    { Name = name; Size = size; Delegations = delegations; ServiceEndpoints = []; AssociatedServiceEndpointPolicies = [] }

type SubnetSpecBuilder () =
    member _.Yield _ =
        {
            Name = ""
            Size = 24
            Delegations = []
            ServiceEndpoints = []
            AssociatedServiceEndpointPolicies = []
        }
    /// Sets the name of the subnet to build
    [<CustomOperation "name">]
    member _.Name(state:SubnetBuildSpec, name) =
        { state with Name = name }
    /// Sets the size for the network prefix to build
    [<CustomOperation "size">]
    member _.Size(state:SubnetBuildSpec, size) =
        { state with Size = size }
    /// Adds any services to delegate this subnet
    [<CustomOperation "add_delegations">]
    member _.AddDelegations(state:SubnetBuildSpec, delegations) =
        { state with Delegations = state.Delegations @ delegations }
    /// Adds service endpoints to build for this subnet
    [<CustomOperation "add_service_endpoints">]
    member _.AddServiceEndpoints(state:SubnetBuildSpec, serviceEndpoints) =
        { state with ServiceEndpoints = state.ServiceEndpoints @ serviceEndpoints }
    /// Associates the built subnet with service endpoint policies
    [<CustomOperation "add_service_endpoint_policies">]
    member _.AddAssociatedServiceEndpointPolicies(state:SubnetBuildSpec, policies) =
        { state with AssociatedServiceEndpointPolicies = state.AssociatedServiceEndpointPolicies @ policies }

let subnetSpec = SubnetSpecBuilder()

/// A specification building an address space and subnets.
type AddressSpaceSpec =
    { Space : string
      Subnets : SubnetBuildSpec list }
open System.Runtime.InteropServices
/// Builder for an address space with automatically carved subnets.
type AddressSpaceBuilder() =
    member _.Yield _ = { Space = ""; Subnets = [] }
    [<CustomOperation("space")>]
    member _.Space(state:AddressSpaceSpec, space) = { state with Space = space }
    [<CustomOperation("subnets")>]
    member _.Subnets(state:AddressSpaceSpec, subnets) = { state with Subnets = state.Subnets @ subnets }
    member private _.buildSubnet(state:AddressSpaceSpec, name:string, size:int, ?delegations:SubnetDelegationService list, ?serviceEndpoints:(EndpointServiceType * Location list) list, ?associatedServiceEndpointPolicies:ResourceId list) =
        let subnetBuildSpec =
            { Name = name
              Size = size
              Delegations = delegations |> Option.defaultValue []
              ServiceEndpoints = serviceEndpoints |> Option.defaultValue []
              AssociatedServiceEndpointPolicies = associatedServiceEndpointPolicies |> Option.defaultValue [] }
        { state with Subnets = state.Subnets @ [ subnetBuildSpec ] }
    [<CustomOperation("build_subnet")>]
    member this.BuildSubnet(state:AddressSpaceSpec, name:string, size:int) =
        this.buildSubnet(state, name, size)
    [<CustomOperation("build_subnet_delegated")>]
    member this.BuildSubnetDelegated(state:AddressSpaceSpec, name:string, size:int, delegations:SubnetDelegationService list) =
        this.buildSubnet(state, name, size, delegations=delegations)

let addressSpace = AddressSpaceBuilder ()

type VirtualNetworkConfig =
    { Name : ResourceName
      AddressSpacePrefixes : string list
      Subnets : SubnetConfig list
      Tags: Map<string,string>
      Peers: (LinkedResource * PeeringMode) list }
    member this.ResourceId = virtualNetworks.resourceId this.Name
    interface IBuilder with
        member this.ResourceId = this.ResourceId
        member this.BuildResources location = [
            { Name = this.Name
              Location = location
              AddressSpacePrefixes = this.AddressSpacePrefixes
              Subnets = this.Subnets |> List.map (fun subnetConfig ->
                  { Name = subnetConfig.Name
                    Prefix = IPAddressCidr.format subnetConfig.Prefix
                    Delegations = subnetConfig.Delegations |> List.map (fun (SubnetDelegationService(delegation)) ->
                        { Name = ResourceName delegation; ServiceName = delegation })
                    ServiceEndpoints = subnetConfig.ServiceEndpoints
                    AssociatedServiceEndpointPolicies = subnetConfig.AssociatedServiceEndpointPolicies
                  })
              Tags = this.Tags
            }
            for (remote, mode) in this.Peers do
                match mode with
                | OneWayToPeer | TwoWay -> 
                    { Location = location
                      OwningVNet = Managed this.ResourceId
                      RemoteVNet = remote } 
                | _ -> ()
                match mode with
                | OneWayFromPeer | TwoWay -> 
                    { Location = location
                      OwningVNet = remote
                      RemoteVNet = Managed this.ResourceId }
                | _ -> ()
        ]

type VirtualNetworkBuilder() =
    member _.Yield _ =
      { Name = ResourceName.Empty
        AddressSpacePrefixes = []
        Subnets = []
        Tags = Map.empty
        Peers = List.empty }
    /// Sets the name of the virtual network
    [<CustomOperation "name">]
    member _.Name(state:VirtualNetworkConfig, name) = { state with Name = ResourceName name }
    /// Adds address spaces prefixes
    [<CustomOperation "add_address_spaces">]
    member _.AddAddressSpaces(state:VirtualNetworkConfig, prefixes) = { state with AddressSpacePrefixes = state.AddressSpacePrefixes @ prefixes }
    /// Adds subnets
    [<CustomOperation "add_subnets">]
    member _.AddSubnets(state:VirtualNetworkConfig, subnets) = { state with Subnets = state.Subnets @ subnets }
    /// Peers this VNet with other VNets to allow communication between the VNets as if they were one
    [<CustomOperation "add_peerings">]
    member _.AddPeers(state:VirtualNetworkConfig, peers) = { state with Peers = state.Peers @ peers }
    member this.AddPeers(state:VirtualNetworkConfig, peers:LinkedResource list) = this.AddPeers (state, peers |> List.map (fun peer -> (peer, TwoWay)) )
    member this.AddPeers(state:VirtualNetworkConfig, peers:VirtualNetworkConfig list) = this.AddPeers (state, peers |> List.map (fun x -> Managed x.ResourceId, PeeringMode.TwoWay))
    member this.AddPeers(state:VirtualNetworkConfig, peers:(VirtualNetworkConfig * PeeringMode) list) = this.AddPeers (state, peers |> List.map (fun (peer, mode) -> (Managed peer.ResourceId, mode)) )
    /// Peers this VNet with another VNet to allow communication between the VNets as if they were one
    [<CustomOperation "add_peering">]
    member this.AddPeer(state:VirtualNetworkConfig, (peer,mode):LinkedResource*PeeringMode) = this.AddPeers(state, [peer,mode])
    member this.AddPeer(state:VirtualNetworkConfig, peer:LinkedResource) = this.AddPeers(state, [peer])
    member this.AddPeer(state:VirtualNetworkConfig, peer:VirtualNetworkConfig) = this.AddPeers(state, [peer])

    [<CustomOperation "build_address_spaces">]
    member _.BuildAddressSpaces(state:VirtualNetworkConfig, addressSpaces:AddressSpaceSpec list) =
        let newSubnets =
            addressSpaces
            |> List.collect (fun addressSpaceConfig ->
                let addressSpace = IPAddressCidr.parse addressSpaceConfig.Space
                let sizes = [
                    for subnet in addressSpaceConfig.Subnets do
                        if subnet.Size > 29 then invalidArg "size" $"Subnet must be of /29 or larger, cannot carve subnet {subnet.Name} of /{subnet.Size}"
                        subnet.Size
                ]
                IPAddressCidr.carveAddressSpace addressSpace sizes
                |> List.zip (addressSpaceConfig.Subnets |> List.map (fun s -> s.Name, s.Delegations, s.ServiceEndpoints, s.AssociatedServiceEndpointPolicies))
                |> List.map (fun ((name, delegations, serviceEndpoints, serviceEndpointPolicies), cidr) ->
                    { Name = ResourceName name
                      Prefix = cidr
                      Delegations = delegations
                      ServiceEndpoints = serviceEndpoints
                      AssociatedServiceEndpointPolicies = serviceEndpointPolicies }
                ))
        let newAddressSpaces = addressSpaces |> List.map (fun addressSpace -> addressSpace.Space)
        { state with
            Subnets = state.Subnets @ newSubnets
            AddressSpacePrefixes = state.AddressSpacePrefixes @ newAddressSpaces }
    interface ITaggable<VirtualNetworkConfig> with member _.Add state tags = { state with Tags = state.Tags |> Map.merge tags }

let vnet = VirtualNetworkBuilder ()