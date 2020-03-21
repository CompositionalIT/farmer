[<AutoOpen>]
module Farmer.Resources.ContainerGroups

open Farmer
open Farmer.Models
open Farmer.Models.ContainerGroups

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
      OsType : ContainerGroupOsType
      /// Restart policy for the container group.
      RestartPolicy : ContainerGroupRestartPolicy
      /// IP address for the container group.
      IpAddress : ContainerGroupIpAddress }

    member this.Key = buildKey this.Name
    /// Gets the ARM expression path to the key of this container group.
    member this.GroupKey = buildKey this.ContainerGroupName.ResourceName
    /// Gets the name of the container group.
    member this.GroupName = this.ContainerGroupName.ResourceName

type ContainerBuilder() =
    member __.Yield _ =
      { Name = ResourceName.Empty
        Image = ""
        Ports = []
        Cpu = 1
        Memory = 1.5<Gb>
        ContainerGroupName = AutomaticPlaceholder
        OsType = ContainerGroupOsType.Linux
        RestartPolicy = ContainerGroupRestartPolicy.Always
        IpAddress = { Type = ContainerGroupIpAddressType.PublicAddress; Ports = [] } }
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
    member __.LinkToGroup(state:ContainerConfig, name) = { state with ContainerGroupName = External name }
    /// Sets the OS type (default Linux)
    [<CustomOperation "os_type">]
    member __.OsType(state:ContainerConfig, osType) = { state with OsType = osType }
    /// Sets the restart policy (default Always)
    [<CustomOperation "restart_policy">]
    member __.RestartPolicy(state:ContainerConfig, restartPolicy) = { state with RestartPolicy = restartPolicy }
    /// Sets the IP addresss (default Public)
    [<CustomOperation "ip_address">]
    member __.IpAddress(state:ContainerConfig, addressType, ports) = { state with IpAddress = { Type = addressType; Ports = ports |> Seq.map(fun (prot, port) -> { ContainerPort.Protocol = prot; ContainerPort.Port = port }) |> Seq.toList } }
    /// Adds a TCP port to be externally accessible
    [<CustomOperation "add_tcp_port">]
    member __.AddTcpPort(state:ContainerConfig, port) = { state with IpAddress = { state.IpAddress with Ports = { Protocol= TCP; Port = port } :: state.IpAddress.Ports } }
    /// Adds a UDP port to be externally accessible
    [<CustomOperation "add_udp_port">]
    member __.AddUdpPort(state:ContainerConfig, port) = { state with IpAddress = { state.IpAddress with Ports = { Protocol= UDP; Port = port } :: state.IpAddress.Ports } }
let container = ContainerBuilder()

module Converters =
    type ContainerCommand = New of ContainerGroup | Replace of old:ContainerGroup * newC:ContainerGroup
    let containerInstance location (existingGroups:ContainerGroup list) (config:ContainerConfig) : ContainerCommand option =
        let container : ContainerInstance =
            { Name = config.Name
              Image = config.Image
              Ports = config.Ports
              Resources =
                { Cpu = config.Cpu
                  Memory = config.Memory } }
        match config.ContainerGroupName with
        | AutomaticallyCreated groupName ->
            { ContainerGroup.Location = location
              ContainerGroup.Name = groupName
              ContainerGroup.ContainerInstances = [ container ]
              ContainerGroup.OsType = config.OsType
              ContainerGroup.RestartPolicy = config.RestartPolicy
              ContainerGroup.IpAddress = config.IpAddress }
            |> New
            |> Some
        | External name ->
            existingGroups
            |> List.tryFind(fun g -> g.Name = name)
            |> Option.map(fun group -> Replace(group, { group with ContainerInstances = container :: group.ContainerInstances }))
        | AutomaticPlaceholder ->
            None

type ArmBuilder.ArmBuilder with
    member this.AddResource(state:ArmConfig, config:ContainerConfig) =
        let existingContainerGroups =
            state.Resources
            |> List.choose(function ContainerGroup g -> Some g | _ -> None)
        let group = Converters.containerInstance state.Location existingContainerGroups config
        match group with
        | Some (Converters.New group) ->
            { state with Resources = ContainerGroup group :: state.Resources }
        | Some (Converters.Replace(oldG, newG)) ->
            { state with Resources = ContainerGroup newG :: (state.Resources |> List.filter ((<>) (ContainerGroup oldG))) }
        | None ->
            printfn "WARNING: Could not locate the linked container group ('%s'). Make sure you have correctly specified the name, and that it was added to the arm { } builder before this one." config.ContainerGroupName.ResourceName.Value
            state
    member this.AddResources (state, configs) = addResources<ContainerConfig> this.AddResource state configs