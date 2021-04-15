[<AutoOpen>]
module Farmer.Builders.ContainerGroups

open Farmer
open Farmer.ContainerGroup
open Farmer.Identity
open Farmer.Arm.ContainerInstance
open Farmer.Arm.Network
open System.Text

//TODO: I think we should rename these to standard F# naming conventioned e.g. VolumeMount, EmptyDir etc.
//TODO: Indeed, this should either be made into a module with let-bound functions, or make use of static
//members by using e.g. optional parameters or overloading.
type volume_mount =
    static member empty_dir volumeName =
        volumeName, Volume.EmptyDirectory
    static member azureFile volumeName shareName (storageAccountName:string) =
        volumeName, Volume.AzureFileShare (ResourceName shareName, Storage.StorageAccountName.Create(storageAccountName).OkValue)
    static member git_repo volumeName repository =
        volumeName, Volume.GitRepo (repository, None, None)
    static member git_repo_directory volumeName  repository directory =
        volumeName, Volume.GitRepo (repository, Some directory, None)
    static member git_repo_directory_revision volumeName repository directory revision =
        volumeName, Volume.GitRepo (repository, Some directory, Some revision)
    static member secret volumeName  (file:string) (secret:byte array) =
        volumeName, Volume.Secret [ SecretFileContents (file, secret) ]
    static member secrets volumeName  (secrets:(string * byte array) list) =
        volumeName, secrets |> List.map SecretFileContents |> Volume.Secret
    static member secret_string volumeName  (file:string) (secret:string) =
        volumeName, Volume.Secret [ SecretFileContents (file, Encoding.UTF8.GetBytes secret) ]
    static member secret_parameter volumeName  (file:string) (secretParameterName:string) =
        volumeName, Volume.Secret [ SecretFileParameter (file, SecureParameter secretParameterName) ]

/// Represents configuration for a single Container.
type ContainerInstanceConfig =
    { /// The name of the container instance
      Name : ResourceName
      /// The container instance image
      Image : string
      /// The commands to execute within the container instance in exec form
      Command : string list
      /// List of ports the container instance listens on
      Ports : Map<uint16, PortAccess>
      /// Max number of CPU cores the container instance may use
      Cpu : float
      /// Max gigabytes of memory the container instance may use
      Memory : float<Gb>
      /// Environment variables for the container
      EnvironmentVariables : Map<string, EnvVar>
      /// Liveliness probe for checking the container's health.
      LivelinessProbe : ContainerProbe option
      /// Readiness probe to wait for the container to be ready to accept requests.
      ReadinessProbe : ContainerProbe option
      /// Volume mounts for the container
      VolumeMounts : Map<string, string> }

/// Represents configuration for an init container that runs on container group startup.
type InitContainerConfig =
    { /// The name of the container instance
      Name : ResourceName
      /// The container instance image
      Image : string
      /// The commands to execute within the container instance in exec form
      Command : string list
      /// Environment variables for the container
      EnvironmentVariables : Map<string, EnvVar>
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
      IpAddress : ContainerGroupIpAddress option
      /// Name of the network profile for this container's group.
      NetworkProfile : ResourceName option
      /// The init containers in this container group.
      InitContainers : InitContainerConfig list
      /// The instances in this container group.
      Instances : ContainerInstanceConfig list
      /// Volumes to mount on the container group.
      Volumes : Map<string, Volume>
      /// Managed identity for the container group.
      Identity : ManagedIdentity
      /// Tags for the container group.
      Tags: Map<string,string> }
    member private this.ResourceId = containerGroups.resourceId this.Name
    member this.SystemIdentity = SystemIdentity this.ResourceId
    interface IBuilder with
        member this.ResourceId = this.ResourceId
        member this.BuildResources location = [
            { Location = location
              Name = this.Name
              ContainerInstances = [
                for instance in this.Instances do
                    {| Name = instance.Name
                       Image = instance.Image
                       Command = instance.Command
                       Ports = instance.Ports |> Map.toSeq |> Seq.map fst |> Set
                       Cpu = instance.Cpu
                       Memory = instance.Memory
                       EnvironmentVariables = instance.EnvironmentVariables
                       LivelinessProbe = instance.LivelinessProbe
                       ReadinessProbe = instance.ReadinessProbe
                       VolumeMounts = instance.VolumeMounts |}
              ]
              OperatingSystem = this.OperatingSystem
              RestartPolicy = this.RestartPolicy
              Identity = this.Identity
              ImageRegistryCredentials = this.ImageRegistryCredentials
              InitContainers = [
                  for initContainer in this.InitContainers do
                      {| Name = initContainer.Name
                         Image = initContainer.Image
                         Command = initContainer.Command
                         EnvironmentVariables = initContainer.EnvironmentVariables
                         VolumeMounts = initContainer.VolumeMounts |}
              ]
              IpAddress = this.IpAddress
              NetworkProfile = this.NetworkProfile
              Volumes = this.Volumes
              Tags = this.Tags }
        ]

