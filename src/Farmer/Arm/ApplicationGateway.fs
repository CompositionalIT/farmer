module Farmer.Arm.ApplicationGateway

open Farmer
open Farmer.ApplicationGateway
open Farmer.Identity

let applicationGateways =
    ResourceType("Microsoft.Network/applicationGateways", "2020-11-01")

let applicationGatewayAuthenticationCertificates =
    ResourceType("Microsoft.Network/applicationGateways/authenticationCertificates", "2020-11-01")

let applicationGatewayBackendHttpSettingsCollection =
    ResourceType("Microsoft.Network/applicationGateways/backendHttpSettingsCollection", "2020-11-01")

let applicationGatewayBackendAddressPools =
    ResourceType("Microsoft.Network/applicationGateways/backendAddressPools", "2020-11-01")

let applicationGatewayFrontendIPConfigurations =
    ResourceType("Microsoft.Network/applicationGateways/frontendIPConfigurations", "2020-11-01")

let applicationGatewayFrontendPorts =
    ResourceType("Microsoft.Network/applicationGateways/frontendPorts", "2020-11-01")

let applicationGatewayHttpListeners =
    ResourceType("Microsoft.Network/applicationGateways/httpListeners", "2020-11-01")

let applicationGatewayPathRules =
    ResourceType("Microsoft.Network/applicationGateways/pathRule", "2020-11-01")

let ApplicationGatewayProbes =
    ResourceType("Microsoft.Network/applicationGateways/probes", "2020-11-01")

let applicationGatewayRedirectConfigurations =
    ResourceType("Microsoft.Network/applicationGateways/redirectConfigurations", "2020-11-01")

let applicationGatewayRequestRoutingRules =
    ResourceType("Microsoft.Network/applicationGateways/requestRoutingRules", "2020-11-01")

let applicationGatewayRewriteRuleSets =
    ResourceType("Microsoft.Network/applicationGateways/rewriteRuleSets", "2020-11-01")

let applicationGatewaySslCertificates =
    ResourceType("Microsoft.Network/applicationGateways/sslCertificates", "2020-11-01")

let applicationGatewaySslProfiles =
    ResourceType("Microsoft.Network/applicationGateways/sslProfiles", "2020-11-01")

let applicationGatewayTrustedRootCertificates =
    ResourceType("Microsoft.Network/applicationGateways/trustedRootCertificates", "2020-11-01")

let applicationGatewayUrlPathMaps =
    ResourceType("Microsoft.Network/applicationGateways/urlPathMap", "2020-11-01")

