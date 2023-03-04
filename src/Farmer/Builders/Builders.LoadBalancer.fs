[<AutoOpen>]
module Farmer.Builders.LoadBalancer

open System
open Farmer
open Farmer.Arm.LoadBalancer
open Farmer.Arm.Network
open Farmer.LoadBalancer
open Farmer.PublicIpAddress

type FrontendIpConfig =
    {
        Name: ResourceName
        PrivateIpAllocationMethod: PrivateIpAddress.AllocationMethod
        PublicIp: LinkedResource option
        Subnet: LinkedResource option
    }

    static member BuildResource frontend = {|
        Name = frontend.Name
        PrivateIpAllocationMethod = frontend.PrivateIpAllocationMethod
        PublicIp = frontend.PublicIp |> Option.map (fun linkedRes -> linkedRes.ResourceId)
        Subnet = frontend.Subnet |> Option.map (fun linkedRes -> linkedRes.ResourceId)
    |}

    static member BuildIp
        (frontend: FrontendIpConfig)
        (lbName: string)
        (lbSku: LoadBalancer.Sku)
        (location: Location)
        : PublicIpAddress option =
        match frontend.PublicIp with
        | Some(Managed resId) ->
            {
                Name = resId.Name
                AllocationMethod = AllocationMethod.Static
                AvailabilityZone = None
                Location = location
                Sku =
                    match lbSku with
                    | Farmer.LoadBalancer.Sku.Basic -> PublicIpAddress.Sku.Basic
                    | Farmer.LoadBalancer.Sku.Standard -> PublicIpAddress.Sku.Standard
                DomainNameLabel = None
                Tags = Map.empty
            }
            |> Some
        | _ -> None


type FrontendIpBuilder() =
    member _.Yield _ = {
        Name = ResourceName.Empty
        PrivateIpAllocationMethod = PrivateIpAddress.DynamicPrivateIp
        PublicIp = None
        Subnet = None
    }

    /// Sets the name of the frontend IP configuration.
    [<CustomOperation "name">]
    member _.Name(state: FrontendIpConfig, name) = { state with Name = ResourceName name }

    /// Sets the frontend's private IP allocation method.
    [<CustomOperation "private_ip_allocation_method">]
    member _.PrivateIpAllocationMethod(state: FrontendIpConfig, allocationMethod) =
        { state with
            PrivateIpAllocationMethod = allocationMethod
        }

    /// Sets the name of the frontend public IP.
    [<CustomOperation "public_ip">]
    member _.PublicIp(state: FrontendIpConfig, publicIp) =
        { state with
            PublicIp = Some(Managed(Farmer.Arm.Network.publicIPAddresses.resourceId (ResourceName publicIp)))
        }

    /// Links the frontend to an existing public IP.
    [<CustomOperation "link_to_public_ip">]
    member _.LinkToPublicIp(state: FrontendIpConfig, publicIp) =
        { state with
            PublicIp = Some(Unmanaged publicIp)
        }

    /// Links the frontend to a subnet in the same deployment.
    [<CustomOperation "link_to_subnet">]
    member _.LinkToSubnet(state: FrontendIpConfig, subnetId) =
        { state with
            Subnet = Some(Managed subnetId)
        }

    /// Links the frontend to an existing subnet.
    [<CustomOperation "link_to_unmanaged_subnet">]
    member _.LinkToUnmanagedSubnet(state: FrontendIpConfig, subnetId) =
        { state with
            Subnet = Some(Unmanaged subnetId)
        }

let frontend = FrontendIpBuilder()

type BackendAddressPoolConfig =
    {
        Name: ResourceName
        LoadBalancer: ResourceName
        LoadBalancerBackendAddresses: System.Net.IPAddress list
        VirtualNetwork: LinkedResource option
    }

    interface IBuilder with
        member this.ResourceId =
            Farmer.Arm.LoadBalancer.loadBalancerBackendAddressPools.resourceId (this.LoadBalancer, this.Name)

        member this.BuildResources _ =
            if String.IsNullOrWhiteSpace(this.LoadBalancer.Value) then
                raiseFarmer "Load balancer must be specified for backend address pool."
            else
                [
                    {
                        Name = this.Name
                        LoadBalancer = this.LoadBalancer
                        LoadBalancerBackendAddresses =
                            this.LoadBalancerBackendAddresses
                            |> List.mapi (fun idx addr -> {|
                                Name = ResourceName $"addr{idx}"
                                VirtualNetwork = this.VirtualNetwork
                                IpAddress = addr
                            |})
                    }
                ]