type ContainerProbeType = LivelinessProbe | ReadinessProbe
type ContainerProbeConfig =
    { ProbeType : ContainerProbeType
      Probe : ContainerProbe }

type ContainerNetworkInterfaceIpConfig = { Subnet : string }
type ContainerNetworkInterfaceConfiguration = { IpConfigs : ContainerNetworkInterfaceIpConfig list }

type NetworkProfileConfig =
    { Name : ResourceName
      ContainerNetworkInterfaceConfigurations : ContainerNetworkInterfaceConfiguration list
      VirtualNetwork : LinkedResource
      Tags: Map<string,string>  }
    interface IBuilder with
        member this.ResourceId = networkProfiles.resourceId this.Name
        member this.BuildResources location = [
            { Name = this.Name
              Location = location
              Dependencies = [
                  match this.VirtualNetwork with
                  | Managed resId -> resId // Only generate dependency if this is managed by Farmer (same template)
                  | _ -> ()
              ] |> Set.ofList
              ContainerNetworkInterfaceConfigurations =
                  this.ContainerNetworkInterfaceConfigurations
                  |> List.map (fun ifconfig -> {| IpConfigs = (ifconfig.IpConfigs |> List.map (fun ipConfig -> {| SubnetName = ResourceName ipConfig.Subnet |})) |})
              VirtualNetwork =
                  match this.VirtualNetwork with
                  | Managed resId
                  | Unmanaged resId -> resId
              Tags = this.Tags }
        ]

