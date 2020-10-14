[<AutoOpen>]
module Farmer.Builders.ContainerGroups

open Farmer
open Farmer.ContainerGroup
open Farmer.Arm.ContainerInstance
open Farmer.Arm.Network
open Farmer.CoreTypes

type volume_mount =
    static member empty_dir volumeName =
        volumeName, Volume.EmptyDirectory
    static member azureFile volumeName shareName (storageAccountName:string) =
        volumeName, Volume.AzureFileShare (ResourceName shareName, Storage.StorageAccountName.Create(storageAccountName).OkValue)
    static member git_repo volumeName repository =
        volumeName, Volume.GitRepo (repository, None, None)
    static member git_repo_directory volumeName  repository directory =
        volumeName, Volume.GitRepo (repository, Some directory, None)
    static member git_repo_directory_revision volumeName  repository directory revision =
        volumeName, Volume.GitRepo (repository, Some directory, Some revision)
    static member secret volumeName  (file:string) (secret:byte array) =
        volumeName, Volume.Secret [ SecretFile (file, secret) ]
    static member secrets volumeName  (secrets:(string * byte array) list) =
        volumeName, secrets |> List.map SecretFile |> Volume.Secret
    static member secret_string volumeName  (file:string) (secret:string) =
        volumeName, Volume.Secret [ SecretFile (file, secret |> System.Text.Encoding.UTF8.GetBytes) ]

/// Represents configuration for a single Container.
type ContainerInstanceConfig =
    { /// The name of the container instance
      Name : ResourceName
      /// The container instance image
      Image : string
      /// List of ports the container instance listens on
      Ports : Map<uint16, PortAccess>
      /// Max number of CPU cores the container instance may use
      Cpu : float
      /// Max gigabytes of memory the container instance may use
      Memory : float<Gb>
      /// Environment variables for the container
      EnvironmentVariables : Map<string, EnvVarValue>
      /// Volume mounts for the container
      VolumeMounts : Map<string, string> }

type ContainerGroupConfig =
    { /// The name of the container group.
      Name : ResourceName
      /// Container group OS.
      OperatingSystem : OS
      /// Restart policy for the container group.
      RestartPolicy : RestartPolicy
      /// Credentials for image registries used by containers in this group.
      ImageRegistryCredentials : ImageRegistryCredential list
      /// IP address for the container group.
      IpAddress : ContainerGroupIpAddress
      /// Name of the network profile for this container's group.
      NetworkProfile : ResourceName option
      /// The instances in this container group.
      Instances : ContainerInstanceConfig list
      /// Volumes to mount on the container group.
      Volumes : Map<string, Volume>
      Tags: Map<string,string>  }
    interface IBuilder with
        member this.DependencyName = this.Name
        member this.BuildResources location = [
            { Location = location
              Name = this.Name
              ContainerInstances = [
                for instance in this.Instances do
                    {| Name = instance.Name
                       Image = instance.Image
                       Ports = instance.Ports |> Map.toSeq |> Seq.map fst |> Set
                       Cpu = instance.Cpu
                       Memory = instance.Memory
                       EnvironmentVariables = instance.EnvironmentVariables
                       VolumeMounts = instance.VolumeMounts |}
              ]
              OperatingSystem = this.OperatingSystem
              RestartPolicy = this.RestartPolicy
              ImageRegistryCredentials = this.ImageRegistryCredentials
              IpAddress = this.IpAddress
              NetworkProfile = this.NetworkProfile
              Volumes = this.Volumes
              Tags = this.Tags }
        ]

