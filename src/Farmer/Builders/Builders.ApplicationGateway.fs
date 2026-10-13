[<AutoOpen>]
module Farmer.Builders.ApplicationGateway

open System
open Farmer
open Farmer.Arm
open Farmer.Arm.Network
open Farmer.PublicIpAddress
open Farmer.Arm.ApplicationGateway
open Farmer.ApplicationGateway

type GatewayIpConfig = {
    Name: ResourceName
    Subnet: LinkedResource option
} with

    static member BuildResource gatewayIp = {|
        Name = gatewayIp.Name
        Subnet =
            gatewayIp.Subnet
            |> Option.map (function
                | Managed resId -> resId
                | Unmanaged resId -> resId)
    |}

    static member Dependencies gatewayIp =
        seq {
            gatewayIp.Subnet
            |> Option.bind (function
                | Managed resId -> Some resId
                | Unmanaged _ -> None)
        }
        |> Seq.choose id
        |> Set.ofSeq

type GatewayIpBuilder() =
    member _.Yield _ = {
        Name = ResourceName.Empty
        Subnet = None
    }

    [<CustomOperation "name">]
    member _.Name(state: GatewayIpConfig, name) = { state with Name = ResourceName name }

    [<CustomOperation "link_to_subnet">]
    member _.LinktoSubnet(state: GatewayIpConfig, vnet: ResourceName, subnet: ResourceName) = {
        state with
            Subnet = Some(Unmanaged(subnets.resourceId (vnet, subnet)))
    }

    member _.LinktoSubnet(state: GatewayIpConfig, vnet: string, subnet: string) = {
        state with
            Subnet = Some(Unmanaged(subnets.resourceId (ResourceName vnet, ResourceName subnet)))
    }

let gatewayIp = GatewayIpBuilder()

// Subtle differences between load balancers
type FrontendIpConfig = {
    Name: ResourceName
    PrivateIpAllocationMethod: PrivateIpAddress.AllocationMethod
    PublicIp: LinkedResource option
} with

    static member BuildResource frontend = {|
        Name = frontend.Name
        PrivateIpAllocationMethod = frontend.PrivateIpAllocationMethod
        PublicIp =
            frontend.PublicIp
            |> Option.map (function
                | Managed resId -> resId
                | Unmanaged resId -> resId)
    |}

    static member BuildIp (frontend: FrontendIpConfig) (location: Location) : PublicIpAddress option =
        match frontend.PublicIp with
        | Some(Managed resId) ->
            {
                Name = resId.Name
                AllocationMethod = AllocationMethod.Static
                AddressVersion = Network.AddressVersion.IPv4
                AvailabilityZones = NoZone
                Location = location
                Sku = PublicIpAddress.Sku.Standard
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
    }

    /// Sets the name of the frontend IP configuration.
    [<CustomOperation "name">]
    member _.Name(state: FrontendIpConfig, name) = { state with Name = ResourceName name }

    /// Sets the frontend's private IP allocation method.
    [<CustomOperation "private_ip_allocation_method">]
    member _.PrivateIpAllocationMethod(state: FrontendIpConfig, allocationMethod) = {
        state with
            PrivateIpAllocationMethod = allocationMethod
    }

    /// Sets the name of the frontend public IP.
    [<CustomOperation "public_ip">]
    member _.PublicIp(state: FrontendIpConfig, publicIp) = {
        state with
            PublicIp = Some(Managed(Farmer.Arm.Network.publicIPAddresses.resourceId (ResourceName publicIp)))
    }

    /// Links the frontend to an existing public IP.
    [<CustomOperation "link_to_public_ip">]
    member _.LinkToPublicIp(state: FrontendIpConfig, publicIp: string) = {
        state with
            PublicIp = Some(Unmanaged(virtualNetworks.resourceId (ResourceName publicIp)))
    }

let frontendIp = FrontendIpBuilder()

type FrontendPortConfig = {
    Name: ResourceName
    Port: uint16
} with

    static member BuildResource frontendPort = {|
        Name = frontendPort.Name
        Port = frontendPort.Port
    |}

