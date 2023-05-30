module Farmer.Builders.NetworkInterface

open Farmer
open Farmer.Arm
open Farmer
open Farmer.Builders
open Farmer.Network
open Farmer.Arm.Network

type NetworkInterfaceConfig =
    {
        Name: ResourceName
        AcceleratedNetworkingflag: FeatureFlag option
        IpForwarding: FeatureFlag option
        IsPrimary: bool option
        VirtualNetwork: LinkedResource option
        SubnetPrefix: IPAddressCidr
        PrivateIpAddress: string
        Tags: Map<string, string>
    }

    interface IBuilder with
        member this.ResourceId = networkInterfaces.resourceId this.Name

        member this.BuildResources location =
            [
                //vnet
                let vnetId =
                    this.VirtualNetwork
                    |> Option.defaultWith (fun _ -> raiseFarmer "Must set 'vnet' for network interface")

                //subnet
                {
                    Subnet.Name = ResourceName "networkInterfaceSubnet"
                    Prefix = IPAddressCidr.format this.SubnetPrefix
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
                let subnetIpConfigs =
                    [
                        {
                            SubnetName = ResourceName "networkInterfaceSubnet"
                            LoadBalancerBackendAddressPools = []
                            PublicIpAddress = None
                            PrivateIpAllocation =
                                match this.PrivateIpAddress with
                                | "" -> Some(AllocationMethod.DynamicPrivateIp)
                                | ip -> Some(AllocationMethod.StaticPrivateIp(System.Net.IPAddress.Parse ip))
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
            ]

type NetworkInterfaceBuilder() =
    member _.Yield _ =
        {
            Name = ResourceName.Empty
            AcceleratedNetworkingflag = None
            IpForwarding = None
            IsPrimary = None
            VirtualNetwork = None
            SubnetPrefix =
                {
                    Address = System.Net.IPAddress.Parse("10.0.100.0")
                    Prefix = 16
                }
            PrivateIpAddress = ""
            Tags = Map.empty
        }

    [<CustomOperation "name">]
    member _.Name(state: NetworkInterfaceConfig, name: string) = { state with Name = ResourceName name }

    [<CustomOperation "accelerated_networking_flag">]
    member _.AcceleratedNetworkingflag(state: NetworkInterfaceConfig, flag: bool) =
        { state with
            AcceleratedNetworkingflag = Some(FeatureFlag.ofBool flag)
        }

    [<CustomOperation "ip_forwarding_flag">]
    member _.IpForwarding(state: NetworkInterfaceConfig, flag: bool) =
        { state with
            IpForwarding = Some(FeatureFlag.ofBool flag)
        }

    [<CustomOperation "is_primary">]
    member _.IsPrimary(state: NetworkInterfaceConfig, isPrimary: bool) =
        { state with
            IsPrimary = Some(isPrimary)
        }

    // linked to managed vnet created by Farmer and linked by user
    [<CustomOperation "link_to_vnet">]
    member _.LinkToVNetId(state: NetworkInterfaceConfig, vnetId: ResourceId) =
        { state with
            VirtualNetwork = Some(Managed vnetId)
        }

    // linked to external existing vnet
    member _.LinkToVNetId(state: NetworkInterfaceConfig, vnetName: string) =
        { state with
            VirtualNetwork = Some(Unmanaged(virtualNetworks.resourceId (ResourceName vnetName)))
        }

    [<CustomOperation "subnet_prefix">]
    member _.SubnetPrefix(state: NetworkInterfaceConfig, prefix) =
        { state with
            SubnetPrefix = IPAddressCidr.parse prefix
        }

    [<CustomOperation "add_static_ip">]
    member _.StaticIpAllocation(state: NetworkInterfaceConfig, addr) = { state with PrivateIpAddress = addr }

let networkInterface = NetworkInterfaceBuilder()