type ContainerGroupBuilder() =
    member private _.AddPort (state, portType, port): ContainerGroupConfig =
        { state with IpAddress =
                        match state.IpAddress with
                        | Some ipAddresses ->
                            { ipAddresses with Ports = ipAddresses.Ports.Add {| Protocol = portType; Port = port |} } |> Some
                        | None -> { Type = IpAddressType.PublicAddress; Ports = [ {| Protocol = portType; Port = port |} ] |> Set.ofList } |> Some }

    member _.Yield _ =
        { Name = ResourceName.Empty
          OperatingSystem = Linux
          RestartPolicy = AlwaysRestart
          Identity = ManagedIdentity.Empty
          ImageRegistryCredentials = []
          InitContainers = []
          IpAddress = None
          NetworkProfile = None
          Instances = []
          Volumes = Map.empty
          Tags = Map.empty }
    member this.Run (state:ContainerGroupConfig) =
        // Automatically apply all public-facing ports to the container group itself.
        state.Instances
        |> Seq.collect(fun i -> i.Ports |> Map.toSeq |> Seq.choose(function (port, PublicPort) -> Some port | _, InternalPort -> None))
        |> Seq.fold (fun (state:ContainerGroupConfig) port -> this.AddPort (state, TCP, port)) state

    member this.AddTcpPort(state:ContainerGroupConfig, port) = this.AddPort (state, TCP, port)

    [<CustomOperation "name">]
    /// Sets the name of the container group.
    member _.Name(state:ContainerGroupConfig, name) = { state with Name = name }
    member this.Name(state:ContainerGroupConfig, name) = this.Name(state, ResourceName name)
    /// Sets the OS type (default Linux)
    [<CustomOperation "operating_system">]
    member _.OsType(state:ContainerGroupConfig, os) = { state with OperatingSystem = os }
    /// Sets the restart policy (default Always)
    [<CustomOperation "restart_policy">]
    member _.RestartPolicy(state:ContainerGroupConfig, restartPolicy) = { state with RestartPolicy = restartPolicy }
    member private _.SetIpAddress(state:ContainerGroupConfig, ipAddressType, ports) =
        { state with
            IpAddress =
                { Type = ipAddressType
                  Ports = ports |> Seq.map(fun (prot, port) -> {| Protocol = prot; Port = port |}) |> Set } |> Some }

    /// Sets the IP addresss to a public address with a DNS label
    [<CustomOperation "public_dns">]
    member this.PublicDns(state, dnsLabel, ports) = this.SetIpAddress(state, PublicAddressWithDns dnsLabel, ports)
    /// Sets the IP addresss to a private address assigned by the vnet
    [<CustomOperation "private_ip">]
    member this.PrivateIp(state:ContainerGroupConfig, ports) = this.SetIpAddress(state, PrivateAddress, ports)
    /// Sets a network profile for the container's group.
    [<CustomOperation "network_profile">]
    member _.NetworkProfile(state:ContainerGroupConfig, networkProfileName:string) = { state with NetworkProfile = Some (ResourceName networkProfileName) }
    member _.NetworkProfile(state:ContainerGroupConfig, networkProfile:NetworkProfileConfig) = { state with NetworkProfile = Some networkProfile.Name }
    /// Adds a UDP port to be externally accessible
    [<CustomOperation "add_udp_port">]
    member this.AddUdpPort(state:ContainerGroupConfig, port) = this.AddPort (state, UDP, port)
    /// Adds container image registry credentials for images in this container group.
    [<CustomOperation "add_registry_credentials">]
    member _.AddRegistryCredentials(state:ContainerGroupConfig, credentials) =
        { state with ImageRegistryCredentials = state.ImageRegistryCredentials @ credentials }
    /// Adds a collection of init containers to this group that run once on startup before other containers in the group.
    [<CustomOperation "add_init_containers">]
    member _.AddInitContainers(state:ContainerGroupConfig, initContainers) = { state with InitContainers = state.InitContainers @ (Seq.toList initContainers) }
    /// Adds a collection of container instances to this group
    [<CustomOperation "add_instances">]
    member _.AddInstances(state:ContainerGroupConfig, instances) = { state with Instances = state.Instances @ (Seq.toList instances) }
    [<CustomOperation "add_volumes">]
    /// Adds volumes to the container group so they can be mounted on containers.
    member _.AddVolumes(state:ContainerGroupConfig, volumes) =
        let newVolumes = volumes |> Map.ofSeq
        let updatedVolumes = state.Volumes |> Map.fold (fun current key vol -> Map.add key vol current) newVolumes
        { state with Volumes = updatedVolumes }
    /// Sets the managed identity on this container group.
    [<CustomOperation "add_identity">]
    member _.AddIdentity(state:ContainerGroupConfig, identity:UserAssignedIdentity) = { state with Identity = state.Identity + identity }
    member this.AddIdentity(state, identity:UserAssignedIdentityConfig) = this.AddIdentity(state, identity.UserAssignedIdentity)
    [<CustomOperation "system_identity">]
    member _.SystemIdentity(state:ContainerGroupConfig) = { state with Identity = { state.Identity with SystemAssigned = Enabled } }
    interface ITaggable<ContainerGroupConfig> with member _.Add state tags = { state with Tags = state.Tags |> Map.merge tags }

/// Creates an image registry credential with a generated SecureParameter for the password.
let registry (server:string) (username:string) =
    { Server = server
      Username = username
      Password = SecureParameter $"{server}-password" }

