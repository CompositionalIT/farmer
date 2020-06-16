[<AutoOpen>]
module Farmer.Builders.ContainerGroups

open Farmer
open Farmer.CoreTypes
open Farmer.ContainerGroup
open Farmer.Arm.ContainerInstance
open Farmer.Arm.Network

/// Represents configuration for a single Container.
type ContainerConfig =
    { /// The name of the container
      Name : ResourceName
      /// The container image
      Image : string
      /// List of ports the container listens on
      Ports : uint16 list
      /// Max number of CPU cores the container may use
      Cpu : int
      /// Max gigabytes of memory the container may use
      Memory : float<Gb>

      /// The name of the container group.
      ContainerGroupName : ResourceRef
      /// Container group OS.
      OsType : OS
      /// Restart policy for the container group.
      RestartPolicy : RestartPolicy
      /// IP address for the container group.
      IpAddress : ContainerGroupIpAddress
      /// Name of the network profile for this container's group.
      NetworkProfile : ResourceName option }

    member this.Key = buildKey this.Name
    /// Gets the ARM expression path to the key of this container group.
    member this.GroupKey = buildKey this.ContainerGroupName.ResourceName
    /// Gets the name of the container group.
    member this.GroupName = this.ContainerGroupName.ResourceName
    interface IBuilder with
        member this.DependencyName = this.ContainerGroupName.ResourceName
        member this.BuildResources location existingResources = [
            let container =
                {| Name = this.Name
                   Image = this.Image
                   Ports = this.Ports
                   Cpu = this.Cpu
                   Memory = this.Memory |}
            match this.ContainerGroupName with
            | AutomaticallyCreated groupName ->
                { Location = location
                  Name = groupName
                  ContainerInstances = [ container ]
                  OsType = this.OsType.ToString()
                  RestartPolicy = this.RestartPolicy
                  IpAddress = this.IpAddress
                  NetworkProfile = this.NetworkProfile
                }
            | External containerGroupName ->
                existingResources
                |> Helpers.mergeResource containerGroupName (fun group -> { group with ContainerInstances = group.ContainerInstances @ [ container ] })
            | AutomaticPlaceholder ->
                failwith "Container Group Name has not been set."
        ]

type ContainerBuilder() =
    member __.Yield _ =
      { Name = ResourceName.Empty
        Image = ""
        Ports = []
        Cpu = 1
        Memory = 1.5<Gb>
        ContainerGroupName = AutomaticPlaceholder
        OsType = Linux
        RestartPolicy = Always
        IpAddress = { Type = PublicAddress; Ports = [] }
        NetworkProfile = None }
    member __.Run state =
        { state with
            ContainerGroupName =
                match state.ContainerGroupName.ResourceNameOpt with
                | Some _ -> state.ContainerGroupName
                | None -> AutomaticallyCreated (ResourceName (sprintf "%s-group" state.Name.Value))
        }
    /// Sets the name of the container instance
    [<CustomOperation "name">]
    member __.Name(state:ContainerConfig, name) = { state with Name = ResourceName name }
    /// Sets the container image.
    [<CustomOperation "image">]
    member __.Image (state:ContainerConfig, image) = { state with Image = image }
    /// Sets the ports the container exposes
    [<CustomOperation "ports">]
    member __.Ports (state:ContainerConfig, ports) = { state with Ports = ports }
    /// Sets the maximum CPU cores the container may use
    [<CustomOperationAttribute "cpu_cores">]
    member __.CpuCount (state:ContainerConfig, cpuCount) = { state with Cpu = cpuCount }
    /// Sets the maximum gigabytes of memory the container may use
    [<CustomOperationAttribute "memory">]
    member __.Memory (state:ContainerConfig, memory) = { state with Memory = memory }
    [<CustomOperation "group_name">]
    /// Sets the name of the container group.
    member __.GroupName(state:ContainerConfig, name) = { state with ContainerGroupName = AutomaticallyCreated name }
    member this.GroupName(state:ContainerConfig, name) = this.GroupName(state, ResourceName name)
    /// Links this container to an already-created container group.
    [<CustomOperation "link_to_container_group">]
    member __.LinkToGroup(state:ContainerConfig, group:ContainerConfig) = { state with ContainerGroupName = External group.GroupName }
    /// Sets the OS type (default Linux)
    [<CustomOperation "os_type">]
    member __.OsType(state:ContainerConfig, osType) = { state with OsType = osType }
    /// Sets the restart policy (default Always)
    [<CustomOperation "restart_policy">]
    member __.RestartPolicy(state:ContainerConfig, restartPolicy) = { state with RestartPolicy = restartPolicy }
    /// Sets the IP addresss (default Public)
    [<System.Obsolete("Prefer to use public_dns, private_ip, or private_static_ip")>]
    [<CustomOperation "ip_address">]
    member __.IpAddress(state:ContainerConfig, addressType, ports) = { state with IpAddress = { Type = addressType; Ports = ports |> Seq.map(fun (prot, port) -> {| Protocol = prot; Port = port |}) |> Seq.toList } }
    /// Sets the IP addresss to a public address with a DNS label
    [<CustomOperation "public_dns">]
    member __.PublicDns(state:ContainerConfig, dnsLabel:string, ports) = { state with IpAddress = { Type = PublicAddressWithDns dnsLabel; Ports = ports |> Seq.map(fun (prot, port) -> {| Protocol = prot; Port = port |}) |> Seq.toList } }
    /// Sets the IP addresss to a private address that is statically assigned
    [<CustomOperation "private_static_ip">]
    member __.PrivateStaticIp(state:ContainerConfig, ip:string, ports) = { state with IpAddress = { Type = PrivateAddressWithIp (System.Net.IPAddress.Parse ip); Ports = ports |> Seq.map(fun (prot, port) -> {| Protocol = prot; Port = port |}) |> Seq.toList } }
    /// Sets the IP addresss to a private address assigned by the vnet
    [<CustomOperation "private_ip">]
    member __.PrivateIp(state:ContainerConfig, ports) = { state with IpAddress = { Type = PrivateAddress; Ports = ports |> Seq.map(fun (prot, port) -> {| Protocol = prot; Port = port |}) |> Seq.toList } }
    /// Sets a network profile for the container's group.
    [<CustomOperation "network_profile">]
    member __.NetworkProfile(state:ContainerConfig, networkProfileName:string) = { state with NetworkProfile = Some (ResourceName networkProfileName) }
    /// Adds a TCP port to be externally accessible
    [<CustomOperation "add_tcp_port">]
    member __.AddTcpPort(state:ContainerConfig, port) = { state with IpAddress = { state.IpAddress with Ports = {| Protocol= TCP; Port = port |} :: state.IpAddress.Ports } }
    /// Adds a UDP port to be externally accessible
    [<CustomOperation "add_udp_port">]
    member __.AddUdpPort(state:ContainerConfig, port) = { state with IpAddress = { state.IpAddress with Ports = {| Protocol= UDP; Port = port |} :: state.IpAddress.Ports } }
