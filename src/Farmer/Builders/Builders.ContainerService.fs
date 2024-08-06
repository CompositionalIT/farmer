[<AutoOpen>]
module Farmer.Builders.ContainerService

open System
open Farmer
open Farmer.Arm
open Farmer.Arm.ContainerService.AddonProfiles
open Farmer.Arm.RoleAssignment
open Farmer.Identity
open Farmer.Vm

type AgentPoolConfig = {
    Name: ResourceName
    Count: int
    EnableFIPS: FeatureFlag option
    MaxPods: int option
    Mode: AgentPoolMode
    OsDiskSize: int<Gb>
    OsType: OS
    VmSize: VMSize
    VirtualNetworkName: ResourceName option
    SubnetName: ResourceName option
} with

    static member Default = {
        Name = ResourceName.Empty
        Count = 1
        EnableFIPS = None
        // Default for CNI is 30, Kubenet default is 110
        // https://docs.microsoft.com/en-us/azure/aks/configure-azure-cni#maximum-pods-per-node
        MaxPods = None
        Mode = System
        OsDiskSize = 0<Gb>
        OsType = OS.Linux
        VirtualNetworkName = None
        SubnetName = None
        VmSize = Standard_DS2_v2
    }

type ApiServerAccessProfileConfig = {
    AuthorizedIPRanges: string list
    EnablePrivateCluster: bool option
}

type NetworkProfileConfig = {
    NetworkPlugin: ContainerService.NetworkPlugin option
    /// If no address is specified, this will use the 2nd address in the service address CIDR
    DnsServiceIP: System.Net.IPAddress option
    /// Usually the default 172.17.0.1/16 is acceptable.
    DockerBridgeCidr: IPAddressCidr option
    /// Load balancer SKU (defaults to basic)
    LoadBalancerSku: LoadBalancer.Sku option
    /// Private IP address CIDR for services in the cluster which should not overlap with the vnet
    /// for the cluster or peer vnets. Defaults to 10.244.0.0/16.
    ServiceCidr: IPAddressCidr option
}

type AddonConfig =
    | AciConnectorLinux of FeatureFlag
    | HttpApplicationRouting of FeatureFlag
    | IngressApplicationGateway of IngressApplicationGateway
    | KubeDashboard of FeatureFlag
    | OmsAgent of OmsAgent

    static member BuildConfig(addons: AddonConfig list) : AddonProfileConfig = {
        // TODO: Clean up with active pattern
        AciConnectorLinux =
            addons
            |> List.tryFind (function
                | AciConnectorLinux _ -> true
                | _ -> false)
            |> function
                | Some(AciConnectorLinux status) -> Some { AciConnectorLinux.Status = status }
                | _ -> None
        HttpApplicationRouting =
            addons
            |> List.tryFind (function
                | HttpApplicationRouting _ -> true
                | _ -> false)
            |> function
                | Some(HttpApplicationRouting status) ->
                    Some {
                        HttpApplicationRouting.Status = status
                    }
                | _ -> None
        IngressApplicationGateway =
            addons
            |> List.tryFind (function
                | IngressApplicationGateway _ -> true
                | _ -> false)
            |> function
                | Some(IngressApplicationGateway gw) -> Some gw
                | _ -> None
        KubeDashboard =
            addons
            |> List.tryFind (function
                | KubeDashboard _ -> true
                | _ -> false)
            |> function
                | Some(KubeDashboard status) -> Some { KubeDashboard.Status = status }
                | _ -> None
        OmsAgent =
            addons
            |> List.tryFind (function
                | OmsAgent _ -> true
                | _ -> false)
            |> function
                | Some(OmsAgent oms) -> Some oms
                | _ -> None
    }