type FrontendPortBuilder() =
    member _.Yield _ = {
        Name = ResourceName.Empty
        Port = uint16 80
    }

    [<CustomOperation "name">]
    member _.Name(state: FrontendPortConfig, name) = { state with Name = ResourceName name }

    [<CustomOperation "port">]
    member _.Port(state: FrontendPortConfig, port: uint16) = { state with Port = port }

    member _.Port(state: FrontendPortConfig, port: int) = { state with Port = uint16 port }

let frontendPort = FrontendPortBuilder()

type HttpListenerConfig = {
    Name: ResourceName
    FrontendIpConfiguration: ResourceName
    BackendAddressPool: ResourceName
    CustomErrorConfigurations:
        {|
            CustomErrorPageUrl: string
            StatusCode: HttpStatusCode
        |} list
    FirewallPolicy: ResourceId option
    FrontendPort: ResourceName
    RequireServerNameIndication: bool
    HostNames: string list
    Protocol: Protocol
    SslCertificate: ResourceName option
    SslProfile: ResourceName option
} with

    static member BuildResource(listener: HttpListenerConfig) = {|
        Name = listener.Name
        BackendAddressPool = listener.BackendAddressPool
        FrontendIpConfiguration = listener.FrontendIpConfiguration
        FrontendPort = listener.FrontendPort
        Protocol = listener.Protocol
        HostNames = listener.HostNames
        RequireServerNameIndication = listener.RequireServerNameIndication
        CustomErrorConfigurations = listener.CustomErrorConfigurations
        FirewallPolicy = listener.FirewallPolicy
        SslCertificate = listener.SslCertificate
        SslProfile = listener.SslProfile
    |}

type HttpListenerBuilder() =
    member _.Yield _ = {
        Name = ResourceName.Empty
        BackendAddressPool = ResourceName.Empty
        FrontendIpConfiguration = ResourceName.Empty
        FrontendPort = ResourceName.Empty
        Protocol = Protocol.Http
        HostNames = []
        RequireServerNameIndication = false
        CustomErrorConfigurations = []
        FirewallPolicy = None
        SslCertificate = None
        SslProfile = None
    }

    [<CustomOperation "name">]
    member _.Name(state: HttpListenerConfig, name) = { state with Name = ResourceName name }

    [<CustomOperation "backend_pool">]
    member _.BackendPool(state: HttpListenerConfig, backendPool: BackendAddressPoolConfig) = {
        state with
            BackendAddressPool = backendPool.Name
    }

    member _.BackendPool(state: HttpListenerConfig, backendPool: string) = {
        state with
            BackendAddressPool = ResourceName backendPool
    }

    [<CustomOperation "frontend_ip">]
    member _.FrontendIp(state: HttpListenerConfig, frontendIp: FrontendIpConfig) = {
        state with
            FrontendIpConfiguration = frontendIp.Name
    }

    member _.FrontendIp(state: HttpListenerConfig, frontendIp: string) = {
        state with
            FrontendIpConfiguration = ResourceName frontendIp
    }

    [<CustomOperation "frontend_port">]
    member _.FrontendPort(state: HttpListenerConfig, frontendPort: string) = {
        state with
            FrontendPort = ResourceName frontendPort
    }

    member _.FrontendPort(state: HttpListenerConfig, frontendPort: FrontendPortConfig) = {
        state with
            FrontendPort = frontendPort.Name
    }

    [<CustomOperation "protocol">]
    member _.Protocol(state: HttpListenerConfig, protocol) = { state with Protocol = protocol }

    [<CustomOperation "ssl_certificate">]
    member _.SslCertificate(state: HttpListenerConfig, sslCertificate: string) = {
        state with
            SslCertificate = Some(ResourceName sslCertificate)
    }

let httpListener = HttpListenerBuilder()

type BackendAddressConfig = {
    Fqdn: string
    IpAddress: System.Net.IPAddress
} with

    static member BuildResource backendAddress = {|
        Fqdn = backendAddress.Fqdn
        IpAddress = backendAddress.IpAddress
    |}

let backend_fqdn (fqdn: string) = BackendAddress.Fqdn fqdn

