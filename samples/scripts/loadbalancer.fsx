#r "nuget:Farmer"

open Farmer
open Farmer.Builders
open Farmer.Network
open Farmer.LoadBalancer
open Farmer.Arm.Network
open Farmer.Arm.LoadBalancer

let (lb: LoadBalancer) = {
    Name = ResourceName "lb-test"
    Location = Location.EastUS
    Sku = {
        Name = Sku.Standard
        Tier = Tier.Regional
    }
    FrontendIpConfigs = [
        {|
            Name = ResourceName "LoadBalancerFrontend"
            AddressVersion = Network.AddressVersion.IPv4
            PublicIp = Some(publicIPAddresses.resourceId "lb-test-pip")
            PrivateIpAllocationMethod = PrivateIpAddress.DynamicPrivateIp
            Subnet = None
        |}
    ]
    BackendAddressPools = [ ResourceName "containerGroups" ]
    LoadBalancingRules = [
        {|
            Name = ResourceName "hw-rule"
            BackendAddressPool = ResourceName "containerGroups"
            BackendPort = 8080us
            FrontendPort = 80us
            Protocol = Some TransmissionProtocol.TCP
            DisableOutboundSnat = None
            EnableTcpReset = None
            FrontendIpConfiguration = ResourceName "LoadBalancerFrontend"
            IdleTimeoutMinutes = None
            LoadDistribution = LoadDistributionPolicy.Default
            Probe = Some(ResourceName "checkHttp")
        |}
    ]
    Probes = [
        {|
            Name = ResourceName "checkHttp"
            Port = 8080us
            Protocol = LoadBalancerProbeProtocol.HTTP
            RequestPath = "/"
            IntervalInSeconds = 15
            NumberOfProbes = 2
        |}
    ]
    Dependencies = Set.empty
    Tags = Map.empty
}

let backendPool = {
    Name = ResourceName "containerGroups"
    LoadBalancer = lb.Name
    LoadBalancerBackendAddresses = [
        {|
            Name = ResourceName "group1"
            IpAddress = System.Net.IPAddress.Parse "10.0.1.4"
            VirtualNetwork = Some(virtualNetworks.resourceId "ha-container-group-vnet" |> Unmanaged)
        |}
        {|
            Name = ResourceName "group2"
            IpAddress = System.Net.IPAddress.Parse "10.0.1.5"
            VirtualNetwork = Some(virtualNetworks.resourceId "ha-container-group-vnet" |> Unmanaged)
        |}
    ]
}

arm {
    location Location.EastUS

    add_resources [
        vnet {
            name "my-vnet"
            add_address_spaces [ "10.0.1.0/24" ]

            add_subnets [
                subnet {
                    name "my-subnet"
                    prefix "10.0.1.0/24"
                    add_delegations [ SubnetDelegationService.ContainerGroups ]
                }
            ]
        }
        loadBalancer {
            name "lb"
            sku Sku.Standard

            add_frontends [
                frontend {
                    name "lb-frontend"
                    public_ip "lb-pip"
                }
            ]

            add_backend_pools [
                backendAddressPool {
                    name "lb-backend"
                    link_to_vnet "my-vnet"
                    load_balancer "lb"
                    add_ip_addresses [ "10.0.1.4"; "10.0.1.5" ]
                }
            ]

            add_probes [
                loadBalancerProbe {
                    name "httpGet"
                    protocol LoadBalancerProbeProtocol.HTTP
                    port 8080
                    request_path "/"
                }
            ]

            add_rules [
                loadBalancingRule {
                    name "rule1"
                    frontend_ip_config "lb-frontend"
                    backend_address_pool "lb-backend"
                    frontend_port 80
                    backend_port 8080
                    protocol TransmissionProtocol.TCP
                    probe "httpGet"
                }
            ]

            add_dependencies [ Farmer.Arm.Network.virtualNetworks.resourceId "my-vnet" ]
        }
    ]
}
|> Writer.quickWrite "loadbalancer"