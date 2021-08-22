[<AutoOpen>]
module Farmer.Builders.ContainerService

open System
open Farmer
open Farmer.Arm
open Farmer.Identity
open Farmer.Vm

type AgentPoolConfig =
    { Name : ResourceName
      Count : int
      MaxPods : int option
      Mode : AgentPoolMode
      OsDiskSize : int<Gb>
      OsType : OS
      VmSize : VMSize
      VirtualNetworkName : ResourceName option
      SubnetName : ResourceName option }
    static member Default = {
            Name = ResourceName.Empty
            Count = 1
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
type ApiServerAccessProfileConfig =
    { AuthorizedIPRanges : string list
      EnablePrivateCluster : bool option }

type NetworkProfileConfig =
    { NetworkPlugin : ContainerService.NetworkPlugin option
      /// If no address is specified, this will use the 2nd address in the service address CIDR
      DnsServiceIP : System.Net.IPAddress option
      /// Usually the default 172.17.0.1/16 is acceptable.
      DockerBridgeCidr : IPAddressCidr option
      /// Load balancer SKU (defaults to basic)
      LoadBalancerSku : LoadBalancer.Sku option
      /// Private IP address CIDR for services in the cluster which should not overlap with the vnet
      /// for the cluster or peer vnets. Defaults to 10.244.0.0/16.
      ServiceCidr : IPAddressCidr option }

type AksConfig =
    { Name : ResourceName
      AgentPools : AgentPoolConfig list
      DnsPrefix : string
      EnableRBAC : bool
      Identity : ManagedIdentity
      ApiServerAccessProfile : ApiServerAccessProfileConfig option
      LinuxProfile : (string * string list) option
      NetworkProfile : NetworkProfileConfig option
      ServicePrincipalClientID : string
      WindowsProfileAdminUserName : string option }
    member private this.ResourceId = managedClusters.resourceId this.Name
    member this.SystemIdentity = SystemIdentity this.ResourceId
    interface IBuilder with
        member this.ResourceId = this.ResourceId
        member this.BuildResources location = [
            // VM itself
            { Name = this.Name
              Location = location
              DnsPrefix =
                  if String.IsNullOrWhiteSpace this.DnsPrefix then
                      String.Format("{0}-{1:x}", this.Name.Value, this.Name.Value.GetHashCode())
                  else this.DnsPrefix
              EnableRBAC = this.EnableRBAC
              Identity = this.Identity
              AgentPoolProfiles =
                match this.AgentPools with
                | [] -> [ { AgentPoolConfig.Default with Count = 3 } ]
                | agentPools -> agentPools
                |> List.map (fun agentPool ->
                    {| Name = agentPool.Name
                       Count = agentPool.Count
                       MaxPods = agentPool.MaxPods
                       Mode = agentPool.Mode
                       OsDiskSize = agentPool.OsDiskSize
                       OsType = agentPool.OsType
                       SubnetName = agentPool.SubnetName
                       VmSize = agentPool.VmSize
                       VirtualNetworkName = agentPool.VirtualNetworkName |})
              ApiServerAccessProfile =
                  this.ApiServerAccessProfile
                  |> Option.map (fun apiAccess ->
                      {| AuthorizedIPRanges = apiAccess.AuthorizedIPRanges
                         EnablePrivateCluster = apiAccess.EnablePrivateCluster |})
              LinuxProfile =
                  this.LinuxProfile
                  |> Option.map (fun (username, keys) -> {| AdminUserName = username; PublicKeys = keys |})
              NetworkProfile =
                  this.NetworkProfile
                  |> Option.map (fun netProfile ->
                        {| NetworkPlugin = netProfile.NetworkPlugin
                           DnsServiceIP =
                               match netProfile.DnsServiceIP with
                               | Some ip -> Some ip
                               | None ->
                                    netProfile.ServiceCidr |> Option.map
                                        (IPAddressCidr.addresses >> Seq.skip 2 >> Seq.head)
                           DockerBridgeCidr = netProfile.DockerBridgeCidr
                           LoadBalancerSku = netProfile.LoadBalancerSku
                           ServiceCidr = netProfile.ServiceCidr |})
              ServicePrincipalProfile =
                  {| ClientId = this.ServicePrincipalClientID
                     ClientSecret =
                         match this.ServicePrincipalClientID with
                         | "msi" -> None
                         | _ -> Some (SecureParameter $"client-secret-for-{this.Name.Value}") |}
              WindowsProfile =
                  this.WindowsProfileAdminUserName
                  |> Option.map (fun username -> {| AdminUserName = username; AdminPassword = SecureParameter $"admin-password-for-{this.Name.Value}" |})
            }
        ]

type AgentPoolBuilder() =
    member _.Yield _ = AgentPoolConfig.Default
    /// Sets the name of the agent pool.
    [<CustomOperation "name">]
    member _.Name(state:AgentPoolConfig, name) = { state with Name = ResourceName name }
    /// Sets the count of VM's in the agent pool.
    [<CustomOperation "count">]
    member _.Count(state:AgentPoolConfig, count) = { state with Count = count }
    /// Sets the agent pool to user mode.
    [<CustomOperation "user_mode">]
    member _.UserMode(state:AgentPoolConfig) = { state with Mode = User }
    /// Sets the disk size for the VM's in the agent pool.
    [<CustomOperation "disk_size">]
    member _.DiskSizeGB(state:AgentPoolConfig, size) = { state with OsDiskSize = size }
    [<CustomOperation "max_pods">]
    member _.MaxPods(state:AgentPoolConfig, maxPods) = { state with MaxPods = maxPods }
    /// Sets the OS type of the VM's in the agent pool.
    [<CustomOperation "os_type">]
    member _.OsType(state:AgentPoolConfig, os) = { state with OsType = os }
    /// Sets the name of a virtual network subnet where this AKS cluster should be attached.
    [<CustomOperation "subnet">]
    member _.SubnetName(state:AgentPoolConfig, subnetName) = { state with SubnetName = Some (ResourceName subnetName) }
    /// Sets the size of the VM's in the agent pool.
    [<CustomOperation "vm_size">]
    member _.VmSize(state:AgentPoolConfig, size) = { state with VmSize = size }
    /// Sets the name of a virtual network in the same region where this AKS cluster should be attached.
    [<CustomOperation "vnet">]
    member _.VNetName(state:AgentPoolConfig, vnetName) = { state with VirtualNetworkName = Some (ResourceName vnetName) }

/// Builds an AKS cluster agent pool ARM resource definition
let agentPool = AgentPoolBuilder()

type NetworkProfileBuilder () =
    /// Sets the SKU to be used for the load balancer.
    [<CustomOperation "load_balancer_sku">]
    member _.LoadBalancerSku(state:NetworkProfileConfig, sku:LoadBalancer.Sku) = { state with LoadBalancerSku = Some sku }

/// Builds a configuration for using the Azure CNI plugin.
type KubenetBuilder() =
    inherit NetworkProfileBuilder()
    member _.Yield _ =
        { NetworkPlugin = Some ContainerService.NetworkPlugin.Kubenet
          LoadBalancerSku = None
          DnsServiceIP = None
          DockerBridgeCidr = None
          ServiceCidr = None }

let kubenetNetworkProfile = KubenetBuilder()

/// Builds a configuration for using the Azure CNI plugin.
type AzureCniBuilder() =
    inherit NetworkProfileBuilder()
    member _.Yield _ =
        { NetworkPlugin = Some ContainerService.NetworkPlugin.AzureCni
          LoadBalancerSku = None
          DnsServiceIP = None
          DockerBridgeCidr = IPAddressCidr.parse "172.17.0.1/16" |> Some
          ServiceCidr = IPAddressCidr.parse "10.224.0.0/16" |> Some }
    member _.Run (config:NetworkProfileConfig) =
        { config with
            DnsServiceIP =
               match config.DnsServiceIP with
               | Some ip -> Some ip
               | None ->
                    config.ServiceCidr |> Option.map
                        (IPAddressCidr.addresses >> Seq.skip 2 >> Seq.head)
        }
    /// Sets the docker bridge CIDR to a network other than the default 17.17.0.1/16.
    [<CustomOperation "docker_bridge">]
    member _.DockerBridge(state:NetworkProfileConfig, dockerBridge) = { state with DockerBridgeCidr = IPAddressCidr.parse dockerBridge |> Some }
    /// Sets the DNS service IP - must be within the service CIDR, default is the second address in the service CIDR.
    [<CustomOperation "dns_service">]
    member _.DnsServiceIP(state:NetworkProfileConfig, dnsIp:string) = { state with DnsServiceIP = System.Net.IPAddress.Parse dnsIp |> Some }
    /// Sets the service cidr to a network other than the default 10.224.0.0/16.
    [<CustomOperation "service_cidr">]
    member _.ServiceCidr(state:NetworkProfileConfig, serviceCidr) = { state with ServiceCidr = IPAddressCidr.parse serviceCidr |> Some }

let azureCniNetworkProfile = AzureCniBuilder()

/// Builds a Linux Profile from a username and list of ssh public keys
let makeLinuxProfile user sshKeys = user, sshKeys

/// Match on type of load balancer for an AKS config's network profile.
/// The default if nothing is specified is a Standard LB.
let private (|BasicLoadBalancer|StandardLoadBalancer|) = function
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
        | _ -> None
    )