type ContainerInstanceBuilder() =
    member _.Yield _ =
        { Name = ResourceName.Empty
          Image = ""
          Command = List.empty
          Ports = Map.empty
          Cpu = 1.0
          Memory = 1.5<Gb>
          EnvironmentVariables = Map.empty
          LivelinessProbe = None
          ReadinessProbe = None
          VolumeMounts = Map.empty }
    /// Sets the name of the container instance.
    [<CustomOperation "name">]
    member _.Name(state:ContainerInstanceConfig, name) = { state with Name = name }
    member this.Name(state:ContainerInstanceConfig, name) = this.Name(state, ResourceName name)
    /// Sets the image of the container instance.
    [<CustomOperation "image">]
    member _.Image (state:ContainerInstanceConfig, image) = { state with Image = image }
    static member private AddPorts (state:ContainerInstanceConfig, accessibility, ports) =
        { state with
            Ports =
                ports
                |> Seq.fold(fun all port -> all.Add(port, accessibility) ) state.Ports }
    /// Sets the ports the container instance exposes. These will automatically be applied to the container group.
    [<CustomOperation "add_public_ports">]
    member _.PublicPorts (state:ContainerInstanceConfig, ports) = ContainerInstanceBuilder.AddPorts(state, PublicPort, ports)
    /// Sets the ports the container instance exposes. These will automatically be applied to the container group.
    [<CustomOperation "add_internal_ports">]
    member _.InternalPorts (state:ContainerInstanceConfig, ports) = ContainerInstanceBuilder.AddPorts(state, InternalPort, ports)
    /// Sets the ports the container instance exposes. These will automatically be applied to the container group.
    [<CustomOperation "add_ports">]
    member _.Ports (state:ContainerInstanceConfig, accessibility, ports) = ContainerInstanceBuilder.AddPorts(state, accessibility, ports)
    /// Sets the maximum CPU cores the container instance may use
    [<CustomOperationAttribute "cpu_cores">]
    member _.CpuCount (state:ContainerInstanceConfig, cpuCount:float) = { state with Cpu = cpuCount }
    member _.CpuCount (state:ContainerInstanceConfig, cpuCount:int) = { state with Cpu = float(cpuCount) }
    /// Sets the maximum gigabytes of memory the container instance may use
    [<CustomOperationAttribute "memory">]
    member _.Memory (state:ContainerInstanceConfig, memory) = { state with Memory = memory }
    [<CustomOperation "env_vars">]
    member _.EnvironmentVariables(state:ContainerInstanceConfig, envVars) =
        { state with EnvironmentVariables = Map.ofList envVars }
    member this.EnvironmentVariables(state, envVars) =
        this.EnvironmentVariables(state, envVars |> List.map(fun (k,v) -> k, EnvValue v))
    /// Adds a volume mount to the container
    [<CustomOperation "add_volume_mount">]
    member _.AddVolumeMount (state:ContainerInstanceConfig, volumeName, mountPath) =
        { state with VolumeMounts = state.VolumeMounts |> Map.add volumeName mountPath }
    /// Adds commands to execute within the container instance
    [<CustomOperation "command_line">]
    member _.CommandLine (state:ContainerInstanceConfig, command) =
        { state with Command = state.Command @ command }
    /// Set readiness and liveliness probes on the container.
    [<CustomOperation "probes">]
    member _.Probes (state:ContainerInstanceConfig, probes:(ContainerProbeConfig) seq) =
        { state with
            LivelinessProbe =
                probes
                |> Seq.tryFind(fun p -> p.ProbeType = ContainerProbeType.LivelinessProbe)
                |> Option.map (fun p -> p.Probe)
            ReadinessProbe =
                probes
                |> Seq.tryFind(fun p -> p.ProbeType = ContainerProbeType.ReadinessProbe)
                |> Option.map (fun p -> p.Probe)
        }

type ProbeBuilder (probeType:ContainerProbeType) =
    member _.Yield _ =
        {
            ProbeType = probeType
            Probe = {
                Exec = []
                HttpGet = None
                InitialDelaySeconds = None
                PeriodSeconds = None
                FailureThreshold = None
                SuccessThreshold = None
                TimeoutSeconds = None
            }
        }
    /// The URI for a GET request for a health or readiness check on this container. The hostname in the URI is ignored.
    [<CustomOperation "http">]
    member _.HttpGet (state:(ContainerProbeConfig), uri:string) =
        { state with Probe = { state.Probe with HttpGet = uri |> System.Uri |> Some } }
    /// A command to execute on this container to check its health or readiness.
    [<CustomOperation "exec">]
    member _.Exec (state:(ContainerProbeConfig), commands:string list) =
        { state with Probe = { state.Probe with Exec = commands } }
    member _.Exec (state:(ContainerProbeConfig), command:string) =
        { state with Probe = { state.Probe with Exec = [ command ] } }
    /// The probe will not run until this delay after container startup. Default is 0 - runs immediately.
    [<CustomOperation "initial_delay_seconds">]
    member _.InitialDelay (state:(ContainerProbeConfig), delay:int) =
        { state with Probe = { state.Probe with InitialDelaySeconds = delay |> Some } }
    /// How often to execute the probe against the container - default is 10 seconds.
    [<CustomOperation "period_seconds">]
    member _.PeriodSeconds (state:(ContainerProbeConfig), delay:int) =
        { state with Probe = { state.Probe with PeriodSeconds = delay |> Some } }
    /// Number of failures before this container is considered unhealthy - default is 3.
    [<CustomOperation "failure_threshold">]
    member _.FailureThreshold (state:(ContainerProbeConfig), delay:int) =
        { state with Probe = { state.Probe with FailureThreshold = delay |> Some } }
    /// Number of successes before this container is considered healthy - default is 1.
    [<CustomOperation "success_threshold">]
    member _.SuccessThreshold (state:(ContainerProbeConfig), delay:int) =
        { state with Probe = { state.Probe with SuccessThreshold = delay |> Some } }
    /// Number of seconds for the probe to run before failing due to a timeout - default is 1 second.
    [<CustomOperation "timeout_seconds">]
    member _.TimeoutSeconds (state:(ContainerProbeConfig), delay:int) =
        { state with Probe = { state.Probe with TimeoutSeconds = delay |> Some } }

