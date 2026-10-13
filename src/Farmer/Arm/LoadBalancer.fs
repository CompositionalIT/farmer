module Farmer.Arm.LoadBalancer

open Farmer
open Farmer.LoadBalancer
open Farmer.PublicIpAddress

let loadBalancers = ResourceType("Microsoft.Network/loadBalancers", "2024-05-01")

let loadBalancerFrontendIPConfigurations =
    ResourceType("Microsoft.Network/loadBalancers/frontendIPConfigurations", "2024-05-01")

let loadBalancerBackendAddressPools =
    ResourceType("Microsoft.Network/loadBalancers/backendAddressPools", "2024-05-01")

let loadBalancerProbes =
    ResourceType("Microsoft.Network/loadBalancers/probes", "2024-05-01")

type LoadBalancer = {
    Name: ResourceName
    Location: Location
    Sku: LoadBalancerSku
    FrontendIpConfigs:
        {|
            Name: ResourceName
            PrivateIpAllocationMethod: PrivateIpAddress.AllocationMethod
            AddressVersion: Network.AddressVersion
            PublicIp: ResourceId option
            Subnet: ResourceId option
            Zones: ZoneSelection
        |} list
    BackendAddressPools: ResourceName list
    LoadBalancingRules:
        {|
            Name: ResourceName
            FrontendIpConfiguration: ResourceName
            BackendAddressPool: ResourceName
            Probe: ResourceName option
            FrontendPort: uint16
            BackendPort: uint16
            Protocol: TransmissionProtocol option // default "All"
            IdleTimeoutMinutes: int option // default 4 minutes
            LoadDistribution: LoadDistributionPolicy
            EnableTcpReset: bool option // default false
            DisableOutboundSnat: bool option
        |} list // default true
    Probes:
        {|
            Name: ResourceName
            Protocol: LoadBalancerProbeProtocol
            Port: uint16
            RequestPath: string
            IntervalInSeconds: int
            NumberOfProbes: int
        |} list
    Dependencies: Set<ResourceId>
    Tags: Map<string, string>
} with

    interface IArmResource with
        member this.ResourceId = loadBalancers.resourceId this.Name

        member this.JsonModel = {|
            loadBalancers.Create(this.Name, this.Location, this.Dependencies, this.Tags) with
                sku = {|
                    name = this.Sku.Name.ArmValue
                    tier = this.Sku.Tier.ArmValue
                |}
                properties = {|
                    frontendIpConfigurations =
                        this.FrontendIpConfigs
                        |> List.map (fun frontend ->
                            let allocationMethod, ip =
                                match frontend.PrivateIpAllocationMethod with
                                | PrivateIpAddress.DynamicPrivateIp -> "Dynamic", null
                                | PrivateIpAddress.StaticPrivateIp ip -> "Static", string ip

                            {|
                                name = frontend.Name.Value
                                properties = {|
                                    privateIPAllocationMethod = allocationMethod
                                    privateIPAddress = ip
                                    publicIPAddress =
                                        frontend.PublicIp
                                        |> Option.map (fun pip -> {| id = pip.Eval() |})
                                        |> Option.defaultValue Unchecked.defaultof<_>
                                    subnet =
                                        frontend.Subnet
                                        |> Option.map (fun subnetId -> {| id = subnetId.Eval() |})
                                        |> Option.defaultValue Unchecked.defaultof<_>
                                |}
                                zones = // Zones are specified on the frontend only for internal load balancers
                                    if frontend.Subnet.IsNone then
                                        null
                                    else
                                        frontend.Zones.ArmValue
                            |})
                    backendAddressPools =
                        this.BackendAddressPools |> List.map (fun backend -> {| name = backend.Value |})
                    loadBalancingRules =
                        this.LoadBalancingRules
                        |> List.map (fun rule -> {|
                            name = rule.Name.Value
                            properties = {|
                                frontendIPConfiguration = {|
                                    id =
                                        loadBalancerFrontendIPConfigurations
                                            .resourceId(this.Name, rule.FrontendIpConfiguration)
                                            .Eval()
                                |}
                                frontendPort = rule.FrontendPort
                                backendPort = rule.BackendPort
                                protocol =
                                    rule.Protocol
                                    |> Option.map (function
                                        | TransmissionProtocol.TCP -> "Tcp"
                                        | TransmissionProtocol.UDP -> "Udp")
                                    |> Option.defaultValue "All"
                                idleTimeoutInMinutes = rule.IdleTimeoutMinutes |> Option.defaultValue 4
                                enableTcpReset = rule.EnableTcpReset |> Option.defaultValue false
                                disableOutboundSnat = rule.DisableOutboundSnat |> Option.defaultValue true
                                loadDistribution = rule.LoadDistribution.ArmValue
                                backendAddressPool = {|
                                    id =
                                        loadBalancerBackendAddressPools
                                            .resourceId(this.Name, rule.BackendAddressPool)
                                            .Eval()
                                |}
                                probe =
                                    rule.Probe
                                    |> Option.map (fun probe -> {|
                                        id = loadBalancerProbes.resourceId(this.Name, probe).Eval()
                                    |})
                                    |> Option.defaultValue Unchecked.defaultof<_>
                            |}
                        |})
                    probes =
                        this.Probes
                        |> List.map (fun probe -> {|
                            name = probe.Name.Value
                            properties = {|
                                protocol = probe.Protocol.ArmValue
                                port = probe.Port
                                requestPath = probe.RequestPath
                                intervalInSeconds = probe.IntervalInSeconds
                                numberOfProbes = probe.NumberOfProbes
                            |}
                        |})
                    inboundNatRules = []
                    outboundNatRules = []
                    inboundNatPools = []
                |}
        |}