type AksConfig = {
    Name: ResourceName
    AddonProfiles: AddonConfig list
    AgentPools: AgentPoolConfig list
    Dependencies: ResourceId Set
    DependencyExpressions: ArmExpression Set
    DnsPrefix: string
    EnableRBAC: bool
    Identity: ManagedIdentity
    IdentityProfile: ManagedClusterIdentityProfile option
    ApiServerAccessProfile: ApiServerAccessProfileConfig option
    LinuxProfile: (string * string list) option
    NetworkProfile: NetworkProfileConfig option
    OidcIssuerProfile: OidcIssuerProfile option
    SecurityProfile: SecurityProfileSettings option
    ServicePrincipalClientID: string
    WindowsProfileAdminUserName: string option
} with

    member private this.ResourceId = managedClusters.resourceId this.Name
    member this.SystemIdentity = SystemIdentity this.ResourceId

    interface IBuilder with
        member this.ResourceId = this.ResourceId

        member this.BuildResources location = [
            {
                Name = this.Name
                Location = location
                AddOnProfiles =
                    match this.AddonProfiles with
                    | [] -> None
                    | addons -> addons |> AddonConfig.BuildConfig |> Some
                Dependencies = this.Dependencies
                DependencyExpressions = this.DependencyExpressions
                DnsPrefix =
                    if String.IsNullOrWhiteSpace this.DnsPrefix then
                        $"{this.Name.Value}-%x{this.Name.Value.GetHashCode()}"
                    else
                        this.DnsPrefix
                EnableRBAC = this.EnableRBAC
                Identity = this.Identity
                IdentityProfile = this.IdentityProfile
                AgentPoolProfiles =
                    match this.AgentPools with
                    | [] -> [
                        {
                            AgentPoolConfig.Default with
                                Count = 3
                        }
                      ]
                    | agentPools -> agentPools
                    |> List.map (fun agentPool -> {|
                        Name = agentPool.Name
                        Count = agentPool.Count
                        EnableFIPS = agentPool.EnableFIPS
                        MaxPods = agentPool.MaxPods
                        Mode = agentPool.Mode
                        OsDiskSize = agentPool.OsDiskSize
                        OsType = agentPool.OsType
                        SubnetName = agentPool.SubnetName
                        VmSize = agentPool.VmSize
                        VirtualNetworkName = agentPool.VirtualNetworkName
                    |})
                ApiServerAccessProfile =
                    this.ApiServerAccessProfile
                    |> Option.map (fun apiAccess -> {|
                        AuthorizedIPRanges = apiAccess.AuthorizedIPRanges
                        EnablePrivateCluster = apiAccess.EnablePrivateCluster
                    |})
                LinuxProfile =
                    this.LinuxProfile
                    |> Option.map (fun (username, keys) -> {|
                        AdminUserName = username
                        PublicKeys = keys
                    |})
                NetworkProfile =
                    this.NetworkProfile
                    |> Option.map (fun netProfile -> {|
                        NetworkPlugin = netProfile.NetworkPlugin
                        DnsServiceIP =
                            match netProfile.DnsServiceIP with
                            | Some ip -> Some ip
                            | None ->
                                netProfile.ServiceCidr
                                |> Option.map (IPAddressCidr.addresses >> Seq.skip 2 >> Seq.head)
                        DockerBridgeCidr = netProfile.DockerBridgeCidr
                        LoadBalancerSku = netProfile.LoadBalancerSku
                        ServiceCidr = netProfile.ServiceCidr
                    |})
                ServicePrincipalProfile = {|
                    ClientId = this.ServicePrincipalClientID
                    ClientSecret =
                        match this.ServicePrincipalClientID with
                        | "msi" -> None
                        | _ -> Some(SecureParameter $"client-secret-for-{this.Name.Value}")
                |}
                OidcIssuerProfile = this.OidcIssuerProfile
                SecurityProfile = this.SecurityProfile
                WindowsProfile =
                    this.WindowsProfileAdminUserName
                    |> Option.map (fun username -> {|
                        AdminUserName = username
                        AdminPassword = SecureParameter $"admin-password-for-{this.Name.Value}"
                    |})
            }
        ]

    /// Returns the OIDC Issuer URL when configured..
    member this.OidcIssuerUrl =
        let aksId = ResourceId.create (managedClusters, this.Name)

        $"reference({aksId.ArmExpression.Value}).oidcIssuerProfile.issuerURL"
        |> ArmExpression.create

