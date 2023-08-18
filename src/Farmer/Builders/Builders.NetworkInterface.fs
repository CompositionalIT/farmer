[<AutoOpen>]
module Farmer.Builders.NetworkInterface

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
    PrivateIpAddress: AllocationMethod
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
                        LoadBalancerBackendAddressPools = []
                        PublicIpAddress = None
                        PrivateIpAllocation = Some(this.PrivateIpAddress)
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
                    NetworkSecurityGroup = None
                    Tags = this.Tags
                }

            | None ->
                match this.SubnetName, this.SubnetPrefix with
                | Some subnetName, Some subnetPrefix ->
                    //subnet
                    {
                        Subnet.Name = ResourceName subnetName
                        Prefix = IPAddressCidr.format subnetPrefix
                        VirtualNetwork = Some(vnetId)
                        NetworkSecurityGroup = None
                        Delegations = []
                        NatGateway = None
                        ServiceEndpoints = []
                        AssociatedServiceEndpointPolicies = []
                        PrivateEndpointNetworkPolicies = None
                        PrivateLinkServiceNetworkPolicies = None
                    }

                    //ipConfig
                    let subnetIpConfigs = [
                        {
                            SubnetName = ResourceName subnetName
                            LoadBalancerBackendAddressPools = []
                            PublicIpAddress = None
                            PrivateIpAllocation = Some(this.PrivateIpAddress)
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
                        NetworkSecurityGroup = None
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
        PrivateIpAddress = AllocationMethod.DynamicPrivateIp
        Tags = Map.empty
    }

    [<CustomOperation "name">]
    member _.Name(state: NetworkInterfaceConfig, name: string) = { state with Name = ResourceName name }

    [<CustomOperation "accelerated_networking_flag">]
    member _.AcceleratedNetworkingflag(state: NetworkInterfaceConfig, flag: bool) = {
        state with
            AcceleratedNetworkingflag = Some(FeatureFlag.ofBool flag)
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
    member _.StaticIpAllocation(state: NetworkInterfaceConfig, addr) = {
        state with
            PrivateIpAddress = AllocationMethod.StaticPrivateIp(System.Net.IPAddress.Parse addr)
    }

    interface ITaggable<NetworkInterfaceConfig> with
        member _.Add state tags = {
            state with
                Tags = state.Tags |> Map.merge tags
        }

let networkInterface = NetworkInterfaceBuilder()