type BackendAddressPoolBuilder() =
    member _.Yield _ = {
        Name = ResourceName.Empty
        LoadBalancer = ResourceName.Empty
        LoadBalancerBackendAddresses = []
        VirtualNetwork = None
    }

    /// Sets the name of the backend address pool.
    [<CustomOperation "name">]
    member _.Name(state: BackendAddressPoolConfig, name) = { state with Name = ResourceName name }

    /// Sets the name of the load balancer for this pool.
    [<CustomOperation "load_balancer">]
    member _.LoadBalancer(state: BackendAddressPoolConfig, lb) =
        { state with
            LoadBalancer = ResourceName lb
        }

    /// Links to an existing vnet for addresses for this pool.
    [<CustomOperation "link_to_vnet">]
    member _.LinkToVirtualNetwork(state: BackendAddressPoolConfig, vnet: string) =
        { state with
            VirtualNetwork = Some(Unmanaged(virtualNetworks.resourceId (ResourceName vnet)))
        }

    member _.LinkToVirtualNetwork(state: BackendAddressPoolConfig, vnet: ResourceId) =
        { state with
            VirtualNetwork = Some(Unmanaged vnet)
        }

    member _.LinkToVirtualNetwork(state: BackendAddressPoolConfig, vnetConfig: VirtualNetworkConfig) =
        { state with
            VirtualNetwork = Some(Unmanaged(virtualNetworks.resourceId vnetConfig.Name))
        }

    /// Links to a vnet that is defined in this same deployment.
    [<CustomOperation "vnet">]
    member _.VirtualNetwork(state: BackendAddressPoolConfig, vnet: string) =
        { state with
            VirtualNetwork = Some(Managed(virtualNetworks.resourceId (ResourceName vnet)))
        }

    member _.VirtualNetwork(state: BackendAddressPoolConfig, vnet: ResourceId) =
        { state with
            VirtualNetwork = Some(Managed vnet)
        }

    member _.VirtualNetwork(state: BackendAddressPoolConfig, vnetConfig: VirtualNetworkConfig) =
        { state with
            VirtualNetwork = Some(Managed(virtualNetworks.resourceId vnetConfig.Name))
        }

    /// Adds IP addresses for this backend pool.
    [<CustomOperation "add_ip_addresses">]
    member _.IpAddresses(state: BackendAddressPoolConfig, backendAddresses: string list) =
        { state with
            LoadBalancerBackendAddresses =
                state.LoadBalancerBackendAddresses
                @ (backendAddresses |> List.map System.Net.IPAddress.Parse)
        }

    member _.IpAddresses(state: BackendAddressPoolConfig, backendAddresses: System.Net.IPAddress list) =
        { state with
            LoadBalancerBackendAddresses = state.LoadBalancerBackendAddresses @ (backendAddresses)
        }

let backendAddressPool = BackendAddressPoolBuilder()

type ProbeConfig =
    {
        /// Name of the probe
        Name: ResourceName
        /// Protocol - TCP requires ACK for success, HTTP(S) require 200 OK for success
        Protocol: LoadBalancerProbeProtocol option
        /// Port 1-65535
        Port: uint16 option
        /// Request path for HTTP(S) probes
        RequestPath: string option
        /// Interval between probes to the backend
        IntervalInSeconds: int
        /// Number of failed probes before removing from pool
        NumberOfProbes: int
    }

    static member BuildResource probe = {|
        Name = probe.Name
        Protocol = probe.Protocol |> Option.defaultValue LoadBalancerProbeProtocol.TCP
        Port = probe.Port |> Option.defaultValue 0us
        RequestPath = probe.RequestPath |> Option.toObj
        IntervalInSeconds = probe.IntervalInSeconds
        NumberOfProbes = probe.NumberOfProbes
    |}