let backend_ip_address (ip: string) =
    BackendAddress.Ip(System.Net.IPAddress.Parse ip)

type BackendAddressPoolConfig = {
    Name: ResourceName
    BackendAddresses: BackendAddress list
}

type BackendAddressPoolBuilder() =
    member _.Yield _ = {
        Name = ResourceName.Empty
        BackendAddresses = []
    }

    [<CustomOperation "name">]
    member _.Name(state: BackendAddressPoolConfig, name) = { state with Name = ResourceName name }

    [<CustomOperation "add_backend_addresses">]
    member _.BackendAddresses(state: BackendAddressPoolConfig, backendAddresses) = {
        state with
            BackendAddresses = state.BackendAddresses @ backendAddresses
    }

let appGatewayBackendAddressPool = BackendAddressPoolBuilder()

type AppGatewayProbeConfig = {
    Name: ResourceName
    Host: string
    Port: uint16 option
    Path: string
    Protocol: Protocol
    IntervalInSeconds: int<Seconds>
    TimeoutInSeconds: int<Seconds>
    UnhealthyThreshold: uint16
    PickHostNameFromBackendHttpSettings: bool
    MinServers: uint16 option
} with

    static member BuildResource(probe: AppGatewayProbeConfig) = {|
        Name = probe.Name
        Host = probe.Host
        Port = probe.Port
        Path = probe.Path
        Protocol = probe.Protocol
        IntervalInSeconds = probe.IntervalInSeconds
        TimeoutInSeconds = probe.TimeoutInSeconds
        UnhealthyThreshold = probe.UnhealthyThreshold
        PickHostNameFromBackendHttpSettings = probe.PickHostNameFromBackendHttpSettings
        MinServers = probe.MinServers
        Match = None
    |}

type AppGatewayProbeBuilder() =
    member _.Yield _ = {
        Name = ResourceName.Empty
        Host = "localhost"
        Path = "/"
        Port = None
        Protocol = Protocol.Http
        IntervalInSeconds = 30<Seconds>
        TimeoutInSeconds = 10<Seconds>
        UnhealthyThreshold = 3us
        PickHostNameFromBackendHttpSettings = false
        MinServers = None
    }

    [<CustomOperation "name">]
    member _.Name(state: AppGatewayProbeConfig, name) = { state with Name = ResourceName name }

    [<CustomOperation "host">]
    member _.Host(state: AppGatewayProbeConfig, host) = { state with Host = host }

    [<CustomOperation "path">]
    member _.Path(state: AppGatewayProbeConfig, path) = { state with Path = path }

    [<CustomOperation "port">]
    member _.Port(state: AppGatewayProbeConfig, port: uint16) = { state with Port = Some port }

    member _.Port(state: AppGatewayProbeConfig, port: int) = { state with Port = Some(uint16 port) }

    [<CustomOperation "protocol">]
    member _.Protocol(state: AppGatewayProbeConfig, protocol) = { state with Protocol = protocol }

let appGatewayProbe = AppGatewayProbeBuilder()

type ConnectionDrainingConfig = {
    DrainTimeoutInSeconds: int<Seconds>
    Enabled: bool
} with

    static member BuildResource connDraining = {|
        DrainTimeoutInSeconds = connDraining.DrainTimeoutInSeconds
        Enabled = connDraining.Enabled
    |}

type ConnectionDrainingBuilder() =
    member _.Yield _ = {
        DrainTimeoutInSeconds = 0<Seconds>
        Enabled = false
    }

    [<CustomOperation "drain_timeout">]
    member _.DrainTimeoutInSeconds(state: ConnectionDrainingConfig, timeout) = {
        state with
            DrainTimeoutInSeconds = timeout
    }

    [<CustomOperation "enabled">]
    member _.Enabled(state: ConnectionDrainingConfig, enabled) = { state with Enabled = enabled }

let connectionDraining = ConnectionDrainingBuilder()

