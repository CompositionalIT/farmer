[<AutoOpen>]
module Farmer.Builders.NetworkInterface

open System.Net.Sockets
open Farmer
open Farmer.Arm
open Farmer
open Farmer.Builders
open Farmer.Network
open Farmer.Arm.Network

type NetworkInterfaceConfig = {
    Name: ResourceName
    AcceleratedNetworkingflag: FeatureFlag option
    IpForwarding: FeatureFlag option
    IsPrimary: bool option
    VirtualNetwork: LinkedResource option
    SubnetName: string option
    SubnetPrefix: IPAddressCidr option
    LinkedSubnet: LinkedResource option
    NetworkSecurityGroup: LinkedResource option
    PrivateIpAddress: AllocationMethod
    PrivateIpAddressVersion: AddressVersion
    Tags: Map<string, string>
} with

    interface IBuilder with
        member this.ResourceId = networkInterfaces.resourceId this.Name

        member this.BuildResources location = [
            //vnet
            let vnetId =
                this.VirtualNetwork
                |> Option.defaultWith (fun _ -> raiseFarmer "Must set 'vnet' for network interface")

            match this.LinkedSubnet with
            | Some subnet ->
                //ipConfig
                let subnetIpConfigs = [
                    {
                        SubnetName = subnet.Name
                        ApplicationSecurityGroups = []
                        LoadBalancerBackendAddressPools = []
                        PublicIpAddress = None
                        PrivateIpAllocation = Some(this.PrivateIpAddress)
                        PrivateIpAddressVersion = this.PrivateIpAddressVersion
                        Primary = this.IsPrimary
                    }
                ]

                //network interface
                {
                    Name = this.Name
                    Location = location
                    EnableAcceleratedNetworking = this.AcceleratedNetworkingflag |> Option.map (fun f -> f.AsBoolean)
                    EnableIpForwarding = this.IpForwarding |> Option.map (fun f -> f.AsBoolean)
                    IpConfigs = subnetIpConfigs
                    Primary = this.IsPrimary
                    VirtualNetwork = vnetId
                    NetworkSecurityGroup = this.NetworkSecurityGroup
                    Tags = this.Tags
                }

            | None ->
                match this.SubnetName, this.SubnetPrefix with
                | Some subnetName, Some subnetPrefix ->
                    //subnet
                    {
                        Subnet.Name = ResourceName subnetName
                        Prefixes = [ IPAddressCidr.format subnetPrefix ]
                        VirtualNetwork = Some(vnetId)
                        RouteTable = None
                        NetworkSecurityGroup = None
                        DefaultOutboundAccess = None
                        Delegations = []
                        NatGateway = None
                        ServiceEndpoints = []
                        AssociatedServiceEndpointPolicies = []
                        PrivateEndpointNetworkPolicies = None
                        PrivateLinkServiceNetworkPolicies = None
                        Dependencies = Set.empty
                    }

                    //ipConfig
                    let subnetIpConfigs = [
                        {
                            SubnetName = ResourceName subnetName
                            ApplicationSecurityGroups = []
                            LoadBalancerBackendAddressPools = []
                            PublicIpAddress = None
                            PrivateIpAllocation = Some(this.PrivateIpAddress)
                            PrivateIpAddressVersion = AddressVersion.IPv4
                            Primary = this.IsPrimary
                        }
                    ]

                    //network interface
                    {
                        Name = this.Name
                        Location = location
                        EnableAcceleratedNetworking =
                            this.AcceleratedNetworkingflag |> Option.map (fun f -> f.AsBoolean)
                        EnableIpForwarding = this.IpForwarding |> Option.map (fun f -> f.AsBoolean)
                        IpConfigs = subnetIpConfigs
                        Primary = this.IsPrimary
                        VirtualNetwork = vnetId
                        NetworkSecurityGroup = this.NetworkSecurityGroup
                        Tags = this.Tags
                    }
                | _ ->
                    raiseFarmer
                        $"subnetName and subnetPrefix must be specified for a new subnet if no existing subnet provided."
        ]

