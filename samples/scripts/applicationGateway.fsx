#r "../../src/Tests/bin/Debug/net5.0/Farmer.dll"

open Farmer

open Farmer.NetworkSecurity
open Farmer.Identity
open Farmer.Builders
open Farmer.ApplicationGateway
open Farmer.Arm.Network
open Farmer.Arm.ApplicationGateway


let gwPolicy = securityRule {
    name "app-gw"
    description "GatewayManager"
    services [ NetworkService ("GatewayManager", Range (65200us,65535us)) ]
    add_source_tag NetworkSecurity.TCP "GatewayManager"
    add_destination_any
}
let gwInternetPolicy = securityRule {
    name "inet-gw"
    description "Internet to gateway"
    services [ "http", 80 ]
    add_source_tag NetworkSecurity.TCP "Internet"
    add_destination_network "10.28.0.0/24"
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
    add_rules [ gwPolicy; gwInternetPolicy; appPolicy ]
}

let net = vnet {
    name "agw-vnet"
    build_address_spaces [
        addressSpace {
            space "10.28.0.0/16"
            subnets [
                subnetSpec {
                    name "gw"
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
let msi = createUserAssignedIdentity "agw-msi"

let backendPoolName = ResourceName "agw-be-pool"

let myAppGateway =
    appGateway {
        name "agw-bldr-test"
        sku_capacity 2
        add_identity msi
        add_ip_configs [
            gatewayIp {
                name "agw-gwip"
                link_to_subnet net.Name net.Subnets.[0].Name
            }
        ]
        add_frontends [
            frontendIp {
                name "frontend-ip"
                public_ip "agw-bldr-pip"
            }
        ]
        add_frontend_ports [
            frontendPort {
                name "port-80"
                port 80
            }
        ]
        add_http_listeners [
            httpListener {
                name "http-listener"
                frontend_ip "frontend-ip"
                frontend_port "port-80"
                backend_pool backendPoolName.Value
            }
        ]
        add_backend_address_pools [
            appGatewayBackendAddressPool {
                name backendPoolName.Value
                add_backend_addresses [
                    backend_ip_address "10.28.1.4"
                    backend_ip_address "10.28.1.5"
                ]
            }
        ]
        add_backend_http_settings_collection [
            backendHttpSettings {
                name "bp-default-web-80-9090-web"
                port 9090us
                probe "agw-probe"
                protocol Protocol.Http
                request_timeout 30<Seconds>
            }
        ]
        add_request_routing_rules [
            basicRequestRoutingRule {
                name "rr"
                http_listener "http-listener"
                backend_address_pool backendPoolName.Value
                backend_http_settings "bp-default-web-80-9090-web"
            }
        ]
        add_probes [
            appGatewayProbe {
                name "agw-probe"
                host "localhost"
                path "/"
                port 8080
                protocol Protocol.Http
            }
        ]
        depends_on myNsg
        depends_on net
   }

arm {
    location Location.EastUS
    add_resources [
        msi
        net
        myNsg
        myAppGateway
    ]
} |> Writer.quickWrite "AGW-bldr"

let publicIp = 
    {|
        ``type`` = "Microsoft.Network/publicIPAddresses"
        apiVersion = "2020-11-01"
        name = "agw-pip"
        location = "eastus"
        sku =
            {|
                name = "Standard"
                tier = "Regional"
            |}
        properties =
            {|
                publicIPAddressVersion = "IPv4"
                publicIPAllocationMethod = "Static"
                idleTimeoutInMinutes = 4
                dnsSettings =
                    {|
                        domainNameLabel = "farmer-agw-pip"
                        fqdn = "farmer-agw-pip.eastus.cloudapp.azure.com"
                    |}
            |}
    |}

let (agw:ApplicationGateway) = {
    Name = ResourceName "agw-test"
    Location = Location.EastUS
    Sku = { Name = Sku.Standard_v2; Tier = Tier.Standard_v2; Capacity = Some 2 }
    Identity = ManagedIdentity.create(Farmer.Arm.ManagedIdentity.userAssignedIdentities.resourceId msi.Name)
    GatewayIPConfigurations = [
        {| 
            Name = ResourceName "agw-gwip"
            Subnet = subnets.resourceId (net.Name, net.Subnets.[0].Name) |> Some
        |}
    ]
    FrontendIpConfigs = [
        {|
            Name = ResourceName "frontend-ip"
            PublicIp = Some (publicIPAddresses.resourceId publicIp.name)
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
    EnableFips = None
    EnableHttp2 = None
    FirewallPolicy = None
    ForceFirewallPolicyAssociation = false
    HttpListeners = [
        {|
            Name = ResourceName "http-listener"
            BackendAddressPool = backendPoolName
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
    BackendAddressPools =
        [
            {| Name = backendPoolName
               Addresses = [
                   BackendAddress.Ip (System.Net.IPAddress.Parse "10.28.1.4")
                   BackendAddress.Ip (System.Net.IPAddress.Parse "10.28.1.5")
               ]
            |}
        ]     
    RequestRoutingRules = [
        {|
            Name = ResourceName "rr"
            RuleType = RuleType.Basic
            HttpListener = ResourceName "http-listener"
            BackendAddressPool = backendPoolName
            BackendHttpSettings = ResourceName "bp-default-web-80-9090-web"
            RedirectConfiguration = None
            RewriteRuleSet = None
            UrlPathMap = None
            Priority = None
        |}
    ]
    RedirectConfigurations = []
    RewriteRuleSets = []
    SslCertificates = [
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
    Dependencies = [
        Arm.NetworkSecurityGroup.networkSecurityGroups.resourceId myNsg.Name
        net.ResourceId
    ] |> Set.ofList
    Tags = Map.empty
}


arm {
    location Location.EastUS
    add_resources [
        msi
        net
        myNsg
    ]
    add_resource (Resource.ofObj publicIp)
    add_resource agw
} |> Writer.quickWrite "AGW"