type BackendHttpSettingsConfig = {
    Name: ResourceName
    AffinityCookieName: string option
    AuthenticationCertificates: ResourceName list
    ConnectionDraining: ConnectionDrainingConfig option
    CookieBasedAffinity: FeatureFlag
    HostName: string option
    Path: string option
    Port: uint16
    Protocol: Protocol
    PickHostNameFromBackendAddress: bool
    RequestTimeoutInSeconds: int<Seconds>
    Probe: ResourceName option
    ProbeEnabled: bool
    TrustedRootCertificates: ResourceName list
} with

    static member BuildResource backendHttpSettings = {|
        Name = backendHttpSettings.Name
        AffinityCookieName = backendHttpSettings.AffinityCookieName
        AuthenticationCertificates = backendHttpSettings.AuthenticationCertificates
        ConnectionDraining =
            backendHttpSettings.ConnectionDraining
            |> Option.map ConnectionDrainingConfig.BuildResource
        CookieBasedAffinity = backendHttpSettings.CookieBasedAffinity
        HostName = backendHttpSettings.HostName
        Path = backendHttpSettings.Path
        Port = backendHttpSettings.Port
        Protocol = backendHttpSettings.Protocol
        PickHostNameFromBackendAddress = backendHttpSettings.PickHostNameFromBackendAddress
        RequestTimeoutInSeconds = backendHttpSettings.RequestTimeoutInSeconds
        Probe = backendHttpSettings.Probe
        ProbeEnabled = backendHttpSettings.ProbeEnabled
        TrustedRootCertificates = backendHttpSettings.TrustedRootCertificates
    |}

type BackendHttpSettingsBuilder() =
    member _.Yield _ = {
        Name = ResourceName.Empty
        AffinityCookieName = None
        AuthenticationCertificates = []
        ConnectionDraining = None
        CookieBasedAffinity = FeatureFlag.Disabled
        HostName = None
        Path = None
        Port = 80us
        Protocol = Protocol.Http
        PickHostNameFromBackendAddress = false
        RequestTimeoutInSeconds = 500<Seconds>
        Probe = None // ResourceName.Empty
        ProbeEnabled = false
        TrustedRootCertificates = []
    }

    [<CustomOperation "name">]
    member _.Name(state: BackendHttpSettingsConfig, name) = { state with Name = ResourceName name }

    [<CustomOperation "affinity_cookie_name">]
    member _.AffinityCookieName(state: BackendHttpSettingsConfig, name) = {
        state with
            AffinityCookieName = Some name
    }

    [<CustomOperation "add_auth_certs">]
    member _.AddAuthCerts(state: BackendHttpSettingsConfig, authCerts) = {
        state with
            AuthenticationCertificates =
                state.AuthenticationCertificates
                @ (authCerts |> List.map (fun authCert -> ResourceName authCert))
    }

    [<CustomOperation "connection_draining">]
    member _.ConnectionDraining(state: BackendHttpSettingsConfig, connDraining) = {
        state with
            ConnectionDraining = Some connDraining
    }

    [<CustomOperation "cookie_based_affinity">]
    member _.CookieBasedAffinity(state: BackendHttpSettingsConfig, cookieBasedAffinity) = {
        state with
            CookieBasedAffinity = cookieBasedAffinity
    }

    [<CustomOperation "host_name">]
    member _.HostName(state: BackendHttpSettingsConfig, name) = { state with HostName = Some name }

    [<CustomOperation "path">]
    member _.Path(state: BackendHttpSettingsConfig, path) = { state with Path = Some path }

    [<CustomOperation "port">]
    member _.Port(state: BackendHttpSettingsConfig, port) = { state with Port = port }

    member _.Port(state: BackendHttpSettingsConfig, port: int) = { state with Port = uint16 port }

    [<CustomOperation "protocol">]
    member _.Protocol(state: BackendHttpSettingsConfig, protocol) = { state with Protocol = protocol }

    [<CustomOperation "pick_host_name_from_backend_address">]
    member _.PickHostNameFromBackendAddress(state: BackendHttpSettingsConfig, pickHostNameFromBackendAddress) = {
        state with
            PickHostNameFromBackendAddress = pickHostNameFromBackendAddress
    }

    [<CustomOperation "request_timeout">]
    member _.RequestTimeoutInSeconds(state: BackendHttpSettingsConfig, reqTimeout) = {
        state with
            RequestTimeoutInSeconds = reqTimeout
    }

    [<CustomOperation "probe">]
    member _.Probe(state: BackendHttpSettingsConfig, probe: string) = {
        state with
            Probe = Some(ResourceName probe)
    }

    member _.Probe(state: BackendHttpSettingsConfig, probe: AppGatewayProbeConfig) = {
        state with
            Probe = Some probe.Name
    }

    [<CustomOperation "probe_enabled">]
    member _.ProbeEnabled(state: BackendHttpSettingsConfig, probeEnabled) = {
        state with
            ProbeEnabled = probeEnabled
    }

    [<CustomOperation "trusted_root_certs">]
    member _.TrustedRootCertificates(state: BackendHttpSettingsConfig, trustedRootCerts) = {
        state with
            TrustedRootCertificates =
                state.TrustedRootCertificates
                @ (trustedRootCerts |> List.map (fun rootCert -> ResourceName rootCert))
    }

