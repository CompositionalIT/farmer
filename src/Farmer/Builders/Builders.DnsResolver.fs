[<AutoOpen>]
module Farmer.Builders.DnsResolver

open System.Net
open Farmer
open Farmer.Arm.Dns
open Farmer.Arm.Dns.DnsResolver
open Farmer.Arm.Dns.DnsForwardingRuleset

type DnsResolverInboundEndpointConfig =
    {
        Name: ResourceName
        DnsResolverId: LinkedResource option
        SubnetId: LinkedResource option
        PrivateIpAllocations: AllocationMethod list
        Dependencies: Set<ResourceId>
        Tags: Map<string, string>
    }

    interface IBuilder with
        member this.BuildResources location =
            [
                {
                    Name = this.Name
                    Location = location
                    DnsResolverId =
                        this.DnsResolverId
                        |> Option.defaultWith (fun _ -> raiseFarmer "Must set 'dns_resolver' for inboundEndpoint.")
                    SubnetId =
                        this.SubnetId
                        |> Option.defaultWith (fun _ -> raiseFarmer "Must set 'subnet' for inboundEndpoint.")
                    PrivateIpAllocations =
                        if this.PrivateIpAllocations.IsEmpty then
                            [ DynamicPrivateIp ]
                        else
                            this.PrivateIpAllocations
                    Dependencies = this.Dependencies
                    Tags = this.Tags
                }
            ]

        member this.ResourceId =
            dnsResolverInboundEndpoints.resourceId (this.DnsResolverId.Value.Name, this.Name)

type DnsResolverOutboundEndpointConfig =
    {
        Name: ResourceName
        DnsResolverId: LinkedResource option
        SubnetId: LinkedResource option
        Dependencies: Set<ResourceId>
        Tags: Map<string, string>
    }

    interface IBuilder with
        member this.BuildResources location =
            [
                {
                    Name = this.Name
                    Location = location
                    DnsResolverId =
                        this.DnsResolverId
                        |> Option.defaultWith (fun _ -> raiseFarmer "Must set 'dns_resolver' for inboundEndpoint.")
                    SubnetId =
                        this.SubnetId
                        |> Option.defaultWith (fun _ -> raiseFarmer "Must set 'subnet' for inboundEndpoint.")
                    Dependencies = this.Dependencies
                    Tags = this.Tags
                }
            ]

        member this.ResourceId =
            dnsResolverOutboundEndpoints.resourceId (this.DnsResolverId.Value.Name, this.Name)

type DnsResolverConfig =
    {
        Name: ResourceName
        VirtualNetworkId: LinkedResource option
        InboundEndpoints: DnsResolverInboundEndpointConfig list
        InboundSubnetName: ResourceName option
        OutboundEndpoints: DnsResolverOutboundEndpointConfig list
        OutboundSubnetName: ResourceName option
        Dependencies: Set<ResourceId>
        Tags: Map<string, string>
    }

    interface IBuilder with
        member this.BuildResources location =
            let vnetId =
                this.VirtualNetworkId
                |> Option.defaultWith (fun _ -> raiseFarmer "Must set 'vnet' for resolver")

            [
                {
                    Name = this.Name
                    Location = location
                    VirtualNetworkId = vnetId
                    Dependencies = this.Dependencies
                    Tags = this.Tags
                }
                match this.InboundSubnetName with
                | None -> ()
                | Some subnet -> // Build an inbound for the specified subnet.
                    {
                        Name = subnet
                        Location = location
                        DnsResolverId = Managed(dnsResolvers.resourceId this.Name)
                        SubnetId =
                            Unmanaged
                                { vnetId.ResourceId with
                                    Type = Arm.Network.subnets
                                    Segments = [ subnet ]
                                }
                        PrivateIpAllocations = [ DynamicPrivateIp ]
                        Dependencies = Set.empty
                        Tags = Map.empty
                    }
                match this.OutboundSubnetName with
                | None -> ()
                | Some subnet -> // Build an outbound for the specified subnet.
                    {
                        Name = subnet
                        Location = location
                        DnsResolverId = Managed(dnsResolvers.resourceId this.Name)
                        SubnetId =
                            Unmanaged
                                { vnetId.ResourceId with
                                    Type = Arm.Network.subnets
                                    Segments = [ subnet ]
                                }
                        Dependencies = Set.empty
                        Tags = Map.empty
                    }
                for inbound in this.InboundEndpoints do
                    let inbound =
                        { inbound with
                            DnsResolverId = Some(Managed(dnsResolvers.resourceId this.Name))
                        }

                    yield! (inbound :> IBuilder).BuildResources location
                for outbound in this.OutboundEndpoints do
                    let outbound =
                        { outbound with
                            DnsResolverId = Some(Managed(dnsResolvers.resourceId this.Name))
                        }

                    yield! (outbound :> IBuilder).BuildResources location
            ]

        member this.ResourceId = dnsResolvers.resourceId this.Name