type AgentPoolBuilder() =
    member _.Yield _ = AgentPoolConfig.Default

    /// Sets the name of the agent pool.
    [<CustomOperation "name">]
    member _.Name(state: AgentPoolConfig, name) = { state with Name = ResourceName name }

    /// Sets the count of VM's in the agent pool.
    [<CustomOperation "count">]
    member _.Count(state: AgentPoolConfig, count) = { state with Count = count }

    /// Enables FIPS compliant nodes in the VM scale set for this agent pool.
    [<CustomOperation "enable_fips">]
    member _.EnableFIPS(state: AgentPoolConfig) = { state with EnableFIPS = Some Enabled }

    [<CustomOperation "enable_fips">]
    member _.EnableFIPS(state: AgentPoolConfig, featureFlag) = {
        state with
            EnableFIPS = Some featureFlag
    }

    /// Sets the agent pool to user mode.
    [<CustomOperation "user_mode">]
    member _.UserMode(state: AgentPoolConfig) = { state with Mode = AgentPoolMode.User }

    /// Sets the disk size for the VM's in the agent pool.
    [<CustomOperation "disk_size">]
    member _.DiskSizeGB(state: AgentPoolConfig, size) = { state with OsDiskSize = size }

    /// Enables the use of a FIPS-compliant image for VMs in the agent pool.
    [<CustomOperation "fips_image">]
    member _.EnableFipsImage(state: AgentPoolConfig, featureFlag) = { state with EnableFIPS = featureFlag }

    [<CustomOperation "max_pods">]
    member _.MaxPods(state: AgentPoolConfig, maxPods) = { state with MaxPods = maxPods }

    /// Sets the OS type of the VM's in the agent pool.
    [<CustomOperation "os_type">]
    member _.OsType(state: AgentPoolConfig, os) = { state with OsType = os }

    /// Sets the name of a virtual network subnet where this AKS cluster should be attached.
    [<CustomOperation "subnet">]
    member _.SubnetName(state: AgentPoolConfig, subnetName) = {
        state with
            SubnetName = Some(ResourceName subnetName)
    }

    /// Sets the size of the VM's in the agent pool.
    [<CustomOperation "vm_size">]
    member _.VmSize(state: AgentPoolConfig, size) = { state with VmSize = size }

    /// Sets the name of a virtual network in the same region where this AKS cluster should be attached.
    [<CustomOperation "vnet">]
    member _.VNetName(state: AgentPoolConfig, vnetName) = {
        state with
            VirtualNetworkName = Some(ResourceName vnetName)
    }

/// Builds an AKS cluster agent pool ARM resource definition
let agentPool = AgentPoolBuilder()

type NetworkProfileBuilder() =
    /// Sets the SKU to be used for the load balancer.
    [<CustomOperation "load_balancer_sku">]
    member _.LoadBalancerSku(state: NetworkProfileConfig, sku: LoadBalancer.Sku) = {
        state with
            LoadBalancerSku = Some sku
    }

/// Builds a configuration for using the Azure CNI plugin.
type KubenetBuilder() =
    inherit NetworkProfileBuilder()

    member _.Yield _ = {
        NetworkPlugin = Some ContainerService.NetworkPlugin.Kubenet
        LoadBalancerSku = None
        DnsServiceIP = None
        DockerBridgeCidr = None
        ServiceCidr = None
    }

let kubenetNetworkProfile = KubenetBuilder()