let backendHttpSettings = BackendHttpSettingsBuilder()

type RequestRoutingRuleConfig = {
    Name: ResourceName
    RuleType: RuleType
    HttpListener: ResourceName
    BackendAddressPool: ResourceName
    BackendHttpSettings: ResourceName
    RedirectConfiguration: ResourceName option
    RewriteRuleSet: ResourceName option
    UrlPathMap: ResourceName option
    Priority: int option
} with

    static member BuildResource(rule: RequestRoutingRuleConfig) = {|
        Name = rule.Name
        RuleType = rule.RuleType
        HttpListener = rule.HttpListener
        BackendAddressPool = rule.BackendAddressPool
        BackendHttpSettings = rule.BackendHttpSettings
        RedirectConfiguration = rule.RedirectConfiguration
        RewriteRuleSet = rule.RewriteRuleSet
        UrlPathMap = rule.UrlPathMap
        Priority = rule.Priority
    |}

type BasicRequestRoutingRuleBuilder() =
    member _.Yield _ = {
        Name = ResourceName.Empty
        RuleType = RuleType.Basic
        HttpListener = ResourceName.Empty
        BackendAddressPool = ResourceName.Empty
        BackendHttpSettings = ResourceName.Empty
        RedirectConfiguration = None
        RewriteRuleSet = None
        UrlPathMap = None
        Priority = None
    }

    [<CustomOperation "name">]
    member _.Name(state: RequestRoutingRuleConfig, name) = { state with Name = ResourceName name }

    [<CustomOperation "http_listener">]
    member _.HttpListener(state: RequestRoutingRuleConfig, listener: string) = {
        state with
            HttpListener = ResourceName listener
    }

    member _.HttpListener(state: RequestRoutingRuleConfig, listener: HttpListenerConfig) = {
        state with
            HttpListener = listener.Name
    }

    [<CustomOperation "backend_address_pool">]
    member _.BackendAddressPool(state: RequestRoutingRuleConfig, backendAddressPool: string) = {
        state with
            BackendAddressPool = ResourceName backendAddressPool
    }

    member _.BackendAddressPool(state: RequestRoutingRuleConfig, backendAddressPool: BackendAddressPoolConfig) = {
        state with
            BackendAddressPool = backendAddressPool.Name
    }

    [<CustomOperation "backend_http_settings">]
    member _.BackendHttpSettings(state: RequestRoutingRuleConfig, httpSettings: string) = {
        state with
            BackendHttpSettings = ResourceName httpSettings
    }

    member _.BackendHttpSettings(state: RequestRoutingRuleConfig, httpSettings: BackendHttpSettingsConfig) = {
        state with
            BackendHttpSettings = httpSettings.Name
    }

    [<CustomOperation "priority">]
    member _.Priority(state: RequestRoutingRuleConfig, priority: int) = { state with Priority = Some priority }

let basicRequestRoutingRule = BasicRequestRoutingRuleBuilder()