type AksBuilder() =
    member _.Yield _ =
        { Name = ResourceName.Empty
          AgentPools = []
          DnsPrefix = ""
          EnableRBAC = false
          Identity = ManagedIdentity.Empty
          ApiServerAccessProfile = None
          LinuxProfile = None
          NetworkProfile = None
          ServicePrincipalClientID = ""
          WindowsProfileAdminUserName = None }
    member _.Run (config:AksConfig) =
        match config.NetworkProfile, config.ApiServerAccessProfile with
        | BasicLoadBalancer, PrivateClusterEnabled ->
            invalidArg "sku" "Private cluster requires a standard SKU load balancer."
        | _ -> ()
        if String.IsNullOrWhiteSpace config.ServicePrincipalClientID then
            raiseFarmer "Missing ServicePrincipalClientID on ManagedCluster - specify 'service_principal_use_msi' or 'service_principal_client_id' to assign one."
        config
    /// Sets the name of the AKS cluster.
    [<CustomOperation "name">]
    member _.Name(state:AksConfig, name) = { state with Name = ResourceName name }
    /// Sets the DNS prefix of the AKS cluster.
    [<CustomOperation "dns_prefix">]
    member _.DnsPrefix(state:AksConfig, dns) = { state with DnsPrefix = dns }
    /// Enable Kubernetes Role-Based Access Control.
    [<CustomOperation "enable_rbac">]
    member _.EnableRBAC(state:AksConfig) = { state with EnableRBAC = true }
    /// Sets the managed identity on this cluster.
    interface IIdentity<AksConfig> with member _.Add state updater = { state with Identity = updater state.Identity }
    /// Adds agent pools to the AKS cluster.
    [<CustomOperation "add_agent_pools">]
    member _.AddAgentPools(state:AksConfig, pools) = { state with AgentPools = state.AgentPools @ pools }
    /// Adds an agent pool to the AKS cluster.
    [<CustomOperation "add_agent_pool">]
    member _.AddAgentPool(state:AksConfig, pool) = { state with AgentPools = state.AgentPools @ [ pool ] }
    /// Enables a private cluster so it is not publicly accessible - only accessed from a virtual network.
    [<CustomOperation "enable_private_cluster">]
    member _.EnablePrivateCluster(state:AksConfig, enabled:bool) =
        let accessProfile =
            match state.ApiServerAccessProfile with
            | None -> { AuthorizedIPRanges = []; EnablePrivateCluster = Some true }
            | Some profile -> { profile with EnablePrivateCluster = Some true }
        { state with ApiServerAccessProfile = Some accessProfile }
    /// Sets the range of Authorized IP addresses that can access the cluster's API server.
    [<CustomOperation "add_api_server_authorized_ip_ranges">]
    member _.AddApiServerAuthorizedIP(state:AksConfig, range:string list) =
        let accessProfile =
            match state.ApiServerAccessProfile with
            | None -> { AuthorizedIPRanges = range; EnablePrivateCluster = None }
            | Some profile -> { profile with AuthorizedIPRanges = profile.AuthorizedIPRanges @ range }
        { state with ApiServerAccessProfile = Some accessProfile }
    /// Sets the network profile for the AKS cluster.
    [<CustomOperation "network_profile">]
    member _.NetworkProfile(state:AksConfig, networkProfile) = { state with NetworkProfile = Some networkProfile }
    /// Sets the linux profile for the AKS cluster.
    [<CustomOperation "linux_profile">]
    member _.LinuxProfile(state:AksConfig, username:string, sshKeys:string list) = { state with LinuxProfile = Some (username, sshKeys) }
    member this.LinuxProfile(state:AksConfig, username:string, sshKey:string) = this.LinuxProfile(state, username, [ sshKey ])
    /// Sets the client id of the service principal for the AKS cluster.
    [<CustomOperation "service_principal_client_id">]
    member _.ServicePrincipalClientID(state:AksConfig, clientId) = { state with ServicePrincipalClientID = clientId }
    /// Uses the managed identity of this resource for the service principal.
    [<CustomOperation "service_principal_use_msi">]
    member _.ServicePrincipalUseMsi(state:AksConfig) = { state with ServicePrincipalClientID = "msi" }
    /// Sets the windows admin username for the AKS cluster.
    [<CustomOperation "windows_username">]
    member _.WindowsUsername(state:AksConfig, username) = { state with WindowsProfileAdminUserName = Some username }

/// Builds an AKS cluster ARM resource definition
let aksBuilder = AksBuilder()
/// Container service is widely known as aks, so supporting that, too.
let aks = aksBuilder