type DnsResolverBuilder() =
    member _.Yield _ =
        {
            Name = ResourceName.Empty
            VirtualNetworkId = None
            InboundEndpoints = []
            InboundSubnetName = None
            OutboundEndpoints = []
            OutboundSubnetName = None
            Dependencies = Set.empty
            Tags = Map.empty
        }

    [<CustomOperation "name">]
    member _.Name(config: DnsResolverConfig, name: string) =
        { config with Name = ResourceName name }

    [<CustomOperation "vnet">]
    member _.VirtualNetwork(config: DnsResolverConfig, id: ResourceId) =
        { config with
            VirtualNetworkId = Some(Managed id)
        }

    member _.VirtualNetwork(config: DnsResolverConfig, name: string) =
        { config with
            VirtualNetworkId = Some(Managed(Farmer.Arm.Network.virtualNetworks.resourceId (ResourceName name)))
        }

    [<CustomOperation "link_to_vnet">]
    member _.LinkToVirtualNetwork(config: DnsResolverConfig, id: ResourceId) =
        { config with
            VirtualNetworkId = Some(Unmanaged id)
        }

    member _.LinkToVirtualNetwork(config: DnsResolverConfig, name: string) =
        { config with
            VirtualNetworkId = Some(Unmanaged(Farmer.Arm.Network.virtualNetworks.resourceId (ResourceName name)))
        }

    [<CustomOperation "inbound_subnet">]
    member _.InboundSubnet(config: DnsResolverConfig, subnetName: string) =
        { config with
            InboundSubnetName = Some(ResourceName subnetName)
        }

    [<CustomOperation "add_inbound_endpoints">]
    member _.AddInboundEndpoints(config: DnsResolverConfig, inboundEndpoints) =
        { config with
            InboundEndpoints = config.InboundEndpoints @ inboundEndpoints
        }

    [<CustomOperation "outbound_subnet">]
    member _.OutboundSubnet(config: DnsResolverConfig, subnetName: string) =
        { config with
            OutboundSubnetName = Some(ResourceName subnetName)
        }

    [<CustomOperation "add_outbound_endpoints">]
    member _.AddOutboundEndpoints(config: DnsResolverConfig, outboundEndpoints) =
        { config with
            OutboundEndpoints = config.OutboundEndpoints @ outboundEndpoints
        }

    interface ITaggable<DnsResolverConfig> with
        member _.Add state tags =
            { state with
                Tags = state.Tags |> Map.merge tags
            }

    interface IDependable<DnsResolverConfig> with
        member _.Add state newDeps =
            { state with
                Dependencies = state.Dependencies + newDeps
            }

let dnsResolver = DnsResolverBuilder()

