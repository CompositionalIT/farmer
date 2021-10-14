#r "../../src/Tests/bin/Debug/net5.0/Farmer.dll"

open Farmer

open Farmer.NetworkSecurity
open Farmer.Identity
open Farmer.Builders
open Farmer.ApplicationGateway
open Farmer.Arm.Network
open Farmer.Arm.ApplicationGateway

let backendPool =
    {
        Name = ResourceName "agw-be-pool"
        ApplicationGateway = ResourceName "agw-test"
        ApplicationGatewayBackendAddresses = [
            {|
                Fqdn = Unchecked.defaultof<_>
                IpAddress = System.Net.IPAddress.Parse "10.0.1.4"
            |}
            {|
                Fqdn = Unchecked.defaultof<_>
                IpAddress = System.Net.IPAddress.Parse "10.0.1.5"
            |}
        ]
    }

let gwPolicy = securityRule {
    name "app-gw"
    description "Public web server access"
    services [ NetworkService ("GatewayManager", Range (65200us,65535us)) ]
    add_source_tag NetworkSecurity.TCP "Internet"
    add_destination_any
}
let appPolicy = securityRule {
    name "app-servers"
    description "Internal app server access"
    services [ "http", 80 ]
    add_source_network NetworkSecurity.TCP "10.28.0.0/24"
    add_destination_network "10.28.1.0/24"
}
let myNsg = nsg {
    name "agw-nsg"
    add_rules [ gwPolicy; appPolicy ]
}
let net = vnet {
    name "agw-vnet"
    build_address_spaces [
        addressSpace {
            space "10.28.0.0/16"
            subnets [
                subnetSpec {
                    name "web"
                    size 24
                    network_security_group myNsg
                }
                subnetSpec {
                    name "apps"
                    size 24
                    network_security_group myNsg
                }
                subnetSpec {
                    name "no-nsg"
                    size 24
                }
            ]
        }
    ]
}
let (agw:ApplicationGateway) = {
    Name = ResourceName "agw-test"
    Location = Location.EastUS
    Sku = { Name = Sku.Standard_v2; Tier = Tier.Standard_v2; Capacity = Some 2 }
    Identity = ManagedIdentity.Empty
    GatewayIPConfigurations = [
        {| 
            Name = ResourceName "agw-gwip"
            Subnet = subnets.resourceId net.Subnets.[0].Name |> Some
        |}
    ]
    FrontendIpConfigs = [
        {|
            Name = ResourceName "frontend-ip"
            PublicIp = Some (publicIPAddresses.resourceId "agw-test-pip")
            PrivateIpAllocationMethod = PrivateIpAddress.DynamicPrivateIp
        |}
    ]
    FrontendPorts = [
        {|
            Name = ResourceName "port-80"
            Port = 80us
        |}
    ]
    CustomErrorConfigurations = []
    EnableFips = false
    EnableHttp2 = false
    FirewallPolicy = None
    ForceFirewallPolicyAssociation = false
    HttpListeners = [
        {|
            Name = ResourceName "http-listener"
            BackendAddressPool = ResourceName "agw-be-pool"
            FrontendIpConfiguration = ResourceName "frontend-ip"
            FrontendPort = ResourceName "port-80"
            Protocol = Protocol.Http
            HostNames = []
            RequireServerNameIndication = false
            CustomErrorConfigurations = []
            FirewallPolicy = None
            SslCertificate = None
            SslProfile = None
        |}
    ]
    BackendAddressPools = [ backendPool.Name ]
    RequestRoutingRules = [
        {|
            Name = ResourceName "rr"
            RuleType = RuleType.Basic
            HttpListener = ResourceName "http-listener"
            BackendAddressPool = ResourceName "agw-be-pool"
            BackendHttpSettings = ResourceName "agw-settings"
            RedirectConfiguration = None
            RewriteRuleSet = None
            UrlPathMap = None
            Priority = None
        |}
    ]
    RedirectConfigurations = []
    RewriteRuleSets = []
    SslCertificates = [
        {|
            Data = None
            Password = None
            Name = ResourceName "cert"
            KeyVaultSecretId = "https://avs-scripting-dev-kv.vault.azure.net/secrets/avs-scripting-dev-eastus-cloudapp-azure-com"
        |}
    ]
    SslPolicy = None
    SslProfiles = []
    TrustedClientCertificates = []
    TrustedRootCertificates = []
    UrlPathMaps = []
    WebApplicationFirewallConfiguration = None
    Zones = []
    BackendHttpSettingsCollection = [
        {|
            Name = ResourceName "bp-default-web-80-9090-web"
            AffinityCookieName = None
            CookieBasedAffinity = FeatureFlag.Disabled
            Path = None
            PickHostNameFromBackendAddress = false
            Port = 9090us
            Probe = ResourceName "agw-probe" |> Some
            ProbeEnabled = false
            Protocol = Protocol.Http
            RequestTimeoutInSeconds = 30<Seconds>
            ConnectionDraining = None
            AuthenticationCertificates = []
            HostName = None
            TrustedRootCertificates = []
        |}
    ]
    Probes = [
        {|
            Name = ResourceName "agw-probe"
            Host = "localhost"
            Path = "/"
            Port = Some 8080us
            Protocol = Protocol.Http
            Match = None
            IntervalInSeconds = 30<Seconds>
            TimeoutInSeconds = 30<Seconds>
            UnhealthyThreshold = 3us
            PickHostNameFromBackendHttpSettings = false
            MinServers = None
        |}
    ]
    AuthenticationCertificates = []
    AutoscaleConfiguration = None
    Dependencies = Set.empty
    Tags = Map.empty
}


arm {
    location Location.EastUS
    add_resources [
        net
        myNsg
    ]
    add_resource agw
    add_resource backendPool
} |> Writer.quickWrite "AGW"