type ApplicationGateway = {
    Name: ResourceName
    Location: Location
    Sku: ApplicationGatewaySku
    Identity: ManagedIdentity
    AuthenticationCertificates: {| Name: ResourceName; Data: string |} list
    AutoscaleConfiguration:
        {|
            MaxCapacity: int option
            MinCapacity: int
        |} option
    FrontendPorts: {| Name: ResourceName; Port: uint16 |} list
    FrontendIpConfigs:
        {|
            Name: ResourceName
            PrivateIpAllocationMethod: PrivateIpAddress.AllocationMethod
            PublicIp: ResourceId option
        |} list
    BackendAddressPools:
        {|
            Name: ResourceName
            Addresses: BackendAddress list
        |} list
    BackendHttpSettingsCollection:
        {|
            Name: ResourceName
            AffinityCookieName: string option
            AuthenticationCertificates: ResourceName list
            ConnectionDraining:
                {|
                    DrainTimeoutInSeconds: int<Seconds>
                    Enabled: bool
                |} option
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
        |} list
    CustomErrorConfigurations:
        {|
            CustomErrorPageUrl: string
            StatusCode: HttpStatusCode
        |} list
    EnableFips: bool option
    EnableHttp2: bool option
    FirewallPolicy: ResourceId option
    ForceFirewallPolicyAssociation: bool
    GatewayIPConfigurations:
        {|
            Name: ResourceName
            Subnet: ResourceId option
        |} list
    HttpListeners:
        {|
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
        |} list
    Probes:
        {|
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
            Match:
                {|
                    Body: string option
                    StatusCodes: uint16 list
                |} option
        |} list
    RedirectConfigurations:
        {|
            Name: ResourceName
            IncludePath: bool
            IncludeQueryString: bool
            PathRules: ResourceName list
            RedirectType: RedirectType
            RequestRoutingRules: ResourceName list
            TargetListener: ResourceName
            TargetUrl: string
            UrlPathMaps: ResourceName list
        |} list
    RequestRoutingRules:
        {|
            Name: ResourceName
            RuleType: RuleType
            HttpListener: ResourceName
            BackendAddressPool: ResourceName
            BackendHttpSettings: ResourceName
            RedirectConfiguration: ResourceName option
            RewriteRuleSet: ResourceName option
            UrlPathMap: ResourceName option
            Priority: int option
        |} list
    RewriteRuleSets:
        {|
            Name: ResourceName
            RewriteRules:
                {|
                    ActionSet:
                        {|
                            RequestHeaderConfigurations:
                                {|
                                    HeaderName: string
                                    HeaderValue: string
                                |} list
                            ResponseHeaderConfigurations:
                                {|
                                    HeaderName: string
                                    HeaderValue: string
                                |} list
                            UrlConfiguration:
                                {|
                                    ModifiedPath: string
                                    ModifiedQueryString: string
                                    Reroute: bool
                                |}
                        |}
                    Conditions:
                        {|
                            IgnoreCase: bool
                            Negate: bool
                            Pattern: string
                            Variable: string
                        |} list
                    Name: string
                    RuleSequence: int
                |} list
        |} list
    SslCertificates:
        {|
            Name: ResourceName
            Data: string option
            KeyVaultSecretId: string
            Password: string option
        |} list
    SslPolicy:
        {|
            CipherSuites: CipherSuite list
            DisabledSslProtocols: SslProtocol list
            MinProtocolVersion: SslProtocol
            PolicyName: PolicyName
            PolicyType: PolicyType
        |} option
    SslProfiles:
        {|
            Name: ResourceName
            ClientAuthConfiguration: {| VerifyClientCertIssuerDN: bool |}
            SslPolicy:
                {|
                    CipherSuites: CipherSuite list
                    DisabledSslProtocols: SslProtocol list
                    MinProtocolVersion: SslProtocol
                    PolicyName: PolicyName
                    PolicyType: PolicyType
                |} option
            TrustedClientCertificates: ResourceName list
        |} list
    TrustedClientCertificates: {| Name: ResourceName; Data: string |} list
    TrustedRootCertificates:
        {|
            Name: ResourceName
            Data: string option
            KeyVaultSecretId: string
        |} list
    UrlPathMaps:
        {|
            Name: ResourceName
            DefaultBackendAddressPool: ResourceName
            DefaultBackendHttpSettings: ResourceName
            DefaultRedirectConfiguration: ResourceName
            DefaultRewriteRuleSet: ResourceName
            PathRules:
                {|
                    Name: ResourceName
                    BackendAddressPool: ResourceName
                    BackendHttpSettings: ResourceName
                    FirewallPolicy: ResourceId
                    Paths: string list
                    RedirectConfiguration: ResourceName
                    RewriteRuleSet: ResourceName
                |} list
        |} list
    WebApplicationFirewallConfiguration:
        {|
            DisabledRuleGroups:
                {|
                    RuleGroupName: string
                    Rules: int list
                |} list
            Enabled: bool
            Exclusions:
                {|
                    MatchVariable: string
                    Selector: string
                    SelectorMatchOperator: string
                |} list
            FileUploadLimitInMb: int<Mb> option
            FirewallMode: FirewallMode option
            MaxRequestBodySize: int option
            MaxRequestBodySizeInKb: int<Kb> option
            RequestBodyCheck: bool option
            RuleSetType: RuleSetType
            RuleSetVersion: string
        |} option
    Zones: uint16 list
    Dependencies: Set<ResourceId>
    Tags: Map<string, string>
} with

    member private this.dependencies =
        [
            this.FrontendIpConfigs
            |> Seq.map (fun ipconfig -> ipconfig.PublicIp)
            |> Seq.choose id
            |> Set.ofSeq
            this.Identity.Dependencies |> Set.ofList
            this.Dependencies
        ]
        |> Set.unionMany

    interface IArmResource with
        member this.ResourceId = applicationGateways.resourceId this.Name

        member this.JsonModel = {|
            applicationGateways.Create(this.Name, this.Location, this.dependencies, this.Tags) with
                identity = this.Identity.ToArmJson
                properties = {|
                    sku = {|
                        name = this.Sku.Name.ArmValue
                        capacity = this.Sku.Capacity |> Option.toNullable
                        tier = this.Sku.Tier.ArmValue
                    |}
                    autoscaleConfiguration =
                        this.AutoscaleConfiguration
                        |> Option.map (fun a -> {|
                            maxCapacity = a.MaxCapacity
                            minCapacity = a.MinCapacity
                        |})
                        |> Option.defaultValue Unchecked.defaultof<_>
                    backendAddressPools =
                        this.BackendAddressPools
                        |> List.map (fun backend -> {|
                            name = backend.Name.Value
                            properties = {|
                                backendAddresses =
                                    backend.Addresses
                                    |> List.map (function
                                        | BackendAddress.Ip ip -> {| fqdn = null; ipAddress = string ip |}
                                        | BackendAddress.Fqdn fqdn -> {| fqdn = fqdn; ipAddress = null |})
                            |}
                        |})
                    backendHttpSettingsCollection =
                        this.BackendHttpSettingsCollection
                        |> List.map (fun settings -> {|
                            name = settings.Name.Value
                            properties = {|
                                affinityCookieName = settings.AffinityCookieName |> Option.toObj
                                authenticationCertificates =
                                    settings.AuthenticationCertificates
                                    |> List.map (
                                        tuple this.Name
                                        >> applicationGatewayAuthenticationCertificates.resourceId
                                        >> ResourceId.AsIdObject
                                    )
                                connectionDraining =
                                    settings.ConnectionDraining
                                    |> Option.map (fun drain -> {|
                                        drainTimeoutInSec = drain.DrainTimeoutInSeconds
                                        enabled = drain.Enabled
                                    |})
                                cookieBasedAffinity = settings.CookieBasedAffinity.ArmValue
                                hostName = settings.HostName |> Option.toObj
                                path = settings.Path |> Option.toObj
                                pickHostNameFromBackendAddress = settings.PickHostNameFromBackendAddress
                                port = settings.Port
                                probe =
                                    settings.Probe
                                    |> Option.map (
                                        tuple this.Name >> ApplicationGatewayProbes.resourceId >> ResourceId.AsIdObject
                                    )
                                    |> Option.defaultValue Unchecked.defaultof<_>
                                probeEnabled = settings.ProbeEnabled
                                protocol = settings.Protocol.ArmValue
                                requestTimeout = settings.RequestTimeoutInSeconds
                                trustedRootCertificates =
                                    settings.TrustedRootCertificates
                                    |> List.map (
                                        tuple this.Name
                                        >> applicationGatewayTrustedRootCertificates.resourceId
                                        >> ResourceId.AsIdObject
                                    )
                            |}
                        |})
                    customErrorConfigurations =
                        this.CustomErrorConfigurations
                        |> List.map (fun conf -> {|
                            customErrorPageUrl = conf.CustomErrorPageUrl
                            statusCode = conf.StatusCode.ArmValue
                        |})
                    enableFips = this.EnableFips |> Option.toNullable
                    enableHttp2 = this.EnableHttp2 |> Option.toNullable
                    firewallPolicy =
                        this.FirewallPolicy
                        |> Option.map ResourceId.AsIdObject
                        |> Option.defaultValue Unchecked.defaultof<_>
                    frontendPorts =
                        this.FrontendPorts
                        |> List.map (fun frontend -> {|
                            name = frontend.Name.Value
                            properties = {| port = frontend.Port |}
                        |})
                    gatewayIPConfigurations =
                        this.GatewayIPConfigurations
                        |> List.map (fun gwip -> {|
                            name = gwip.Name.Value
                            properties = {|
                                subnet =
                                    gwip.Subnet
                                    |> Option.map ResourceId.AsIdObject
                                    |> Option.defaultValue Unchecked.defaultof<_>
                            |}
                        |})
                    httpListeners =
                        this.HttpListeners
                        |> List.map (fun listener -> {|
                            name = listener.Name.Value
                            properties = {|
                                customErrorConfigurations =
                                    listener.CustomErrorConfigurations
                                    |> List.map (fun cfg -> {|
                                        customErrorPageUrl = cfg.CustomErrorPageUrl
                                        statusCode = cfg.StatusCode.ArmValue
                                    |})
                                firewallPolicy =
                                    listener.FirewallPolicy
                                    |> Option.map ResourceId.AsIdObject
                                    |> Option.defaultValue Unchecked.defaultof<_>
                                frontendIPConfiguration =
                                    (this.Name, listener.FrontendIpConfiguration)
                                    |> applicationGatewayFrontendIPConfigurations.resourceId
                                    |> ResourceId.AsIdObject
                                frontendPort =
                                    (this.Name, listener.FrontendPort)
                                    |> applicationGatewayFrontendPorts.resourceId
                                    |> ResourceId.AsIdObject
                                hostNames = listener.HostNames
                                protocol = listener.Protocol.ArmValue
                                requireServerNameIndication = listener.RequireServerNameIndication
                                sslCertificate =
                                    listener.SslCertificate
                                    |> Option.map (
                                        tuple this.Name
                                        >> applicationGatewaySslCertificates.resourceId
                                        >> ResourceId.AsIdObject
                                    )
                                    |> Option.defaultValue Unchecked.defaultof<_>
                                sslProfile =
                                    listener.SslProfile
                                    |> Option.map (
                                        tuple this.Name
                                        >> applicationGatewaySslProfiles.resourceId
                                        >> ResourceId.AsIdObject
                                    )
                                    |> Option.defaultValue Unchecked.defaultof<_>
                            |}
                        |})
                    frontendIPConfigurations =
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
                                |}
                            |})
                    probes =
                        this.Probes
                        |> List.map (fun probe -> {|
                            name = probe.Name.Value
                            properties = {|
                                host = probe.Host
                                port = probe.Port |> Option.toNullable
                                path = probe.Path
                                protocol = probe.Protocol.ArmValue
                                pickHostNameFromBackendHttpSettings = probe.PickHostNameFromBackendHttpSettings
                                ``match`` =
                                    probe.Match
                                    |> Option.map (fun m -> {|
                                        body = m.Body |> Option.toObj
                                        statusCodes = m.StatusCodes |> List.map string
                                    |})
                                minServers = probe.MinServers |> Option.toNullable
                                interval = probe.IntervalInSeconds
                                timeout = probe.TimeoutInSeconds
                                unhealthyThreshold = probe.UnhealthyThreshold
                            |}
                        |})
                    redirectConfigurations =
                        this.RedirectConfigurations
                        |> List.map (fun cfg -> {|
                            name = cfg.Name.Value
                            properties = {|
                                includePath = cfg.IncludePath
                                includeQueryString = cfg.IncludeQueryString
                                pathRules =
                                    cfg.PathRules
                                    |> List.map (
                                        tuple this.Name
                                        >> applicationGatewayPathRules.resourceId
                                        >> ResourceId.AsIdObject
                                    )
                                redirectType = cfg.RedirectType.ArmValue
                                requestRoutingRules =
                                    cfg.RequestRoutingRules
                                    |> List.map (
                                        tuple this.Name
                                        >> applicationGatewayRequestRoutingRules.resourceId
                                        >> ResourceId.AsIdObject
                                    )
                                targetListener =
                                    applicationGatewayHttpListeners.resourceId (this.Name, cfg.TargetListener)
                                    |> ResourceId.AsIdObject
                                targetUrl = cfg.TargetUrl
                                urlPathMaps =
                                    cfg.UrlPathMaps
                                    |> List.map (
                                        tuple this.Name
                                        >> applicationGatewayUrlPathMaps.resourceId
                                        >> ResourceId.AsIdObject
                                    )
                            |}
                        |})
                    requestRoutingRules =
                        this.RequestRoutingRules
                        |> List.map (fun routingRule -> {|
                            name = routingRule.Name.Value
                            properties = {|
                                backendAddressPool =
                                    applicationGatewayBackendAddressPools.resourceId (
                                        this.Name,
                                        routingRule.BackendAddressPool
                                    )
                                    |> ResourceId.AsIdObject
                                backendHttpSettings =
                                    applicationGatewayBackendHttpSettingsCollection.resourceId (
                                        this.Name,
                                        routingRule.BackendHttpSettings
                                    )
                                    |> ResourceId.AsIdObject
                                httpListener =
                                    applicationGatewayHttpListeners.resourceId (this.Name, routingRule.HttpListener)
                                    |> ResourceId.AsIdObject
                                priority = routingRule.Priority |> Option.toNullable
                                redirectConfiguration =
                                    routingRule.RedirectConfiguration
                                    |> Option.map (
                                        tuple this.Name
                                        >> applicationGatewayRedirectConfigurations.resourceId
                                        >> ResourceId.AsIdObject
                                    )
                                    |> Option.defaultValue Unchecked.defaultof<_>
                                rewriteRuleSet =
                                    routingRule.RewriteRuleSet
                                    |> Option.map (
                                        tuple this.Name
                                        >> applicationGatewayRewriteRuleSets.resourceId
                                        >> ResourceId.AsIdObject
                                    )
                                    |> Option.defaultValue Unchecked.defaultof<_>
                                ruleType = routingRule.RuleType.ArmValue
                                urlPathMap =
                                    routingRule.UrlPathMap
                                    |> Option.map (
                                        tuple this.Name
                                        >> applicationGatewayUrlPathMaps.resourceId
                                        >> ResourceId.AsIdObject
                                    )
                                    |> Option.defaultValue Unchecked.defaultof<_>
                            |}
                        |})
                    rewriteRuleSets =
                        this.RewriteRuleSets
                        |> List.map (fun ruleSet -> {|
                            name = ruleSet.Name.Value
                            properties = {|
                                rewriteRules =
                                    ruleSet.RewriteRules
                                    |> List.map (fun rule -> {|
                                        actionSet = {|
                                            requestHeaderConfigurations =
                                                rule.ActionSet.RequestHeaderConfigurations
                                                |> List.map (fun cfg -> {|
                                                    headerName = cfg.HeaderName
                                                    headerValue = cfg.HeaderValue
                                                |})
                                            responseHeaderConfigurations =
                                                rule.ActionSet.ResponseHeaderConfigurations
                                                |> List.map (fun cfg -> {|
                                                    headerName = cfg.HeaderName
                                                    headerValue = cfg.HeaderValue
                                                |})
                                            urlConfiguration = {|
                                                modifiedPath = rule.ActionSet.UrlConfiguration.ModifiedPath
                                                modifiedQueryString =
                                                    rule.ActionSet.UrlConfiguration.ModifiedQueryString
                                                reroute = rule.ActionSet.UrlConfiguration.Reroute
                                            |}
                                        |}
                                        conditions =
                                            rule.Conditions
                                            |> List.map (fun c -> {|
                                                ignoreCase = c.IgnoreCase
                                                negate = c.Negate
                                                pattern = c.Pattern
                                                variable = c.Variable
                                            |})
                                        name = rule.Name
                                        ruleSequence = rule.RuleSequence
                                    |})
                            |}
                        |})
                    sslCertificates =
                        this.SslCertificates
                        |> List.map (fun cert -> {|
                            name = cert.Name.Value
                            properties = {|
                                data = cert.Data |> Option.toObj
                                keyVaultSecretId = cert.KeyVaultSecretId
                                password = cert.Password |> Option.toObj
                            |}
                        |})
                    sslPolicy =
                        this.SslPolicy
                        |> Option.map (fun sslPolicy -> {|
                            cipherSuites = sslPolicy.CipherSuites |> List.map CipherSuite.toString
                            disabledSslProtocols = sslPolicy.DisabledSslProtocols |> List.map SslProtocol.toString
                            minProtocolVersion = sslPolicy.MinProtocolVersion.ArmValue
                            policyName = sslPolicy.PolicyName.ArmValue
                            policyType = sslPolicy.PolicyType.ArmValue
                        |})
                        |> Option.defaultValue Unchecked.defaultof<_>
                    sslProfiles =
                        this.SslProfiles
                        |> List.map (fun sslProfile -> {|
                            name = sslProfile.Name.Value
                            properties = {|
                                clientAuthConfiguration = {|
                                    verifyClientCertIssuerDN =
                                        sslProfile.ClientAuthConfiguration.VerifyClientCertIssuerDN
                                |}
                                sslPolicy =
                                    sslProfile.SslPolicy
                                    |> Option.map (fun sslPolicy -> {|
                                        cipherSuites = sslPolicy.CipherSuites |> List.map CipherSuite.toString
                                        disabledSslProtocols =
                                            sslPolicy.DisabledSslProtocols |> List.map SslProtocol.toString
                                        minProtocolVersion = sslPolicy.MinProtocolVersion.ArmValue
                                        policyName = sslPolicy.PolicyName.ArmValue
                                        policyType = sslPolicy.PolicyType.ArmValue
                                    |})
                                    |> Option.defaultValue Unchecked.defaultof<_>
                                trustedClientCertificates =
                                    sslProfile.TrustedClientCertificates
                                    |> List.map (
                                        tuple this.Name
                                        >> applicationGatewayTrustedRootCertificates.resourceId
                                        >> ResourceId.AsIdObject
                                    )
                            |}
                        |})
                    trustedClientCertificates =
                        this.TrustedClientCertificates
                        |> List.map (fun cert -> {|
                            name = cert.Name.Value
                            properties = {| data = cert.Data |}
                        |})
                    trustedRootCertificates =
                        this.TrustedRootCertificates
                        |> List.map (fun cert -> {|
                            name = cert.Name.Value
                            properties = {|
                                data = cert.Data |> Option.toObj
                                keyVaultSecretId = cert.KeyVaultSecretId
                            |}
                        |})
                    urlPathMaps =
                        this.UrlPathMaps
                        |> List.map (fun pathMap -> {|
                            name = pathMap.Name.Value
                            properties = {|
                                defaultBackendAddressPool =
                                    applicationGatewayBackendAddressPools.resourceId (
                                        this.Name,
                                        pathMap.DefaultBackendAddressPool
                                    )
                                    |> ResourceId.AsIdObject
                                defaultBackendHttpSettings =
                                    applicationGatewayBackendHttpSettingsCollection.resourceId (
                                        this.Name,
                                        pathMap.DefaultBackendHttpSettings
                                    )
                                    |> ResourceId.AsIdObject
                                defaultRedirectConfiguration =
                                    applicationGatewayRedirectConfigurations.resourceId (
                                        this.Name,
                                        pathMap.DefaultRedirectConfiguration
                                    )
                                    |> ResourceId.AsIdObject
                                defaultRewriteRuleSet =
                                    applicationGatewayRewriteRuleSets.resourceId (
                                        this.Name,
                                        pathMap.DefaultRewriteRuleSet
                                    )
                                    |> ResourceId.AsIdObject
                                pathRules =
                                    pathMap.PathRules
                                    |> List.map (fun pathRule -> {|
                                        name = pathRule.Name.Value
                                        properties = {|
                                            backendAddressPool =
                                                applicationGatewayBackendAddressPools.resourceId (
                                                    this.Name,
                                                    pathRule.BackendAddressPool
                                                )
                                                |> ResourceId.AsIdObject
                                            backendHttpSettings =
                                                applicationGatewayBackendHttpSettingsCollection.resourceId (
                                                    this.Name,
                                                    pathRule.BackendHttpSettings
                                                )
                                                |> ResourceId.AsIdObject
                                            firewallPolicy = pathRule.FirewallPolicy |> ResourceId.AsIdObject
                                            redirectConfiguration =
                                                applicationGatewayRedirectConfigurations.resourceId (
                                                    this.Name,
                                                    pathRule.RedirectConfiguration
                                                )
                                                |> ResourceId.AsIdObject
                                            rewriteRuleSet =
                                                applicationGatewayRewriteRuleSets.resourceId (
                                                    this.Name,
                                                    pathRule.RewriteRuleSet
                                                )
                                                |> ResourceId.AsIdObject
                                            paths = pathRule.Paths
                                        |}
                                    |})
                            |}
                        |})
                    webApplicationFirewallConfiguration =
                        this.WebApplicationFirewallConfiguration
                        |> Option.map (fun cfg -> {|
                            disabledRuleGroups =
                                cfg.DisabledRuleGroups
                                |> List.map (fun ruleGroup -> {|
                                    ruleGroupName = ruleGroup.RuleGroupName
                                    rules = ruleGroup.Rules
                                |})
                            enabled = cfg.Enabled
                            exclusions =
                                cfg.Exclusions
                                |> List.map (fun e -> {|
                                    matchVariable = e.MatchVariable
                                    selector = e.Selector
                                    selectorMatchOperator = e.SelectorMatchOperator
                                |})
                            fileUploadLimitInMb = cfg.FileUploadLimitInMb
                            firewallMode = cfg.FirewallMode |> Option.map FirewallMode.toString
                            maxRequestBodySize = cfg.MaxRequestBodySize |> Option.defaultValue Unchecked.defaultof<_>
                            maxRequestBodySizeInKb =
                                cfg.MaxRequestBodySizeInKb |> Option.defaultValue Unchecked.defaultof<_>
                            requestBodyCheck = cfg.RequestBodyCheck |> Option.defaultValue Unchecked.defaultof<_>
                            ruleSetType = cfg.RuleSetType.ArmValue
                            ruleSetVersion = cfg.RuleSetVersion
                        |})
                        |> Option.defaultValue Unchecked.defaultof<_>
                |}
                zones = this.Zones |> List.map string
        |}