type SslCertificateConfig = {
    Name: ResourceName
    KeyVaultSecretId: string option
} with

    static member BuildResource(conf: SslCertificateConfig) = {|
        Name = conf.Name
        Data = None // TODO: needs implementation after further testing.
        KeyVaultSecretId = conf.KeyVaultSecretId
        Password = None // TODO: needs implementation, will generate password parameter.
    |}

type SslCertificateBuilder() =
    member _.Yield _ = {
        Name = ResourceName.Empty
        KeyVaultSecretId = None
    }

    [<CustomOperation "name">]
    member _.Name(config: SslCertificateConfig, name: string) = { config with Name = ResourceName name }

    [<CustomOperation "key_vault_secret_id">]
    member _.KeyVaultSecretId(config: SslCertificateConfig, secretId: string) = {
        config with
            KeyVaultSecretId = Some secretId
    }

let sslCertificate = SslCertificateBuilder()

type AppGatewayConfig = {
    Name: ResourceName
    Sku: ApplicationGatewaySku
    Identity: Identity.ManagedIdentity
    GatewayIpConfigs: GatewayIpConfig list
    FrontendIpConfigs: FrontendIpConfig list
    FrontendPorts: FrontendPortConfig list
    BackendAddressPools: BackendAddressPoolConfig list
    BackendHttpSettingsCollection: BackendHttpSettingsConfig list
    HttpListeners: HttpListenerConfig list
    Probes: AppGatewayProbeConfig list
    RequestRoutingRules: RequestRoutingRuleConfig list
    SslCertificates: SslCertificateConfig list
    Dependencies: Set<ResourceId>
    Tags: Map<string, string>
} with

    interface IBuilder with
        member this.ResourceId = applicationGateways.resourceId this.Name

        member this.BuildResources location =
            let frontendPublicIps =
                this.FrontendIpConfigs
                |> List.map (fun frontend -> FrontendIpConfig.BuildIp frontend location)
                |> List.choose id

            {
                Name = this.Name
                Location = location
                Sku = this.Sku
                Identity = this.Identity
                GatewayIPConfigurations = this.GatewayIpConfigs |> List.map GatewayIpConfig.BuildResource
                FrontendIpConfigs = this.FrontendIpConfigs |> List.map FrontendIpConfig.BuildResource
                FrontendPorts = this.FrontendPorts |> List.map FrontendPortConfig.BuildResource
                BackendAddressPools =
                    this.BackendAddressPools
                    |> List.map (fun p -> {|
                        Name = p.Name
                        Addresses = p.BackendAddresses
                    |})
                BackendHttpSettingsCollection =
                    this.BackendHttpSettingsCollection
                    |> List.map BackendHttpSettingsConfig.BuildResource
                HttpListeners = this.HttpListeners |> List.map HttpListenerConfig.BuildResource
                Probes = this.Probes |> List.map AppGatewayProbeConfig.BuildResource
                RequestRoutingRules = this.RequestRoutingRules |> List.map RequestRoutingRuleConfig.BuildResource
                SslCertificates = this.SslCertificates |> List.map SslCertificateConfig.BuildResource

                Dependencies =
                    frontendPublicIps
                    |> List.map (fun pip -> publicIPAddresses.resourceId pip.Name)
                    |> Set.ofList
                    |> Set.union this.Dependencies
                Tags = this.Tags

                // TODO Implement properties below
                AuthenticationCertificates = []
                AutoscaleConfiguration = None
                CustomErrorConfigurations = []
                EnableFips = None
                EnableHttp2 = None
                FirewallPolicy = None
                ForceFirewallPolicyAssociation = false
                RedirectConfigurations = []
                RewriteRuleSets = []
                SslPolicy = None
                SslProfiles = []
                TrustedClientCertificates = []
                TrustedRootCertificates = []
                UrlPathMaps = []
                WebApplicationFirewallConfiguration = None
                Zones = []

            }
            :> IArmResource
            :: (frontendPublicIps |> Seq.cast<IArmResource> |> List.ofSeq)


