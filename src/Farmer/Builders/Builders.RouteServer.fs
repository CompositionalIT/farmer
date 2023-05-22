[<AutoOpen>]
module Farmer.Builders.Builders_RouteServer

open Farmer
open Farmer.Arm
open Farmer.RouteServer


type RSBGPConnectionConfig =
    {
        ConnectionName: string
        PeerIp: string
        PeerAsn: int
    }

type RSBGPConnectionBuilder() =
    member _.Yield _ =
        {
            ConnectionName = ""
            PeerIp = ""
            PeerAsn = 0
        }

    [<CustomOperation "connectionName">]
    member _.ConnectionName(state: RSBGPConnectionConfig, connectionName) =
        { state with
            ConnectionName = connectionName
        }

    [<CustomOperation "peerIp">]
    member _.PeerIp(state: RSBGPConnectionConfig, peerIp) = { state with PeerIp = peerIp }

    [<CustomOperation "peerAsn">]
    member _.PeerAsn(state: RSBGPConnectionConfig, peerAsn) = { state with PeerAsn = peerAsn }

let routeServerBGPConnection = RSBGPConnectionBuilder()

type RouteServerConfig =
    {
        Name: ResourceName
        Sku: RouteServer.Sku
        AllowBranchToBranchTraffic: FeatureFlag option
        HubRoutingPreference: HubRoutingPreference option
        BGPConnections: RSBGPConnectionConfig list
        VirtualNetwork: LinkedResource option
        SubnetPrefix: string
        Tags: Map<string, string>
    }

    interface IBuilder with
        member this.ResourceId = routeServers.resourceId this.Name

        member this.BuildResources location =
            [
                let vnetId =
                    this.VirtualNetwork
                    |> Option.defaultWith (fun _ -> raiseFarmer "Must set 'vnet' for route server")

                //public ip
                {
                    PublicIpAddress.Name = ResourceName $"{this.Name.Value}-publicip"
                    AvailabilityZone = None
                    Location = location
                    Sku = PublicIpAddress.Sku.Standard
                    AllocationMethod = PublicIpAddress.AllocationMethod.Static
                    DomainNameLabel = None
                    Tags = this.Tags
                }

                //subnet
                {
                    Subnet.Name = ResourceName "RouteServerSubnet"
                    Prefix = this.SubnetPrefix
                    VirtualNetwork = Some(vnetId)
                    NetworkSecurityGroup = None
                    Delegations = []
                    NatGateway = None
                    ServiceEndpoints = []
                    AssociatedServiceEndpointPolicies = []
                    PrivateEndpointNetworkPolicies = None
                    PrivateLinkServiceNetworkPolicies = None
                }

                //ip configuration
                {
                    RouteServerIPConfig.Name = ResourceName $"{this.Name.Value}-ipconfig"
                    RouteServer = Managed(routeServers.resourceId this.Name)
                    PublicIpAddress = LinkedResource.Managed(publicIPAddresses.resourceId $"{this.Name.Value}-publicip")
                    SubnetId =
                        LinkedResource.Managed(
                            subnets.resourceId (ResourceName vnetId.Name.Value, ResourceName "RouteServerSubnet")
                        )
                }

                //route server
                {
                    RouteServer.Name = this.Name
                    Location = location
                    Sku = this.Sku
                    AllowBranchToBranchTraffic =
                        this.AllowBranchToBranchTraffic |> Option.defaultValue FeatureFlag.Disabled
                    HubRoutingPreference =
                        this.HubRoutingPreference
                        |> Option.defaultValue HubRoutingPreference.ExpressRoute
                    Tags = this.Tags
                }

                //bgp connections
                for connection in this.BGPConnections do
                    {
                        RouteServerBGPConnection.Name = this.Name
                        RouteServer = Managed(routeServers.resourceId this.Name)
                        ConnectionName = connection.ConnectionName
                        PeerIp = connection.PeerIp
                        PeerAsn = connection.PeerAsn
                        IpConfig =
                            LinkedResource.Managed(
                                routeServerIPConfigs.resourceId (
                                    ResourceName this.Name.Value,
                                    ResourceName $"{this.Name.Value}-ipconfig"
                                )
                            )
                    }
            ]

type RouteServerBuilder() =
    member _.Yield _ =
        {
            Name = ResourceName.Empty
            Sku = Standard
            AllowBranchToBranchTraffic = None
            HubRoutingPreference = None
            BGPConnections = []
            VirtualNetwork = None
            SubnetPrefix = ""
            Tags = Map.empty
        }

    [<CustomOperation "name">]
    member _.Name(state: RouteServerConfig, name: string) = { state with Name = ResourceName name }

    [<CustomOperation "sku">]
    member _.Sku(state: RouteServerConfig, sku) = { state with Sku = sku }

    [<CustomOperation "allowBranchToBranchTraffic">]
    member _.AllowBranchToBranchTraffic(state: RouteServerConfig, flag: bool) =
        { state with
            AllowBranchToBranchTraffic = Some(FeatureFlag.ofBool flag)
        }

    [<CustomOperation "routingPreference">]
    member _.HubRoutingPreference(state: RouteServerConfig, routingPreference) =
        { state with
            HubRoutingPreference = Some(routingPreference)
        }

    [<CustomOperation "add_BGPConnections">]
    member _.AddIPConfigs(state: RouteServerConfig, connections: RSBGPConnectionConfig list) =
        { state with
            BGPConnections = connections @ state.BGPConnections
        }

    [<CustomOperation "subnetPrefix">]
    member _.SubnetPrefix(state: RouteServerConfig, prefix) = { state with SubnetPrefix = prefix }

    // linked to managed vnet created by Farmer and linked by user
    [<CustomOperation "linkToVnet">]
    member _.LinkToVNetId(state: RouteServerConfig, vnetId: ResourceId) =
        { state with
            VirtualNetwork = Some(Managed vnetId)
        }

    // linked to external existing vnet
    member _.LinkToVNetId(state: RouteServerConfig, vnetName: string) =
        { state with
            VirtualNetwork = Some(Unmanaged(virtualNetworks.resourceId (ResourceName vnetName)))
        }

let routeServer = RouteServerBuilder()