/// Builds a configuration for using the Azure CNI plugin.
type AzureCniBuilder() =
    inherit NetworkProfileBuilder()

    member _.Yield _ = {
        NetworkPlugin = Some ContainerService.NetworkPlugin.AzureCni
        LoadBalancerSku = None
        DnsServiceIP = None
        DockerBridgeCidr = IPAddressCidr.parse "172.17.0.1/16" |> Some
        ServiceCidr = IPAddressCidr.parse "10.224.0.0/16" |> Some
    }

    member _.Run(config: NetworkProfileConfig) = {
        config with
            DnsServiceIP =
                match config.DnsServiceIP with
                | Some ip -> Some ip
                | None ->
                    config.ServiceCidr
                    |> Option.map (IPAddressCidr.addresses >> Seq.skip 2 >> Seq.head)
    }

    /// Sets the docker bridge CIDR to a network other than the default 17.17.0.1/16.
    [<CustomOperation "docker_bridge">]
    member _.DockerBridge(state: NetworkProfileConfig, dockerBridge) = {
        state with
            DockerBridgeCidr = IPAddressCidr.parse dockerBridge |> Some
    }

    /// Sets the DNS service IP - must be within the service CIDR, default is the second address in the service CIDR.
    [<CustomOperation "dns_service">]
    member _.DnsServiceIP(state: NetworkProfileConfig, dnsIp: string) = {
        state with
            DnsServiceIP = System.Net.IPAddress.Parse dnsIp |> Some
    }

    /// Sets the service cidr to a network other than the default 10.224.0.0/16.
    [<CustomOperation "service_cidr">]
    member _.ServiceCidr(state: NetworkProfileConfig, serviceCidr) = {
        state with
            ServiceCidr = IPAddressCidr.parse serviceCidr |> Some
    }

let azureCniNetworkProfile = AzureCniBuilder()

/// Builds a Linux Profile from a username and list of ssh public keys
let makeLinuxProfile user sshKeys = user, sshKeys

/// Match on type of load balancer for an AKS config's network profile.
/// The default if nothing is specified is a Standard LB.
let private (|BasicLoadBalancer|StandardLoadBalancer|) =
    function
    | Some netProfile ->
        match netProfile.LoadBalancerSku with
        | Some LoadBalancer.Sku.Basic -> BasicLoadBalancer
        | _ -> StandardLoadBalancer
    | _ -> StandardLoadBalancer

/// Match when private cluster is enabled on an AKS config's API access profile.
let private (|PrivateClusterEnabled|_|) =
    Option.bind (fun apiAccess ->
        match apiAccess.EnablePrivateCluster with
        | Some true -> Some PrivateClusterEnabled
        | _ -> None)

