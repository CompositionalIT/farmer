[<AutoOpen>]
module Farmer.Builders.ContainerService

open Farmer
open Farmer.Arm.ContainerService
open Farmer.CoreTypes
open Farmer.Vm

type AgentPoolConfig =
    { Name : ResourceName
      Count : int
      Mode : AgentPoolMode
      OsDiskSize : int<Gb>
      OsType : OS
      VmSize : VMSize
    }

type AksConfig =
    { Name : ResourceName
      AgentPools : AgentPoolConfig list
      DnsPrefix : string
      EnableRBAC : bool
      LinuxProfile : (string * string list) option
      ServicePrincipalClientID : string option
      WindowsProfileAdminUserName : string option
    }
    interface IBuilder with
        member this.DependencyName = this.Name
        member this.BuildResources location = [
            // VM itself
            { Name = this.Name
              Location = location
              DnsPrefix = this.DnsPrefix
              EnableRBAC = this.EnableRBAC
              AgentPoolProfiles = this.AgentPools |> List.map (fun agentPool ->
                  {| Name = agentPool.Name
                     Count = agentPool.Count
                     Mode = agentPool.Mode
                     OsDiskSize = agentPool.OsDiskSize
                     OsType = agentPool.OsType
                     VmSize = agentPool.VmSize |})
              LinuxProfile =
                  this.LinuxProfile
                  |> Option.map (fun (username, keys) -> {| AdminUserName = username; PublicKeys = keys |})
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
          Mode = System
          OsDiskSize = 0<Gb>
          OsType = OS.Linux
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
    /// Sets the OS type of the VM's in the agent pool.
    [<CustomOperation "os_type">]
    member _.OsType(state:AgentPoolConfig, os) = { state with OsType = os }
    /// Sets the size of the VM's in the agent pool.
    [<CustomOperation "vm_size">]
    member _.VmSize(state:AgentPoolConfig, size) = { state with VmSize = size }

/// Builds an AKS cluster agent pool ARM resource definition
let agentPool = AgentPoolBuilder()

/// Builds a Linux Profile from a username and list of ssh public keys
let make_linux_profile user sshKeys = user, sshKeys

type ContainerServiceBuilder() =
    member _.Yield _ =
        { Name = ResourceName.Empty
          AgentPools = []
          DnsPrefix = ""
          EnableRBAC = false
          LinuxProfile = None
          ServicePrincipalClientID = None
          WindowsProfileAdminUserName = None
        }
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
    /// Sets the linux profile for the AKS cluster.
    [<CustomOperation "linux_profile">]
    member _.LinuxProfile(state:AksConfig, username:string, sshKey:string) = { state with LinuxProfile = Some (username, [ sshKey ]) }
    member _.LinuxProfile(state:AksConfig, username:string, sshKeys:string list) = { state with LinuxProfile = Some (username, sshKeys) }
    /// Sets the client id of the service principal for the AKS cluster.
    [<CustomOperation "service_principal_client_id">]
    member _.ServicePrincipalClientID(state:AksConfig, clientId) = { state with ServicePrincipalClientID = Some clientId }
    /// Sets the windows admin username for the AKS cluster.
    [<CustomOperation "windows_username">]
    member _.WindowsUsername(state:AksConfig, username) = { state with WindowsProfileAdminUserName = Some username }

/// Builds an AKS cluster ARM resource definition
let containerService = ContainerServiceBuilder()
/// Container service is widely known as aks, so supporting that, too.
let aks = containerService