type DnsResolverInboundEndpointBuilder() =
    member _.Yield _ =
        {
            Name = ResourceName.Empty
            DnsResolverId = None
            SubnetId = None
            PrivateIpAllocations = []
            Dependencies = Set.empty
            Tags = Map.empty
        }

    member _.Run(config: DnsResolverInboundEndpointConfig) =
        if config.SubnetId.IsNone then
            raiseFarmer "inboundEndpoint requires a 'subnet'."

        if config.PrivateIpAllocations.Length = 0 then
            { config with
                PrivateIpAllocations = [ DynamicPrivateIp ]
            }
        else
            config

    [<CustomOperation "name">]
    member _.Name(config: DnsResolverInboundEndpointConfig, name: string) =
        { config with Name = ResourceName name }

    [<CustomOperation "dns_resolver">]
    member _.DnsResolverId(config: DnsResolverInboundEndpointConfig, id: ResourceId) =
        { config with
            DnsResolverId = Some(Managed id)
        }

    member _.DnsResolverId(config: DnsResolverInboundEndpointConfig, dnsResolver: DnsResolverConfig) =
        { config with
            DnsResolverId = Some(Managed (dnsResolver :> IBuilder).ResourceId)
        }

    member _.DnsResolverId(config: DnsResolverInboundEndpointConfig, name: string) =
        { config with
            DnsResolverId = Some(Managed(dnsResolvers.resourceId (ResourceName name)))
        }

    [<CustomOperation "link_to_dns_resolver">]
    member _.LinkToDnsResolverId(config: DnsResolverInboundEndpointConfig, id: ResourceId) =
        { config with
            DnsResolverId = Some(Unmanaged id)
        }

    member _.LinkToDnsResolverId(config: DnsResolverInboundEndpointConfig, dnsResolver: DnsResolverConfig) =
        { config with
            DnsResolverId = Some(Unmanaged (dnsResolver :> IBuilder).ResourceId)
        }

    member _.LinkToDnsResolverId(config: DnsResolverInboundEndpointConfig, name: string) =
        { config with
            DnsResolverId = Some(Unmanaged(dnsResolvers.resourceId (ResourceName name)))
        }

    [<CustomOperation "subnet">]
    member _.Subnet(config: DnsResolverInboundEndpointConfig, id: ResourceId) =
        { config with
            SubnetId = Some(Managed id)
        }

    [<CustomOperation "link_to_subnet">]
    member _.LinkToSubnet(config: DnsResolverInboundEndpointConfig, id: ResourceId) =
        { config with
            SubnetId = Some(Unmanaged id)
        }

    [<CustomOperation "add_dynamic_ip">]
    member _.DynamicIpAllocation(state: DnsResolverInboundEndpointConfig) =
        { state with
            PrivateIpAllocations = state.PrivateIpAllocations @ [ AllocationMethod.DynamicPrivateIp ]
        }

    [<CustomOperation "add_static_ip">]
    member _.StaticIpAllocation(state: DnsResolverInboundEndpointConfig, addr: System.Net.IPAddress) =
        { state with
            PrivateIpAllocations = state.PrivateIpAllocations @ [ AllocationMethod.StaticPrivateIp addr ]
        }

    member _.StaticIpAllocation(state: DnsResolverInboundEndpointConfig, addr: string) =
        { state with
            PrivateIpAllocations =
                state.PrivateIpAllocations
                @ [ AllocationMethod.StaticPrivateIp(System.Net.IPAddress.Parse addr) ]
        }

    interface ITaggable<DnsResolverInboundEndpointConfig> with
        member _.Add state tags =
            { state with
                Tags = state.Tags |> Map.merge tags
            }

    interface IDependable<DnsResolverInboundEndpointConfig> with
        member _.Add state newDeps =
            { state with
                Dependencies = state.Dependencies + newDeps
            }

let dnsInboundEndpoint = DnsResolverInboundEndpointBuilder()

type DnsResolverOutboundEndpointBuilder() =
    member _.Yield _ =
        {
            Name = ResourceName.Empty
            DnsResolverId = None
            SubnetId = None
            Dependencies = Set.empty
            Tags = Map.empty
        }

    [<CustomOperation "name">]
    member _.Name(config: DnsResolverOutboundEndpointConfig, name: string) =
        { config with Name = ResourceName name }

    [<CustomOperation "dns_resolver">]
    member _.DnsResolverId(config: DnsResolverOutboundEndpointConfig, id: ResourceId) =
        { config with
            DnsResolverId = Some(Managed id)
        }

    member _.DnsResolverId(config: DnsResolverOutboundEndpointConfig, dnsResolver: DnsResolverConfig) =
        { config with
            DnsResolverId = Some(Managed (dnsResolver :> IBuilder).ResourceId)
        }

    member _.DnsResolverId(config: DnsResolverOutboundEndpointConfig, name: string) =
        { config with
            DnsResolverId = Some(Managed(dnsResolvers.resourceId (ResourceName name)))
        }

    [<CustomOperation "link_to_dns_resolver">]
    member _.LinkToDnsResolverId(config: DnsResolverOutboundEndpointConfig, id: ResourceId) =
        { config with
            DnsResolverId = Some(Unmanaged id)
        }

    member _.LinkToDnsResolverId(config: DnsResolverOutboundEndpointConfig, dnsResolver: DnsResolverConfig) =
        { config with
            DnsResolverId = Some(Unmanaged (dnsResolver :> IBuilder).ResourceId)
        }

    member _.LinkToDnsResolverId(config: DnsResolverOutboundEndpointConfig, name: string) =
        { config with
            DnsResolverId = Some(Unmanaged(dnsResolvers.resourceId (ResourceName name)))
        }

    [<CustomOperation "subnet">]
    member _.Subnet(config: DnsResolverOutboundEndpointConfig, id: ResourceId) =
        { config with
            SubnetId = Some(Managed id)
        }

    [<CustomOperation "link_to_subnet">]
    member _.LinkToSubnet(config: DnsResolverOutboundEndpointConfig, id: ResourceId) =
        { config with
            SubnetId = Some(Unmanaged id)
        }

    interface ITaggable<DnsResolverOutboundEndpointConfig> with
        member _.Add state tags =
            { state with
                Tags = state.Tags |> Map.merge tags
            }

    interface IDependable<DnsResolverOutboundEndpointConfig> with
        member _.Add state newDeps =
            { state with
                Dependencies = state.Dependencies + newDeps
            }