type AksBuilder() =
    member _.Yield _ = {
        Name = ResourceName.Empty
        Dependencies = Set.empty
        DependencyExpressions = Set.empty
        AddonProfiles = []
        AgentPools = []
        DnsPrefix = ""
        EnableRBAC = false
        Identity = ManagedIdentity.Empty
        IdentityProfile = None
        ApiServerAccessProfile = None
        LinuxProfile = None
        NetworkProfile = None
        OidcIssuerProfile = None
        SecurityProfile = None
        ServicePrincipalClientID = ""
        WindowsProfileAdminUserName = None
    }

    member _.Run(config: AksConfig) =
        match config.NetworkProfile, config.ApiServerAccessProfile with
        | BasicLoadBalancer, PrivateClusterEnabled ->
            invalidArg "sku" "Private cluster requires a standard SKU load balancer."
        | _ -> ()

        if String.IsNullOrWhiteSpace config.ServicePrincipalClientID then
            raiseFarmer
                "Missing ServicePrincipalClientID on ManagedCluster - specify 'service_principal_use_msi' or 'service_principal_client_id' to assign one."

        config

    /// Sets the name of the AKS cluster.
    [<CustomOperation "name">]
    member _.Name(state: AksConfig, name) = { state with Name = ResourceName name }

    /// Sets the DNS prefix of the AKS cluster.
    [<CustomOperation "dns_prefix">]
    member _.DnsPrefix(state: AksConfig, dns) = { state with DnsPrefix = dns }

    /// Enable Kubernetes Role-Based Access Control.
    [<CustomOperation "enable_rbac">]
    member _.EnableRBAC(state: AksConfig) = { state with EnableRBAC = true }

    interface IIdentity<AksConfig> with
        member _.Add state updater = {
            state with
                Identity = updater state.Identity
        }

    interface IDependable<AksConfig> with
        member _.Add state newDeps = {
            state with
                Dependencies = state.Dependencies + newDeps
        }

    [<CustomOperation "depends_on_expression">]
    member _.DependencyExpressions(state: AksConfig, dependencyExpr: ArmExpression) = {
        state with
            DependencyExpressions = state.DependencyExpressions.Add dependencyExpr
    }

    /// Adds agent pools to the AKS cluster.
    [<CustomOperation "add_agent_pools">]
    member _.AddAgentPools(state: AksConfig, pools) = {
        state with
            AgentPools = state.AgentPools @ pools
    }

    /// Adds an agent pool to the AKS cluster.
    [<CustomOperation "add_agent_pool">]
    member _.AddAgentPool(state: AksConfig, pool) = {
        state with
            AgentPools = state.AgentPools @ [ pool ]
    }

    member private _.enablePrivateCluster(state: AksConfig, enabled: bool) =
        let accessProfile =
            match state.ApiServerAccessProfile with
            | None -> {
                AuthorizedIPRanges = []
                EnablePrivateCluster = Some true
              }
            | Some profile -> {
                profile with
                    EnablePrivateCluster = Some true
              }

        {
            state with
                ApiServerAccessProfile = Some accessProfile
        }

    /// Enables a private cluster so it is not publicly accessible - only accessed from a virtual network.
    [<CustomOperation "enable_private_cluster">]
    member this.EnablePrivateCluster(state: AksConfig, enabled: bool) =
        this.enablePrivateCluster (state, enabled)

    [<CustomOperation "enable_private_cluster">]
    member this.EnablePrivateCluster(state: AksConfig) = this.enablePrivateCluster (state, true)

    /// Sets the range of Authorized IP addresses that can access the cluster's API server.
    [<CustomOperation "add_api_server_authorized_ip_ranges">]
    member _.AddApiServerAuthorizedIP(state: AksConfig, range: string list) =
        let accessProfile =
            match state.ApiServerAccessProfile with
            | None -> {
                AuthorizedIPRanges = range
                EnablePrivateCluster = None
              }
            | Some profile -> {
                profile with
                    AuthorizedIPRanges = profile.AuthorizedIPRanges @ range
              }

        {
            state with
                ApiServerAccessProfile = Some accessProfile
        }

    /// Enables any addons.
    [<CustomOperation "addons">]
    member _.Addons(state: AksConfig, addons: AddonConfig list) = { state with AddonProfiles = addons }

    /// Sets the kubelet identity for managing access to an Azure Container Registry
    [<CustomOperation "kubelet_identity">]
    member _.KubeletIdentity(state: AksConfig, identity: ResourceId) =
        match state.IdentityProfile with
        | None -> {
            state with
                IdentityProfile = Some { KubeletIdentity = Some identity }
          }
        | Some identityProfile -> {
            state with
                IdentityProfile =
                    Some {
                        identityProfile with
                            KubeletIdentity = Some identity
                    }
          }

    member this.KubeletIdentity(state: AksConfig, identity: UserAssignedIdentity.UserAssignedIdentityConfig) =
        this.KubeletIdentity(state, identity.ResourceId)

    /// Sets the network profile for the AKS cluster.
    [<CustomOperation "network_profile">]
    member _.NetworkProfile(state: AksConfig, networkProfile) = {
        state with
            NetworkProfile = Some networkProfile
    }

    /// Sets the linux profile for the AKS cluster.
    [<CustomOperation "linux_profile">]
    member _.LinuxProfile(state: AksConfig, username: string, sshKeys: string list) = {
        state with
            LinuxProfile = Some(username, sshKeys)
    }

    member this.LinuxProfile(state: AksConfig, username: string, sshKey: string) =
        this.LinuxProfile(state, username, [ sshKey ])

    /// Sets the client id of the service principal for the AKS cluster.
    [<CustomOperation "service_principal_client_id">]
    member _.ServicePrincipalClientID(state: AksConfig, clientId) = {
        state with
            ServicePrincipalClientID = clientId
    }

    /// Uses the managed identity of this resource for the service principal.
    [<CustomOperation "service_principal_use_msi">]
    member _.ServicePrincipalUseMsi(state: AksConfig) = {
        state with
            ServicePrincipalClientID = "msi"
    }

    /// Enables the AKS cluster to have an OIDC identity.
    [<CustomOperation "oidc_issuer">]
    member _.OidcIssuer(state: AksConfig, featureFlag) = {
        state with
            OidcIssuerProfile = Some { Enabled = featureFlag }
    }

    /// Enables Workload Identity for the AKS cluster.
    [<CustomOperation "enable_workload_identity">]
    member _.EnableWorkloadIdentity(state: AksConfig) = {
        state with
            // Workload identity uses the OIDC tokens issues by this cluster
            OidcIssuerProfile = Some { Enabled = Enabled }
            SecurityProfile =
                state.SecurityProfile
                |> Option.map (fun security -> {
                    security with
                        WorkloadIdentity = Some Enabled
                })
                |> Option.defaultValue {
                    SecurityProfileSettings.Default with
                        WorkloadIdentity = Some Enabled
                }
                |> Some
    }

    /// Enables Defender for the AKS cluster.
    member private _.enableDefender(state: AksConfig, defenderSettings) = {
        state with
            SecurityProfile =
                state.SecurityProfile
                |> Option.map (fun security -> {
                    security with
                        Defender = defenderSettings
                })
                |> Option.defaultValue {
                    SecurityProfileSettings.Default with
                        Defender = defenderSettings
                }
                |> Some
    }

    [<CustomOperation "enable_defender">]
    member this.EnableDefender(state: AksConfig, logAnalyticsResourceId: ResourceId) =
        let defenderSettings =
            Some {|
                SecurityMonitoring = Enabled
                LogAnalyticsResourceId = Some logAnalyticsResourceId
            |}

        this.enableDefender (state, defenderSettings)

    [<CustomOperation "enable_defender">]
    member this.EnableDefender(state: AksConfig) =
        let defenderSettings =
            Some {|
                SecurityMonitoring = Enabled
                LogAnalyticsResourceId = None
            |}

        this.enableDefender (state, defenderSettings)

    /// Enables image cleaner to remove unused images.
    member private _.enableImageCleaner(state: AksConfig, imageCleaner) = {
        state with
            SecurityProfile =
                state.SecurityProfile
                |> Option.map (fun security -> {
                    security with
                        ImageCleanerSettings = imageCleaner
                })
                |> Option.defaultValue {
                    SecurityProfileSettings.Default with
                        ImageCleanerSettings = imageCleaner
                }
                |> Some
    }

    /// Enables image cleaner to remove unused container images in the AKS cluster on a weekly interval.
    [<CustomOperation "enable_image_cleaner">]
    member this.EnableImageCleaner(state: AksConfig) =
        let imageCleaner =
            Some {|
                Enabled = Enabled
                Interval = TimeSpan.FromDays 7
            |}

        this.enableImageCleaner (state, imageCleaner)

    /// Enables image cleaner to remove unused container images in the AKS cluster on a custom interval (minimum 24 hours, max 90 days).
    [<CustomOperation "enable_image_cleaner">]
    member this.EnableImageCleaner(state: AksConfig, interval) =
        let interval =
            if interval < TimeSpan.FromHours 24 then
                TimeSpan.FromHours 24
            elif interval > TimeSpan.FromDays 90 then
                TimeSpan.FromDays 90
            else
                interval

        let imageCleaner =
            Some {|
                Enabled = Enabled
                Interval = interval
            |}

        this.enableImageCleaner (state, imageCleaner)

    /// Sets the windows admin username for the AKS cluster.
    [<CustomOperation "windows_username">]
    member _.WindowsUsername(state: AksConfig, username) = {
        state with
            WindowsProfileAdminUserName = Some username
    }

/// Builds an AKS cluster ARM resource definition
let aksBuilder = AksBuilder()
/// Container service is widely known as aks, so supporting that, too.
let aks = aksBuilder