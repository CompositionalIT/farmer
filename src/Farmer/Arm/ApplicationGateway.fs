module Farmer.Arm.ApplicationGateway

open Farmer
open Farmer.ApplicationGateway
open Farmer.Identity

let ApplicationGateways = ResourceType ("Microsoft.Network/applicationGateways", "2020-11-01")
let ApplicationGatewayAuthenticationCertificates = ResourceType ("Microsoft.Network/applicationGateways/authenticationCertificates", "2020-11-01")
let ApplicationGatewayBackendHttpSettingsCollection = ResourceType ("Microsoft.Network/applicationGateways/backendHttpSettingsCollection", "2020-11-01")
let ApplicationGatewayBackendAddressPools = ResourceType ("Microsoft.Network/applicationGateways/backendAddressPools", "2020-11-01")
let ApplicationGatewayFrontendIPConfigurations = ResourceType ("Microsoft.Network/applicationGateways/frontendIPConfigurations", "2020-11-01")
let ApplicationGatewayFrontendPorts = ResourceType ("Microsoft.Network/applicationGateways/frontendPorts", "2020-11-01")
let ApplicationGatewayHttpListeners = ResourceType ("Microsoft.Network/applicationGateways/httpListeners", "2020-11-01")
let ApplicationGatewayPathRules = ResourceType ("Microsoft.Network/applicationGateways/pathRule", "2020-11-01")
let ApplicationGatewayProbes = ResourceType ("Microsoft.Network/applicationGateways/probes", "2020-11-01")
let ApplicationGatewayRedirectConfigurations = ResourceType ("Microsoft.Network/applicationGateways/redirectConfigurations", "2020-11-01")
let ApplicationGatewayRequestRoutingRules = ResourceType ("Microsoft.Network/applicationGateways/requestRoutingRules", "2020-11-01")
let ApplicationGatewayRewriteRuleSets = ResourceType ("Microsoft.Network/applicationGateways/rewriteRuleSets", "2020-11-01")
let ApplicationGatewaySslCertificates = ResourceType ("Microsoft.Network/applicationGateways/sslCertificates", "2020-11-01")
let ApplicationGatewaySslProfiles = ResourceType ("Microsoft.Network/applicationGateways/sslProfiles", "2020-11-01")
let ApplicationGatewayTrustedRootCertificates = ResourceType ("Microsoft.Network/applicationGateways/trustedRootCertificates", "2020-11-01")
let ApplicationGatewayUrlPathMaps = ResourceType ("Microsoft.Network/applicationGateways/urlPathMap", "2020-11-01")


module ResourceId =
    let asId (resourceId: ResourceId) =
        {| id = resourceId.Eval() |}