type ContainerGroupBuilder() =
    member __.Yield _ =
        { Name = ResourceName.Empty
          OperatingSystem = Linux
          RestartPolicy = AlwaysRestart
          ImageRegistryCredentials = []
          IpAddress = { Type = PublicAddress; Ports = Set.empty }
          NetworkProfile = None
          Instances = []
          Volumes = Map.empty
          Tags = Map.empty }
    member this.Run (state:ContainerGroupConfig) =
        // Automatically apply all public-facing ports to the container group itself.
        state.Instances
        |> Seq.collect(fun i -> i.Ports |> Map.toSeq |> Seq.choose(function (port, PublicPort) -> Some port | _, InternalPort -> None))
        |> Seq.fold (fun (state:ContainerGroupConfig) port ->
            { state with
                IpAddress =
                    { state.IpAddress with
                        Ports = state.IpAddress.Ports.Add {| Protocol = TCP; Port = port |} } }) state

    member __.AddTcpPort(state:ContainerGroupConfig, port) = { state with IpAddress = { state.IpAddress with Ports = state.IpAddress.Ports.Add {| Protocol = TCP; Port = port |} } }

    [<CustomOperation "name">]
    /// Sets the name of the container group.
    member __.Name(state:ContainerGroupConfig, name) = { state with Name = name }
    member this.Name(state:ContainerGroupConfig, name) = this.Name(state, ResourceName name)
    /// Sets the OS type (default Linux)
    [<CustomOperation "operating_system">]
    member __.OsType(state:ContainerGroupConfig, os) = { state with OperatingSystem = os }
    /// Sets the restart policy (default Always)
    [<CustomOperation "restart_policy">]
    member __.RestartPolicy(state:ContainerGroupConfig, restartPolicy) = { state with RestartPolicy = restartPolicy }
    member private _.SetIpAddress(state:ContainerGroupConfig, ipAddressType, ports) =
        { state with
            IpAddress =
                { Type = ipAddressType
                  Ports = ports |> Seq.map(fun (prot, port) -> {| Protocol = prot; Port = port |}) |> Set } }

    /// Sets the IP addresss to a public address with a DNS label
    [<CustomOperation "public_dns">]
    member this.PublicDns(state, dnsLabel, ports) = this.SetIpAddress(state, PublicAddressWithDns dnsLabel, ports)
    /// Sets the IP addresss to a private address assigned by the vnet
    [<CustomOperation "private_ip">]
    member this.PrivateIp(state:ContainerGroupConfig, ports) = this.SetIpAddress(state, PrivateAddress, ports)
    /// Sets a network profile for the container's group.
    [<CustomOperation "network_profile">]
    member __.NetworkProfile(state:ContainerGroupConfig, networkProfileName:string) = { state with NetworkProfile = Some (ResourceName networkProfileName) }
    /// Adds a UDP port to be externally accessible
    [<CustomOperation "add_udp_port">]
    member __.AddUdpPort(state:ContainerGroupConfig, port) = { state with IpAddress = { state.IpAddress with Ports = state.IpAddress.Ports.Add {| Protocol = UDP; Port = port |} } }
    /// Adds container image registry credentials for images in this container group.
    [<CustomOperation "add_registry_credentials">]
    member _.AddRegistryCredentials(state:ContainerGroupConfig, credentials) =
        { state with ImageRegistryCredentials = state.ImageRegistryCredentials @ credentials }
    /// Adds a collection of container instances to this group
    [<CustomOperation "add_instances">]
    member __.AddInstances(state:ContainerGroupConfig, instances) = { state with Instances = state.Instances @ (Seq.toList instances) }
    [<CustomOperation "add_volumes">]
    /// Adds volumes to the container group so they can be mounted on containers.
    member __.AddVolumes(state:ContainerGroupConfig, volumes) =
        let newVolumes = volumes |> Map.ofSeq
        let updatedVolumes = state.Volumes |> Map.fold (fun current key vol -> Map.add key vol current) newVolumes
        { state with Volumes = updatedVolumes }
    [<CustomOperation "add_tags">]
    member _.Tags(state:ContainerGroupConfig, pairs) =
        { state with
            Tags = pairs |> List.fold (fun map (key,value) -> Map.add key value map) state.Tags }
    [<CustomOperation "add_tag">]
    member this.Tag(state:ContainerGroupConfig, key, value) = this.Tags(state, [ (key,value) ])

/// Creates an image registry credential with a generated SecureParameter for the password.
let registry (server:string) (username:string) =
    { Server = server
      Username = username
      Password = SecureParameter (sprintf "%s-password" server) }