let liveliness = ProbeBuilder(LivelinessProbe)
let readiness = ProbeBuilder(ReadinessProbe)

type InitContainerBuilder() =
    member _.Yield _ =
        { Name = ResourceName.Empty
          Image = ""
          Command = List.empty
          EnvironmentVariables = Map.empty
          VolumeMounts = Map.empty }
    /// Sets the name of the init container.
    [<CustomOperation "name">]
    member _.Name(state:InitContainerConfig, name) = { state with Name = name }
    member this.Name(state:InitContainerConfig, name) = this.Name(state, ResourceName name)
    /// Sets the image of the init container.
    [<CustomOperation "image">]
    member _.Image (state:InitContainerConfig, image) = { state with Image = image }
    /// Sets the environment variables for the init container.
    [<CustomOperation "env_vars">]
    member _.EnvironmentVariables(state:InitContainerConfig, envVars) =
        { state with EnvironmentVariables = Map.ofList envVars }
    member this.EnvironmentVariables(state, envVars) =
        this.EnvironmentVariables(state, envVars |> List.map(fun (k,v) -> k, EnvValue v))
    /// Adds a volume mount to the init container
    [<CustomOperation "add_volume_mount">]
    member _.AddVolumeMount (state:InitContainerConfig, volumeName, mountPath) =
        { state with VolumeMounts = state.VolumeMounts |> Map.add volumeName mountPath }
    /// Adds commands to execute within the init container
    [<CustomOperation "command_line">]
    member _.CommandLine (state:InitContainerConfig, command) =
        { state with Command = state.Command @ command }

let containerGroup = ContainerGroupBuilder()
let containerInstance = ContainerInstanceBuilder()
let initContainer = InitContainerBuilder()

type NetworkProfileBuilder() =
    member _.Yield _ =
        { Name = ResourceName.Empty
          ContainerNetworkInterfaceConfigurations = []
          VirtualNetwork = Managed (virtualNetworks.resourceId ResourceName.Empty)
          Tags = Map.empty }
    /// Sets the name of the network profile instance
    [<CustomOperation "name">]
    member _.Name(state:NetworkProfileConfig, name) = { state with Name = ResourceName name }
    /// Sets a single target subnet for the network profile (typical case of single subnet)
    [<CustomOperation "subnet">]
    member _.SubnetName(state:NetworkProfileConfig, subnet) = { state with ContainerNetworkInterfaceConfigurations = [ { IpConfigs = [ { Subnet = subnet } ] } ] }
    /// Sets a single target subnet for the network profile (typical case of single subnet)
    [<CustomOperation "add_ip_configs">]
    member _.AddIpConfigs(state:NetworkProfileConfig, configs) = { state with ContainerNetworkInterfaceConfigurations = state.ContainerNetworkInterfaceConfigurations @ configs }
    /// Sets the virtual network for the profile
    [<CustomOperation "vnet">]
    member _.VirtualNetwork(state:NetworkProfileConfig, vnet) =
        { state with VirtualNetwork = Managed(virtualNetworks.resourceId(ResourceName vnet)) }
    /// Links to an existing vnet.
    [<CustomOperation "link_to_vnet">]
    member _.LinkToVirtualNetwork(state:NetworkProfileConfig, vnet) =
        { state with VirtualNetwork = Unmanaged(virtualNetworks.resourceId(ResourceName vnet)) }
    member _.LinkToVirtualNetwork(state:NetworkProfileConfig, resourceId) =
        { state with VirtualNetwork = Unmanaged resourceId }
    interface ITaggable<NetworkProfileConfig> with member _.Add state tags = { state with Tags = state.Tags |> Map.merge tags }

let networkProfile = NetworkProfileBuilder ()
