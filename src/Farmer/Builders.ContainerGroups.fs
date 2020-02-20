[<AutoOpen>]
module Farmer.Resources.ContainerGroups

open Farmer
open Farmer.Models
open Farmer.Models.ContainerGroups

/// Represents configuration for a single Container.
type ContainerInstanceConfig =
    { /// The name of the container
      Name : ResourceName
      /// The container image
      Image : string
      /// List of ports the container listens on
      Ports : uint16 list
      /// Max number of CPU cores the container may use
      Cpu : int
      /// Max gigabytes of memory the container may use
      Memory : float<Gb> }
    member this.Key = buildKey this.Name

type ContainerInstanceBuilder() =
    member _.Yield _ = { Name = ResourceName.Empty; Image = ""; Ports = []; Cpu = 1; Memory = 1.5<Gb> }
    /// Sets the name of the container instance
    [<CustomOperation "name">]
    member _.Name(state:ContainerInstanceConfig, name) = { state with Name = ResourceName name }
    /// Sets the container image.
    [<CustomOperation "image">]
    member _.Image (state:ContainerInstanceConfig, image) = { state with Image = image }
    /// Sets the ports the container exposes
    [<CustomOperation "ports">]
    member _.Ports (state:ContainerInstanceConfig, ports) = { state with Ports = ports }
    /// Sets the maximum CPU cores the container may use
    [<CustomOperationAttribute "cpu">]
    member _.CpuCount (state:ContainerInstanceConfig, cpuCount) = { state with Cpu = cpuCount }
    /// Sets the maximum gigabytes of memory the container may use
    [<CustomOperationAttribute "memory">]
    member _.Memory (state:ContainerInstanceConfig, memory) = { state with Memory = memory }
let containerInstance = ContainerInstanceBuilder()

/// Represents configuration on a group of Azure Containers.
type ContainerGroupConfig =
    { /// The name of the container group.
      Name : ResourceName
      /// Container group OS.
      OsType : ContainerGroupOsType
      /// Container instances for the container group.
      ContainerInstances : ContainerInstanceConfig list
      /// Restart policy for the container group.
      RestartPolicy : ContainerGroupRestartPolicy
      /// IP address for the container group.
      IpAddress : ContainerGroupIpAddress }

    /// Gets the ARM expression path to the key of this container group.
    member this.Key = buildKey this.Name

type ContainerGroupBuilder() =
    member _.Yield _ =
        { Name = ResourceName.Empty
          ContainerInstances = []
          OsType = ContainerGroupOsType.Linux
          RestartPolicy = ContainerGroupRestartPolicy.Always
          IpAddress = { Type = ContainerGroupIpAddressType.PublicAddress; Ports = [] }
        }
    [<CustomOperation "name">]
    /// Sets the name of the container group.
    member _.Name(state:ContainerGroupConfig, name) = { state with Name = name }
    member this.Name(state:ContainerGroupConfig, name) = this.Name(state, ResourceName name)
    [<CustomOperation "add_container">]
    /// Adds a single container instance.
    member _.AddContainer(state:ContainerGroupConfig, instance) = { state with ContainerInstances = instance :: state.ContainerInstances }
    /// Adds container instances.
    [<CustomOperation "add_containers">]
    member _.AddContainers(state:ContainerGroupConfig, instances) = { state with ContainerInstances = instances @ state.ContainerInstances }
    /// Sets the OS type (default Linux)
    [<CustomOperation "os_type">]
    member _.OsType(state:ContainerGroupConfig, osType) = { state with OsType = osType }
    /// Sets the restart policy (default Always)
    [<CustomOperation "restart_policy">]
    member _.RestartPolicy(state:ContainerGroupConfig, restartPolicy) = { state with RestartPolicy = restartPolicy }
    /// Sets the IP addresss (default Public)
    [<CustomOperation "ip_address">]
    member _.IpAddress(state:ContainerGroupConfig, ipAddress) = { state with IpAddress = ipAddress }
    /// Adds a TCP port to be externally accessible
    [<CustomOperation "add_tcp_port">]
    member _.AddTcpPort(state:ContainerGroupConfig, port) = { state with IpAddress = { state.IpAddress with Ports = { Protocol=System.Net.Sockets.ProtocolType.Tcp; Port = port } :: state.IpAddress.Ports } }
    /// Adds a UDP port to be externally accessible
    [<CustomOperation "add_udp_port">]
    member _.AddUdpPort(state:ContainerGroupConfig, port) = { state with IpAddress = { state.IpAddress with Ports = { Protocol=System.Net.Sockets.ProtocolType.Udp; Port = port } :: state.IpAddress.Ports } }

module Converters =
    let containerInstance (config:ContainerInstanceConfig) : ContainerInstance =
        { Name = config.Name
          Image = config.Image
          Ports = config.Ports
          Resources =
            { Cpu = config.Cpu
              Memory = config.Memory } }
    let containerGroup location (config:ContainerGroupConfig) : ContainerGroup =
        { Location = location
          Name = config.Name
          ContainerInstances = config.ContainerInstances |> List.map containerInstance
          OsType = config.OsType
          RestartPolicy = config.RestartPolicy
          IpAddress = config.IpAddress }

/// Represents a group of Azure Containers.
let containerGroup = ContainerGroupBuilder()