type ContainerInstanceBuilder() =
    member __.Yield _ =
        { Name = ResourceName.Empty
          Image = ""
          Ports = Map.empty
          Cpu = 1.0
          Memory = 1.5<Gb>
          EnvironmentVariables = Map.empty
          VolumeMounts = Map.empty }
    /// Sets the name of the container instance.
    [<CustomOperation "name">]
    member __.Name(state:ContainerInstanceConfig, name) = { state with Name = name }
    member this.Name(state:ContainerInstanceConfig, name) = this.Name(state, ResourceName name)
    /// Sets the image of the container instance.
    [<CustomOperation "image">]
    member __.Image (state:ContainerInstanceConfig, image) = { state with Image = image }
    static member private AddPorts (state:ContainerInstanceConfig, accessibility, ports) =
        { state with
            Ports =
                ports
                |> Seq.fold(fun all port -> all.Add(port, accessibility) ) state.Ports }
    /// Sets the ports the container instance exposes. These will automatically be applied to the container group.
    [<CustomOperation "add_public_ports">]
    member __.PublicPorts (state:ContainerInstanceConfig, ports) = ContainerInstanceBuilder.AddPorts(state, PublicPort, ports)
    /// Sets the ports the container instance exposes. These will automatically be applied to the container group.
    [<CustomOperation "add_internal_ports">]
    member __.InternalPorts (state:ContainerInstanceConfig, ports) = ContainerInstanceBuilder.AddPorts(state, InternalPort, ports)
    /// Sets the ports the container instance exposes. These will automatically be applied to the container group.
    [<CustomOperation "add_ports">]
    member __.Ports (state:ContainerInstanceConfig, accessibility, ports) = ContainerInstanceBuilder.AddPorts(state, accessibility, ports)
    /// Sets the maximum CPU cores the container instance may use
    [<CustomOperationAttribute "cpu_cores">]
    member __.CpuCount (state:ContainerInstanceConfig, cpuCount:float) = { state with Cpu = cpuCount }
    member __.CpuCount (state:ContainerInstanceConfig, cpuCount:int) = { state with Cpu = float(cpuCount) }
    /// Sets the maximum gigabytes of memory the container instance may use
    [<CustomOperationAttribute "memory">]
    member __.Memory (state:ContainerInstanceConfig, memory) = { state with Memory = memory }
    [<CustomOperation "env_vars">]
    member __.EnvironmentVariables(state:ContainerInstanceConfig, envVars) =
        { state with EnvironmentVariables=Map.ofList envVars }
    /// Adds a volume mount to the container
    [<CustomOperation "add_volume_mount">]
    member __.AddVolumeMount (state:ContainerInstanceConfig, volumeName, mountPath) =
        { state with VolumeMounts = state.VolumeMounts |> Map.add volumeName mountPath }

let env_var (name:string) (value:string) = name, EnvValue value
let secure_env_var (name:string) (value:string) = name, EnvSecureValue value

let containerGroup = ContainerGroupBuilder()
let containerInstance = ContainerInstanceBuilder()

type ContainerNetworkInterfaceIpConfig = { Subnet : string }
type ContainerNetworkInterfaceConfiguration = { IpConfigs : ContainerNetworkInterfaceIpConfig list }

type NetworkProfileConfig =
    { Name : ResourceName
      ContainerNetworkInterfaceConfigurations : ContainerNetworkInterfaceConfiguration list
      VirtualNetwork : ResourceName
      Tags: Map<string,string>  }
    interface IBuilder with
        member this.DependencyName = this.Name
        member this.BuildResources location = [
            { Name = this.Name
              Location = location
              ContainerNetworkInterfaceConfigurations =
                this.ContainerNetworkInterfaceConfigurations
                |> List.map (fun ifconfig -> {| IpConfigs = (ifconfig.IpConfigs |> List.map (fun ipConfig -> {| SubnetName = ResourceName ipConfig.Subnet |})) |})
              VirtualNetwork = this.VirtualNetwork
              Tags = this.Tags }
        ]

type NetworkProfileBuilder() =
    member __.Yield _ =
        { Name = ResourceName.Empty
          ContainerNetworkInterfaceConfigurations = []
          VirtualNetwork = ResourceName.Empty
          Tags = Map.empty }
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
    [<CustomOperation "add_tags">]
    member _.Tags(state:NetworkProfileConfig, pairs) =
        { state with
            Tags = pairs |> List.fold (fun map (key,value) -> Map.add key value map) state.Tags }
    [<CustomOperation "add_tag">]
    member this.Tag(state:NetworkProfileConfig, key, value) = this.Tags(state, [ (key,value) ])

let networkProfile = NetworkProfileBuilder ()