type NetworkInterfaceBuilder() =
    member _.Yield _ = {
        Name = ResourceName.Empty
        AcceleratedNetworkingflag = None
        IpForwarding = None
        IsPrimary = None
        VirtualNetwork = None
        SubnetName = None
        SubnetPrefix = None
        LinkedSubnet = None
        NetworkSecurityGroup = None
        PrivateIpAddress = AllocationMethod.DynamicPrivateIp
        PrivateIpAddressVersion = IPv4
        Tags = Map.empty
    }

    [<CustomOperation "name">]
    member _.Name(state: NetworkInterfaceConfig, name: string) = { state with Name = ResourceName name }

    [<CustomOperation "accelerated_networking_flag">]
    member _.AcceleratedNetworkingflag(state: NetworkInterfaceConfig, flag: bool) = {
        state with
            AcceleratedNetworkingflag = Some(FeatureFlag.ofBool flag)
    }

    [<CustomOperation "ip_v6">]
    member _.IpV6(state: NetworkInterfaceConfig) = {
        state with
            PrivateIpAddressVersion = IPv6
    }

    [<CustomOperation "ip_forwarding_flag">]
    member _.IpForwarding(state: NetworkInterfaceConfig, flag: bool) = {
        state with
            IpForwarding = Some(FeatureFlag.ofBool flag)
    }

    [<CustomOperation "is_primary">]
    member _.IsPrimary(state: NetworkInterfaceConfig, isPrimary: bool) = {
        state with
            IsPrimary = Some(isPrimary)
    }

    // linked to managed vnet created by Farmer and linked by user
    [<CustomOperation "link_to_vnet">]
    member _.LinkToVNetId(state: NetworkInterfaceConfig, vnetId: ResourceId) = {
        state with
            VirtualNetwork = Some(Managed vnetId)
    }

    // linked to external existing vnet
    member _.LinkToVNetId(state: NetworkInterfaceConfig, vnetName: string) = {
        state with
            VirtualNetwork = Some(Unmanaged(virtualNetworks.resourceId (ResourceName vnetName)))
    }

    // create subnet through Farmer. Need to specify subnet_name and subnet_prefix
    [<CustomOperation "subnet_name">]
    member _.SubnetName(state: NetworkInterfaceConfig, name) = { state with SubnetName = Some(name) }

    [<CustomOperation "subnet_prefix">]
    member _.SubnetPrefix(state: NetworkInterfaceConfig, prefix) = {
        state with
            SubnetPrefix = Some(IPAddressCidr.parse prefix)
    }

    // linked to external existing subnet
    [<CustomOperation "link_to_subnet">]
    member _.LinkToSubnet(state: NetworkInterfaceConfig, name: string) = {
        state with
            LinkedSubnet = Some(Unmanaged(subnets.resourceId (ResourceName name)))
    }

    [<CustomOperation "add_static_ip">]
    member _.StaticIpAllocation(state: NetworkInterfaceConfig, addr) =
        let ipAddress = System.Net.IPAddress.Parse addr

        {
            state with
                PrivateIpAddress = AllocationMethod.StaticPrivateIp(ipAddress)
                PrivateIpAddressVersion =
                    match ipAddress.AddressFamily with
                    | AddressFamily.InterNetworkV6 -> IPv6
                    | _ -> IPv4
        }

    /// Sets the network security group for network interface
    [<CustomOperation "network_security_group">]
    member _.NetworkSecurityGroup(state: NetworkInterfaceConfig, nsg: IArmResource) = {
        state with
            NetworkSecurityGroup = Some(Managed nsg.ResourceId)
    }

    member _.NetworkSecurityGroup(state: NetworkInterfaceConfig, nsg: ResourceId) = {
        state with
            NetworkSecurityGroup = Some(Managed nsg)
    }

    member _.NetworkSecurityGroup(state: NetworkInterfaceConfig, nsg: NsgConfig) = {
        state with
            NetworkSecurityGroup = Some(Managed (nsg :> IBuilder).ResourceId)
    }

    /// Links the network interface to an existing network security group.
    [<CustomOperation "link_to_network_security_group">]
    member _.LinkToNetworkSecurityGroup(state: NetworkInterfaceConfig, nsg: IArmResource) = {
        state with
            NetworkSecurityGroup = Some(Unmanaged(nsg.ResourceId))
    }

    member _.LinkToNetworkSecurityGroup(state: NetworkInterfaceConfig, nsg: ResourceId) = {
        state with
            NetworkSecurityGroup = Some(Unmanaged nsg)
    }

    member _.LinkToNetworkSecurityGroup(state: NetworkInterfaceConfig, nsg: NsgConfig) = {
        state with
            NetworkSecurityGroup = Some(Unmanaged (nsg :> IBuilder).ResourceId)
    }

    interface ITaggable<NetworkInterfaceConfig> with
        member _.Add state tags = {
            state with
                Tags = state.Tags |> Map.merge tags
        }

let networkInterface = NetworkInterfaceBuilder()