type ApplicationGateway =
    { Name : ResourceName
      Location : Location
      Sku : ApplicationGatewaySku
      Identity: ManagedIdentity
      AuthenticationCertificates:
        {| Name: ResourceName
           Data: string |} list
      AutoscaleConfiguration:
        {| MaxCapacity: int option
           MinCapacity: int |} option
      FrontendPorts : 
        {| Name : ResourceName
           Port : uint16 |} list
      FrontendIpConfigs :
        {| Name : ResourceName
           PrivateIpAllocationMethod : PrivateIpAddress.AllocationMethod
           PublicIp : ResourceId option |} list
      BackendAddressPools : ResourceName list
      BackendHttpSettingsCollection : 
        {| Name: ResourceName
           AffinityCookieName: string option
           AuthenticationCertificates: ResourceName list
           ConnectionDraining:
            {| DrainTimeoutInSeconds: int<Seconds>
               Enabled: bool |} option
           CookieBasedAffinity: FeatureFlag
           HostName: string option
           Path: string option
           Port: uint16
           Protocol: Protocol
           CookieBasedAffinity: FeatureFlag
           PickHostNameFromBackendAddress: bool
           RequestTimeoutInSeconds: int<Seconds>
           Probe: ResourceName option
           ProbeEnabled : bool
           TrustedRootCertificates : ResourceName list
       |} list
      CustomErrorConfigurations:
          {| CustomErrorPageUrl: string
             StatusCode: HttpStatusCode |} list
      EnableFips: bool
      EnableHttp2: bool
      FirewallPolicy: ResourceId option
      ForceFirewallPolicyAssociation: bool
      GatewayIPConfigurations:
          {| Name: ResourceName
             Subnet: ResourceId option |} list
      HttpListeners :
          {|  /// Name of the listener
              Name : ResourceName
              FrontendIpConfiguration : ResourceName
              BackendAddressPool : ResourceName
              CustomErrorConfigurations:  
                {| CustomErrorPageUrl: string
                   StatusCode: HttpStatusCode |} list
              FirewallPolicy : ResourceId option
              FrontendPort : ResourceName
              RequireServerNameIndication : bool
              HostNames : string list
              Protocol : Protocol
              SslCertificate : ResourceName
              SslProfile : ResourceName
          |} list
      Probes :
          {|  /// Name of the probe
              Name : ResourceName
              Host: string
              Port: uint16 option
              Path: string
              Protocol : Protocol
              IntervalInSeconds : int<Seconds>
              TimeoutInSeconds : int<Seconds>
              UnhealthyThreshold : uint16
              PickHostNameFromBackendHttpSettings : bool
              MinServers : uint16 option
              Match :
                {| Body: string option
                   StatusCodes: uint16 list |}
          |} list
      RedirectConfigurations:
        {| Name: ResourceName
           IncludePath: bool
           IncludeQueryString: bool
           PathRules: ResourceName list
           RedirectType: RedirectType
           RequestRoutingRules: ResourceName list
           TargetListener: ResourceName
           TargetUrl: string
           UrlPathMaps: ResourceName list |} list
      RequestRoutingRules : 
        {|  Name: ResourceName
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
        {|  Name: ResourceName
            RewriteRules:
              {| ActionSet: 
                  {| RequestHeaderConfigurations:
                       {| HeaderName: string
                          HeaderValue: string |} list 
                     ResponseHeaderConfigurations:
                       {| HeaderName: string
                          HeaderValue: string |} list
                     UrlConfiguration:
                       {| ModifiedPath: string
                          ModifiedQueryString: string
                          Reroute: bool |} |}
                 Conditions:
                  {| IgnoreCase: bool
                     Negate: bool
                     Pattern: string
                     Variable: string |} list
                 Name: string
                 RuleSequence: int 
             |} list 
         |} list
      SslCertificates:
        {| Name: ResourceName
           Data: string option
           KeyVaultSecretId: string
           Password: string option |} list
      SslPolicy:
        {| CipherSuites: CipherSuite list
           DisabledSslProtocols: SslProtocol list
           MinProtocolVersion: SslProtocol
           PolicyName: PolicyName
           PolicyType: PolicyType |} option
      SslProfiles:
          {| Name: ResourceName
             ClientAuthConfiguration: 
               {| VerifyClientCertIssuerDN: bool |}
             SslPolicy: 
               {| CipherSuites: CipherSuite list
                  DisabledSslProtocols: SslProtocol list
                  MinProtocolVersion: SslProtocol
                  PolicyName: PolicyName
                  PolicyType: PolicyType |} option
             TrustedClientCertificates: ResourceName list
          |} list
      TrustedClientCertificates:
          {| Name: ResourceName
             Data: string |} list
      TrustedRootCertificates:
          {| Name: ResourceName
             Data: string option
             KeyVaultSecretId: string |} list
      UrlPathMaps:
          {| Name: ResourceName
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
          {| DisabledRuleGroups:
               {| RuleGroupName: string
                  Rules: int list |} list
             Enabled: bool
             Exclusions:
               {| MatchVariable: string
                  Selector: string
                  SelectorMatchOperator: string |} list
             FileUploadLimitInMb: int<Mb> option
             FirewallMode: FirewallMode option
             MaxRequestBodySize: int option
             MaxRequestBodySizeInKb: int<Kb> option
             RequestBodyCheck: bool option
             RuleSetType: RuleSetType
             RuleSetVersion: string |} option
      Zones: uint16 list
      Dependencies: Set<ResourceId>
      Tags: Map<string,string> }
    interface IArmResource with
        member this.ResourceId = ApplicationGateways.resourceId this.Name
        member this.JsonModel =
            {| ApplicationGateways.Create (this.Name, this.Location, this.Dependencies, this.Tags) with
                identity =
                    if this.Identity = ManagedIdentity.Empty then Unchecked.defaultof<_>
                    else this.Identity.ToArmJson
                properties =
                    {|
                        sku =
                            {|
                                name = this.Sku.Name.ArmValue
                                capacity = this.Sku.Capacity |> Option.toNullable
                                tier = this.Sku.Tier.ArmValue
                            |}
                        autoscaleConfiguration = this.AutoscaleConfiguration |> Option.map (fun a -> 
                            {|
                                maxCapacity = a.MaxCapacity
                                minCapacity = a.MinCapacity
                            |}
                        ) |> Option.defaultValue Unchecked.defaultof<_>
                        backendAddressPools = this.BackendAddressPools |> List.map (fun backend ->
                            {| name = backend.Value |}
                        )
                        backendHttpSettingsCollection = this.BackendHttpSettingsCollection |> List.map (fun settings ->
                            {|
                              name = settings.Name.Value
                              properties = 
                                {|
                                    affinityCookieName = settings.AffinityCookieName |> Option.toObj
                                    authenticationCertificates = 
                                        settings.AuthenticationCertificates
                                        |> List.map (ApplicationGatewayAuthenticationCertificates.resourceId >> ResourceId.asId)
                                    connectionDraining = settings.ConnectionDraining |> Option.map (fun drain ->
                                        {|
                                          drainTimeoutInSec = drain.DrainTimeoutInSeconds
                                          enabled = drain.Enabled
                                        |}
                                    )
                                    cookieBasedAffinity = settings.CookieBasedAffinity
                                    hostName = settings.HostName |> Option.toObj
                                    path = settings.Path |> Option.toObj
                                    pickHostNameFromBackendAddress = settings.PickHostNameFromBackendAddress
                                    port = settings.Port
                                    probe = settings.Probe |> Option.map (ApplicationGatewayProbes.resourceId >> ResourceId.asId)
                                    probeEnabled = settings.ProbeEnabled
                                    protocol = settings.Protocol.ArmValue
                                    requestTimeout = settings.RequestTimeoutInSeconds
                                    trustedRootCertificates = 
                                        settings.TrustedRootCertificates
                                        |> List.map (ApplicationGatewayTrustedRootCertificates.resourceId >> ResourceId.asId)
                                |}
                            |}
                        )
                        customErrorConfigurations = this.CustomErrorConfigurations |> List.map (fun conf ->
                          {|
                            customErrorPageUrl = conf.CustomErrorPageUrl
                            statusCode = conf.StatusCode.ArmValue
                          |}
                        )
                        enableFips = this.EnableFips
                        enableHttp2 = this.EnableHttp2
                        firewallPolicy = this.FirewallPolicy |> Option.map ResourceId.asId |> Option.defaultValue Unchecked.defaultof<_>
                        frontendPorts = this.FrontendPorts |> List.map (fun frontend ->
                            {|
                                name = frontend.Name.Value
                                properties = 
                                    {| port = frontend.Port |}
                            |}
                        )
                        gatewayIPConfigurations = this.GatewayIPConfigurations |> List.map (fun gwip ->
                            {|
                                name = gwip.Name.Value
                                properties = 
                                    {| subnet = gwip.Subnet |> Option.map ResourceId.asId |> Option.defaultValue Unchecked.defaultof<_> |}
                            |}
                        )
                        httpListeners = this.HttpListeners |> List.map (fun listener ->
                          {|
                            name = listener.Name.Value
                            properties = 
                                {|
                                  customErrorConfigurations = listener.CustomErrorConfigurations |> List.map (fun cfg ->
                                    {|
                                      customErrorPageUrl = cfg.CustomErrorPageUrl
                                      statusCode = cfg.StatusCode.ArmValue
                                    |}
                                  )
                                  firewallPolicy = listener.FirewallPolicy |> Option.map ResourceId.asId |> Option.defaultValue Unchecked.defaultof<_>
                                  frontendIPConfiguration = listener.FrontendIpConfiguration |> ApplicationGatewayFrontendIPConfigurations.resourceId |> ResourceId.asId
                                  frontendPort = listener.FrontendPort |> ApplicationGatewayFrontendPorts.resourceId |> ResourceId.asId
                                  hostName = listener.HostNames
                                  protocol = listener.Protocol.ArmValue
                                  requireServerNameIndication = listener.RequireServerNameIndication
                                  sslCertificate = listener.SslCertificate |> ApplicationGatewaySslCertificates.resourceId |> ResourceId.asId
                                  sslProfile = listener.SslProfile |> ApplicationGatewaySslProfiles.resourceId |> ResourceId.asId
                                |}
                          |}
                        )
                        frontendIPConfigurations = this.FrontendIpConfigs |> List.map (fun frontend ->
                            let allocationMethod, ip =
                                match frontend.PrivateIpAllocationMethod with
                                | PrivateIpAddress.DynamicPrivateIp -> "Dynamic", null
                                | PrivateIpAddress.StaticPrivateIp ip -> "Static", string ip
                            {| name = frontend.Name.Value
                               properties =
                                   {|  privateIPAllocationMethod = allocationMethod
                                       privateIPAddress = ip
                                       publicIPAddress =
                                           frontend.PublicIp |> Option.map (fun pip -> {| id = pip.Eval() |} )
                                           |> Option.defaultValue Unchecked.defaultof<_>
                                   |}
                            |}
                        )
                        probes = this.Probes |> List.map (fun probe ->
                            {|
                                name = probe.Name.Value
                                properties =
                                    {|
                                        host = probe.Host
                                        port = probe.Port |> Option.toNullable
                                        path = probe.Path
                                        protocol = probe.Protocol.ArmValue
                                        pickHostNameFromBackendHttpSettings = probe.PickHostNameFromBackendHttpSettings
                                        ``match`` = {| body = probe.Match.Body |> Option.toObj
                                                       statusCodes = probe.Match.StatusCodes |> List.map string |}
                                        minServers = probe.MinServers |> Option.toNullable
                                        interval = probe.IntervalInSeconds
                                        timeoutInSeconds = probe.TimeoutInSeconds
                                        unhealthyThreshold = probe.UnhealthyThreshold
                                    |}
                            |}
                        )
                        redirectConfigurations = this.RedirectConfigurations |> List.map (fun cfg ->
                            {|
                                name = cfg.Name.Value
                                properties = 
                                    {|
                                        includePath = cfg.IncludePath
                                        includeQueryString = cfg.IncludeQueryString
                                        pathRules = cfg.PathRules |> List.map (ApplicationGatewayPathRules.resourceId >> ResourceId.asId)
                                        redirectType = cfg.RedirectType.ArmValue
                                        requestRoutingRules = cfg.RequestRoutingRules |> List.map (ApplicationGatewayRequestRoutingRules.resourceId >> ResourceId.asId)
                                        targetListener = ApplicationGatewayHttpListeners.resourceId cfg.TargetListener |> ResourceId.asId
                                        targetUrl = cfg.TargetUrl
                                        urlPathMaps = cfg.UrlPathMaps |> List.map (ApplicationGatewayUrlPathMaps.resourceId >> ResourceId.asId)
                                    |}
                            |}
                        )
                        requestRoutingRules = this.RequestRoutingRules |> List.map (fun routingRule ->
                            {|
                                name = routingRule.Name.Value
                                properties = 
                                    {|
                                        backendAddressPool = ApplicationGatewayBackendAddressPools.resourceId routingRule.BackendAddressPool |> ResourceId.asId
                                        backendHttpSettings = ApplicationGatewayBackendHttpSettingsCollection.resourceId routingRule.BackendHttpSettings |> ResourceId.asId
                                        httpListener = ApplicationGatewayHttpListeners.resourceId routingRule.HttpListener |> ResourceId.asId
                                        priority = routingRule.Priority |> Option.toNullable
                                        redirectConfiguration =  routingRule.RedirectConfiguration |> Option.map (ApplicationGatewayRedirectConfigurations.resourceId >> ResourceId.asId) |> Option.defaultValue Unchecked.defaultof<_>
                                        rewriteRuleSet = routingRule.RewriteRuleSet |> Option.map (ApplicationGatewayRewriteRuleSets.resourceId >> ResourceId.asId) |> Option.defaultValue Unchecked.defaultof<_>
                                        ruleType = routingRule.RuleType
                                        urlPathMap = routingRule.UrlPathMap |> Option.map (ApplicationGatewayUrlPathMaps.resourceId >> ResourceId.asId) |> Option.defaultValue Unchecked.defaultof<_>
                                    |}
                            |}
                        )
                        rewriteRuleSets = this.RewriteRuleSets |> List.map (fun ruleSet ->
                            {|
                              name = ruleSet.Name.Value
                              properties =
                                {|
                                    rewriteRules = ruleSet.RewriteRules |> List.map (fun rule ->
                                        {|
                                            actionSet = 
                                                {|  requestHeaderConfigurations = rule.ActionSet.RequestHeaderConfigurations |> List.map (fun cfg ->
                                                        {| headerName = cfg.HeaderName
                                                           headerValue = cfg.HeaderValue |}
                                                    )
                                                    responseHeaderConfigurations = rule.ActionSet.ResponseHeaderConfigurations |> List.map (fun cfg ->
                                                        {| headerName = cfg.HeaderName
                                                           headerValue = cfg.HeaderValue |}
                                                    )
                                                    urlConfiguration = 
                                                        {|
                                                          modifiedPath = rule.ActionSet.UrlConfiguration.ModifiedPath
                                                          modifiedQueryString = rule.ActionSet.UrlConfiguration.ModifiedQueryString
                                                          reroute = rule.ActionSet.UrlConfiguration.Reroute
                                                        |}
                                                |}
                                            conditions = rule.Conditions |> List.map (fun c ->
                                                {|
                                                  ignoreCase = c.IgnoreCase
                                                  negate = c.Negate
                                                  pattern = c.Pattern
                                                  variable = c.Variable
                                                |}
                                            )
                                            name = rule.Name
                                            ruleSequence = rule.RuleSequence
                                        |}
                                    )
                                |}
                            |}
                        )
                        sslCertificates = this.SslCertificates |> List.map (fun cert ->
                            {|
                                name = cert.Name.Value
                                properties =
                                 {|
                                    data = cert.Data |> Option.toObj
                                    keyVaultSecretId = cert.KeyVaultSecretId
                                    password = cert.Password |> Option.toObj
                                 |}
                            |}
                        )
                        sslPolicy = this.SslPolicy |> Option.map (fun sslPolicy ->
                            {|
                              cipherSuites = sslPolicy.CipherSuites |> List.map CipherSuite.toString
                              disabledSslProtocols = sslPolicy.DisabledSslProtocols |> List.map SslProtocol.toString
                              minProtocolVersion = sslPolicy.MinProtocolVersion.ArmValue
                              policyName = sslPolicy.PolicyName.ArmValue
                              policyType = sslPolicy.PolicyType.ArmValue
                            |}
                        ) |> Option.defaultValue Unchecked.defaultof<_>
                        sslProfiles = this.SslProfiles |> List.map (fun sslProfile ->
                            {| 
                              name = sslProfile.Name.Value
                              properties =
                                {|
                                  clientAuthConfiguration =
                                    {| verifyClientCertIssuerDN = sslProfile.ClientAuthConfiguration.VerifyClientCertIssuerDN |}
                                  sslPolicy = sslProfile.SslPolicy |> Option.map (fun sslPolicy ->
                                      {|
                                          cipherSuites = sslPolicy.CipherSuites |> List.map CipherSuite.toString
                                          disabledSslProtocols = sslPolicy.DisabledSslProtocols |> List.map SslProtocol.toString
                                          minProtocolVersion = sslPolicy.MinProtocolVersion.ArmValue
                                          policyName = sslPolicy.PolicyName.ArmValue
                                          policyType = sslPolicy.PolicyType.ArmValue
                                      |}
                                  ) |> Option.defaultValue Unchecked.defaultof<_>
                                  trustedClientCertificates =
                                    sslProfile.TrustedClientCertificates 
                                    |> List.map (ApplicationGatewayTrustedRootCertificates.resourceId >> ResourceId.asId)
                                |}
                            |}
                        )
                        trustedClientCertificates = this.TrustedClientCertificates |> List.map (fun cert ->
                          {| name = cert.Name.Value
                             properties = 
                                {| data = cert.Data |}
                          |}
                        )
                        trustedRootCertificates = this.TrustedRootCertificates |> List.map (fun cert ->
                          {| name = cert.Name.Value
                             properties = 
                                {| data = cert.Data |> Option.toObj
                                   keyVaultSecretId = cert.KeyVaultSecretId |}
                          |}
                        )
                        urlPathMaps = this.UrlPathMaps |> List.map (fun pathMap ->
                            {|
                                name = pathMap.Name.Value
                                properties =
                                    {|
                                       defaultBackendAddressPool = ApplicationGatewayBackendAddressPools.resourceId pathMap.DefaultBackendAddressPool |> ResourceId.asId
                                       defaultBackendHttpSettings = ApplicationGatewayBackendHttpSettingsCollection.resourceId pathMap.DefaultBackendHttpSettings |> ResourceId.asId
                                       defaultRedirectConfiguration = ApplicationGatewayRedirectConfigurations.resourceId pathMap.DefaultRedirectConfiguration |> ResourceId.asId
                                       defaultRewriteRuleSet = ApplicationGatewayRewriteRuleSets.resourceId pathMap.DefaultRewriteRuleSet |> ResourceId.asId
                                       pathRules = pathMap.PathRules |> List.map (fun pathRule ->
                                          {|
                                              name = pathRule.Name.Value
                                              properties =
                                              {|
                                                backendAddressPool = ApplicationGatewayBackendAddressPools.resourceId pathRule.BackendAddressPool |> ResourceId.asId
                                                backendHttpSettings = ApplicationGatewayBackendHttpSettingsCollection.resourceId pathRule.BackendHttpSettings |> ResourceId.asId
                                                firewallPolicy = pathRule.FirewallPolicy |> ResourceId.asId
                                                redirectConfiguration = ApplicationGatewayRedirectConfigurations.resourceId pathRule.RedirectConfiguration |> ResourceId.asId
                                                rewriteRuleSet = ApplicationGatewayRewriteRuleSets.resourceId pathRule.RewriteRuleSet |> ResourceId.asId
                                                paths = pathRule.Paths
                                              |}
                                          |}
                                       )
                                   |}
                            |}
                        )
                        webApplicationFirewallConfiguration = this.WebApplicationFirewallConfiguration |> Option.map (fun cfg ->
                            {|
                              disabledRuleGroups = cfg.DisabledRuleGroups |> List.map (fun ruleGroup ->
                                {|
                                  ruleGroupName = ruleGroup.RuleGroupName
                                  rules = ruleGroup.Rules
                                |}
                              )
                              enabled = cfg.Enabled
                              exclusions = cfg.Exclusions |> List.map (fun e ->
                                {|
                                  matchVariable = e.MatchVariable
                                  selector = e.Selector
                                  selectorMatchOperator = e.SelectorMatchOperator
                                |}
                              )
                              fileUploadLimitInMb = cfg.FileUploadLimitInMb
                              firewallMode = cfg.FirewallMode |> Option.map FirewallMode.toString
                              maxRequestBodySize = cfg.MaxRequestBodySize |> Option.defaultValue Unchecked.defaultof<_>
                              maxRequestBodySizeInKb = cfg.MaxRequestBodySizeInKb |> Option.defaultValue Unchecked.defaultof<_>
                              requestBodyCheck = cfg.RequestBodyCheck |> Option.defaultValue Unchecked.defaultof<_>
                              ruleSetType = cfg.RuleSetType.ArmValue
                              ruleSetVersion = cfg.RuleSetVersion
                            |}
                        ) |> Option.defaultValue Unchecked.defaultof<_>
                        zones = this.Zones |> List.map string
                    |}
            |} :> _

type BackendAddressPool =
    {   /// Name of the backend address pool
        Name : ResourceName
        /// Name of the load balancer where this pool will be added.
        ApplicationGateway : ResourceName
        /// Addresses of backend services.
        ApplicationGatewayBackendAddresses :
            {|  Fqdn : string
                /// IP Address of the backend resource in the pool
                IpAddress : System.Net.IPAddress
            |} list
    }
    interface IArmResource with
        member this.ResourceId = ApplicationGatewayBackendAddressPools.resourceId (this.ApplicationGateway, this.Name)
        member this.JsonModel =
            let dependencies =
                seq {
                    yield ApplicationGateways.resourceId this.ApplicationGateway
                } |> Set.ofSeq
            {| ApplicationGatewayBackendAddressPools.Create(this.Name, dependsOn=dependencies) with
                name = $"{this.ApplicationGateway.Value}/{this.Name.Value}"
                properties =
                    {| ApplicationGatewayBackendAddresses = this.ApplicationGatewayBackendAddresses |> List.map (fun addr ->
                        {|  properties =
                                {| fqdn = addr.Fqdn
                                   ipAddress = string addr.IpAddress |}
                        |}
                       )
                    |}
            |} :> _