let dnsOutboundEndpoint = DnsResolverOutboundEndpointBuilder()

type DnsForwardingRuleConfig =
    {
        Name: ResourceName
        ForwardingRulesetId: LinkedResource option
        DomainName: string option
        ForwardingRuleState: FeatureFlag option
        TargetDnsServers: System.Net.IPEndPoint list
    }

    interface IBuilder with
        member this.BuildResources _ =
            [
                {
                    ForwardingRule.Name = this.Name
                    ForwardingRulesetId =
                        this.ForwardingRulesetId
                        |> Option.defaultWith (fun _ -> raiseFarmer "DNS forwarding rule must be linked to a ruleset.")
                    DomainName =
                        this.DomainName
                        |> Option.defaultWith (fun () -> raiseFarmer "DNS forwarding rule requires a domain.")
                    ForwardingRuleState = this.ForwardingRuleState
                    TargetDnsServers = this.TargetDnsServers
                }
            ]

        member this.ResourceId =
            match this.ForwardingRulesetId with
            | None -> raiseFarmer "DNS forwarding rule must be linked to a ruleset."
            | Some ruleset -> dnsForwardingRulesetForwardingRules.resourceId (ruleset.Name / this.Name)

type DnsForwardingRuleBuilder() =
    member _.Yield _ =
        {
            Name = ResourceName.Empty
            ForwardingRulesetId = None
            DomainName = None
            ForwardingRuleState = None
            TargetDnsServers = []
        }

    member this.Run config =
        if config.DomainName.IsNone then
            raiseFarmer "Must set 'domain_name' for forwarding rule."

        config

    [<CustomOperation "name">]
    member _.Name(config: DnsForwardingRuleConfig, name: string) =
        { config with Name = ResourceName name }

    [<CustomOperation "forwarding_ruleset_id">]
    member _.ForwardingRulesetId(config: DnsForwardingRuleConfig, id: ResourceId) =
        { config with
            ForwardingRulesetId = Some(Unmanaged id)
        }

    member _.ForwardingRulesetId(config: DnsForwardingRuleConfig, name: string) =
        { config with
            ForwardingRulesetId = Some(Unmanaged(dnsForwardingRulesets.resourceId (ResourceName name)))
        }

    [<CustomOperation "domain_name">]
    member _.DomainName(config: DnsForwardingRuleConfig, domainName: string) =
        let domainNameWithDot =
            if domainName.EndsWith "." then
                domainName
            else
                $"{domainName}."

        { config with
            DomainName = Some domainNameWithDot
        }

    [<CustomOperation "state">]
    member _.State(config: DnsForwardingRuleConfig, state) =
        { config with
            ForwardingRuleState = Some state
        }

    [<CustomOperation "add_target_dns_servers">]
    member _.AddTargetDnsServers(config: DnsForwardingRuleConfig, targetDnsServers: System.Net.IPEndPoint list) =
        { config with
            TargetDnsServers = config.TargetDnsServers @ targetDnsServers
        }

    member _.AddTargetDnsServers(config: DnsForwardingRuleConfig, targetDnsServers: string list) =
        let parseIpEndpoint (s: string) =
            let colonIdx = s.LastIndexOf ":"

            if colonIdx > 0 then
                if s.[colonIdx - 1] = ']' then // IPv6 with port
                    IPEndPoint(IPAddress.Parse(s.Substring(0, colonIdx)), System.Int32.Parse(s.Substring(colonIdx + 1)))
                elif not (s.Substring(0, colonIdx).Contains(":")) then // not IPv6 because this is the only colon.
                    IPEndPoint(IPAddress.Parse(s.Substring(0, colonIdx)), System.Int32.Parse(s.Substring(colonIdx + 1)))
                else
                    IPEndPoint(IPAddress.Parse(s.Substring(0, colonIdx)), System.Int32.Parse(s.Substring(colonIdx + 1)))
            else
                IPEndPoint(IPAddress.Parse(s), 53)

        let targetDnsServers = targetDnsServers |> List.map parseIpEndpoint
        // Need to fulfill this since it's not in netstandard2.0
        { config with
            TargetDnsServers = config.TargetDnsServers @ targetDnsServers
        }