type BackendAddressPool = {
    /// Name of the backend address pool
    Name: ResourceName
    /// Name of the load balancer where this pool will be added.
    LoadBalancer: ResourceName
    /// Addresses of backend services.
    LoadBalancerBackendAddresses:
        {|
            Name: ResourceName
            VirtualNetwork: LinkedResource option
            Subnet: LinkedResource option
            IpAddress: System.Net.IPAddress
        |} list
} with

    interface IArmResource with
        member this.ResourceId =
            loadBalancerBackendAddressPools.resourceId (this.LoadBalancer, this.Name)

        member this.JsonModel =
            let dependencies =
                seq {
                    yield loadBalancers.resourceId this.LoadBalancer

                    for addr in this.LoadBalancerBackendAddresses do
                        if (addr.Subnet |> Option.isSome) then
                            match addr.Subnet with
                            | Some(Managed subnetId) -> yield subnetId
                            | _ -> ()
                        else
                            match addr.VirtualNetwork with
                            | Some(Managed vnetId) -> yield vnetId
                            | _ -> ()
                }
                |> Set.ofSeq

            {|
                loadBalancerBackendAddressPools.Create(this.Name, dependsOn = dependencies) with
                    name = $"{this.LoadBalancer.Value}/{this.Name.Value}"
                    properties = {|
                        loadBalancerBackendAddresses =
                            this.LoadBalancerBackendAddresses
                            |> List.map (fun addr -> {|
                                name = addr.Name.Value
                                properties = {|
                                    ipAddress = string addr.IpAddress
                                    subnet =
                                        match addr.Subnet with
                                        | Some(Managed subnetId) -> {| id = subnetId.Eval() |}
                                        | Some(Unmanaged subnetId) -> {| id = subnetId.Eval() |}
                                        | None -> Unchecked.defaultof<_>
                                    virtualNetwork =
                                        match addr.Subnet with
                                        | None ->
                                            match addr.VirtualNetwork with
                                            | Some(Managed vnetId) -> {| id = vnetId.Eval() |}
                                            | Some(Unmanaged vnetId) -> {| id = vnetId.Eval() |}
                                            | None -> Unchecked.defaultof<_>
                                        | Some _ -> Unchecked.defaultof<_>
                                |}
                            |})
                    |}
            |}