type ProbeBuilder() =
    member _.Yield _ = {
        Name = ResourceName.Empty
        Protocol = None
        Port = None
        RequestPath = None
        IntervalInSeconds = 15
        NumberOfProbes = 2
    }

    member _.Run(config: ProbeConfig) =
        match config.Port with
        | None -> raiseFarmer "A 'port' value is required for probes."
        | _ -> ()

        match config.Protocol with
        | Some LoadBalancerProbeProtocol.HTTP
        | Some LoadBalancerProbeProtocol.HTTPS ->
            if config.RequestPath.IsNone then
                raiseFarmer "Set 'request_path' for HTTP or HTTPS probes."
        | _ -> ()

        { config with
            Name = config.Name.IfEmpty $"{config.Protocol.Value}-{config.Port.Value}"
        }

    /// Sets the name of the connectivity probe.
    [<CustomOperation "name">]
    member _.Name(state: ProbeConfig, name) = { state with Name = ResourceName name }

    /// Sets the protocol for connections to probe.
    [<CustomOperation "protocol">]
    member _.Protocol(state: ProbeConfig, protocol) = { state with Protocol = Some protocol }

    /// Sets the port for connections to probe.
    [<CustomOperation "port">]
    member _.Port(state: ProbeConfig, port: uint16) = { state with Port = Some port }

    member _.Port(state: ProbeConfig, port: int) = { state with Port = Some(uint16 port) }

    /// Sets the request path for HTTP and HTTPS connection probes.
    [<CustomOperation "request_path">]
    member _.RequestPath(state: ProbeConfig, requestPath: string) =
        { state with
            RequestPath = Some requestPath
        }

    /// Sets the interval in seconds between probes.
    [<CustomOperation "interval">]
    member _.Interval(state: ProbeConfig, interval: int) =
        { state with
            IntervalInSeconds = interval
        }

    member _.Interval(state: ProbeConfig, interval: TimeSpan) =
        { state with
            IntervalInSeconds = int interval.TotalSeconds
        }

    /// Sets the number of probes to consider this backend a failure and remove from the pool.
    [<CustomOperation "number_of_probes">]
    member _.NumberOfProbes(state: ProbeConfig, numberOfProbes) =
        { state with
            NumberOfProbes = numberOfProbes
        }

let loadBalancerProbe = ProbeBuilder()

type LoadBalancingRuleConfig =
    {
        Name: ResourceName
        FrontendIpConfiguration: ResourceName
        BackendAddressPool: ResourceName
        Probe: ResourceName option
        FrontendPort: uint16
        BackendPort: uint16
        Protocol: TransmissionProtocol option // default "All"
        IdleTimeoutMinutes: int option // default 4 minutes
        LoadDistribution: Farmer.LoadBalancer.LoadDistributionPolicy
        EnableTcpReset: bool option // default false
        DisableOutboundSnat: bool option
    } // default true

    static member BuildResource rule = {|
        Name = rule.Name
        FrontendIpConfiguration = rule.FrontendIpConfiguration
        BackendAddressPool = rule.BackendAddressPool
        Probe = rule.Probe
        FrontendPort = rule.FrontendPort
        BackendPort = rule.BackendPort
        Protocol = rule.Protocol
        IdleTimeoutMinutes = rule.IdleTimeoutMinutes
        LoadDistribution = rule.LoadDistribution
        EnableTcpReset = rule.EnableTcpReset
        DisableOutboundSnat = rule.DisableOutboundSnat
    |}