let container = ContainerBuilder()

type ContainerNetworkInterfaceIpConfig =
    {
        Subnet : string
    }
type ContainerNetworkInterfaceConfiguration =
    {
        IpConfigs : ContainerNetworkInterfaceIpConfig list
    }
type NetworkProfileConfig =
    {
        Name : ResourceName
        ContainerNetworkInterfaceConfigurations : ContainerNetworkInterfaceConfiguration list
        VirtualNetwork : ResourceName
    }
    interface IBuilder with
        member this.DependencyName = this.Name
        member this.BuildResources location _ = [
            { Name = this.Name
              Location = location
              ContainerNetworkInterfaceConfigurations =
                  this.ContainerNetworkInterfaceConfigurations
                  |> List.map (fun ifconfig -> {| IpConfigs = (ifconfig.IpConfigs |> List.map (fun ipConfig -> {| SubnetName = ResourceName ipConfig.Subnet |})) |})
              VirtualNetwork = this.VirtualNetwork
            }
        ]

type NetworkProfileBuilder() =
    member __.Yield _ =
      { Name = ResourceName.Empty
        ContainerNetworkInterfaceConfigurations = []
        VirtualNetwork = ResourceName.Empty }
    /// Sets the name of the network profile instance
    [<CustomOperation "name">]
    member __.Name(state:NetworkProfileConfig, name) = { state with Name = ResourceName name }
    /// Sets a single target subnet for the network profile (typical case of single subnet)
    [<CustomOperation "subnet">]
    member __.SubnetName(state:NetworkProfileConfig, subnet) = { state with ContainerNetworkInterfaceConfigurations = [ { IpConfigs = [ { Subnet = subnet } ] } ] }
    /// Sets a single target subnet for the network profile (typical case of single subnet)
    [<CustomOperation "add_ip_configs">]
    member __.AddIpConfigs(state:NetworkProfileConfig, configs) = { state with ContainerNetworkInterfaceConfigurations = state.ContainerNetworkInterfaceConfigurations @ configs }
    /// Sets the virtual network for the profile
    [<CustomOperation "vnet">]
    member __.VirtualNetwork(state:NetworkProfileConfig, vnet) = { state with VirtualNetwork = ResourceName vnet }

let networkProfile = NetworkProfileBuilder ()