let dnsForwardingRule = DnsForwardingRuleBuilder()

type DnsForwardingRulesetConfig =
    {
        Name: ResourceName
        DnsResolverOutboundEndpointIds: ResourceId Set
        Rules: DnsForwardingRuleConfig list
        VnetLinks: LinkedResource list
        Dependencies: Set<ResourceId>
        Tags: Map<string, string>
    }

    interface IBuilder with
        member this.BuildResources location =
            seq {
                yield
                    {
                        Name = this.Name
                        Location = location
                        DnsResolverOutboundEndpointIds = this.DnsResolverOutboundEndpointIds
                        Dependencies = this.Dependencies
                        Tags = this.Tags
                    }
                    :> IArmResource

                for ruleConfig in this.Rules do
                    let ruleWithRuleset =
                        { ruleConfig with
                            ForwardingRulesetId = Some(Managed (this :> IBuilder).ResourceId)
                        }

                    for rule in (ruleWithRuleset :> IBuilder).BuildResources location do
                        yield rule

                for vnetLink in this.VnetLinks do
                    yield
                        {
                            VirtualNetworkLink.Name = this.Name / vnetLink.Name
                            ForwardingRulesetId = Managed (this :> IBuilder).ResourceId
                            VirtualNetworkId = vnetLink
                        }
            }
            |> List.ofSeq

        member this.ResourceId = dnsForwardingRulesets.resourceId this.Name

type DnsForwardingRulesetBuilder() =
    member _.Yield _ =
        {
            Name = ResourceName.Empty
            DnsResolverOutboundEndpointIds = Set.empty
            Rules = []
            VnetLinks = []
            Dependencies = Set.empty
            Tags = Map.empty
        }

    [<CustomOperation "name">]
    member _.Name(config: DnsForwardingRulesetConfig, name: string) =
        { config with Name = ResourceName name }

    [<CustomOperation "add_resolver_outbound_endpoints">]
    member _.AddResolverOutboundEndpoints(config: DnsForwardingRulesetConfig, outboundIds: ResourceId list) =
        { config with
            DnsResolverOutboundEndpointIds = config.DnsResolverOutboundEndpointIds |> Set.union (Set.ofList outboundIds)
        }

    member _.AddResolverOutboundEndpoints
        (
            config: DnsForwardingRulesetConfig,
            outboundEndpoints: DnsResolverOutboundEndpointConfig list
        ) =
        let outboundEndpointIds =
            outboundEndpoints
            |> List.map (fun oe -> (oe :> IBuilder).ResourceId)
            |> Set.ofList

        { config with
            DnsResolverOutboundEndpointIds = config.DnsResolverOutboundEndpointIds |> Set.union outboundEndpointIds
        }

    [<CustomOperation "add_rules">]
    member _.AddRules(config: DnsForwardingRulesetConfig, rules: DnsForwardingRuleConfig list) =
        { config with
            Rules = config.Rules @ rules
        }

    [<CustomOperation "add_vnet_links">]
    member _.AddVnetLinks(config: DnsForwardingRulesetConfig, vnetLinks: ResourceId list) =
        { config with
            VnetLinks = config.VnetLinks @ (vnetLinks |> List.map Unmanaged)
        }

    member _.AddVnetLinks(config: DnsForwardingRulesetConfig, vnetBuilders: IBuilder list) =
        { config with
            VnetLinks = config.VnetLinks @ (vnetBuilders |> List.map (fun b -> Unmanaged b.ResourceId))
        }

    interface ITaggable<DnsForwardingRulesetConfig> with
        member _.Add state tags =
            { state with
                Tags = state.Tags |> Map.merge tags
            }

    interface IDependable<DnsForwardingRulesetConfig> with
        member _.Add state newDeps =
            { state with
                Dependencies = state.Dependencies + newDeps
            }

let dnsForwardingRuleset = DnsForwardingRulesetBuilder()