type AppGatewayBuilder() =
    member _.Yield _ : AppGatewayConfig = {
        Name = ResourceName.Empty
        Sku = {
            Name = Sku.Standard_v2
            Capacity = None
            Tier = Tier.Standard_v2
        }
        Identity = Identity.ManagedIdentity.Empty
        GatewayIpConfigs = []
        FrontendIpConfigs = []
        FrontendPorts = []
        BackendAddressPools = []
        BackendHttpSettingsCollection = []
        HttpListeners = []
        Probes = []
        RequestRoutingRules = []
        SslCertificates = []
        Dependencies = Set.empty
        Tags = Map.empty
    }

    [<CustomOperation "name">]
    member _.Name(state: AppGatewayConfig, name) = { state with Name = ResourceName name }

    [<CustomOperation "sku">]
    member _.Sku(state: AppGatewayConfig, skuName) = {
        state with
            Sku = { state.Sku with Name = skuName }
    }

    [<CustomOperation "tier">]
    member _.Tier(state: AppGatewayConfig, skuTier) = {
        state with
            Sku = { state.Sku with Tier = skuTier }
    }

    [<CustomOperation "sku_capacity">]
    member _.SkuCapacity(state: AppGatewayConfig, skuCapacity) = {
        state with
            Sku = {
                state.Sku with
                    Capacity = Some skuCapacity
            }
    }
    // Sets the managed identity on this Application Gateway.
    interface IIdentity<AppGatewayConfig> with
        member _.Add state updater = {
            state with
                Identity = updater state.Identity
        }
    // Support for adding tags to this Application Gateway.
    interface ITaggable<AppGatewayConfig> with
        member _.Add state tags = {
            state with
                Tags = state.Tags |> Map.merge tags
        }
    // Support for adding dependencies to this Application Gateway.
    interface IDependable<AppGatewayConfig> with
        member _.Add state newDeps = {
            state with
                Dependencies = state.Dependencies + newDeps
        }

    [<CustomOperation "add_ip_configs">]
    member _.AddIpConfigs(state: AppGatewayConfig, ipConfigs) = {
        state with
            GatewayIpConfigs = state.GatewayIpConfigs @ ipConfigs
    }

    [<CustomOperation "add_frontends">]
    member _.AddFrontends(state: AppGatewayConfig, frontends) = {
        state with
            FrontendIpConfigs = state.FrontendIpConfigs @ frontends
    }

    [<CustomOperation "add_frontend_ports">]
    member _.AddFrontendPorts(state: AppGatewayConfig, frontendPorts) = {
        state with
            FrontendPorts = state.FrontendPorts @ frontendPorts
    }

    [<CustomOperation "add_backend_address_pools">]
    member _.AddBackendAddressPools(state: AppGatewayConfig, backendAddressPools) = {
        state with
            BackendAddressPools = state.BackendAddressPools @ backendAddressPools
    }

    [<CustomOperation "add_backend_http_settings_collection">]
    member _.AddBackendHttpSettingsCollection
        (state: AppGatewayConfig, backendHttpSettings: BackendHttpSettingsConfig list)
        =
        {
            state with
                BackendHttpSettingsCollection = state.BackendHttpSettingsCollection @ backendHttpSettings
        }

    [<CustomOperation "add_http_listeners">]
    member _.AddHttpListeners(state: AppGatewayConfig, httpListeners: HttpListenerConfig list) = {
        state with
            HttpListeners = state.HttpListeners @ httpListeners
    }

    [<CustomOperation "add_probes">]
    member _.AddProbes(state: AppGatewayConfig, probes) = {
        state with
            Probes = state.Probes @ probes
    }

    [<CustomOperation "add_request_routing_rules">]
    member _.AddRequestRoutingRules(state: AppGatewayConfig, reqRoutingRules: RequestRoutingRuleConfig list) = {
        state with
            RequestRoutingRules = state.RequestRoutingRules @ reqRoutingRules
    }

    [<CustomOperation "add_ssl_certificates">]
    member _.AddSslCertificates(state: AppGatewayConfig, sslCertificates: SslCertificateConfig list) = {
        state with
            SslCertificates = state.SslCertificates @ sslCertificates
    }

let appGateway = AppGatewayBuilder()