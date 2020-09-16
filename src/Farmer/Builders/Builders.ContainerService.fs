[<AutoOpen>]
module Farmer.Builders.ContainerService

open Farmer
open Farmer.Arm.ContainerService
open Farmer.CoreTypes
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

type NetworkProfileConfig =
    { NetworkPlugin : ContainerService.NetworkPlugin
      /// If no address is specified, this will use the 2nd address in the service address CIDR
      DnsServiceIP : System.Net.IPAddress option
      /// Usually the default 172.17.0.1/16 is acceptable.
      DockerBridgeCidr : IPAddressCidr
      /// Private IP address CIDR for services in the cluster which should not overlap with the vnet
      /// for the cluster or peer vnets. Defaults to 10.244.0.0/16.
      ServiceCidr : IPAddressCidr }

type AksConfig =
    { Name : ResourceName
      AgentPools : AgentPoolConfig list
      DnsPrefix : string
      EnableRBAC : bool
      LinuxProfile : (string * string list) option
      NetworkProfile : NetworkProfileConfig option
      ServicePrincipalClientID : string option
      WindowsProfileAdminUserName : string option }
    interface IBuilder with
        member this.DependencyName = this.Name
        member this.BuildResources location = [
            // VM itself
            { Name = this.Name
              Location = location
              DnsPrefix = this.DnsPrefix
              EnableRBAC = this.EnableRBAC
              AgentPoolProfiles =
                this.AgentPools
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
              LinuxProfile =
                  this.LinuxProfile
                  |> Option.map (fun (username, keys) -> {| AdminUserName = username; PublicKeys = keys |})
              NetworkProfile =
                  this.NetworkProfile
                  |> Option.map (fun netProfile ->
                        {| NetworkPlugin = netProfile.NetworkPlugin
                           DnsServiceIP =
                               netProfile.DnsServiceIP
                               |> Option.defaultWith (fun _ ->
                                    netProfile.ServiceCidr
                                    |> IPAddressCidr.addresses
                                    |> Seq.skip 2
                                    |> Seq.head)
                           DockerBridgeCidr = netProfile.DockerBridgeCidr
                           ServiceCidr = netProfile.ServiceCidr |})
              ServicePrincipalProfile =
                  this.ServicePrincipalClientID
                  |> Option.map (fun clientId -> {| ClientId = clientId; ClientSecret = SecureParameter (sprintf "client-secret-for-%s" this.Name.Value) |})
              WindowsProfile =
                  this.WindowsProfileAdminUserName
                  |> Option.map (fun username -> {| AdminUserName = username; AdminPassword = SecureParameter (sprintf "admin-password-for-%s" this.Name.Value) |})
            }
        ]

type AgentPoolBuilder() =
    member _.Yield _ =
        { Name = ResourceName.Empty
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

type AzureCniBuilder() =
    member _.Yield _ =
        { NetworkPlugin = ContainerService.NetworkPlugin.AzureCni
          DnsServiceIP = None
          DockerBridgeCidr = IPAddressCidr.parse "172.17.0.1/16"
          ServiceCidr = IPAddressCidr.parse "10.224.0.0/16" }
    member _.Run (config:NetworkProfileConfig) =
        { config with
            DnsServiceIP =
                config.DnsServiceIP
                |> Option.defaultWith (fun _ -> config.ServiceCidr |> IPAddressCidr.addresses |> Seq.skip 2 |> Seq.head)
                |> Some
        }
    /// Sets the docker bridge CIDR to a network other than the default 17.17.0.1/16.
    [<CustomOperation "docker_bridge">]
    member _.DockerBridge(state:NetworkProfileConfig, dockerBridge) = { state with DockerBridgeCidr = IPAddressCidr.parse dockerBridge }
    /// Sets the DNS service IP - must be within the service CIDR, default is the second address in the service CIDR.
    [<CustomOperation "dns_service">]
    member _.DnsServiceIP(state:NetworkProfileConfig, dnsIp) = { state with DnsServiceIP = System.Net.IPAddress.Parse dnsIp |> Some }
    /// Sets the service cidr to a network other than the default 10.224.0.0/16.
    [<CustomOperation "service_cidr">]
    member _.ServiceCidr(state:NetworkProfileConfig, serviceCidr) = { state with ServiceCidr = IPAddressCidr.parse serviceCidr }

let azureCniNetworkProfile = AzureCniBuilder()

/// Builds a Linux Profile from a username and list of ssh public keys
let make_linux_profile user sshKeys = user, sshKeys

type AksBuilder() =
    member _.Yield _ =
        { Name = ResourceName.Empty
          AgentPools = []
          DnsPrefix = ""
          EnableRBAC = false
          LinuxProfile = None
          NetworkProfile = None
          ServicePrincipalClientID = None
          WindowsProfileAdminUserName = None }
    /// Sets the name of the AKS cluster.
    [<CustomOperation "name">]
    member _.Name(state:AksConfig, name) = { state with Name = ResourceName name }
    /// Sets the DNS prefix of the AKS cluster.
    [<CustomOperation "dns_prefix">]
    member _.DnsPrefix(state:AksConfig, dns) = { state with DnsPrefix = dns }
    /// Enable Kubernetes Role-Based Access Control.
    [<CustomOperation "enable_rbac">]
    member _.EnableRBAC(state:AksConfig) = { state with EnableRBAC = true }
    /// Adds agent pools to the AKS cluster.
    [<CustomOperation "add_agent_pools">]
    member _.AddAgentPools(state:AksConfig, pools) = { state with AgentPools = state.AgentPools @ pools }
    /// Adds an agent pool to the AKS cluster.
    [<CustomOperation "add_agent_pool">]
    member _.AddAgentPool(state:AksConfig, pool) = { state with AgentPools = state.AgentPools @ [ pool ] }
    /// Sets the network profile for the AKS cluster.
    [<CustomOperation "network_profile">]
    member _.NetworkProfile(state:AksConfig, networkProfile) = { state with NetworkProfile = Some networkProfile }
    /// Sets the linux profile for the AKS cluster.
    [<CustomOperation "linux_profile">]
    member _.LinuxProfile(state:AksConfig, username:string, sshKeys:string list) = { state with LinuxProfile = Some (username, sshKeys) }
    member this.LinuxProfile(state:AksConfig, username:string, sshKey:string) = this.LinuxProfile(state, username, [ sshKey ])
    /// Sets the client id of the service principal for the AKS cluster.
    [<CustomOperation "service_principal_client_id">]
    member _.ServicePrincipalClientID(state:AksConfig, clientId) = { state with ServicePrincipalClientID = Some clientId }
    /// Sets the windows admin username for the AKS cluster.
    [<CustomOperation "windows_username">]
    member _.WindowsUsername(state:AksConfig, username) = { state with WindowsProfileAdminUserName = Some username }

/// Builds an AKS cluster ARM resource definition
let aksBuilder = AksBuilder()
/// Container service is widely known as aks, so supporting that, too.
let aks = aksBuilder