type LoadBalancingRuleBuilder() =
    member _.Yield _ = {
        Name = ResourceName.Empty
        FrontendIpConfiguration = ResourceName.Empty
        BackendAddressPool = ResourceName.Empty
        Probe = None
        FrontendPort = 0us
        BackendPort = 0us
        Protocol = None // default "All"
        IdleTimeoutMinutes = None // default 4 minutes
        LoadDistribution = Farmer.LoadBalancer.LoadDistributionPolicy.Default
        EnableTcpReset = None
        DisableOutboundSnat = None // default true
    }

    /// Sets the name of the load balancing rule.
    [<CustomOperation "name">]
    member _.Name(state: LoadBalancingRuleConfig, name) = { state with Name = ResourceName name }

    /// Sets the name of the load balancing rule.
    [<CustomOperation "frontend_ip_config">]
    member _.FrontendIpConfig(state: LoadBalancingRuleConfig, frontendIpConfig: string) =
        { state with
            FrontendIpConfiguration = ResourceName frontendIpConfig
        }

    member _.FrontendIpConfig(state: LoadBalancingRuleConfig, frontendIpConfig: FrontendIpConfig) =
        { state with
            FrontendIpConfiguration = frontendIpConfig.Name
        }

    /// Sets the name of the load balancing rule.
    [<CustomOperation "backend_address_pool">]
    member _.BackendAddressPool(state: LoadBalancingRuleConfig, backendAddressPool: string) =
        { state with
            BackendAddressPool = ResourceName backendAddressPool
        }

    member _.BackendAddressPool(state: LoadBalancingRuleConfig, backendAddressPool: BackendAddressPoolConfig) =
        { state with
            BackendAddressPool = backendAddressPool.Name
        }

    /// Sets the probe to use for this load balancing rule.
    [<CustomOperation "probe">]
    member _.Probe(state: LoadBalancingRuleConfig, probe: string) =
        { state with
            Probe = Some(ResourceName probe)
        }

    member _.Probe(state: LoadBalancingRuleConfig, probe: ProbeConfig) = { state with Probe = Some probe.Name }

    /// Sets the frontend port for this rule.
    [<CustomOperation "frontend_port">]
    member _.FrontendPort(state: LoadBalancingRuleConfig, frontendPort: uint16) =
        { state with
            FrontendPort = frontendPort
        }

    member _.FrontendPort(state: LoadBalancingRuleConfig, frontendPort: int) =
        { state with
            FrontendPort = uint16 frontendPort
        }

    /// Sets the port on the backend pool for this rule.
    [<CustomOperation "backend_port">]
    member _.BackendPort(state: LoadBalancingRuleConfig, backendPort: uint16) =
        { state with BackendPort = backendPort }

    member _.BackendPort(state: LoadBalancingRuleConfig, backendPort: int) =
        { state with
            BackendPort = uint16 backendPort
        }

    /// Sets the load balancing protocol for this rule
    [<CustomOperation "protocol">]
    member _.Protocol(state: LoadBalancingRuleConfig, protocol: TransmissionProtocol) =
        { state with Protocol = Some protocol }

    /// Sets the idle timeout in minutes for this rule, keeping it between 4 and 30 minutes.
    [<CustomOperation "idle_timeout_minutes">]
    member _.IdleTimeoutMinutes(state: LoadBalancingRuleConfig, idleTimeoutMin: int) =
        { state with
            IdleTimeoutMinutes =
                if idleTimeoutMin <= 4 then 4
                elif idleTimeoutMin > 30 then 30
                else idleTimeoutMin
                |> Some
        }

    /// Sets the load distribution policy for this rule
    [<CustomOperation "load_distribution_policy">]
    member _.LoadDistributionPolicy(state: LoadBalancingRuleConfig, loadDistributionPolicy: LoadDistributionPolicy) =
        { state with
            LoadDistribution = loadDistributionPolicy
        }

    /// If set, this allows the TCP connection to the load balancer to be reset by a timeout or connection termination.
    [<CustomOperation "enable_tcp_reset">]
    member _.EnableTcpReset(state: LoadBalancingRuleConfig) =
        { state with
            EnableTcpReset = Some true
        }

    /// If set, this allows the backend pool to use this load balancer for outbound connections (disabled by default).
    [<CustomOperation "enable_outbound_snat">]
    member _.EnableOutboundSnat(state: LoadBalancingRuleConfig) =
        { state with
            DisableOutboundSnat = Some false
        }

let loadBalancingRule = LoadBalancingRuleBuilder()

type LoadBalancerConfig =
    {
        Name: ResourceName
        Sku: LoadBalancerSku
        FrontendIpConfigs: FrontendIpConfig list
        BackendAddressPools: BackendAddressPoolConfig list
        LoadBalancingRules: LoadBalancingRuleConfig list
        Probes: ProbeConfig list
        Dependencies: Set<ResourceId>
        Tags: Map<string, string>
    }

    interface IBuilder with
        member this.ResourceId = loadBalancers.resourceId this.Name

        member this.BuildResources location =
            let frontendPublicIps =
                this.FrontendIpConfigs
                |> List.map (fun frontend -> FrontendIpConfig.BuildIp frontend this.Name.Value this.Sku.Name location)
                |> List.choose id

            let backendPools =
                this.BackendAddressPools
                |> List.map (fun pool -> { pool with LoadBalancer = this.Name })
                |> List.map (fun be -> (be :> IBuilder).BuildResources location)
                |> List.concat

            {
                Name = this.Name
                Location = location
                Sku = this.Sku
                FrontendIpConfigs = this.FrontendIpConfigs |> List.map FrontendIpConfig.BuildResource
                BackendAddressPools = this.BackendAddressPools |> List.map (fun p -> p.Name)
                LoadBalancingRules = this.LoadBalancingRules |> List.map LoadBalancingRuleConfig.BuildResource
                Probes = this.Probes |> List.map ProbeConfig.BuildResource
                Dependencies =
                    frontendPublicIps
                    |> List.map (fun pip -> publicIPAddresses.resourceId pip.Name)
                    |> Set.ofList
                    |> Set.union this.Dependencies
                Tags = this.Tags
            }
            :> IArmResource
            :: backendPools
            @ (frontendPublicIps |> Seq.cast<IArmResource> |> List.ofSeq)


type LoadBalancerBuilder() =
    member _.Yield _ = {
        Name = ResourceName.Empty
        Sku = {
            Name = LoadBalancer.Sku.Basic
            Tier = LoadBalancer.Tier.Regional
        }
        FrontendIpConfigs = []
        BackendAddressPools = []
        LoadBalancingRules = []
        Probes = []
        Dependencies = Set.empty
        Tags = Map.empty
    }

    /// Sets the name of the load balancer.
    [<CustomOperation "name">]
    member _.Name(state: LoadBalancerConfig, name) = { state with Name = ResourceName name }

    /// Sets the sku of the load balancer (default is 'basic').
    [<CustomOperation "sku">]
    member _.Sku(state: LoadBalancerConfig, skuName) =
        { state with
            Sku = { state.Sku with Name = skuName }
        }

    /// Sets the tier of the load balancer (default is 'regional').
    [<CustomOperation "tier">]
    member _.Tier(state: LoadBalancerConfig, skuTier) =
        { state with
            Sku = { state.Sku with Tier = skuTier }
        }

    /// Add one or more frontend IP configs.
    [<CustomOperation "add_frontends">]
    member _.AddFrontends(state: LoadBalancerConfig, frontends) =
        { state with
            FrontendIpConfigs = state.FrontendIpConfigs @ frontends
        }

    /// Add one or more backend pools.
    [<CustomOperation "add_backend_pools">]
    member _.AddBackendPools(state: LoadBalancerConfig, backends) =
        { state with
            BackendAddressPools = state.BackendAddressPools @ backends
        }

    /// Add one or more load balancing rules.
    [<CustomOperation "add_rules">]
    member _.AddLoadBalancingRules(state: LoadBalancerConfig, rules) =
        { state with
            LoadBalancingRules = state.LoadBalancingRules @ rules
        }

    /// Add one or more probes.
    [<CustomOperation "add_probes">]
    member _.AddProbes(state: LoadBalancerConfig, probes) =
        { state with
            Probes = state.Probes @ probes
        }

    /// Add any additional dependencies that must be built before this - for backwards compatibility since this implements IDependable now.
    [<CustomOperation "add_dependencies">]
    member _.AddDependencies(state: LoadBalancerConfig, deps: ResourceId list) =
        { state with
            Dependencies = deps |> Set.ofList |> Set.union state.Dependencies
        }

    interface IDependable<LoadBalancerConfig> with
        member _.Add state newDeps =
            { state with
                Dependencies = state.Dependencies + newDeps
            }

    interface ITaggable<LoadBalancerConfig> with
        member _.Add state tags =
            { state with
                Tags = state.Tags |> Map.merge tags
            }

let loadBalancer = LoadBalancerBuilder()
